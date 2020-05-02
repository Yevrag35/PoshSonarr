using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace MG.Sonarr.Results
{
    /// <summary>
    /// The class defining a response from the "/episode" endpoint.
    /// </summary>
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class EpisodeResult : BaseEpisodeResult, IComparable<EpisodeResult>, IEquatable<EpisodeResult>
    {
        [JsonProperty("episodeFile")]
        public EpisodeFile EpisodeFile { get; private set; }

        [JsonIgnore]
        public bool HasAired => this.AirDateUtc.HasValue && DateTime.UtcNow.CompareTo(this.AirDateUtc.Value) >= 0 ? true : false;

        [JsonProperty("seasonNumber")]
        public int SeasonNumber { get; private set; }

        [JsonProperty("series")]
        public SeriesResult Series { get; private set; }

        public int CompareTo(EpisodeResult other)
        {
            return this.AbsoluteEpisodeNumber.GetValueOrDefault().CompareTo(other.AbsoluteEpisodeNumber.GetValueOrDefault());
        }
        public bool Equals(EpisodeResult other) => this.EpisodeId.Equals(other.EpisodeId);
    }
}
