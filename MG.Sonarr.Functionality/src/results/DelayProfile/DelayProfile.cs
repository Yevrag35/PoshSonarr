using MG.Api.Json.Extensions;
using MG.Api.Rest;
using MG.Api.Rest.Generic;
using MG.Sonarr.Functionality;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace MG.Sonarr.Results
{
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class DelayProfile : BaseResult, IComparable<DelayProfile>, ISupportsTagUpdate
    {
        private const int DEFAULT_ORDER = 2147483647;
        private const string EP = "/delayprofile";
        private const string ENDPOINT = EP + "/{0}";

        #region JSON PROPERTIES
        [JsonProperty("id", Order = 8)]
        public virtual int Id { get; protected private set; }

        [JsonProperty("enableTorrent", Order = 2)]
        public bool EnableTorrent { get; set; }

        [JsonProperty("enableUsenet", Order = 1)]
        public bool EnableUsenet { get; set; }

        [JsonIgnore]
        object ISupportsTagUpdate.Id => this.Id;

        [JsonIgnore]
        public bool IsDefault => this.Order == DEFAULT_ORDER;

        [JsonProperty("order", Order = 6)]
        public int Order { get; protected private set; }

        [JsonProperty("preferredProtocol", Order = 3)]
        public DownloadProtocol PreferredProtocol { get; set; }

        [JsonProperty("tags", Order = 9)]
        public HashSet<int> Tags { get; set; }

        [JsonProperty("torrentDelay", Order = 5)]
        public int TorrentDelay { get; set; }

        [JsonProperty("usenetDelay", Order = 4)]
        public int UsenetDelay { get; set; }

        #endregion

        public int CompareTo(DelayProfile other) => this.Order.CompareTo(other.Order);
        public string GetEndpoint() => EP;
        public void SetOrder(int newOrder)
        {
            if (this.IsDefault)
                throw new InvalidOperationException("Cannot set the order of the default delay profile.");

            this.Order = newOrder;
        }
    }
}