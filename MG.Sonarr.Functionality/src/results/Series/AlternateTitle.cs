using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace MG.Sonarr.Results
{ 
    /// <summary>
    /// A class that represents an alternative name of a series.
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class AlternateTitle : BaseResult, IComparable<AlternateTitle>
    {
        [JsonProperty("seasonNumber", Order = 2)]
        private int? _realSeasonNumber { get; set; }

        [JsonProperty("sceneSeasonNumber", Order = 1)]
        private int? _sceneSeasonNumber { get; set; }
        
        [JsonIgnore]
        public int? SeasonNumber
        {
            get
            {
                if (_sceneSeasonNumber.HasValue && _sceneSeasonNumber.Value > -1)
                    return _sceneSeasonNumber;

                else if (_realSeasonNumber.HasValue && _realSeasonNumber > -1)
                    return _realSeasonNumber;

                else
                    return null;
            }
        }

        [JsonProperty("title", Order = 3)]
        public string Title { get; private set; }

        public int CompareTo(AlternateTitle other)
        {
            int bySeason = this.CompareSeasonNumbers(other);

            return bySeason == 0 
                ? this.Title.CompareTo(other.Title)
                : bySeason;
        }
        private int CompareSeasonNumbers(AlternateTitle other)
        {
            return this.SeasonNumber.GetValueOrDefault().CompareTo(other.SeasonNumber.GetValueOrDefault());
        }
    }
}
