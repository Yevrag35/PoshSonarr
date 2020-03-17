using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Results
{
    public class RecordCollection : ResultCollectionBase<LogRecord>
    {
        public LogRecord this[int index] => base.InnerList[index];

        [JsonConstructor]
        internal RecordCollection(IEnumerable<LogRecord> records) : base(records) { }

        public IEnumerable<ExceptionLogRecord> GetExceptionRecords()
        {
            return base.InnerList.OfType<ExceptionLogRecord>();
        }
        public IEnumerable<LogRecord> GetNonExceptionRecords()
        {
            return base.InnerList.Where(x => !(x is ExceptionLogRecord));
        }
    }
}
