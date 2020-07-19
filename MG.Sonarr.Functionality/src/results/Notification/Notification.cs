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
    public class Notification : MessageProvider, ISupportsTagUpdate
    {
        object ISupportsTagUpdate.Id => this.Name;

        #region JSON PROPERTIES

        [JsonProperty("id", Order = 1)]
        public virtual int Id { get; protected private set; }

        [JsonProperty("onGrab")]
        public bool OnGrab { get; set; }

        [JsonProperty("onDownload")]
        public bool OnDownload { get; set; }

        [JsonProperty("onUpgrade")]
        public bool OnUpgrade { get; set; }

        [JsonProperty("onRename")]
        public bool OnRename { get; set; }

        [JsonProperty("supportsOnGrab")]
        public bool SupportsOnGrab { get; protected private set; }

        [JsonProperty("supportsOnDownload")]
        public bool SupportsOnDownload { get; protected private set; }

        [JsonProperty("supportsOnUpgrade")]
        public bool SupportsOnUpgrade { get; protected private set; }

        [JsonProperty("supportsOnRename")]
        public bool SupportsOnRename { get; protected private set; }

        [JsonProperty("includeHealthWarnings")]
        public virtual bool IncludeHealthWarnings { get; protected private set; }

        [JsonProperty("tags", Order = 8)]
        public HashSet<int> Tags { get; set; }

        #endregion
        public string GetEndpoint() => ApiEndpoint.Notification;
        public void SetName(string name) => base.Name = name;
    }
}