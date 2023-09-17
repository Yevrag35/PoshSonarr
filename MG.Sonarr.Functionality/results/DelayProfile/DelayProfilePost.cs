using MG.Sonarr.Functionality;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Results
{
    [JsonObject(MemberSerialization.OptIn)]
    public class DelayProfilePost : DelayProfile
    {
        #region JSON PROPERTIES
        [JsonIgnore]
        public sealed override int Id { get; protected private set; }

        public static DelayProfilePost NewPost(bool enableTor, bool enableUse, int order, string protocol, 
            int torrentDelay, int usenetDelay, IEnumerable<int> tags)
        {
            if (order <= 0)
                throw new ArgumentException("The order of the Delay Profile cannot be lower than 1.");

            var dp = new DelayProfilePost
            {
                EnableTorrent = enableTor,
                EnableUsenet = enableUse,
                PreferredProtocol = protocol,
                Tags = new HashSet<int>(tags),
                TorrentDelay = torrentDelay,
                UsenetDelay = usenetDelay
            };
            dp.SetOrder(order);
            return dp;
        }

        public static DelayProfilePost NewPost(bool enableTor, bool enableUse, int order, string protocol,
            int torrentDelay, int usenetDelay, HashSet<int> tags)
        {
            if (order <= 0)
                throw new ArgumentException("The order of the Delay Profile cannot be lower than 1.");

            var dp = new DelayProfilePost
            {
                EnableTorrent = enableTor,
                EnableUsenet = enableUse,
                PreferredProtocol = protocol,
                Tags = tags,
                TorrentDelay = torrentDelay,
                UsenetDelay = usenetDelay
            };
            dp.SetOrder(order);
            return dp;
        }

        #endregion
    }
}