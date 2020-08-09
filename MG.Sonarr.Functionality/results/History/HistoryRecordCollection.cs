using MG.Sonarr.Functionality;
using MG.Sonarr.Functionality.Collections;
using MG.Sonarr.Results.Collections;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace MG.Sonarr.Results
{
    public class HistoryRecordCollection : ResultListBase<HistoryRecord>, IReadOnlyList<IRecord>
    {
        IRecord IReadOnlyList<IRecord>.this[int index] => base.InnerList[index];

        [JsonConstructor]
        internal HistoryRecordCollection(IEnumerable<HistoryRecord> records)
            : base(records) { }

        IEnumerator<IRecord> IEnumerable<IRecord>.GetEnumerator() => base.InnerList.GetEnumerator();
    }
}
