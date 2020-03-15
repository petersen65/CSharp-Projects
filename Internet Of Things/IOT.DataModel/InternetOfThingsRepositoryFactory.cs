using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOT.DataModel
{
    public static class InternetOfThingsRepositoryFactory
    {
        public static IInternetOfThingsRepository CreateInternetOfThingsRepository()
        {
            return new InternetOfThingsContext();
        }

        public static IPartitionRepository CreatePartitionRepository()
        {
            return new InternetOfThingsContext();
        }

        public static IThingRepository CreateThingRepository()
        {
            return new InternetOfThingsContext();
        }
    }
}
