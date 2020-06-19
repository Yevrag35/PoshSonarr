using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Results
{
    [JsonObject(MemberSerialization.OptIn)]
    public class IndexerOptions : BaseResult
    {
        [JsonProperty("maximumSize")]
        private int _maxSize;

        [JsonProperty("minimumAge")]
        private int _minAgeInMins;

        [JsonProperty("retention")]
        private int _retDays;

        [JsonProperty("rssSyncInterval")]
        private int _rssIntInMins;

        #region JSON PROPERTIES
        [JsonProperty("id")]
        public int Id { get; private set; }

        [JsonIgnore]
        public int MaximumSizeInGB
        {
            get => _maxSize;
            set
            {
                if (value < 0)
                    throw new ArgumentException("Cannot set maximum size to a negative number.");

                _maxSize = value;
            }
        }

        [JsonIgnore]
        public int MinimumAgeInMins
        {
            get => _minAgeInMins;
            set
            {
                if (value < 0)
                    throw new ArgumentException("Cannot set minimum age to a negative number.");

                _minAgeInMins = value;
            }
        }

        [JsonIgnore]
        public int RetentionInDays
        {
            get => _retDays;
            set
            {
                if (value < 0)
                    throw new ArgumentException("Cannot set retention to a negative number.");

                _retDays = value;
            }
        }

        [JsonIgnore]
        public int RssSyncIntervalInMins
        {
            get => _rssIntInMins;
            set
            {
                if (value < 0 || (value >= 1 && value <= 9) || value > 120)
                    throw new ArgumentException("Rss Sync Interval must be 0 or between 10 and 120.");

                _rssIntInMins = value;
            }
        }

        #endregion
    }
}