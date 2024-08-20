using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Diagnostics;
using GrupoRoga.Models;
using Newtonsoft.Json.Linq;
using System.Xml.Linq;
using Twilio.Base;
using Twilio.TwiML.Messaging;
using GrupoRoga.Helpers;
using System.Collections.Generic;
using GrupoRoga.Data;
using System.Linq;
using System.Text.RegularExpressions;
using Twilio.Rest.Conversations.V1;

namespace GrupoRoga
{
    public class InstagramWebhook
    {

        private string MENU_CIUDAD = "¡Hola! Bienvenida(o) {NOMBRE}.\r\nPor favor selecciona la ciudad en la que vives: \r\n\r\n1. Cd. Juárez\r\n2. Chihuahua\r\n3. Culiacán\r\n4. Hermosillo\r\n5. León\r\n6. Mexicali\r\n7. Monterrey\r\n8. Saltillo\r\n9. Tijuana\r\n10. Mazatlán\r\n\r\nEscribe el número de la opción deseada para continuar.";
        private string MENU_OPCIONES = "¡Bienvenido a Consentilinea!\r\n¿En qué podemos ayudarte?\r\n\r\n1. Abonar a mi crédito\r\n2. Solicitar mi crédito / Quiero comprar \r\n3. Estatus de mi crédito\r\n4. Garantía de mi compra\r\n5. Préstamo en efectivo\r\n6. Conocer el estatus de mi pedido\r\n7. Ubicación de tiendas\r\n9. Regresar al menú principal \r\n0. Regresar a la opción anterior\r\n\r\nEscribe el número de la opción deseada para continuar.";
        private string AVISO_PRIVACIDAD_PASITO = "En el aviso de privacidad encontrarás información sobre el uso de tus datos personales:\r\n\r\nhttps://bit.ly/MP-avisodeprivacidad";
        private string AVISO_PRIVACIDAD_VILLAREAL = "En el aviso de privacidad encontrarás información sobre el uso de tus datos personales:\r\n\r\nhttps://bit.ly/VR-avisodeprivacidad";
        private static InstagramHelper helper;
        private SQLService DB;
        private User user;
        private Utils utils;
        private AppSettings config;
        private TwilioHelper twilio;
        public InstagramWebhook()
        {
            helper = new InstagramHelper();
            DB = new SQLService();
            utils = new Utils();
            config = new AppSettings();
            twilio = new TwilioHelper();
        }

        [FunctionName("InstagramWebhook")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            Response resp = null;
            Debug.WriteLine("QUERY: " + req.QueryString);

            try
            {
                string hub_challenge = ValidateWebhook(req);
                if (!String.IsNullOrEmpty(hub_challenge)) return new OkObjectResult(hub_challenge);
                string bodyString = await new StreamReader(req.Body).ReadToEndAsync();
                InstagramRequest body = JsonConvert.DeserializeObject<InstagramRequest>(bodyString);
                Sender sender = body?.entry?.FirstOrDefault()?.messaging?.FirstOrDefault()?.sender ?? null;
                Models.Message message = body?.entry?.FirstOrDefault()?.messaging?.FirstOrDefault()?.message ?? null;

                Console.WriteLine("BODY: " + bodyString);

                string sender_id = sender?.id ?? null,
                    mid = message?.mid ?? null,
                    text = message?.text?.Trim() ?? null;
                Boolean is_echo = message?.is_echo ?? false;

                //$attachments = isset($_POST["entry"][0]["messaging"][0]["message"]["attachments"]) ? $_POST["entry"][0]["messaging"][0]["message"]["attachments"] : null;

                if (string.IsNullOrEmpty(sender_id)) throw new Exception("Campo entry[0].messaging[0].sender.id requerido");
                if (string.IsNullOrEmpty(mid)) throw new Exception("Campo entry[0].messaging[0].message.mid requerido");
                if (is_echo) throw new Exception("Mensaje no procesado al ser echo.");

                GetOrCreateUser(sender_id);
                twilio.AddMessageToConversation(user.identity, text, user.conversation_sid);

                switch (user.estatus)
                {
                    case "bienvenida":
                        //Status updated because the channel was created with no status
                        await UpdateStatus("bienvenida");
                        await SendMessage(user, "¡Hola! Bienvenida(o) a Consentilinea\r\n\r\nEscribe tu nombre completo");
                        await UpdateStatus("validar_nombre");
                        break;
                    case "validar_nombre":
                        Match match = Regex.Match(text.Trim(), @"/^([A-Za-zÑñÁáÉéÍíÓóÚú]+['\\-]{0,1}[A-Za-zÑñÁáÉéÍíÓóÚú]+)(\\s+([A-Za-zÑñÁáÉéÍíÓóÚú]+['\\-]{0,1}[A-Za-zÑñÁáÉéÍíÓóÚú]+))*$/", RegexOptions.IgnoreCase);
                        if (!match.Success)
                        {
                            await SendMessage(user, MENU_CIUDAD.Replace("{NOMBRE}", text));
                            await UpdateStatus("validar_ciudad", new JObject()
                            {
                                ["nombre"] = text
                            });
                        }
                        else
                        {
                            await SendMessage(user, "¿Nos podrías brindar tu nombre completo?");
                        }
                        break;
                    case "validar_ciudad":
                        string[] options = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10" };
                        if (options.Contains(text))
                        {
                            //Insertar registo en tabla Cliente_Estatus
                            int id_cliente = InsertCliente(user);
                            string aviso_privacidad = text.Trim().Equals("2") ? AVISO_PRIVACIDAD_PASITO : AVISO_PRIVACIDAD_VILLAREAL;
                            await SendMessage(user, aviso_privacidad);
                            await SendMessage(user, MENU_OPCIONES);
                            await UpdateStatus("menu", new JObject()
                            {
                                ["id_cliente"] = id_cliente,
                                ["ciudad"] = ObtenerCiudadNombre(text)
                            });
                        }
                        else
                        {
                            await SendMessage(user, string.Concat($"Opción {text} no válida. \n\n ", MENU_CIUDAD.Replace("{NOMBRE}", "")));
                        }
                        break;
                    case "enviar_menu":
                        await SendMessage(user, MENU_OPCIONES);
                        await UpdateStatus("menu");
                        break;
                    case "menu":
                        string[] options_menu = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };
                        if (!options_menu.Contains(text))
                        {
                            if (text.Equals("0"))
                            {
                                await SendMessage(user, MENU_CIUDAD.Replace("{NOMBRE}", user.nombre));
                                await UpdateStatus("validar_ciudad");
                            }
                            else
                            {
                                await SendMessage(user, string.Concat($"Opción {text} no válida\n\n", MENU_OPCIONES));
                                await UpdateStatus("menu");
                            }
                            break;
                        }

                        JObject result = await utils.POST(config.TWILIO_FUNCTION_HORARIO_ATENCION, new JObject()
                        {
                            ["ciudad"] = user.ciudad,
                            ["opcion"] = text
                        });

                        Boolean disponible = result.ContainsKey("disponible") ? result.SelectToken("disponible").Value<bool>() : false;
                        string mensaje_horario = result.ContainsKey("horario") ? result["horario"].ToString() : "Error al obtener horarios de atención.";

                        if (disponible)
                        {
                            await SendMessage(user, "En un momento un asesor se comunicará contigo.");
                            await UpdateStatus("comunicando_con_agente", new JObject()
                            {
                                ["opcion"] = text,
                                ["fuera_horario"] = 0
                            });
                            await CreateFlexTask(user, "Una vez hayas terminado, ayúdanos calificando la atención que te brindamos accediendo al siguiente enlace:\r\n\r\nhttps://roga-8372.twil.io/roga-encuesta.html?p=1829&&c=1");

                        }
                        else
                        {
                            await SendMessage(user, $"Horario fuera de atención: {mensaje_horario}\n\n¿Quieres que un agente se comunique contigo?\r\n1. Sí\r\n2. No");
                            await UpdateStatus("validacion_agente_comunique", new JObject()
                            {
                                ["opcion"] = text,
                                ["fuera_horario"] = 1
                            });
                        }
                        break;
                    case "validacion_agente_comunique":
                        string[] options_fuera_horario = { "1", "2" };

                        if (!options_fuera_horario.Contains(text))
                        {
                            await SendMessage(user, $"Opción no válida\n\n¿Quieres que un agente se comunique contigo?\r\n1. Sí\r\n2. No");
                        }
                        else
                        {
                            if (text.Equals("1"))
                            {
                                await SendMessage(user, "Proporciona tu número telefónico");
                                await UpdateStatus("guardar_numero_telefonico");
                            }
                            else
                            {
                                await CreateFlexTask(user, "Gracias por comunicarte.");
                            }
                        }
                        break;
                    case "guardar_numero_telefonico":
                        Match match_numero = Regex.Match(text, @"^[0-9]{10}$", RegexOptions.IgnoreCase);
                        if (match_numero.Success)
                        {
                            //Se crea tarea reservada
                            await CreateFlexTask(user, "Un agente se contactará contigo mediante el número que te comunicaste a primera hora de horario de atención.");
                        }
                        else
                        {
                            await SendMessage(user, "Número telefónico no válido, por favor ingresa tu número telefónico a 10 dígitos");
                        }
                        break;
                    case "comunicando_con_agente":
                        await SendMessage(user, "En un momento un asesor se comunicará contigo.");
                        break;
                    case "flex":
                        Console.WriteLine("Log");
                        break;
                    default:
                        throw new Exception("Estatus no válido");
                }
                resp = new Response(Response.SUCCESS, $"OK", null);
            }
            catch (APIException ex)
            {
                Console.WriteLine($"Exception: {ex._Message}");
                resp = new Response(Response.ERROR, $"Error", new JObject()
                {
                    ["Message"] = ex._Message,
                    ["SystemMessage"] = ex._Exception.Message,
                    ["Identity"] = user != null ? user.identity : "",
                    ["ConversationSid"] = user != null ? user.conversation_sid : ""
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                resp = new Response(Response.ERROR, $"Error", new JObject()
                {
                    ["Message"] = ex.Message,
                    ["Identity"] = user != null ? user.identity : "",
                    ["ConversationSid"] = user != null ? user.conversation_sid : ""
                });
            }

            return new OkObjectResult(resp);
        }

        private async Task<string> SendMessage(User user, string message, Boolean trigger_webhook = false)
        {
            string author = "system";
            try
            {
                JObject result = await helper.SendMessage(user.sender_id, message);
                if (result == null) throw new Exception($"Error al enviar mensaje de instagram sender_id {user.sender_id}");

                string sid = twilio.AddMessageToConversation(author, message, user.conversation_sid, trigger_webhook);
                return sid;
            }
            catch (Exception ex)
            {
                throw new APIException("Error al enviar mensaje", ex);
            }
        }

        private void GetOrCreateUser(string sender_id)
        {
            try
            {

                string identity = string.Concat("instagram:", sender_id);
                ParticipantConversationResource conversation = twilio.findExistingConversation(identity);
                user = new User();
                JObject attributes = new JObject();
                user.sender_id = sender_id;
                user.identity = identity;

                if (conversation == null)
                {
                    string conversation_sid = twilio.CreateConversation(sender_id);
                    twilio.AddParticipantToConversation(conversation_sid, identity);
                    twilio.SetWebhookToConversation(conversation_sid, TwilioHelper.WebhookTypes.URL, $"{config.AZURE_FUNCTION_WEBHOOK_URL}?sender_id={sender_id}");
                    user.estatus = "bienvenida";
                    user.conversation_sid = conversation_sid;
                    attributes = new JObject()
                    {
                        ["estatus"] = "bienvenida"
                    };
                }
                else
                {
                    string attributes_string = conversation.ConversationAttributes;
                    attributes = JObject.Parse(attributes_string);
                    user.conversation_sid = conversation.ConversationSid.ToString();
                }

                user.opcion = attributes.ContainsKey("opcion") ? attributes["opcion"].ToString() : "";
                user.ciudad = attributes.ContainsKey("ciudad") ? attributes["ciudad"].ToString() : "";
                user.estatus = attributes.ContainsKey("estatus") ? attributes["estatus"].ToString() : "";
                user.nombre = attributes.ContainsKey("nombre") ? attributes["nombre"].ToString() : "";
                user.attributes = attributes;

                return;
            }
            catch (Exception ex)
            {
                throw new APIException($"Error al obtener usuario de atributos", ex);
            }
        }
        private async Task UpdateStatus(string status, JObject payload = null)
        {
            await Task.Run(() =>
            {
                try
                {
                    JObject attributes = user.attributes;
                    JObject json = new JObject()
                    {
                        ["estatus"] = status
                    };
                    attributes.Merge(json);
                    if (payload != null) attributes.Merge(payload);

                    return twilio.UpdateConversationAttributes(user.conversation_sid, attributes);
                }
                catch (Exception ex)
                {
                    throw new APIException($"Error al actualizar estatus de usuario", ex);
                }
            });
        }

        private int InsertCliente(User user)
        {
            try
            {
                int id = DB.Insert(new JObject()
                {
                    ["Numero"] = "",
                    ["Nombre"] = user.nombre,
                    ["Fecha"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    ["Estatus"] = "ABIERTO",
                    ["Ciudad"] = user.ciudad,
                    ["Canal"] = "Instagram",
                    ["Opcion"] = user.opcion,
                    ["ConversationSID"] = user.conversation_sid
                }, $"{DB.SCHEMA}.Cliente_Estatus");

                if (id == 0) throw new Exception("Error al insertar cliente en DB");
                return id;
            }
            catch (Exception ex)
            {
                throw new APIException($"Error al insertar cliente.", ex);
            }
        }

        /*
        private async Task UpdateClient(User user, JObject payload)
        {
            await Task.Run(() =>
            {
                try
                {
                    //int id = DB.Update(payload, $"id={user.id}", $"{DB.SCHEMA}.Cliente_Estatus");
                    //if (id == 0) throw new Exception("Error actualizando estatus de cliente");
                    //return id > 0;
                    return 1;
                }
                catch (Exception ex)
                {
                    throw new APIException($"Error al actualizar estatus de cliente", ex);
                }
            });
        }
        */

        private async Task CreateFlexTask(User user, string body)
        {
            try
            {
                await UpdateStatus("flex");
                //Se setea el webhook para ejecutar el workflow de studio que mandara a flex, cuando studio manda a flex elimina el mismo el webhook.
                twilio.SetWebhookToConversation(user.conversation_sid, TwilioHelper.WebhookTypes.WORKFLOW, null, config.TWILIO_INSTAGRAM_WORKFLOWSID);

                //Se manda la bander xTwilioWebhookEnabled para ejecutar el workflow
                await SendMessage(user, body, true);
            }
            catch (Exception ex)
            {
                throw new APIException($"Error al actualizar estatus de cliente", ex);
            }
        }

        public string ValidateWebhook(HttpRequest req)
        {
            IDictionary<string, string> queryParams = req.GetQueryParameterDictionary();
            string hub_challenge = queryParams.ContainsKey("hub.challenge") ? queryParams["hub.challenge"] : "";
            return hub_challenge;
        }

        private string ObtenerCiudadNombre(string code)
        {
            switch (code)
            {
                case "1": return "CiudadJuarez";
                case "2": return "Chihuahua";
                case "3": return "Culiacan";
                case "4": return "Hermosillo";
                case "5": return "Leon";
                case "6": return "Mexicali";
                case "7": return "Monterrey";
                case "8": return "Saltillo";
                case "9": return "Tijuana";
                case "10": return "Tijuana";
                default: return code;
            }
        }
    }
}
