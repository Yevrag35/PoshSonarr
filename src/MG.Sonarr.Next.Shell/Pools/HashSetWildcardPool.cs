using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Shell.Components;

namespace MG.Sonarr.Next.Shell.Pools
{
    public sealed class HashSetWildcardPool : SonarrObjectPool<HashSet<Wildcard>>
    {
        const int DEFAULT_CAPACITY = 5;
        const int MAX_CAPACITY = 10000;
        const int MAX_POOL = 20;

        protected override int MaxPoolCapacity => MAX_POOL;

        public HashSetWildcardPool()
            : base()
        {
        }

        protected override HashSet<Wildcard> Construct()
        {
            return new(DEFAULT_CAPACITY);
        }
        protected override HashSet<Wildcard> GetItemFromBag()
        {
            var set = base.GetItemFromBag(out bool wasConstructed);
            if (!wasConstructed)
            {
                CapSetCapacity(set, DEFAULT_CAPACITY);
            }

            return set;
        }
        protected override bool ResetObject(HashSet<Wildcard> obj)
        {
            ArgumentNullException.ThrowIfNull(obj);

            obj.Clear();
            return true;
        }
        private static void CapSetCapacity<T>(HashSet<T> set, [ConstantExpected] int defaultCapacity)
        {
            int capacity = set.EnsureCapacity(defaultCapacity);
            if (capacity > MAX_CAPACITY)
            {
                set.TrimExcess();
            }
        }
    }
}
