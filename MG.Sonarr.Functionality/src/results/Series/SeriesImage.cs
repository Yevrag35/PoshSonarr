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
        /// <summary>
        /// The type of image used for the cover.
        /// </summary>
        [JsonProperty("coverType")]
        public CoverType CoverType { get; private set; }

        /// <summary>
        /// The relative Uri of the <see cref="SeriesImage"/>.
        /// </summary>
        [JsonProperty("url")]
        public Uri Url { get; private set; }

        /// <summary>
        /// The default comparison method.  If both <see cref="MG.Sonarr.Functionality.CoverType"/>
        /// are the same, then the <see cref="SeriesImage.Url"/> is compared.
        /// </summary>
        /// <param name="other">The other <see cref="SeriesImage"/> to compare this instance against.</param>
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
