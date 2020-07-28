using MG.Sonarr.Functionality;
using MG.Sonarr.Functionality.Converters;
using MG.Sonarr.Functionality.Extensions;
using MG.Sonarr.Functionality.Strings;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MG.Sonarr.Results
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class Restriction : BaseResult, ISupportsTagUpdate
    {
        [JsonProperty("ignored", Order = 2)]
        [JsonConverter(typeof(TermStoreConverter))]
        public JsonStringSet Ignored { get; private set; }

        [JsonProperty("required", Order = 1)]
        [JsonConverter(typeof(TermStoreConverter))]
        public JsonStringSet Required { get; private set; }

        [JsonProperty("id", Order = 4)]
        public int Id { get; private set; }
        object ISupportsTagUpdate.Id => this.Id;

        [JsonProperty("tags", Order = 3)]
        public HashSet<int> Tags { get; set; } = new HashSet<int>();

        [JsonConstructor]
        public Restriction()
        {
            this.Ignored = new JsonStringSet();
            this.Required = new JsonStringSet();
        }

        public string GetEndpoint() => ApiEndpoints.Restriction;

        public override string ToJson()
        {
            if (this.Ignored.Count > 0)
                this.Ignored.RemoveWhere(x => string.IsNullOrEmpty(x));

            if (this.Required.Count > 0)
                this.Required.RemoveWhere(x => string.IsNullOrEmpty(x));

            return base.ToJson();
        }
    }
}
