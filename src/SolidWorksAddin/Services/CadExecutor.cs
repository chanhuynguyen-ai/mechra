using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

namespace SwCursor.SolidWorksAddin.Services
{
    /// <summary>
    /// Deterministic native SOLIDWORKS executor for the Mechra v0.2 vertical slice.
    /// The planner emits versioned intents; only this class is allowed to mutate CAD.
    /// </summary>
    public sealed class CadExecutor
    {
        private const string PlateSketchName = "Mechra-Plate-Sketch";
        private const string PlateExtrudeName = "Mechra-Plate-Extrude";
        private readonly SldWorks _swApp;
        private readonly ModelContextService _context;
        private readonly int _threadId = Thread.CurrentThread.ManagedThreadId;

        public CadExecutor(SldWorks swApp, ModelContextService context)
        {
            _swApp = swApp ?? throw new ArgumentNullException(nameof(swApp));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        private sealed class Baseline
        {
            public string Features;
            public string Configuration;
            public int Bodies;
            public double Volume;
            public CadVerificationSnapshot Plate;
        }

        // Synchronous on the SOLIDWORKS UI thread: never leave an undo group open across await.
        public CadExecutionResult Execute(CadPlan plan, ModelContextSnapshot reviewedContext)
        {
            if (Thread.CurrentThread.ManagedThreadId != _threadId)
                return Fail("CAD execution must run on the SOLIDWORKS UI thread.");
            string error = CadValidation.ValidatePlan(plan);
            if (error != null) return Fail(error);
            if (!ModelRevision.Same(reviewedContext, _context.Capture()))
                return Fail("The active Part or its model state changed. Generate and review a new plan.");
            ModelDoc2 model = _swApp.IActiveDoc2;
            error = PartStateIssue(model);
            if (error != null) return Fail(error);
            CadOperation op = plan.operations[0];
            error = Preflight(model, op);
            if (error != null) return Fail(error);
            Baseline baseline = CaptureBaseline(model);
            if (baseline == null) return Fail("Cannot read the pre-edit checkpoint. No changes were made.");

            bool recording = false;
            bool finished = false;
            bool oldFeatureDialog = model.ShowFeatureErrorDialog;
            bool oldPrompt = _swApp.GetUserPreferenceToggle((int)swUserPreferenceToggle_e.swInputDimValOnCreate);
            CadExecutionResult result;
            try
            {
                model.Extension.StartRecordingUndoObject();
                recording = true;
                model.ShowFeatureErrorDialog = false;
                _swApp.SetUserPreferenceToggle((int)swUserPreferenceToggle_e.swInputDimValOnCreate, false);
                result = op.kind == "create_plate" ? CreatePlate(model, op) : ModifyPlateThickness(model, op);
                if (result.success)
                {
                    result.local_verification = CadValidation.Verify(result.verification);
                    result.success = result.local_verification.passed;
                    if (!result.success) result.message = result.local_verification.message;
                }
            }
            catch (Exception ex)
            {
                result = Fail("CAD operation failed: " + ex.Message);
            }
            finally
            {
                // Restore application preferences even if a COM operation throws.
                try { model.ShowFeatureErrorDialog = oldFeatureDialog; } catch { }
                try { _swApp.SetUserPreferenceToggle((int)swUserPreferenceToggle_e.swInputDimValOnCreate, oldPrompt); } catch { }
                try {
                    if (model.SketchManager.ActiveSketch != null) model.SketchManager.InsertSketch(true);
                    model.ClearSelection2(true);
                } catch { }
                if (recording)
                {
                    try { finished = model.Extension.FinishRecordingUndoObject2("Mechra: " + plan.summary, false); }
                    catch { finished = false; }
                }
            }
            if (!finished)
            {
                result.success = false;
                result.message += " Undo group could not be finalized; inspect the Part and SOLIDWORKS Undo list.";
                return result; // Do not guess which unrelated Undo entry would be affected.
            }
            if (!result.success)
            {
                bool changed = !MatchesBaseline(model, baseline);
                if (changed)
                {
                    try {
                        model.EditUndo2(1);
                        bool rollbackRebuild = model.EditRebuild3();
                        result.rollback_verified = rollbackRebuild && MatchesBaseline(model, baseline);
                    } catch { result.rollback_verified = false; }
                    result.message += result.rollback_verified
                        ? " Rolled back; feature/body/dimension/volume checkpoint matches."
                        : " Rollback could not be verified. Stop editing and inspect the Part.";
                }
                return result;
            }
            result.message = "Native operation rebuilt and verified. To revert, select the named Mechra transaction in the SOLIDWORKS Undo list.";
            return result;
        }

        private static string PartStateIssue(ModelDoc2 model)
        {
            if (model == null || model.GetType() != (int)swDocumentTypes_e.swDocPART)
                return "Chọn File > New > Part trong SOLIDWORKS, rồi gửi lại yêu cầu.";
            if (model.IsOpenedReadOnly()) return "Part đang ở chế độ chỉ đọc. Mở bản có thể chỉnh sửa trước.";
            if (model.SketchManager.ActiveSketch != null)
                return "Hãy thoát chế độ sửa sketch trước khi lập kế hoạch.";
            if (model.GetConfigurationCount() != 1)
                return "v0.2 hỗ trợ Part có một configuration.";
            return null;
        }

        // Read-only readiness check. Never rebuilds, changes selection, creates an
        // undo group or writes geometry; it does not claim post-execution verification.
        public CadReadinessReport CheckPart()
        {
            var report = new CadReadinessReport();
            try
            {
                if (Thread.CurrentThread.ManagedThreadId != _threadId)
                    throw new InvalidOperationException("Part inspection must run on the SOLIDWORKS UI thread.");
                ModelDoc2 model = _swApp.IActiveDoc2;
                string issue = PartStateIssue(model);
                if (issue != null) { report.create_issue = report.edit_issue = issue; return report; }
                report.document_title = model.GetTitle();
                report.solid_bodies = BodyCount(model, swBodyType_e.swSolidBody);
                report.surface_bodies = BodyCount(model, swBodyType_e.swSheetBody);
                report.create_issue = Preflight(model, new CadOperation { kind = "create_plate" });
                report.edit_issue = Preflight(model, new CadOperation { kind = "modify_plate_thickness" });
                return report;
            }
            catch (Exception ex)
            {
                report.create_issue = report.edit_issue = "Không thể kiểm tra Part: " + ex.Message;
                return report;
            }
        }

        private static string Preflight(ModelDoc2 model, CadOperation op)
        {
            string scopeError = ValidateFeatureScope(model, op.kind == "modify_plate_thickness");
            if (scopeError != null) return scopeError;
            // This slice deliberately rejects extra solid/sheet bodies and existing sketch work.
            int solids = BodyCount(model, swBodyType_e.swSolidBody);
            int sheets = BodyCount(model, swBodyType_e.swSheetBody);
            if (solids < 0 || sheets < 0) return "Could not inspect bodies; no changes were made.";
            if (sheets != 0) return "Sheet/surface bodies are outside the v0.2 plate slice.";
            if (op.kind == "create_plate")
            {
                if (solids != 0 || FindFeature(model, PlateExtrudeName) != null
                    || FindFeature(model, PlateSketchName) != null || FindLastSketchFeature(model) != null)
                    return "Part này đã có hình hoặc sketch. Chọn File > New > Part, rồi gửi lại lệnh tạo plate. Bản v0.2 cần một Part trống.";
                if (FindFirstReferencePlane(model) == null) return "No reference plane is available.";
            }
            else
            {
                if (solids != 1) return "Thickness edits require exactly one solid body.";
                Feature feature = FindFeature(model, PlateExtrudeName);
                Feature sketch = FindFeature(model, PlateSketchName);
                if (feature == null || sketch == null) return "Mechra plate sketch/extrusion could not be resolved.";
                IExtrudeFeatureData2 data = feature.GetDefinition() as IExtrudeFeatureData2;
                if (data == null || data.GetEndCondition(true) != (int)swEndConditions_e.swEndCondBlind)
                    return "Only the existing Mechra blind extrusion is supported.";
                bool warning;
                if (feature.GetErrorCode2(out warning) != 0) return "Resolve the existing feature error before editing.";
                double width, height;
                if (!MeasureRectangleSketchSides(sketch, out width, out height)) return "The plate sketch is no longer a rectangle.";
                var before = BuildVerification(model, sketch, feature, "modify_plate_thickness",
                    width, height, data.GetDepth(true) * 1000.0, true);
                if (!CadValidation.Verify(before).passed) return "Existing plate geometry differs from a plain plate; no changes were made.";
            }
            return null;
        }

        private static string ValidateFeatureScope(ModelDoc2 model, bool allowPlate)
        {
            Feature f = model.IFirstFeature();
            int guard = 0;
            while (f != null && guard++ < 5000)
            {
                string type = SafeType(f);
                if (!CadValidation.FeatureAllowed(new FeatureSnapshot { name = f.Name, type_name = type }, allowPlate))
                    return "v0.2 cần Part trống để tạo mới hoặc plate do Mechra tạo để sửa chiều dày. Nếu muốn tạo plate mới: File > New > Part, rồi gửi lại yêu cầu. Feature ngoài phạm vi: " + f.Name + " (" + (type ?? "unknown") + "). Bấm Check Part rồi Save log để lưu chi tiết.";
                f = f.IGetNextFeature();
            }
            return f == null ? null : "Feature traversal limit reached; no changes were made.";
        }

        private Baseline CaptureBaseline(ModelDoc2 model)
        {
            var snapshot = _context.Capture();
            int count = BodyCount(model, swBodyType_e.swSolidBody);
            if (count < 0) return null;
            var baseline = new Baseline { Features = ModelRevision.FeatureSignature(snapshot),
                Configuration = snapshot.configuration, Bodies = count,
                Volume = count == 0 ? 0 : ReadVolumeMm3(model) };
            if (count != 0 && !CadValidation.Positive(baseline.Volume)) return null;
            Feature feature = FindFeature(model, PlateExtrudeName);
            if (feature != null)
            {
                Feature sketch = FindFeature(model, PlateSketchName);
                double width, height;
                var data = feature.GetDefinition() as IExtrudeFeatureData2;
                if (data == null || !MeasureRectangleSketchSides(sketch, out width, out height)) return null;
                baseline.Plate = BuildVerification(model, sketch, feature, "modify_plate_thickness",
                    width, height, data.GetDepth(true) * 1000.0, true);
                if (!CadValidation.Verify(baseline.Plate).passed) return null;
            }
            return baseline;
        }

        private bool MatchesBaseline(ModelDoc2 model, Baseline before)
        {
            try {
                Baseline after = CaptureBaseline(model);
                if (before == null || after == null || before.Features != after.Features
                    || before.Configuration != after.Configuration || before.Bodies != after.Bodies
                    || Math.Abs(before.Volume - after.Volume) > Math.Max(1e-9, before.Volume * 1e-7)) return false;
                if (before.Plate == null) return after.Plate == null;
                if (after.Plate == null) return false;
                return Math.Abs(before.Plate.measured_width_mm - after.Plate.measured_width_mm) < 1e-6
                    && Math.Abs(before.Plate.measured_height_mm - after.Plate.measured_height_mm) < 1e-6
                    && Math.Abs(before.Plate.measured_thickness_mm - after.Plate.measured_thickness_mm) < 1e-6;
            } catch { return false; }
        }

        private CadExecutionResult CreatePlate(ModelDoc2 model, CadOperation op)
        {
            double widthMm = ReadPositive(op, "width_mm");
            double heightMm = ReadPositive(op, "height_mm");
            double thicknessMm = ReadPositive(op, "thickness_mm");
            if (!ValidDimension(widthMm) || !ValidDimension(heightMm) || !ValidDimension(thicknessMm))
                return Fail("Plate dimensions must be > 0 and <= 10,000 mm.");

            if (FindFeature(model, PlateExtrudeName) != null)
                return Fail("This Part already contains Mechra-Plate-Extrude. Edit the existing feature instead.");
            if (HasSolidBody(model))
                return Fail("Part này đã có mô hình. Chọn File > New > Part, rồi gửi lại lệnh tạo plate. Tài liệu hiện tại được giữ nguyên.");

            double width = widthMm / 1000.0;
            double height = heightMm / 1000.0;
            double thickness = thicknessMm / 1000.0;

            model.ClearSelection2(true);
            Feature referencePlane = FindFirstReferencePlane(model);
            if (referencePlane == null || !referencePlane.Select2(false, 0))
                return Fail("Could not select a reference plane in the active Part.");

            SketchManager sketchManager = model.SketchManager;
            sketchManager.InsertSketch(true);
            object rectangle = sketchManager.CreateCornerRectangle(
                -width / 2.0, -height / 2.0, 0.0,
                 width / 2.0,  height / 2.0, 0.0);
            if (rectangle == null)
            {
                SafeExitSketch(sketchManager);
                model.ClearSelection2(true);
                return Fail("SOLIDWORKS did not create the rectangular sketch.");
            }

            string dimensionError;
            if (!AddRectangleDrivingDimensions(model, rectangle, width, height, out dimensionError))
            {
                SafeExitSketch(sketchManager);
                model.ClearSelection2(true);
                return Fail("Rectangle was created but driving dimensions failed: " + dimensionError);
            }

            sketchManager.InsertSketch(true);
            Feature plateSketch = FindLastSketchFeature(model);
            if (plateSketch == null)
                return Fail("The sketch exists, but its feature could not be located after leaving sketch mode.");
            plateSketch.Name = PlateSketchName;

            model.ClearSelection2(true);
            if (!plateSketch.Select2(false, 0))
                return Fail("Could not select Mechra-Plate-Sketch for extrusion.");

            Feature extrude = model.FeatureManager.FeatureExtrusion3(
                true, false, false,
                (int)swEndConditions_e.swEndCondBlind,
                (int)swEndConditions_e.swEndCondBlind,
                thickness, 0.0,
                false, false,
                false, true,
                0.0, 0.0,
                false, false,
                false, false,
                true, false, true,
                (int)swStartConditions_e.swStartSketchPlane,
                0.0, false);

            if (extrude == null)
                return Fail("SOLIDWORKS could not create the native Boss-Extrude feature.");

            extrude.Name = PlateExtrudeName;
            bool rebuildOk = model.EditRebuild3();

            bool warning;
            int errorCode = extrude.GetErrorCode2(out warning);
            if (!rebuildOk || errorCode != 0)
                return Fail("Plate was created but rebuild/feature validation failed (code " + errorCode + ").");

            CadVerificationSnapshot verification = BuildVerification(
                model, plateSketch, extrude, "create_plate",
                widthMm, heightMm, thicknessMm, rebuildOk);

            if (verification == null)
                return Fail("Plate was created, but read-back measurement could not be collected.");

            return Ok(
                string.Format(CultureInfo.InvariantCulture,
                    "Created native plate {0:g} x {1:g} x {2:g} mm.",
                    widthMm, heightMm, thicknessMm),
                verification,
                "Created a native rectangular sketch with driving width/height dimensions.",
                "Created a native blind Boss-Extrude named Mechra-Plate-Extrude.",
                "Rebuilt the Part and collected sketch/extrude/volume measurements.");
        }

        private CadExecutionResult ModifyPlateThickness(ModelDoc2 model, CadOperation op)
        {
            double thicknessMm = ReadPositive(op, "thickness_mm");
            if (!ValidDimension(thicknessMm))
                return Fail("Thickness must be > 0 and <= 10,000 mm.");

            string featureName = ReadString(op, "feature_name") ?? PlateExtrudeName;
            Feature feature = FindFeature(model, featureName);
            if (feature == null) return Fail("Could not find feature '" + featureName + "'.");

            Feature sketchFeature = FindFeature(model, PlateSketchName);
            if (sketchFeature == null)
                return Fail("Could not find Mechra-Plate-Sketch for read-back verification.");

            double sideAmm;
            double sideBmm;
            if (!MeasureRectangleSketchSides(sketchFeature, out sideAmm, out sideBmm))
                return Fail("Could not measure the existing plate sketch before editing thickness.");
            // A thickness-only edit must preserve the current in-plane dimensions.
            double widthMm = sideAmm;
            double heightMm = sideBmm;

            IExtrudeFeatureData2 definition = feature.GetDefinition() as IExtrudeFeatureData2;
            if (definition == null)
                return Fail("Feature '" + featureName + "' is not a readable extrusion feature.");

            if (!definition.AccessSelections(model, null))
                return Fail("Could not access the selections that define '" + featureName + "'.");

            double thickness = thicknessMm / 1000.0;
            try
            {
                definition.SetEndCondition(true, (int)swEndConditions_e.swEndCondBlind);
                definition.SetDepth(true, thickness);

                bool modified = feature.ModifyDefinition(definition, model, null);
                if (!modified)
                {
                    try { definition.ReleaseSelectionAccess(); } catch { }
                    return Fail("SOLIDWORKS rejected the new extrusion definition.");
                }
            }
            catch
            {
                try { definition.ReleaseSelectionAccess(); } catch { }
                throw;
            }

            bool rebuildOk = model.EditRebuild3();

            bool warning;
            int errorCode = feature.GetErrorCode2(out warning);
            if (!rebuildOk || errorCode != 0)
                return Fail("Thickness was changed but rebuild/feature validation failed (code " + errorCode + ").");

            CadVerificationSnapshot verification = BuildVerification(
                model, sketchFeature, feature, "modify_plate_thickness",
                widthMm, heightMm, thicknessMm, rebuildOk);
            if (verification == null)
                return Fail("Thickness changed, but read-back measurement could not be collected.");

            return Ok(
                string.Format(CultureInfo.InvariantCulture,
                    "Changed the existing native extrusion thickness to {0:g} mm.", thicknessMm),
                verification,
                "Modified the existing Mechra-Plate-Extrude definition; no replacement body was generated.",
                "Rebuilt the Part and collected post-edit measurements.");
        }

        private static bool AddRectangleDrivingDimensions(
            ModelDoc2 model, object rawRectangle, double width, double height, out string error)
        {
            error = null;
            var segments = new List<ISketchSegment>();
            Array array = rawRectangle as Array;
            if (array == null)
            {
                error = "rectangle API did not return a segment array";
                return false;
            }

            foreach (object item in array)
            {
                ISketchSegment segment = item as ISketchSegment;
                if (segment != null) segments.Add(segment);
            }
            if (segments.Count < 4)
            {
                error = "expected four sketch segments, received " + segments.Count;
                return false;
            }

            ISketchSegment widthSegment = PickAxisSegment(segments, true);
            ISketchSegment heightSegment = PickAxisSegment(segments, false);
            if (widthSegment == null || heightSegment == null)
            {
                error = "could not identify width/height sketch segments";
                return false;
            }

            double margin = Math.Max(0.01, Math.Max(width, height) * 0.15);
            if (!AddDrivingDimension(model, widthSegment, 0.0, -height / 2.0 - margin, width, out error))
                return false;
            if (!AddDrivingDimension(model, heightSegment, width / 2.0 + margin, 0.0, height, out error))
                return false;

            model.ClearSelection2(true);
            return true;
        }

        private static ISketchSegment PickAxisSegment(List<ISketchSegment> segments, bool horizontal)
        {
            foreach (ISketchSegment segment in segments)
            {
                ISketchLine line = segment as ISketchLine;
                if (line == null || segment.ConstructionGeometry) continue;
                ISketchPoint a = line.GetStartPoint2() as ISketchPoint;
                ISketchPoint b = line.GetEndPoint2() as ISketchPoint;
                if (a == null || b == null) continue;
                double dx = Math.Abs(a.X - b.X), dy = Math.Abs(a.Y - b.Y);
                if (horizontal ? dx > 1e-12 && dy < 1e-9 : dy > 1e-12 && dx < 1e-9) return segment;
            }
            return null;
        }

        private static bool AddDrivingDimension(
            ModelDoc2 model, ISketchSegment segment,
            double textX, double textY, double expectedValue, out string error)
        {
            error = null;
            model.ClearSelection2(true);
            if (!segment.Select4(false, null))
            {
                error = "failed to select a sketch segment for dimensioning";
                return false;
            }

            IDisplayDimension display = model.AddDimension2(textX, textY, 0.0) as IDisplayDimension;
            if (display == null)
            {
                error = "SOLIDWORKS did not create a display dimension";
                return false;
            }

            IDimension dimension = display.GetDimension2(0) as IDimension;
            if (dimension == null)
            {
                error = "could not access the driving model dimension";
                return false;
            }

            if (dimension.DrivenState != (int)swDimensionDrivenState_e.swDimensionDriving)
            {
                error = "SOLIDWORKS created a reference dimension instead of a driving dimension";
                return false;
            }
            dimension.SystemValue = expectedValue;
            if (Math.Abs(dimension.SystemValue - expectedValue) > 1e-9)
            {
                error = "driving dimension did not retain the requested value";
                return false;
            }
            return true;
        }

        private static CadVerificationSnapshot BuildVerification(
            ModelDoc2 model, Feature sketchFeature, Feature extrudeFeature, string operation,
            double expectedWidthMm, double expectedHeightMm, double expectedThicknessMm, bool rebuildOk)
        {
            if (BodyCount(model, swBodyType_e.swSolidBody) != 1) return null;
            double sideAmm;
            double sideBmm;
            if (!MeasureRectangleSketchSides(sketchFeature, out sideAmm, out sideBmm))
                return null;

            double measuredWidthMm;
            double measuredHeightMm;
            measuredWidthMm = sideAmm;
            measuredHeightMm = sideBmm;

            IExtrudeFeatureData2 data = extrudeFeature.GetDefinition() as IExtrudeFeatureData2;
            if (data == null) return null;
            double measuredThicknessMm = data.GetDepth(true) * 1000.0;

            double measuredVolumeMm3 = ReadVolumeMm3(model);
            if (measuredVolumeMm3 <= 0) return null;

            return new CadVerificationSnapshot
            {
                operation = operation,
                rebuild_ok = rebuildOk,
                expected_width_mm = expectedWidthMm,
                expected_height_mm = expectedHeightMm,
                expected_thickness_mm = expectedThicknessMm,
                measured_width_mm = measuredWidthMm,
                measured_height_mm = measuredHeightMm,
                measured_thickness_mm = measuredThicknessMm,
                expected_volume_mm3 = expectedWidthMm * expectedHeightMm * expectedThicknessMm,
                measured_volume_mm3 = measuredVolumeMm3,
                feature_names = new List<string> { sketchFeature.Name, extrudeFeature.Name }
            };
        }

        private static bool MeasureRectangleSketchSides(Feature sketchFeature, out double sideAmm, out double sideBmm)
        {
            sideAmm = 0;
            sideBmm = 0;
            ISketch sketch = sketchFeature?.GetSpecificFeature2() as ISketch;
            if (sketch == null) return false;

            Array raw = sketch.GetSketchSegments() as Array;
            if (raw == null) return false;
            var horizontal = new List<double>();
            var vertical = new List<double>();
            foreach (object item in raw)
            {
                ISketchSegment segment = item as ISketchSegment;
                if (segment == null) return false;
                if (segment.ConstructionGeometry) continue;
                ISketchLine line = segment as ISketchLine;
                if (line == null) return false;
                ISketchPoint a = line.GetStartPoint2() as ISketchPoint;
                ISketchPoint b = line.GetEndPoint2() as ISketchPoint;
                if (a == null || b == null) return false;
                double dx = Math.Abs(a.X - b.X), dy = Math.Abs(a.Y - b.Y);
                if (dx > 1e-12 && dy < 1e-9) horizontal.Add(dx * 1000.0);
                else if (dy > 1e-12 && dx < 1e-9) vertical.Add(dy * 1000.0);
                else return false;
            }
            if (horizontal.Count != 2 || vertical.Count != 2
                || Math.Abs(horizontal[0] - horizontal[1]) > 1e-6
                || Math.Abs(vertical[0] - vertical[1]) > 1e-6) return false;
            sideAmm = horizontal[0];
            sideBmm = vertical[0];
            return CadValidation.Dimension(sideAmm) && CadValidation.Dimension(sideBmm);
        }

        private static double ReadVolumeMm3(ModelDoc2 model)
        {
            try
            {
                int status;
                object raw = model.Extension.GetMassProperties2(1, out status, false);
                Array values = raw as Array;
                if (status != 0 || values == null || values.Length < 4) return 0;
                double volumeM3 = Convert.ToDouble(values.GetValue(3), CultureInfo.InvariantCulture);
                return CadValidation.Positive(volumeM3) ? volumeM3 * 1000000000.0 : 0;
            }
            catch
            {
                return 0;
            }
        }

        private static int BodyCount(ModelDoc2 model, swBodyType_e bodyType)
        {
            try
            {
                PartDoc part = model as PartDoc;
                if (part == null) return -1;
                object raw = part.GetBodies2((int)bodyType, false);
                if (raw == null) return 0;
                Array bodies = raw as Array;
                return bodies == null ? -1 : bodies.Length;
            }
            catch { return -1; }
        }

        private static bool HasSolidBody(ModelDoc2 model) => BodyCount(model, swBodyType_e.swSolidBody) != 0;

        private static Feature FindLastSketchFeature(ModelDoc2 model)
        {
            Feature feature = model.IFirstFeature();
            Feature last = null;
            int guard = 0;
            while (feature != null && guard++ < 5000)
            {
                if (string.Equals(SafeType(feature), "ProfileFeature", StringComparison.OrdinalIgnoreCase))
                    last = feature;
                feature = feature.IGetNextFeature();
            }
            return last;
        }

        private static Feature FindFirstReferencePlane(ModelDoc2 model)
        {
            Feature feature = model.IFirstFeature();
            int guard = 0;
            while (feature != null && guard++ < 5000)
            {
                if (string.Equals(SafeType(feature), "RefPlane", StringComparison.OrdinalIgnoreCase))
                    return feature;
                feature = feature.IGetNextFeature();
            }
            return null;
        }

        private static Feature FindFeature(ModelDoc2 model, string name)
        {
            PartDoc part = model as PartDoc;
            return part == null ? null : part.FeatureByName(name) as Feature;
        }

        private static string SafeType(Feature feature)
        {
            try { string type = feature.GetTypeName2(); return type == "ICE" ? feature.GetTypeName() : type; }
            catch { return null; }
        }

        private static void SafeExitSketch(SketchManager manager)
        {
            try { manager.InsertSketch(true); } catch { }
        }

        private static bool ValidDimension(double value) => CadValidation.Dimension(value);

        private static double ReadPositive(CadOperation op, string key)
        {
            if (op?.inputs == null || !op.inputs.ContainsKey(key) || op.inputs[key] == null) return -1.0;
            try { return Convert.ToDouble(op.inputs[key], CultureInfo.InvariantCulture); }
            catch { return -1.0; }
        }

        private static string ReadString(CadOperation op, string key)
        {
            if (op?.inputs == null || !op.inputs.ContainsKey(key) || op.inputs[key] == null) return null;
            return Convert.ToString(op.inputs[key], CultureInfo.InvariantCulture);
        }

        private static CadExecutionResult Ok(
            string message, CadVerificationSnapshot verification, params string[] steps)
        {
            var result = new CadExecutionResult
            {
                success = true,
                message = message,
                verification = verification
            };
            result.steps.AddRange(steps ?? new string[0]);
            return result;
        }

        private static CadExecutionResult Fail(string message)
        {
            return new CadExecutionResult { success = false, message = message };
        }
    }
}
