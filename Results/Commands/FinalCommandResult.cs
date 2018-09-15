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
        private long _id;
        private string _message;
        private JObject _body;
        private string _priority;
        private string _status;
        private DateTime _queued;
        private DateTime _started;
        private DateTime _ended;
        private string _duration;
        private string _trigger;
        private bool _manual;
        private bool _updateScheduledTask;

        public long Id => _id;
        public string Message => _message;
        public string Priority => _priority;
        public string Status => _status;
        public DateTime Queued => _queued;
        public DateTime Started => _started;
        public DateTime Ended => _ended;
        public TimeSpan Duration => TimeSpan.Parse(_duration);
        public string Trigger => _trigger;
        public bool Manual => _manual;
        public bool SendUpdatesToClient => (bool)_body["sendUpdatesToClient"];
        public bool UpdateScheduledTask => _updateScheduledTask;

        private protected FinalCommandResult(IDictionary dict) => MatchResultsToProperties(dict);

        public static explicit operator FinalCommandResult(Dictionary<object, object> dict) =>
            new FinalCommandResult(dict);
        public static explicit operator FinalCommandResult(Dictionary<string, object> dict) =>
            new FinalCommandResult(dict);
    }
}
