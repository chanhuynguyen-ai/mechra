using System.Collections.Generic;

namespace SwCursor.SolidWorksAddin.Services
{
    public sealed class AgentReply
    {
        public string message { get; set; }
        public string action { get; set; }
        public bool requires_confirmation { get; set; }
        public CadPlan plan { get; set; }
    }

    public sealed class CadPlan
    {
        public string version { get; set; }
        public string document_type { get; set; }
        public string summary { get; set; }
        public bool requires_confirmation { get; set; }
        public List<CadOperation> operations { get; set; } = new List<CadOperation>();
    }

    public sealed class CadOperation
    {
        public string id { get; set; }
        public string kind { get; set; }
        public string intent { get; set; }
        public Dictionary<string, object> inputs { get; set; } = new Dictionary<string, object>();
        public List<string> depends_on { get; set; } = new List<string>();
    }

    public sealed class CadVerificationSnapshot
    {
        public string operation { get; set; }
        public bool rebuild_ok { get; set; }
        public double expected_width_mm { get; set; }
        public double expected_height_mm { get; set; }
        public double expected_thickness_mm { get; set; }
        public double measured_width_mm { get; set; }
        public double measured_height_mm { get; set; }
        public double measured_thickness_mm { get; set; }
        public double expected_volume_mm3 { get; set; }
        public double measured_volume_mm3 { get; set; }
        public List<string> feature_names { get; set; } = new List<string>();
    }

    public sealed class CadVerificationReply
    {
        public bool passed { get; set; }
        public string message { get; set; }
        public List<string> checks { get; set; } = new List<string>();
    }

    public sealed class CadReadinessReport
    {
        public string document_title { get; set; }
        public int solid_bodies { get; set; } = -1;
        public int surface_bodies { get; set; } = -1;
        public string create_issue { get; set; } = "Part has not been inspected.";
        public string edit_issue { get; set; } = "Part has not been inspected.";
        public bool can_create => create_issue == null;
        public bool can_edit => edit_issue == null;
    }

    public sealed class CadExecutionResult
    {
        public string failure_phase { get; set; }
        public long elapsed_ms { get; set; }
        public List<string> trace { get; set; } = new List<string>();
        public bool success { get; set; }
        public bool rollback_verified { get; set; }
        public CadVerificationReply local_verification { get; set; }
        public string message { get; set; }
        public List<string> steps { get; set; } = new List<string>();
        public CadVerificationSnapshot verification { get; set; }
    }
}
