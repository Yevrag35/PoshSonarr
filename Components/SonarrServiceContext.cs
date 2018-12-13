using MG.Api;
using Sonarr.Api;
using Sonarr.Api.Results;
using System;

namespace Sonarr
{
    public static class SonarrServiceContext
    {
        public static ApiUrl Value { get; set; }
        public static ApiKey ApiKey { get; set; }
        public static bool IsSet => Value != null && ApiKey != null;

        public static StatusResult SonarrSeries { get; set; }
        public static bool HasSeries => SonarrSeries != null;
        public static bool NoApiPrefix { get; set; }

        public static void Clear()
        {
            Value = null;
            ApiKey = null;
            SonarrSeries = null;
        }
    }
}
