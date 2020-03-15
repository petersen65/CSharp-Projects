using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOT.Server.Provisioning
{
    public enum ProvisioningStage
    {
        None,
        StorePartitionInDatabase,
        CreatePartitionInServiceBus,
        CreatePartitionInTableStorage,
        CreatePartitionInAccessControl,
        ActivatePartition,
        StoreThingInDatabase,
        CreateThingInServiceBus,
        CreateThingInServiceIdentity,
        CreateThingInRuleIngestion,
        CreateThingInAccessControl,
        CreateThingInRuleCommand,
        CreateThingConnectionString,
        ActivateThing
    }
}
