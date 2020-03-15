using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace IOT.DataModel
{
    public class PartitionException : ApplicationException
    {
        public PartitionException() : base()
        {
        }

        public PartitionException(string message) : base(message)
        {
        }

        public PartitionException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected PartitionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
