using System;
using System.Collections.Generic;
using System.Linq;

namespace SwCursor.SolidWorksAddin.Services
{
    public sealed class ModelContextSnapshot
    {
        public string document_id { get; set; }
        public int update_stamp { get; set; }
        public string configuration { get; set; }
        public string document_title { get; set; }
        public string path { get; set; }
        public string document_type { get; set; }
        public EquationStateSnapshot equations { get; set; }
        public List<FeatureSnapshot> features { get; set; } = new List<FeatureSnapshot>();
    }

    public sealed class EquationStateSnapshot
    {
        // Negative defaults prevent partial JSON from being treated as a valid empty read.
        public int count { get; set; } = -1;
        public int disabled_count { get; set; } = -1;
        public bool? linked_to_file { get; set; }
    }

    public sealed class FeatureSnapshot
    {
        public string name { get; set; }
        public string type_name { get; set; }
    }

    public static class ModelRevision
    {
        public static bool Same(ModelContextSnapshot before, ModelContextSnapshot after)
        {
            return before != null && after != null && !string.IsNullOrEmpty(before.document_id)
                && before.document_id == after.document_id && before.document_type == after.document_type
                && before.update_stamp == after.update_stamp
                && before.configuration == after.configuration && before.path == after.path
                && before.document_title == after.document_title
                && SameEquations(before.equations, after.equations)
                && FeatureSignature(before) == FeatureSignature(after);
        }

        public static bool SameEquations(EquationStateSnapshot before, EquationStateSnapshot after)
        {
            if (before == null || after == null) return before == after;
            return before.count == after.count && before.disabled_count == after.disabled_count
                && before.linked_to_file == after.linked_to_file;
        }

        public static string FeatureSignature(ModelContextSnapshot snapshot)
        {
            return string.Join("\n", snapshot.features.Select(f => f.name + "|" + f.type_name));
        }

    }
}
