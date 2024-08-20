using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using GrupoRoga.Helpers;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using GrupoRoga.Models;
using System.Net;
using System.Web;

namespace GrupoRoga
{
    public class InstagramFlexWebhook
    {
        private static InstagramHelper helper;

        public InstagramFlexWebhook()
        {
            helper = new InstagramHelper();
        }

        [FunctionName("InstagramFlexWebhook")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,ILogger log)
        {
            Response resp;

            try
            {
                string bodyString = await new StreamReader(req.Body).ReadToEndAsync();
                IDictionary<string, string> queryParams = req.GetQueryParameterDictionary();

                Console.WriteLine($"Body {bodyString}");
                Console.WriteLine("QUERY: " + req.QueryString);

                var queryDictionary = HttpUtility.ParseQueryString(bodyString);
                var json = new Dictionary<string, string>();

                foreach (var parameter in queryDictionary)
                {
                    var key = (string)parameter;
                    var value = queryDictionary.Get(key);
                    json.Add(key, value);
                }

                string source = json.ContainsKey("Source") ? json["Source"].ToString() : null,
                    sender_id = queryParams.ContainsKey("sender_id") ? queryParams["sender_id"].ToString() : null,
                    body = json.ContainsKey("Body") ? json["Body"].ToString() : null,
                    conversation_sid = json.ContainsKey("ConversationSid") ? json["ConversationSid"].ToString() : null;

                if (string.IsNullOrEmpty(sender_id)) throw new Exception("Parámetro sender_id requerido");
                if (string.IsNullOrEmpty(conversation_sid)) throw new Exception("Campo ConversationSid requerido");
                if (string.IsNullOrEmpty(body)) throw new Exception("Campo Body requerido");
                if (string.IsNullOrEmpty(source)) throw new Exception("Campo Source requerido");
                if (!source.Equals("SDK")) throw new Exception("Campo Source no válido requerido");

                JObject result = await helper.SendMessage(sender_id, body);
                if (result == null) throw new Exception($"Error al enviar mensaje de instagram sender_id {sender_id}");
                resp = new Response(Response.SUCCESS, $"OK", null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                resp = new Response(Response.ERROR, $"Error", new JObject()
                {
                    ["Message"] = ex.Message
                });
            }

            return new OkObjectResult(resp);
        }
    }
}
