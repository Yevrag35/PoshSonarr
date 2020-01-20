﻿using MG.Api.Json.Extensions;
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
    public class DelayProfile : BaseResult, ISupportsTagUpdate
    {
        private const int DEFAULT_ORDER = 2147483647;
        private const string EP = "/delayprofile";
        private const string ENDPOINT = EP + "/{0}";

        //[JsonProperty("tags", Order = 9)]
        //private HashSet<int> _appliedTagIds;

        #region JSON PROPERTIES
        [JsonProperty("id", Order = 8)]
        public virtual int Id { get; protected private set; }

        [JsonProperty("enableTorrent", Order = 2)]
        public bool EnableTorrent { get; set; }

        [JsonProperty("enableUsenet", Order = 1)]
        public bool EnableUsenet { get; set; }

        [JsonIgnore]
        object ISupportsTagUpdate.Identifier => this.Id;

        [JsonIgnore]
        public bool IsDefault => this.Order == DEFAULT_ORDER;

        [JsonProperty("order", Order = 6)]
        public int Order { get; protected private set; }

        [JsonProperty("preferredProtocol", Order = 3)]
        public DownloadProtocol PreferredProtocol { get; set; }

        [JsonProperty("tags", Order = 9)]
        public HashSet<int> Tags { get; set; }

        //[JsonIgnore]
        //public int[] Tags { get; protected private set; }

        [JsonProperty("torrentDelay", Order = 5)]
        public int TorrentDelay { get; set; }

        [JsonProperty("usenetDelay", Order = 4)]
        public int UsenetDelay { get; set; }

        #endregion

        //public void AddTags(IEnumerable<int> tagIds)
        //{
        //    foreach (int id in tagIds)
        //    {
        //        _appliedTagIds.Add(id);
        //    }
        //}
        
        public string GetEndpoint() => EP;

        //[OnDeserialized]
        //private void OnDeserialized(StreamingContext ctx)
        //{
        //    if (_appliedTagIds != null)
        //        this.Tags = _appliedTagIds.ToArray();
        //}

        //public void RemoveTags(IEnumerable<int> tagIds)
        //{
        //    foreach (int id in tagIds)
        //    {
        //        _appliedTagIds.Remove(id);
        //    }
        //}

        public void SetOrder(int newOrder)
        {
            if (this.IsDefault)
                throw new InvalidOperationException("Cannot set the order of the default delay profile.");

            this.Order = newOrder;
        }
    }
}