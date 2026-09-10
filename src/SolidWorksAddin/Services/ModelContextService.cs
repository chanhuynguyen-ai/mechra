using System;
using System.Collections.Generic;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

namespace SwCursor.SolidWorksAddin.Services
{
    public sealed class ModelContextSnapshot
    {
        public string document_title { get; set; }
        public string path { get; set; }
        public string document_type { get; set; }
        public List<FeatureSnapshot> features { get; set; } = new List<FeatureSnapshot>();
    }

    public sealed class FeatureSnapshot
    {
        public string name { get; set; }
        public string type_name { get; set; }
    }

    public sealed class ModelContextService
    {
        private readonly SldWorks _swApp;

        public ModelContextService(SldWorks swApp) => _swApp = swApp;

        public ModelContextSnapshot Capture()
        {
            var model = _swApp?.IActiveDoc2;
            if (model == null)
                return new ModelContextSnapshot { document_type = "none" };

            var snapshot = new ModelContextSnapshot
            {
                document_title = model.GetTitle(),
                path = model.GetPathName(),
                document_type = ToDocumentType(model.GetType())
            };

            Feature feature = model.IFirstFeature();
            int guard = 0;
            while (feature != null && guard++ < 5000)
            {
                snapshot.features.Add(new FeatureSnapshot
                {
                    name = feature.Name,
                    type_name = SafeTypeName(feature)
                });
                feature = feature.IGetNextFeature();
            }
            return snapshot;
        }

        private static string SafeTypeName(Feature feature)
        {
            try { return feature.GetTypeName2(); }
            catch { return null; }
        }

        private static string ToDocumentType(int type)
        {
            if (type == (int)swDocumentTypes_e.swDocPART) return "part";
            if (type == (int)swDocumentTypes_e.swDocASSEMBLY) return "assembly";
            if (type == (int)swDocumentTypes_e.swDocDRAWING) return "drawing";
            return "unknown";
        }
    }
}
