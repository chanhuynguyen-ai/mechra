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
        private readonly JavaScriptSerializer _json = new JavaScriptSerializer();

        public AgentClient(string baseUrl = "http://127.0.0.1:8765")
        {
            _http = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(30) };
        }

        public async Task<string> ChatAsync(string message, ModelContextSnapshot context)
        {
            var payload = _json.Serialize(new { message, context });
            using (var content = new StringContent(payload, Encoding.UTF8, "application/json"))
            using (var response = await _http.PostAsync("/v1/chat", content).ConfigureAwait(false))
            {
                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                var obj = _json.DeserializeObject(body) as System.Collections.Generic.Dictionary<string, object>;
                return obj != null && obj.ContainsKey("message") ? Convert.ToString(obj["message"]) : body;
            }
        }

        public void Dispose() => _http.Dispose();
    }
}
