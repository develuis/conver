using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrupoRoga
{
    public class AppSettings
    {
        public string AZURE_SQL_CONECTION_STRING { get; set; }
        public string TWILIO_ACCOUNT_SID { get; set; }
        public string TWILIO_AUTH_TOKEN { get; set; }
        public string AZURE_SQL_SCHEMA { get; set; }
        public string INSTAGRAM_ACCESS_TOKEN { get; set; }
        public string TWILIO_FUNCTION_HORARIO_ATENCION { get; set; }
        //public string TWILIO_FUNCTION_CREAR_TASK { get; set; }
        public string AZURE_FUNCTION_WEBHOOK_URL { get; set; }
        public string TWILIO_INSTAGRAM_WORKFLOWSID { get; set; }

        public AppSettings()
        {
            try
            {
                AZURE_SQL_CONECTION_STRING = Environment.GetEnvironmentVariable("AZURE_SQL_CONECTION_STRING") ?? throw new Exception("AZURE_SQL_CONECTION_STRING");
                AZURE_SQL_SCHEMA = Environment.GetEnvironmentVariable("AZURE_SQL_SCHEMA") ?? throw new Exception("AZURE_SQL_SCHEMA");
                TWILIO_ACCOUNT_SID = Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID") ?? throw new Exception("TWILIO_ACCOUNT_SID");
                TWILIO_AUTH_TOKEN = Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN") ?? throw new Exception("TWILIO_AUTH_TOKEN");
                INSTAGRAM_ACCESS_TOKEN = Environment.GetEnvironmentVariable("INSTAGRAM_ACCESS_TOKEN") ?? throw new Exception("INSTAGRAM_ACCESS_TOKEN");
                TWILIO_FUNCTION_HORARIO_ATENCION = Environment.GetEnvironmentVariable("TWILIO_FUNCTION_HORARIO_ATENCION") ?? throw new Exception("TWILIO_FUNCTION_HORARIO_ATENCION");
                //TWILIO_FUNCTION_CREAR_TASK = Environment.GetEnvironmentVariable("TWILIO_FUNCTION_CREAR_TASK") ?? throw new Exception("TWILIO_FUNCTION_CREAR_TASK");
                AZURE_FUNCTION_WEBHOOK_URL = Environment.GetEnvironmentVariable("AZURE_FUNCTION_WEBHOOK_URL") ?? throw new Exception("AZURE_FUNCTION_WEBHOOK_URL");
                TWILIO_INSTAGRAM_WORKFLOWSID = Environment.GetEnvironmentVariable("TWILIO_INSTAGRAM_WORKFLOWSID") ?? throw new Exception("TWILIO_INSTAGRAM_WORKFLOWSID");
            }
            catch(Exception ex){
                throw new Exception($"Variable no configurada: {ex.Message}");
            }
        }
    }
}
