using System;

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
