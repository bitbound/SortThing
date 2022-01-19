using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SortThing.Services
{
    public interface ISystemTime
    {
        DateTimeOffset Now { get; }
    }

    public class SystemTime : ISystemTime
    {
        public DateTimeOffset Now => DateTimeOffset.Now;
    }
}
