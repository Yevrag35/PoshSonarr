using MG.Sonarr.Next.Collections;

namespace MG.Sonarr.Next.Shell.Pools
{
    public sealed class SortedIntSetPool : SonarrObjectPool<SortedSet<int>>
    {
        const int MAX_POOL_CAPACITY = 20;
        protected override int MaxPoolCapacity => MAX_POOL_CAPACITY;
        public SortedIntSetPool()
            : base()
        {
        }


        protected override SortedSet<int> Construct()
        {
            return new();
        }
        protected override bool ResetObject(SortedSet<int> obj)
        {
            ArgumentNullException.ThrowIfNull(obj);

            obj.Clear();
            return true;
        }
    }
}
