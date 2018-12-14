using Newtonsoft.Json.Linq;
using System;
using System.Collections;

namespace Sonarr.Api.Results
{
    public class CommandResult : SonarrResult
    {
        internal override string[] SkipThese => null;

        public string Name { get; internal set; }
        public DateTime? StartedOn { get; internal set; }
        public bool SendUpdatesToClient { get; internal set; }
        public string State { get; internal set; }
        public long Id { get; internal set; }

        public CommandResult() : base() { }

        public static explicit operator CommandResult(JObject job) =>
            FromJObject<CommandResult>(job);
    }
}
