using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOT.DataModel
{
    public interface IThingRepository : IDisposable
    {
        IQueryable<Thing> AllThings { get; }
        IQueryable<Thing> ActiveThings { get; }

        bool ThingExists(Guid id, bool all = true);
        bool ThingActive(Guid id);
        void ActivateThing(Guid id);
        void DeactivateThing(Guid id);
        Thing GetThingById(Guid id, bool all = true);
        void InsertThing(Thing thing, Guid id, string area);
    }
}
