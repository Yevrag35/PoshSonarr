using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr
{
    public enum AnyAllNoneSet
    {
        All,
        Any,
        None
    }
    public abstract class AnyAllSet<T> : HashSet<T>
    {

        //public bool IsAll { get; protected set; }
        public AnyAllNoneSet Type { get; protected set; }
        protected IEqualityComparer<string> StringComparer { get; } = SonarrFactory.NewIgnoreCase();

        protected AnyAllSet() : base(new StringIgnoreCase())
        {
        }

        protected AnyAllSet(IEnumerable<T> items) : base(items, new StringIgnoreCase())
        {
        }

        protected string GetAllKey(IEnumerable<string> keys) => keys.FirstOrDefault(x => x.Equals(AnyAllNoneSet.All.ToString(), StringComparison.CurrentCultureIgnoreCase));
        protected string GetAnyKey(IEnumerable<string> keys) => keys.FirstOrDefault(x => x.Equals(AnyAllNoneSet.Any.ToString(), StringComparison.CurrentCultureIgnoreCase));
        protected string GetNoneKey(IEnumerable<string> keys) => keys.FirstOrDefault(x => x.Equals(AnyAllNoneSet.None.ToString(), StringComparison.CurrentCultureIgnoreCase));

        private class StringIgnoreCase : IEqualityComparer<T>
        {
            public bool Equals(T x, T y)
            {
                if (x is string xs && y is string ys)
                    return xs.Equals(ys, StringComparison.CurrentCultureIgnoreCase);

                else
                    return x.Equals(y);
            }
            public int GetHashCode(T item)
            {
                if (item is string itemStr)
                    return itemStr.ToLower().GetHashCode();

                else
                    return item.GetHashCode();
            }
        }
    }
}
