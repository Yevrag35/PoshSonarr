using System;
using System.Collections;
using System.Collections.Generic;

namespace Sonarr.Api.Results
{
    public class CommandResult : SonarrResult
    {
        private string _name;
        private DateTime? _startedOn;
        private bool _sendUpdatesToClient;
        private string _state;
        private long _id;

        public string Name => _name;
        public DateTime? StartedOn => ToLocalTime(_startedOn);
        public bool SendUpdatesToClient => _sendUpdatesToClient;
        public string State => _state;
        public long Id => _id;

        internal protected CommandResult(IDictionary dict) => MatchResultsToProperties(dict);

        public static explicit operator CommandResult(Dictionary<object, object> dict) =>
            new CommandResult(dict);
        public static explicit operator CommandResult(Dictionary<string, object> dict) =>
            new CommandResult(dict);
    }
}
