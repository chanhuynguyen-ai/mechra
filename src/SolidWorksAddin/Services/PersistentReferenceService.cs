using System;
using SolidWorks.Interop.sldworks;

namespace SwCursor.SolidWorksAddin.Services
{
    /// <summary>
    /// Stable identity primitive for selectable SOLIDWORKS objects.
    /// Store the returned Base64 token with semantic metadata; never rely on
    /// face/edge list indices as the primary long-lived identity.
    /// </summary>
    public sealed class PersistentReferenceService
    {
        public string Capture(ModelDoc2 model, object entity)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            object raw = model.Extension.GetPersistReference3(entity);
            var bytes = raw as byte[];
            if (bytes == null || bytes.Length == 0)
                throw new InvalidOperationException("SOLIDWORKS did not return a persistent reference ID.");
            return Convert.ToBase64String(bytes);
        }

        public object Resolve(ModelDoc2 model, string token, out int state)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (string.IsNullOrWhiteSpace(token)) throw new ArgumentNullException(nameof(token));

            byte[] bytes = Convert.FromBase64String(token);
            return model.Extension.GetObjectByPersistReference3(bytes, out state);
        }
    }
}
