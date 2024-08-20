using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrupoRoga.Models
{
    public class Response
    {
        public static readonly string SUCCESS = "success";
        public static readonly string ERROR = "error";
        public static readonly string FAIL = "fail";

        public string estatus { get; set; }
        public string mensaje { get; set; }
        public Object data { get; set; }


        public Response(string _estatus, string _mensaje, Object _data)
        {
            estatus = _estatus;
            mensaje = _mensaje;
            data = _data;
        }
    }
}
