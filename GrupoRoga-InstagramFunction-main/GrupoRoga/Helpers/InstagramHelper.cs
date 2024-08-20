using GrupoRoga.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

using System.Net.Http;
using System.Threading.Tasks;

namespace GrupoRoga.Helpers
{
    public class InstagramHelper
    {
        public string ACCESS_TOKEN;

        public InstagramHelper()
        {
            AppSettings appSettings = new AppSettings();
            ACCESS_TOKEN = appSettings.INSTAGRAM_ACCESS_TOKEN ?? "";
        }

        public async Task<JObject> SendMessage(string sender_id, string message)
        {
            string url = $"https://graph.facebook.com/v12.0/me/messages?access_token={ACCESS_TOKEN}";

            JObject response = await POST(url, new JObject()
            {
                ["recipient"] = new JObject()
                {
                    ["id"] = sender_id,
                },
                ["message"] = new JObject()
                {
                    ["text"] = message
                }
            });

            return response.ContainsKey("message_id") ? response : null;
        }

        private static async Task<JObject> POST(string url, JObject json)
        {
            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Content = new StringContent(json.ToString(Formatting.None), null, "application/json");
                var response = await client.SendAsync(request);
                //response.EnsureSuccessStatusCode();
                string response_string = await response.Content.ReadAsStringAsync();
                return JObject.Parse(response_string);
            }catch(Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

    }
}
