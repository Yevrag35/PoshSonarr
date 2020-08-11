using MG.Sonarr.Functionality;
using MG.Sonarr.Results.Collections;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace MG.Sonarr.Results
{
    [JsonObject(MemberSerialization.OptIn)]
    public class WantedMissingPage : RecordPageBase, IRecordPage
    {
        #region JSON PROPERTIES
        [JsonProperty("records", Order = 6)]
        public EpisodeCollection Records { get; private set; }
        IReadOnlyList<IRecord> IRecordPage.Records => this.Records;

        #endregion
    }
}