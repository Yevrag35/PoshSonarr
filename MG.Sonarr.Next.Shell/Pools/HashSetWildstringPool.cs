using MG.Sonarr.Next.Services.Collections;
using MG.Sonarr.Next.Shell.Components;

namespace MG.Sonarr.Next.Shell.Pools
{
    public sealed class HashSetWildstringPool : SonarrObjectPool<HashSet<WildcardString>>
    {
        const int DEFAULT_CAPACITY = 5;
        const int MAX_CAPACITY = 10000;
        const int MAX_POOL = 20;

        public HashSetWildstringPool()
            : base()
        {
        }

        protected override int MaxPoolCapacity => MAX_POOL;

        protected override HashSet<WildcardString> Construct()
        {
            return new(DEFAULT_CAPACITY);
        }
        protected override HashSet<WildcardString> GetItem()
        {
            var set = base.GetItem(out bool wasConstructed);
            if (!wasConstructed)
            {
                CapSetCapacity(set, DEFAULT_CAPACITY);
            }

            return set;
        }
        protected override bool ResetObject(HashSet<WildcardString> obj)
        {
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
