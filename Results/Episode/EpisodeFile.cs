using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Sonarr.Api.Results
{
    public class EpisodeFile : SonarrResult
    {
        private long _seriesId;
        private long _seasonNumber;
        private string _relativePath;
        private string _path;
        private long _size;
        private DateTime? _dateAdded;
        private string _sceneName;
        private JObject _quality;
        private JObject _mediaInfo;
        private bool _qualityCutoffNotMet;
        private long _id;

        public long SeriesId => _seriesId;
        public long SeasonNumber => _seasonNumber;
        public string RelativePath => _relativePath;
        public string Path => _path;
        public long Size => _size;
        public DateTime? DateAdded => ToLocalTime(_dateAdded);
        public DateTime? DateAddedUtc => _dateAdded;
        public string SceneName => _sceneName;
        public EpisodeQuality Quality => (EpisodeQuality)_quality;
        public MediaInfo MediaInfo => (MediaInfo)_mediaInfo;
        public bool QualityCutoffNotMet => _qualityCutoffNotMet;
        public long Id => _id;

        private EpisodeFile(IDictionary dict) => MatchResultsToProperties(dict);

        public static explicit operator EpisodeFile(JObject job)
        {
            if (job != null)
            {
                var dict = JsonConvert.DeserializeObject<Dictionary<object, object>>(JsonConvert.SerializeObject(job));
                return new EpisodeFile(dict);
            }
            else
            {
                return null;
            }
        }
        public static explicit operator EpisodeFile(Dictionary<object, object> dict) =>
            dict != null ? new EpisodeFile(dict) : null;
    }
}
