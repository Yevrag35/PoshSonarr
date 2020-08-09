using MG.Sonarr.Functionality;
using MG.Sonarr.Results.Collections;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace MG.Sonarr.Results
{
    [JsonObject(MemberSerialization.OptIn)]
    public class HistoryRecordPage : RecordPageBase, IRecordPage
    {
        [JsonProperty("records", Order = 6)]
        public HistoryRecordCollection Records { get; private set; }
        IReadOnlyList<IRecord> IRecordPage.Records => this.Records;
    }
}