using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOT.Server.Provisioning
{
    public class ThingDescription
    {
        public Guid Id { get; private set; }
        public string Description { get; private set; }
        public string Area { get; private set; }
        public int CommandTopic { get; private set; }
        public string ConnectionString { get; private set; }

        public ThingDescription(string description, string area, Guid? id = null)
        {
            Description = description;
            Area = area;
            Id = id ?? Guid.NewGuid();
        }

        public override string ToString()
        {
            return Id.ToString();
        }

        internal void Assert()
        {
            if (Id == Guid.Empty)
                throw new ArgumentException("Id");

            if (string.IsNullOrWhiteSpace(Description))
                throw new ArgumentException("Description");

            if (string.IsNullOrWhiteSpace(Area))
                throw new ArgumentException("Area");
        }

        internal void SetCommandTopic(int ct)
        {
            CommandTopic = ct;
        }

        internal void SetConnectionString(string cs)
        {
            ConnectionString = cs;
        }
    }
}
