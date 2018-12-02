using MG.Api;
using Sonarr.Api;
using System;

namespace Sonarr
{
    internal static class SonarrServiceContext
    {
        internal static ApiUrl Value { get; set; }
        internal static ApiKey ApiKey { get; set; }
        internal static bool IsSet => Value != null && ApiKey != null;

        internal static ApiResult SonarrSeries { get; set; }
        internal static bool HasSeries => SonarrSeries != null;
        internal static bool NoApiPrefix { get; set; }

        internal static void Clear()
        {
            Value = null;
            ApiKey = null;
            SonarrSeries = null;
        }
    }
}
