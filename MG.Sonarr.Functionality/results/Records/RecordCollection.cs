using MG.Sonarr.Results.Collections;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Results
{
    public class RecordCollection : ResultListBase<LogRecord>
    {
        [JsonConstructor]
        internal RecordCollection(IEnumerable<LogRecord> records)
            : base(records.OrderBy(x => x.Time))
        {
        }
    }
}
