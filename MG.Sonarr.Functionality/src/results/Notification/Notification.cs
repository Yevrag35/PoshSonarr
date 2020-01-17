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
    public class Notification : Provider, ISupportsTagUpdate
    {
        private const string EP = "/notification";

        [JsonProperty("tags", Order = 8)]
        private HashSet<int> _appliedTagIds;

        object ISupportsTagUpdate.Identifier => this.Name;

        #region JSON PROPERTIES
        [JsonProperty("link")]
        public string Link { get; protected private set; }

        [JsonProperty("id", Order = 1)]
        public virtual int NotificationId { get; protected private set; }

        [JsonProperty("onGrab")]
        public bool OnGrab { get; set; }

        [JsonProperty("onDownload")]
        public bool OnDownload { get; set; }

        [JsonProperty("onUpgrade")]
        public bool OnUpgrade { get; set; }

        [JsonProperty("onRename")]
        public bool OnRename { get; set; }

        [JsonProperty("onHealthIssue")]
        public virtual bool OnHealthIssue { get; set; }

        [JsonProperty("supportsOnGrab")]
        public bool SupportsOnGrab { get; protected private set; }

        [JsonProperty("supportsOnDownload")]
        public bool SupportsOnDownload { get; protected private set; }

        [JsonProperty("supportsOnUpgrade")]
        public bool SupportsOnUpgrade { get; protected private set; }

        [JsonProperty("supportsOnRename")]
        public bool SupportsOnRename { get; protected private set; }

        [JsonProperty("supportsOnHealthIssue")]
        public virtual bool SupportsOnHealthIssue { get; protected private set; }

        [JsonProperty("includeHealthWarnings")]
        public virtual bool IncludeHealthWarnings { get; protected private set; }

        [JsonIgnore]
        public override int[] Tags { get; protected set; }

        [JsonProperty("testCommand")]
        public virtual  string TestCommand { get; protected private set; }

        #endregion

        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx)
        {
            if (_appliedTagIds != null)
                this.Tags = _appliedTagIds.ToArray();
        }

        public void AddTags(params int[] tagIds)
        {
            if (tagIds != null)
            {
                foreach (int id in tagIds)
                {
                    if (!_appliedTagIds.Contains(id))
                        _appliedTagIds.Add(id);
                }
            }
        }
        public sealed override string GetEndpoint() => EP;
        public void RemoveTags(params int[] tagIds)
        {
            if (tagIds != null)
            {
                foreach (int id in tagIds)
                {
                    if (_appliedTagIds.Contains(id))
                        _appliedTagIds.Remove(id);
                }
            }
        }
    }
}