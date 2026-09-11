using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace SwCursor.SolidWorksAddin.Services
{
    public sealed class AgentClient : IDisposable
    {
        private readonly HttpClient _http;
        private string _sessionId = Guid.NewGuid().ToString("N");
        public void ResetSession() { _sessionId = Guid.NewGuid().ToString("N"); }
        private readonly JavaScriptSerializer _json = new JavaScriptSerializer();

        public AgentClient(string baseUrl = "http://127.0.0.1:8765")
        {
            _http = new HttpClient
            {
                BaseAddress = new Uri(baseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        public Task<AgentReply> ChatAsync(string message, ModelContextSnapshot context)
        {
            return PostAsync<AgentReply>("/v1/chat", new { message, context, session_id = _sessionId });
        }

        public Task<CadVerificationReply> VerifyAsync(CadVerificationSnapshot verification)
        {
            if (verification == null) throw new ArgumentNullException(nameof(verification));
            return PostAsync<CadVerificationReply>("/v1/verify", verification);
        }

        private async Task<T> PostAsync<T>(string path, object payload)
        {
            string json = _json.Serialize(payload);
            using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
            using (var response = await _http.PostAsync(path, content).ConfigureAwait(false))
            {
                string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                T result = _json.Deserialize<T>(body);
                if (result == null)
                    throw new InvalidOperationException("Mechra Agent returned an empty response.");
                return result;
            }
        }

        public void Dispose() => _http.Dispose();
    }
}
