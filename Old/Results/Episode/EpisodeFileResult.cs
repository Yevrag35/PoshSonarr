using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Sonarr.Api.Results
{
    public class EpisodeFileResult : SonarrResult
    {
        internal override string[] SkipThese => new string[3] { "RelativePath", "FileName", "ParentFolder" };

        public long SeriesId { get; internal set; }
        public long SeasonNumber { get; internal set; }
        //public string RelativePath { get; internal set; }
        public string FileName =>
            Path != null ?
            Path.Split(new string[1] { @"\" }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault() :
            null;

        public string ParentFolder =>
            Path != null ?
            Path.Substring(0, Path.LastIndexOf(@"\")) :
            null;

        internal string Path { get; set; }
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
