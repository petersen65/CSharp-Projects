using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOT.Core.Policy
{
    public class CatchAllPolicy : RetryPolicy<Exception>
    {
        public CatchAllPolicy() : base(retry: 1, ignore: true)
        {
        }
    }
}
