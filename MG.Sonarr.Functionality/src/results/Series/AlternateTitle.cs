using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace MG.Sonarr.Results
{ 
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class AlternateTitle : BaseResult, IComparable<AlternateTitle>
    {
        [JsonProperty("sceneSeasonNumber")]
        public int? SceneSeasonNumber { get; set; }

        [JsonProperty("seasonNumber")]
        public int? SeasonNumber { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        public int CompareTo(AlternateTitle other)
        {
            int bySeason = this.CompareSeasonNumbers(other);

            return bySeason == 0 
                ? this.Title.CompareTo(other.Title)
                : bySeason;
        }
        private int CompareSeasonNumbers(AlternateTitle other)
        {
            if (this.SeasonNumber.HasValue && other.SeasonNumber.HasValue)
            {
                return this.SeasonNumber.Value.CompareTo(other.SeasonNumber.Value);
            }
            else if (this.SeasonNumber.HasValue && !other.SeasonNumber.HasValue)
            {
                return 1;
            }
            else if (!this.SeasonNumber.HasValue && other.SeasonNumber.HasValue)
            {
                return -1;
            }
            else
            {
                return 0;
            }
        }
    }
}
