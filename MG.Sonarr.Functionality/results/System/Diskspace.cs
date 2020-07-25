using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;

namespace MG.Sonarr.Results
{
    /// <summary>
    /// The class that defines a response from the "/diskspace" endpoint.
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class Diskspace : BaseResult
    {
        /// <summary>
        /// The amount of freespace left in the disk calculated in bytes.
        /// </summary>
        [JsonProperty("freeSpace")]
        public long? FreeSpace { get; private set; }

        [JsonProperty("label")]
        public string Label { get; private set; }

        [JsonProperty("path")]
        public string Path { get; private set; }

        [JsonIgnore]
        public decimal? PercentUsed
        {
            get
            {
                if (this.FreeSpace.HasValue && this.TotalSpace.HasValue)
                {
                    decimal spaceUsed = this.TotalSpace.Value - FreeSpace.Value;
                    decimal percent = Math.Round(spaceUsed / this.TotalSpace.Value * 100.00M, 2);
                    return percent;
                }
                else
                    return null;
            }
        }
        /// <summary>
        /// The total space of the disk in bytes.
        /// </summary>
        [JsonProperty("totalSpace")]
        public long? TotalSpace { get; private set; }
    }
}
