using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Sonarr.Api.Results
{
    public class EpisodeFile : SonarrResult
    {
        //private long _seriesId;
        //private long _seasonNumber;
        //private string _relativePath;
        //private string _path;
        //private long _size;
        //private DateTime? _dateAdded;
        //private string _sceneName;
        //private JObject _quality;
        //private JObject _mediaInfo;
        //private bool _qualityCutoffNotMet;
        //private long _id;

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

        //private EpisodeFile(IDictionary dict) => MatchResultsToProperties(dict);

        //public static explicit operator EpisodeFile(JObject job)
        //{
        //    if (job != null)
        //    {
        //        var dict = JsonConvert.DeserializeObject<Dictionary<object, object>>(JsonConvert.SerializeObject(job));
        //        return new EpisodeFile(dict);
        //    }
        //    else
        //    {
        //        return null;
        //    }
        //}
        //public static explicit operator EpisodeFile(Dictionary<object, object> dict) =>
        //    dict != null ? new EpisodeFile(dict) : null;
    }
}
