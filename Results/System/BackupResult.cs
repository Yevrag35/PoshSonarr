using System;
using System.Collections;
using System.Collections.Generic;

namespace Sonarr.Api.Results
{
    public class BackupResult : SonarrResult
    {
        private string _name;
        private string _path;
        private string _type;
        private DateTime _time;
        private long _id;

        public string Name => _name;
        public string Path => _path;
        public string Type => _type;
        public DateTime Time => _time.ToLocalTime();
        public long Id => _id;

        private protected BackupResult(IDictionary dict) => MatchResultsToProperties(dict);

        public static explicit operator BackupResult(Dictionary<object, object> dict) =>
            new BackupResult(dict);

        public static explicit operator BackupResult(Dictionary<string, object> dict) =>
            new BackupResult(dict);
    }
}
