using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SwCursor.SolidWorksAddin.Services
{
    // Pure contract/measurement logic. No COM calls; tested by tests/CadValidationTests.cs.
    public static class CadValidation
    {
        public static bool Positive(double value) => !double.IsNaN(value) && !double.IsInfinity(value) && value > 0;
        public static bool Dimension(double value) => Positive(value) && value <= 10000;

        public static string ValidatePlan(CadPlan plan)
        {
            if (plan == null || plan.version != "0.2" || plan.document_type != "part")
                return "Unsupported plan version or document type.";
            if (!plan.requires_confirmation) return "Plan must explicitly require review before applying.";
            if (plan.operations == null || plan.operations.Count != 1)
                return "v0.2 requires exactly one CAD operation.";
            CadOperation op = plan.operations[0];
            if (op == null || string.IsNullOrWhiteSpace(op.id) || string.IsNullOrWhiteSpace(op.intent)
                || op.inputs == null || op.depends_on == null || op.depends_on.Count != 0)
                return "Invalid operation contract.";
            string[] required;
            string[] allowed;
            if (op.kind == "create_plate")
            {
                required = new[] { "width_mm", "height_mm", "thickness_mm" };
                allowed = required.Concat(new[] { "plane" }).ToArray();
                if (op.inputs.ContainsKey("plane") && !Equals(op.inputs["plane"], "first_reference_plane"))
                    return "Unsupported reference plane.";
            }
            else if (op.kind == "modify_plate_thickness")
            {
                required = new[] { "thickness_mm" };
                allowed = new[] { "thickness_mm", "feature_name" };
                if (op.inputs.ContainsKey("feature_name") && !Equals(op.inputs["feature_name"], "Mechra-Plate-Extrude"))
                    return "Only Mechra-Plate-Extrude can be edited in v0.2.";
            }
            else return "Unsupported CAD operation.";
            if (op.inputs.Keys.Except(allowed).Any()) return "Unexpected operation input.";
            foreach (string key in required)
            {
                object value;
                if (!op.inputs.TryGetValue(key, out value) || !Numeric(value) || !Dimension(Convert.ToDouble(value, CultureInfo.InvariantCulture)))
                    return "Dimensions must be finite JSON numbers, > 0 and <= 10,000 mm.";
            }
            return null;
        }

        private static bool Numeric(object value) => value is double || value is float || value is decimal
            || value is int || value is long || value is short || value is byte;

        public static CadVerificationReply Verify(CadVerificationSnapshot v)
        {
            var result = new CadVerificationReply { passed = true };
            if (v == null) { result.passed = false; result.message = "No measurements."; return result; }
            Action<string, bool> check = (name, ok) => {
                result.passed &= ok;
                result.checks.Add(name + (ok ? ": PASS" : ": FAIL"));
            };
            check("operation", v.operation == "create_plate" || v.operation == "modify_plate_thickness");
            check("rebuild", v.rebuild_ok);
            double[] expected = { v.expected_width_mm, v.expected_height_mm, v.expected_thickness_mm };
            double[] measured = { v.measured_width_mm, v.measured_height_mm, v.measured_thickness_mm };
            string[] names = { "width", "height", "thickness" };
            for (int i = 0; i < 3; i++)
                check(string.Format(CultureInfo.InvariantCulture, "{0} {1:g} / {2:g} mm", names[i], measured[i], expected[i]),
                    Dimension(expected[i]) && Positive(measured[i]) && Math.Abs(expected[i] - measured[i]) <= Math.Min(0.05, expected[i] * 0.005));
            double volume = expected[0] * expected[1] * expected[2];
            check("expected volume consistency", Positive(volume) && Positive(v.expected_volume_mm3)
                && Math.Abs(volume - v.expected_volume_mm3) <= volume * 1e-9);
            check(string.Format(CultureInfo.InvariantCulture, "volume {0:g} / {1:g} mm3", v.measured_volume_mm3, volume),
                Positive(volume) && Positive(v.measured_volume_mm3) && Math.Abs(volume - v.measured_volume_mm3) <= volume * 0.005);
            check("native features", v.feature_names != null && v.feature_names.Contains("Mechra-Plate-Sketch")
                && v.feature_names.Contains("Mechra-Plate-Extrude"));
            result.message = result.passed ? "Native Part verification passed." : "Native Part verification failed.";
            return result;
        }
    }
}
