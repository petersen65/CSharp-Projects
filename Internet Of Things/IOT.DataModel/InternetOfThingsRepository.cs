using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOT.DataModel
{
    public interface IInternetOfThingsRepository : IPartitionRepository, IThingRepository, IDisposable
    {
    }
}
