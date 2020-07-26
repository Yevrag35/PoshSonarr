using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace MG.Sonarr.Results
{
    /// <summary>
    /// <para type="description">Represents a response object from "/filesystem/mediaFiles".</para>
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class MediaFile : BaseResult
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("relativePath")]
        public string RelativePath { get; set; }
    }
}
