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
        [JsonProperty("sceneSeasonNumber")]
        public int? SceneSeasonNumber { get; private set; }

        [JsonProperty("seasonNumber")]
        public int? SeasonNumber { get; private set; }

        [JsonProperty("title")]
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
