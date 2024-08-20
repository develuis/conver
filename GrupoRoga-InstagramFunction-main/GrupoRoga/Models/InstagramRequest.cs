using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrupoRoga.Models
{
    public class InstagramRequest
    {
        public List<Entry> entry { get; set; }

        [JsonProperty("object")]
        public string Object { get; set; }

    }

    public class Entry
    {
        public string id { get; set; }
        public List<Messaging> messaging { get; set; }
        public string time { get; set; }
    }

    public class Messaging
    {
        public Message message { get; set; }
        public Recipient recipient { get; set; }
        public Sender sender { get; set; }
        public string timestamp { get; set; }
    }

    public class Message
    {
        public string mid { get; set; }
        public string text { get; set; }
        public Boolean is_echo { get; set; }
    }

    public class Recipient
    {
        public string id { get; set; }
    }

    public class Sender
    {
        public string id { get; set; }
    }
}
