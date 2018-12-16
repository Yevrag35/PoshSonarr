using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Sonarr.Api.Results
{
    public class EpisodeFileResult : SonarrResult
    {
        internal override string[] SkipThese => null;

        public long SeriesId { get; internal set; }
        public long SeasonNumber { get; internal set; }
        public string RelativePath { get; internal set; }
        public string Path { get; internal set; }
        public long Size { get; internal set; }
        public DateTime? DateAdded { get; internal set; }
        public DateTime? DateAddedUtc { get; internal set; }
        public string SceneName { get; internal set; }
        public EpisodeQuality Quality { get; internal set; }
        public MediaInfo MediaInfo { get; internal set; }
        public bool QualityCutoffNotMet { get; internal set; }
        public long Id { get; internal set; }

        public EpisodeFileResult() : base() { }

        public static explicit operator EpisodeFileResult(JObject job) =>
            FromJObject<EpisodeFileResult>(job);
    }
}
