using MG.Sonarr.Results.Collections;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace MG.Sonarr.Functionality.Results
{
    [JsonObject(MemberSerialization.OptIn)]
    public class HistoryRecordPage : RecordPageBase, IRecordPage
    {
        [JsonProperty("records", Order = 6)]
        public ResultListBase<HistoryRecord> Records { get; private set; }
        IReadOnlyList<IRecord> IRecordPage.Records => this.Records;
    }
}