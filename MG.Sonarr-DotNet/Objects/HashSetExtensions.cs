using System;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr
{
    public static class HashSetExtensions
    {
        public static void AddRange<T>(this HashSet<T> hashSet, IEnumerable<T> items)
        {
            foreach (T item in items)
            {
                hashSet.Add(item);
            }
        }
    }
}
