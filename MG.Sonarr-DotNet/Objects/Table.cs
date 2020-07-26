using MG.Sonarr.Functionality;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr
{
    public abstract class Table<T> where T : IConvertible
    {
        protected IEqualityComparer<T> Comparer { get; }

        protected const string ADD = "Add";
        protected const string REMOVE = "Remove";
        protected const string REPLACE = "Replace";

        public int Count => this.Add.Count + this.Remove.Count + this.Set.Count;

        public HashSet<T> Add { get; }
        protected string AddKey { get; set; }
        public HashSet<T> Remove { get; }
        protected string RemoveKey { get; set; }
        public HashSet<T> Set { get; }

        protected Table(IEqualityComparer<T> comparer)
        {
            this.Comparer = comparer;
            this.Add = new HashSet<T>(this.Comparer);
            this.Remove = new HashSet<T>(this.Comparer);
            this.Set = new HashSet<T>(this.Comparer);
        }

        protected IEnumerable<T> ConvertFromObject(object o)
        {
            if (o is IEnumerable enumerable)
            {
                foreach (IConvertible item in enumerable)
                {
                    yield return (T)Convert.ChangeType(item, typeof(T));
                }
            }
            else if (o is IConvertible icon)
            {
                yield return (T)Convert.ChangeType(icon, typeof(T));
            }
        }
        protected bool TryGetKey(string key, IEnumerable<string> keys, IEqualityComparer<string> comparer, StringComparison comparison, out string realKey)
        {
            realKey = null;
            if (keys.Contains(key, comparer))
            {
                realKey = keys.FirstOrDefault(x => x.Equals(key, comparison));
            }
            return realKey != null;
        }
    }
}
