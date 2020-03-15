using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace IOT.Server.Provisioning
{
    public class ProvisioningException : ApplicationException
    {
        public ProvisioningStage Stage { get; set; }

        public ProvisioningException() : base()
        {
            Stage = ProvisioningStage.None;
        }

        public ProvisioningException(string message) : base(message)
        {
            Stage = ProvisioningStage.None;
        }

        public ProvisioningException(string message, Exception innerException) : base(message, innerException)
        {
            Stage = ProvisioningStage.None;
        }

        protected ProvisioningException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            Stage = ProvisioningStage.None;
        }
    }
}
