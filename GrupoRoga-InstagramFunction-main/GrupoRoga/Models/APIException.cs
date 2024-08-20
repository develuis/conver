using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace GrupoRoga.Models
{
    public class APIException : Exception
    {
        public string _Message;
        public Exception _Exception;
        public APIException(string message, Exception ex) {
            _Message = message;
            _Exception = ex;
        }
    }
}
