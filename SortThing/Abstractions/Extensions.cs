using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SortThing.Abstractions
{
    public static class Extensions
    {
        public static T Apply<T>(this T self, Action<T> action)
        {
            action(self);
            return self;
        }
    }
}
