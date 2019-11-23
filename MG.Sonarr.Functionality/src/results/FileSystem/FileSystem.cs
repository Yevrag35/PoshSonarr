using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace MG.Sonarr.Results
{
    /// <summary>
    /// <para type="description">Represents a response object from "/filesystem".</para>
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class FileSystem : BaseResult
    {
        [JsonProperty("directories")]
        public List<SonarrDirectory> Directories { get; set; }
    }

    /// <summary>
    /// <para type="description">Represents a repsonse object from a "/filesystem" request as an individual directory result.</para>
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class SonarrDirectory : BaseResult
    {
        private const string PATH = "path";
        private static readonly char BACKSLASH = char.Parse(@"\");

        [JsonExtensionData]
        private IDictionary<string, JToken> _data;

        [JsonProperty("dateModified")]
        public DateTime LastModified { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonIgnore]
        public string Path { get; private set; }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (_data.ContainsKey(PATH))
            {
                JToken pathTok = _data[PATH];
                if (pathTok != null)
                {
                    this.Path = pathTok.ToObject<string>().TrimEnd(BACKSLASH);
                }
            }
        }
    }
}
