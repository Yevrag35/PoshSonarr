using System;
using System.Collections;
using System.Collections.Generic;

namespace Sonarr.Api.Results
{
    public class CommandResult : SonarrResult
    {
        //private string _name;
        //private DateTime? _startedOn;
        //private bool _sendUpdatesToClient;
        //private string _state;
        //private long _id;

        public string Name { get; internal set; }
        public DateTime? StartedOn { get; internal set; }
        public bool SendUpdatesToClient { get; internal set; }
        public string State { get; internal set; }
        public long Id { get; internal set; }

        //internal protected CommandResult(IDictionary dict) => MatchResultsToProperties(dict);

        //public static explicit operator CommandResult(Dictionary<object, object> dict) =>
        //    new CommandResult(dict);
        //public static explicit operator CommandResult(Dictionary<string, object> dict) =>
        //    new CommandResult(dict);
    }
}
