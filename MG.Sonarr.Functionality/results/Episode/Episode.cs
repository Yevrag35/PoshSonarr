using MG.Sonarr.Functionality;
using MG.Sonarr.Functionality.Url;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace MG.Sonarr.Results
{
    /// <summary>
    /// The class defining a response from the "/episode" endpoint.
    /// </summary>
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class EpisodeResult : BaseEpisodeResult, IComparable<EpisodeResult>, IEquatable<EpisodeResult>, IEpisode, IRecord
    {
        [JsonIgnore]
        DateTime? IEpisode.AirDate => base.AirDateUtc;

        [JsonProperty("episodeFile")]
        public EpisodeFile EpisodeFile { get; private set; }

        [JsonIgnore]
        int? IEpisode.EpisodeNumber => base.EpisodeNumber;

        [JsonIgnore]
        long IRecord.Id => base.EpisodeId;

        [JsonIgnore]
        public bool HasAired => this.AirDateUtc.HasValue && DateTime.UtcNow.CompareTo(this.AirDateUtc.Value) >= 0 ? true : false;

        [JsonProperty("seasonNumber")]
        public int? SeasonNumber { get; private set; }

        [JsonProperty("series")]
        public SeriesResult Series { get; private set; }

        public int CompareTo(EpisodeResult other)
        {
            if (other == null)
                return 1;

            int compare = this.SeasonNumber.GetValueOrDefault().CompareTo(other.SeasonNumber.GetValueOrDefault());
            if (compare == 0)
            {
                compare = this.EpisodeNumber.CompareTo(other.EpisodeNumber);
            }
            return compare;
        }
        int IComparable<IRecord>.CompareTo(IRecord other) => base.EpisodeId.CompareTo(other.Id);
        public bool Equals(EpisodeResult other) => this.EpisodeId.Equals(other.EpisodeId);
        bool IEquatable<IRecord>.Equals(IRecord other) => this.EpisodeId.Equals(other.Id);
        public static IComparer<EpisodeResult> GetComparer() => new EpisodeComparer();

        public static IUrlParameter GetSeriesIdParameter(int seriesId) => UrlParameter.Create("seriesId", seriesId);

        private class EpisodeComparer : IComparer<EpisodeResult>
        {
            public int Compare(EpisodeResult x, EpisodeResult y)
            {
                if (x != null && y != null)
                {
                    int compare = x.SeasonNumber.GetValueOrDefault().CompareTo(y.SeasonNumber.GetValueOrDefault());
                    if (compare == 0)
                    {
                        compare = x.EpisodeNumber.CompareTo(y.EpisodeNumber);
                    }
                    return compare;
                }
                else if (x != null && y == null)
                    return -1;

                else if (x == null && y != null)
                    return 1;

                else
                    return 0;
            }
        }
    }
}
