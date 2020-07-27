using MG.Sonarr.Functionality;
using MG.Sonarr.Functionality.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;

namespace MG.Sonarr.Results
{
    /// <summary>
    /// The class that defines a response from the "/series" endpoint.
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    [Serializable]
    public sealed class SeriesResult : SearchSeries, ISeries, ISupportsTagUpdate
    {
        private const string EP = "/series";

        /// <summary>
        /// An array of alternative titles the series is known as.
        /// </summary>
        [JsonProperty("alternateTitles")]
        public AlternateTitleCollection AlternateTitles { get; private set; }

        /// <summary>
        /// The unique ID of the series within Sonarr.
        /// </summary>
        [JsonProperty("id")]
        public int Id { get; private set; }

        [JsonIgnore]
        object ISupportsTagUpdate.Id => this.Id;

        /// <summary>
        /// Indicates whether the series is monitored for new episodes.
        /// </summary>
        [Obsolete]
        [JsonIgnore]
        public bool Monitored
        {
            get => this.IsMonitored;
            set
            {
                Console.WriteLine("The property \"Monitored\" is deprecated and will be removed from future releases.  Use \"IsMonitored\" instead.");
                this.IsMonitored = value;
            }
        }

        /// <summary>
        /// The containing folder's path for this series.
        /// </summary>
        [JsonProperty("path")]
        public string Path { get; set; }

        /// <summary>
        /// The ID of the <see cref="QualityProfile"/> that this series adheres to.
        /// </summary>
        [JsonProperty("qualityProfileId")]
        public int QualityProfileId { get; set; }

        /// <summary>
        /// The remote poster of the series.
        /// </summary>
        [JsonProperty("remotePoster")]
        public string RemotePoster { get; private set; }

        /// <summary>
        /// The size (in bytes) of all downloaded episode files from all seasons.
        /// </summary>
        [JsonProperty("sizeOnDisk")]
        [JsonConverter(typeof(SizeConverter))]
        public Size SizeOnDisk { get; private set; }
        //public long SizeOnDisk { get; private set; }

        /// <summary>
        /// A unique collection of tag ID's applied to the series.
        /// </summary>
        [JsonProperty("tags")]
        public HashSet<int> Tags { get; set; }

        /// <summary>
        /// Indicates whether the series keeps episode files organized by season number.
        /// </summary>
        [JsonProperty("seasonFolder")]
        public bool UsingSeasonFolders { get; set; }

        public bool HasAlternateTitles() => this.AlternateTitles != null && this.AlternateTitles.Count > 0;

        //public decimal ToSize(ByteUnit inUnit) => this.ToSize(inUnit, -1);
        //public decimal ToSize(ByteUnit inUnit, int numberOfDecimalPlaces)
        //{
        //    long? byteSize = this.Seasons?.GetSeasonFileSize();
        //    if (byteSize.GetValueOrDefault() > 0)
        //    {
        //        return this.ToDecimalSize(byteSize.Value, inUnit, numberOfDecimalPlaces);
        //    }
        //    else
        //        return 0M;
        //}

        //private decimal ToDecimalSize(long size, ByteUnit inUnit, int numberOfDecimalPlaces)
        //{
        //    switch (inUnit)
        //    {
        //        case ByteUnit.MB:
        //            return SizedResult.Calculate(this.SizeOnDisk, SizedResult.MB, numberOfDecimalPlaces);

        //        case ByteUnit.KB:
        //            return SizedResult.Calculate(this.SizeOnDisk, SizedResult.KB, numberOfDecimalPlaces);

        //        case ByteUnit.TB:
        //            return SizedResult.Calculate(this.SizeOnDisk, SizedResult.TB, numberOfDecimalPlaces);

        //        default:
        //            return SizedResult.Calculate(this.SizeOnDisk, SizedResult.GB, numberOfDecimalPlaces);
        //    }
        //}
    }
}
