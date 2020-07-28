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
        internal RecordCollection(IEnumerable<LogRecord> records) : base(records) { }

        public IList<ExceptionLogRecord> GetExceptionRecords()
        {
            return base.InnerList.OfType<ExceptionLogRecord>().ToList();
        }
        public IList<LogRecord> GetNonExceptionRecords()
        {
            return base.InnerList.Where(x => !(x is ExceptionLogRecord)).ToList();
        }
    }
}
