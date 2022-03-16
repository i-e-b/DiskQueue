using System;
using System.Collections.Generic;

namespace DiskQueue.Tests.Helpers
{
    public static class Extensions
    {
        public static IEnumerable<T> OrEmpty<T>(this IEnumerable<T>? src)
        {
            if (src is null) return Array.Empty<T>();
            return src;
        }
    }
}