using MG.Sonarr.Functionality;
using Newtonsoft.Json;
using System;

namespace MG.Sonarr.Results
{
    /// <summary>
    /// The class that defines a response from the "/MediaCover" endpoint.
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class SeriesImage : BaseResult, IComparable<SeriesImage>
    {
        [JsonProperty("coverType")]
        public CoverType CoverType { get; private set; }

        [JsonProperty("url")]
        public Uri Url { get; private set; }

        public int CompareTo(SeriesImage other)
        {
            int cct = this.CompareCoverType(other.CoverType);
            if (cct == 0)
                return this.Url.ToString().CompareTo(other.Url.ToString());

            else
                return cct;
        }
        private int CompareCoverType(CoverType type) => this.CoverType.ToString().CompareTo(type.ToString());
    }
}
