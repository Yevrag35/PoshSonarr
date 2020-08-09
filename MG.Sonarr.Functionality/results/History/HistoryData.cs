using Newtonsoft.Json;
using System;

namespace MG.Sonarr.Functionality.Results
{
    [JsonObject(MemberSerialization.OptIn)]
    public class HistoryData
    {
        #region JSON PROPERTIES
        [JsonProperty("downloadClient", Order = 3)]
        public string DownloadClient { get; private set; }

        [JsonProperty("droppedPath", Order = 1)]
        public string DroppedPath { get; private set; }

        [JsonProperty("importedPath", Order = 2)]
        public string ImportedPath { get; private set; }

        #endregion
    }
}