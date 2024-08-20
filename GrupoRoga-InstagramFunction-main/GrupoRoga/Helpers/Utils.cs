using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GrupoRoga.Helpers
{
    public class Utils
    {
        public async Task<JObject> POST(string url, JObject json)
        {
            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Content = new StringContent(json.ToString(Formatting.None), null, "application/json");
                var response = await client.SendAsync(request);                
                string response_string = await response.Content.ReadAsStringAsync();
                return JObject.Parse(response_string);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
