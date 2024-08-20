using GrupoRoga.Models;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Twilio;
using Twilio.Rest.Conversations.V1;
using Twilio.Rest.Conversations.V1.Conversation;
using Twilio.Rest.Taskrouter.V1.Workspace;
using Twilio.TwiML.Voice;

namespace GrupoRoga.Helpers
{
    public class TwilioHelper
    {

        public enum WebhookTypes
        {
            URL, WORKFLOW
        };

        private AppSettings config;
        public TwilioHelper()
        {
            config = new AppSettings();
            string TWILIO_ACCOUNT_SID = config.TWILIO_ACCOUNT_SID;
            string TWILIO_AUTH_TOKEN = config.TWILIO_AUTH_TOKEN;
            TwilioClient.Init(TWILIO_ACCOUNT_SID, TWILIO_AUTH_TOKEN);
        }

        public string AddMessageToConversation(string author, string body, string conversationSid, Boolean sendWehbook = false)
        {
            try
            {
                var message = MessageResource.Create(
                    author: author,
                    body: body,
                    pathConversationSid: conversationSid,
                    xTwilioWebhookEnabled: sendWehbook ? MessageResource.WebhookEnabledTypeEnum.True : MessageResource.WebhookEnabledTypeEnum.False
                );
                return message.ConversationSid;
            }
            catch (Exception ex)
            {
                throw new APIException("Error al agregar mensaje a conversation", ex);
            }
        }

        public ParticipantConversationResource findExistingConversation(string identity)
        {
            var participantConversations = ParticipantConversationResource.Read(
                identity: identity, 
                limit: 20
            );

            ParticipantConversationResource conversation = null;

            foreach (var _conversation in participantConversations)
            {
                if (!_conversation.ConversationState.Equals(ParticipantConversationResource.StateEnum.Closed))
                    conversation = _conversation;
            }

            return conversation;
        }

        public string CreateConversation(string sender_id)
        {
            try
            {
                var conversation = ConversationResource.Create(
                   friendlyName: $"Conversation_Instagram:{sender_id}",
                   xTwilioWebhookEnabled: ConversationResource.WebhookEnabledTypeEnum.True
                );
                return conversation.Sid;
            }catch(Exception ex) {
                throw new APIException("Error al crear conversation", ex);
            }
        }

        public string AddParticipantToConversation(string conversation_sid, string identity)
        {
            try
            {
                var participant = ParticipantResource.Create(
                   identity: identity,
                   pathConversationSid: conversation_sid
                );

                return participant.Sid;
            }
            catch (Exception ex)
            {
                throw new APIException("Error al agregar participante a conversation", ex);
            }
        }

        public ConversationResource UpdateConversationAttributes(string conversation_sid, JObject attributes)
        {
            try
            {
                ConversationResource conversation = ConversationResource.Update(
                    attributes: attributes.ToString(Formatting.None),
                    pathSid: conversation_sid
                );
                return conversation;
            }
            catch (Exception ex)
            {
                throw new APIException("Error al configurar webhook de conversation", ex);
            }
        }

        public string SetWebhookToConversation(string conversation_sid, WebhookTypes type, string url = null, string workflow_sid = null)
        {
            try
            {
                WebhookResource webhook = null;

                if (type == WebhookTypes.URL)
                {
                    if (url == null) throw new Exception("SetWebhookToConversation parámetro url requerido");
                    webhook = WebhookResource.Create(
                        configurationMethod: WebhookResource.MethodEnum.Post,
                        configurationFilters: new List<string> {
                            "onMessageAdded"
                        },
                        configurationUrl: url,
                        target: WebhookResource.TargetEnum.Webhook,
                        pathConversationSid: conversation_sid
                    );
                }else if (type == WebhookTypes.WORKFLOW)
                {
                    if (workflow_sid == null) throw new Exception("SetWebhookToConversation parámetro workflow_sid requerido");
                    webhook = WebhookResource.Create(
                       target: WebhookResource.TargetEnum.Studio,
                       configurationFilters: new List<string> {
                            "onMessageAdded"
                       },
                       pathConversationSid: conversation_sid,
                       configurationFlowSid: workflow_sid
                   );
                }
                else
                {
                    throw new Exception($"SetWebhookToConversation parámetro {type} no válido");
                }

                return webhook.Sid;
            }
            catch (Exception ex)
            {
                throw new APIException("Error al configurar webhook de conversation", ex);
            }
        }

        public string CreateTask(string workflowSid, string workspaceSid, JObject attributes)
        {
            try
            {
                var task = TaskResource.Create(
                    attributes: attributes.ToString(Formatting.None),
                    timeout: 604800, // 1 semana 
                    workflowSid: workflowSid,
                    pathWorkspaceSid: workspaceSid
                    //taskChannel: config.TWILIO_TASK_CHANNEL_SID
                );
                return task.Sid;
            }
            catch(Exception ex)
            {
                throw new APIException("Error al crear task", ex);
            }
        }
    }
}
