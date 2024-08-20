using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Twilio.Rest.Conversations.V1;

namespace GrupoRoga.Models
{
    internal class User
    {
        public string sender_id { get; set; }
        public string identity { get; set; }
        public string estatus { get; set; }
        public string nombre { get; set; }
        public string ciudad { get; set; }
        public string opcion { get; set; }
        public string conversation_sid { get; set; }
        public JObject attributes { get; set; }

        //public ParticipantConversationResource conversation { get; set; }
    }
}
