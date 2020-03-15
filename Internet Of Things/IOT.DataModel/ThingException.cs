using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace IOT.DataModel
{
    public class ThingException : ApplicationException
    {
        public ThingException() : base()
        {
        }

        public ThingException(string message) : base(message)
        {
        }

        public ThingException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ThingException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
