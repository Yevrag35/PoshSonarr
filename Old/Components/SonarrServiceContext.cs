using MG.Api;
using Sonarr.Api;
using Sonarr.Api.Results;
using System;
using System.Collections.Generic;

namespace Sonarr
{
    public static class SonarrServiceContext
    {
        public static ApiUrl Value { get; set; }
        public static ApiKey ApiKey { get; set; }
        public static string BaseUrl { get; set; }
        public static bool IsSet => Value != null && ApiKey != null;

        public static SeriesResult SonarrSeries { get; set; }
        public static bool HasSeries => SonarrSeries != null;
        public static bool NoApiPrefix { get; set; }

        internal static Dictionary<int, SeriesResult> SeriesDictionary = new Dictionary<int, SeriesResult>();

        internal static ApiCaller TheCaller { get; set; }

        public static void Clear()
        {
            Value = null;
            ApiKey = null;
            SonarrSeries = null;
        }
    }
}
