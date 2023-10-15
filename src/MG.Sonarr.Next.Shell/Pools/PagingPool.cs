using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Services.Http.Queries;

namespace MG.Sonarr.Next.Shell.Pools
{
    public sealed class PagingPool : SonarrObjectPool<PagingParameter>
    {
        const int MAX_CAPACITY = 6;
        protected override int MaxPoolCapacity => MAX_CAPACITY;

        protected override PagingParameter Construct()
        {
            return new();
        }

        protected override bool ResetObject(PagingParameter obj)
        {
            ArgumentNullException.ThrowIfNull(obj);

            obj.Reset();
            return true;
        }
    }
}
