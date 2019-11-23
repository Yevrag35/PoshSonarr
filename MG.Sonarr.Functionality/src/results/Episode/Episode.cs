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
        private EpisodeFile _epf;
        public EpisodeFile EpisodeFile => _epf;

        public bool HasAired => this.AirDateUtc.HasValue && DateTime.UtcNow.CompareTo(this.AirDateUtc.Value) >= 0 ? true : false;

        [JsonProperty("seasonNumber")]
        private int _sn;
        public int SeasonNumber => _sn;

        [JsonProperty("series")]
        private SeriesResult _sr;
        public SeriesResult Series => _sr;

        public int CompareTo(EpisodeResult other)
        {
            if (this.AbsoluteEpisodeNumber.HasValue && !other.AbsoluteEpisodeNumber.HasValue)
                return 1;

            else if (!this.AbsoluteEpisodeNumber.HasValue && other.AbsoluteEpisodeNumber.HasValue)
                return -1;

            else if (!this.AbsoluteEpisodeNumber.HasValue && !other.AbsoluteEpisodeNumber.HasValue)
                return 0;

            else
            {
                return this.AbsoluteEpisodeNumber.Value.CompareTo(other.AbsoluteEpisodeNumber.Value);
            }
        }

        public bool Equals(EpisodeResult other) => this.EpisodeId.Equals(other.EpisodeId);
    }
}
