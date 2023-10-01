using MG.Sonarr.Next.Services.Collections;
using MG.Sonarr.Next.Services.Http.Queries;

namespace MG.Sonarr.Next.Shell.Pools
{
    public sealed class PagingPool : SonarrObjectPool<PagingParameter>
    {
        protected override int MaxPoolCapacity => 6;

        protected override PagingParameter Construct()
        {
            return new();
        }

        protected override bool ResetObject(PagingParameter obj)
        {
            //obj.Reset();
            return true;
        }
    }
}
