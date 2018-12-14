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
        internal override string[] SkipThese => null;

        public long Id { get; internal set; }
        public string Message { get; internal set; }
        public string Priority { get; internal set; }
        public string Status { get; internal set; }
        public DateTime? Queued { get; internal set; }
        public DateTime? Started { get; internal set; }
        public DateTime? Ended { get; internal set; }
        public string Duration { get; internal set; }
        public string Trigger { get; internal set; }
        public bool Manual { get; internal set; }
        public bool SendUpdatesToClient { get; internal set; }
        public bool UpdateScheduledTask { get; internal set; }

        public FinalCommandResult() : base() { }

        public static explicit operator FinalCommandResult(JObject job) =>
            FromJObject<FinalCommandResult>(job);
    }
}
