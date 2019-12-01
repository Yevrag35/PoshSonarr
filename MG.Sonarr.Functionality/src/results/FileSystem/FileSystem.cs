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
        private List<SonarrDirectory> _dirs;
        
        [JsonIgnore]
        public SonarrDirectory[] Directories { get; private set; }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx)
        {
            if (_dirs != null && _dirs.Count > 0)
            {
                if (_dirs.Count > 1)
                    _dirs.Sort();

                this.Directories = _dirs.ToArray();
            }
        }
    }

    /// <summary>
    /// <para type="description">Represents a repsonse object from a "/filesystem" request as an individual directory result.</para>
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class SonarrDirectory : BaseResult, IComparable<SonarrDirectory>
    {
        private const string PATH = "path";
        private static readonly char BACKSLASH = char.Parse(@"\");

        [JsonExtensionData]
        private IDictionary<string, JToken> _data { get; set; } = new Dictionary<string, JToken>(3);

        [JsonProperty("dateModified")]
        public DateTime LastModified { get; private set; }

        [JsonProperty("name")]
        public string Name { get; private set; }

        [JsonIgnore]
        public string Path { get; private set; }

        public int CompareTo(SonarrDirectory other) => this.Name.CompareTo(other.Name);

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
