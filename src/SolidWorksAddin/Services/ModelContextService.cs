using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

namespace SwCursor.SolidWorksAddin.Services
{
    public sealed class ModelContextService
    {
        private readonly SldWorks _swApp;
        private sealed class Identity { public string Value = Guid.NewGuid().ToString("N"); }
        private readonly ConditionalWeakTable<ModelDoc2, Identity> _identities =
            new ConditionalWeakTable<ModelDoc2, Identity>();

        public ModelContextService(SldWorks swApp) => _swApp = swApp;

        public ModelContextSnapshot Capture()
        {
            var model = _swApp?.IActiveDoc2;
            if (model == null)
                return new ModelContextSnapshot { document_type = "none" };

            var snapshot = new ModelContextSnapshot
            {
                document_id = _identities.GetValue(model, _ => new Identity()).Value,
                update_stamp = model.GetUpdateStamp(),
                configuration = model.ConfigurationManager.ActiveConfiguration.Name,
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
            if (feature != null) throw new InvalidOperationException("Feature traversal limit reached.");
            return snapshot;
        }

        private static string SafeTypeName(Feature feature)
        {
            try { string type = feature.GetTypeName2(); return type == "ICE" ? feature.GetTypeName() : type; }
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
