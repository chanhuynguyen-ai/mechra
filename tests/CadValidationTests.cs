using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using SwCursor.SolidWorksAddin.Services;

public sealed class VerificationCase
{
    public string name { get; set; }
    public CadVerificationSnapshot snapshot { get; set; }
    public bool passed { get; set; }
}
public sealed class FeatureScopeCase
{
    public string name { get; set; }
    public List<FeatureSnapshot> features { get; set; }
    public bool allow_plate { get; set; }
    public bool accepted { get; set; }
}
public sealed class RectangleCase
{
    public string name { get; set; }
    public List<SketchEdgeSnapshot> edges { get; set; }
    public bool valid { get; set; }
    public double width_mm { get; set; }
    public double height_mm { get; set; }
}
public sealed class EquationCase
{
    public string name { get; set; }
    public EquationStateSnapshot state { get; set; }
    public bool native_accepted { get; set; }
}
public static class CadValidationTests
{
    private static int _count;
    private static void Check(bool ok, string name) { _count++; if (!ok) throw new Exception(name); }
    private static CadPlan Plan() => new CadPlan { version="0.2", document_type="part", requires_confirmation=true,
        operations=new List<CadOperation> { new CadOperation {id="op-1", kind="create_plate", intent="create native plate",
            inputs=new Dictionary<string,object>{{"width_mm",100},{"height_mm",60},{"thickness_mm",5}}} } };
    private static ModelContextSnapshot Context() => new ModelContextSnapshot {document_id="doc-A", document_type="part",
        document_title="Part1", configuration="Default", update_stamp=1,
        equations=new EquationStateSnapshot {count=0,disabled_count=0,linked_to_file=false},
        features=new List<FeatureSnapshot> {new FeatureSnapshot {name="Front",type_name="RefPlane"}}};
    public static int Main(string[] args)
    {
        try {
            Check(CadValidation.ValidatePlan(Plan()) == null,"valid plan");
            foreach (object value in new object[]{-1,0,10001,true,"5",double.NaN,double.PositiveInfinity,null}) {
                var p=Plan(); p.operations[0].inputs["thickness_mm"]=value;
                Check(CadValidation.ValidatePlan(p)!=null,"invalid thickness: "+value);
            }
            var plan=Plan();plan.requires_confirmation=false;Check(CadValidation.ValidatePlan(plan)!=null,"no review");
            plan=Plan();plan.operations.Add(plan.operations[0]);Check(CadValidation.ValidatePlan(plan)!=null,"multiple operations");
            plan=Plan();plan.operations[0].kind="delete_model";Check(CadValidation.ValidatePlan(plan)!=null,"unsupported kind");
            plan=Plan();plan.operations[0].inputs["macro"]="run code";Check(CadValidation.ValidatePlan(plan)!=null,"unexpected input");
            plan=Plan();plan.operations[0].depends_on.Add("op-2");Check(CadValidation.ValidatePlan(plan)!=null,"invalid dependency");
            plan=Plan();plan.operations[0].kind="modify_plate_thickness";
            plan.operations[0].inputs=new Dictionary<string,object>{{"thickness_mm",8},{"feature_name","Other"}};
            Check(CadValidation.ValidatePlan(plan)!=null,"wrong feature target");
            plan.operations[0].inputs["feature_name"]="Mechra-Plate-Extrude";
            Check(CadValidation.ValidatePlan(plan)==null,"valid edit");
            var before=Context();var after=Context();Check(ModelRevision.Same(before,after),"same revision");
            after.update_stamp=2;Check(!ModelRevision.Same(before,after),"stamp mismatch");
            after=Context();after.document_id="doc-B";Check(!ModelRevision.Same(before,after),"same title different document");
            after=Context();after.configuration="other";Check(!ModelRevision.Same(before,after),"configuration changed");
            after=Context();after.features[0].name="renamed";Check(!ModelRevision.Same(before,after),"rename without stamp change");
            after=Context();after.path="C:\\saved.sldprt";Check(!ModelRevision.Same(before,after),"save as");
            after=Context();after.equations.count=1;Check(!ModelRevision.Same(before,after),"equation added without stamp change");
            after=Context();after.equations.disabled_count=1;Check(!ModelRevision.Same(before,after),"disabled equation added");
            after=Context();after.equations.linked_to_file=true;Check(!ModelRevision.Same(before,after),"equation file linked");
            after=Context();after.equations=null;Check(!ModelRevision.Same(before,after),"equation API read failed");
            Check(!ModelRevision.Same(null,after),"no reviewed document");
            foreach (var item in new JavaScriptSerializer().Deserialize<List<VerificationCase>>(File.ReadAllText(args[0])))
                Check(CadValidation.Verify(item.snapshot).passed==item.passed,"verification: "+item.name);
            foreach (var item in new JavaScriptSerializer().Deserialize<List<FeatureScopeCase>>(File.ReadAllText(args[1])))
                Check(item.features.All(feature => CadValidation.FeatureAllowed(feature, item.allow_plate)) == item.accepted,
                    "feature scope: " + item.name);
            foreach (var item in new JavaScriptSerializer().Deserialize<List<RectangleCase>>(File.ReadAllText(args[2])))
            {
                var measurement = RectangleGeometry.Measure(item.edges);
                Check(measurement.valid == item.valid, "rectangle: " + item.name);
                if (item.valid) Check(Math.Abs(measurement.width_mm - item.width_mm) < 1e-7
                    && Math.Abs(measurement.height_mm - item.height_mm) < 1e-7, "rectangle dimensions: " + item.name);
            }
            foreach (var item in new JavaScriptSerializer().Deserialize<List<EquationCase>>(File.ReadAllText(args[3])))
                Check((CadValidation.EquationIssue(item.state) == null) == item.native_accepted,
                    "equation state: " + item.name);
            Check(!RectangleGeometry.Measure(null).valid, "null sketch edges");
            var nonfinite = new List<SketchEdgeSnapshot> { new SketchEdgeSnapshot { x1=double.NaN },
                new SketchEdgeSnapshot(), new SketchEdgeSnapshot(), new SketchEdgeSnapshot() };
            Check(!RectangleGeometry.Measure(nonfinite).valid, "nonfinite sketch coordinates");
            var invalid=new CadVerificationSnapshot {measured_volume_mm3=double.NaN};
            Check(!CadValidation.Verify(invalid).passed,"nonfinite measurements");
            Console.WriteLine("PASS: "+_count+" C# contract, document-revision, feature-scope, rectangle and verification checks. No SOLIDWORKS COM calls.");
            return 0;
        } catch(Exception ex) { Console.Error.WriteLine("FAIL: "+ex.Message); return 1; }
    }
}
