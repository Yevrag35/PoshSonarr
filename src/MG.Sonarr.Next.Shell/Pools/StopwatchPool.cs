using MG.Sonarr.Next.Collections;

namespace MG.Sonarr.Next.Shell.Pools
{
    public sealed class StopwatchPool : SonarrObjectPool<Stopwatch>
    {
        protected override int MaxPoolCapacity => 10;

        protected override Stopwatch Construct()
        {
            return new();
        }

        protected override bool ResetObject(Stopwatch obj)
        {
            ArgumentNullException.ThrowIfNull(obj);

            if (obj.IsRunning)
            {
                obj.Stop();
            }

            obj.Reset();
            return true;
        }
    }
}
