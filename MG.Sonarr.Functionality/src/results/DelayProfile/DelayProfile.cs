using MG.Api.Json.Extensions;
using MG.Api.Rest;
using MG.Api.Rest.Generic;
using MG.Sonarr.Functionality;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MG.Sonarr.Results
{
    [JsonObject(MemberSerialization.OptIn)]
    public class DelayProfile : BaseResult
    {
        private const int DEFAULT_ORDER = 2147483647;
        private const string ENDPOINT = "/tag/{0}";

        #region JSON PROPERTIES
        [JsonProperty("id", Order = 8)]
        public int DelayProfileId { get; private set; }

        [JsonProperty("enableTorrent", Order = 2)]
        public bool EnableTorrent { get; set; }

        [JsonProperty("enableUsenet", Order = 1)]
        public bool EnableUsenet { get; set; }

        [JsonProperty("order", Order = 6)]
        public int Order { get; private set; }

        [JsonProperty("preferredProtocol", Order = 3)]
        public DownloadProtocol PreferredProtocol { get; set; }

        //public Tag[] Tags { get; private set; }
        [JsonProperty("tags", Order = 7)]
        public int[] Tags { get; private set; }

        [JsonProperty("torrentDelay", Order = 5)]
        public int TorrentDelay { get; set; }

        [JsonProperty("usenetDelay", Order = 4)]
        public int UsenetDelay { get; set; }

        #endregion

        public void SetOrder(int newOrder)
        {
            if (DEFAULT_ORDER == this.Order)
                throw new InvalidOperationException("Cannot set the order of the default delay profile.");

            this.Order = newOrder;
        }
    }
}