using MG.Api;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Sonarr.Api.Results
{
    public abstract class SonarrResult : DynamicResult
    {
        internal abstract string[] SkipThese { get; }

        internal SonarrResult(IDictionary dict)
            : base() => MatchToInternalSet(dict, SkipThese);

        public SonarrResult() { }

        public static T FromJObject<T>(JObject job) where T : SonarrResult
        {
            if (job == null)
                return null;

            else
            {
                IDictionary dict = JsonConvert.DeserializeObject<Dictionary<object, object>>(JsonConvert.SerializeObject(job));
                var newOfThis = Activator.CreateInstance<T>();
                newOfThis.MatchToInternalSet(dict, newOfThis.SkipThese);
                return newOfThis;
            }
        }

        public override string ToJson(Formatting asFormat) => JsonConvert.SerializeObject(this, Formatting.Indented);
    }
}
