using MG.Sonarr.Next.Services.Collections;

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
            if (obj is null)
            {
                return false;
            }

            obj.Clear();
            return true;
        }
    }
}
