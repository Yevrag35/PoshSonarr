using System;
using System.Collections.Generic;

namespace MG.Sonarr.Functionality.Extensions
{
    /// <summary>
    /// A static class for extending <see cref="HashSet{T}"/> to allow adding multiple values at once.
    /// </summary>
    public static class HashSetExtensions
    {
        public static bool AddRange<T>(this HashSet<T> hashSet, IEnumerable<T> values)
        {
            bool result = true;
            foreach (T val in values)
            {
                bool check = hashSet.Add(val);
                if (!check)
                    result = false;
            }
            return result;
        }
    }
}
