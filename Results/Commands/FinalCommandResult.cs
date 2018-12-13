using MG.Api;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Sonarr.Api.Results
{
    public class FinalCommandResult : SonarrResult
    {
        //private long _id;
        //private string _message;
        //private JObject _body;
        //private string _priority;
        //private string _status;
        //private DateTime? _queued;
        //private DateTime? _started;
        //private DateTime? _ended;
        //private string _duration;
        //private string _trigger;
        //private bool _manual;
        //private bool _updateScheduledTask;

        public long Id { get; internal set; }
        public string Message { get; internal set; }
        public string Priority { get; internal set; }
        public string Status { get; internal set; }
        public DateTime? Queued { get; internal set; }
        public DateTime? Started { get; internal set; }
        public DateTime? Ended { get; internal set; }
        public TimeSpan? Duration { get; internal set; }
        public string Trigger { get; internal set; }
        public bool Manual { get; internal set; }
        public bool SendUpdatesToClient { get; internal set; }
        public bool UpdateScheduledTask { get; internal set; }

        //private protected FinalCommandResult(IDictionary dict) => MatchResultsToProperties(dict);

        //public static explicit operator FinalCommandResult(Dictionary<object, object> dict) =>
        //    new FinalCommandResult(dict);
        //public static explicit operator FinalCommandResult(Dictionary<string, object> dict) =>
        //    new FinalCommandResult(dict);
    }
}
