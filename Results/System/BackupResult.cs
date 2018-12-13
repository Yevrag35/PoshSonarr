using System;
using System.Collections;
using System.Collections.Generic;

namespace Sonarr.Api.Results
{
    public class BackupResult : SonarrResult
    {
        //private string _name;
        //private string _path;
        //private string _type;
        //private DateTime? _time;
        //private long _id;

        public string Name { get; internal set; }
        public string Path { get; internal set; }
        public string Type { get; internal set; }
        public DateTime? Time { get; internal set; }
        public long Id { get; internal set; }

        //private protected BackupResult(IDictionary dict) => MatchResultsToProperties(dict);

        //public static explicit operator BackupResult(Dictionary<object, object> dict) =>
        //    new BackupResult(dict);

        //public static explicit operator BackupResult(Dictionary<string, object> dict) =>
        //    new BackupResult(dict);
    }
}
