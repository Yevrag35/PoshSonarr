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
    }
}
