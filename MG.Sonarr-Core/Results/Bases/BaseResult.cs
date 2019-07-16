using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Results
{
    public abstract class BaseResult : ISonarrResult
    {
        public virtual string ToJson()
        {
            return JsonConvert.SerializeObject(this, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                DefaultValueHandling = DefaultValueHandling.Populate,
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Include,
                MissingMemberHandling = MissingMemberHandling.Error
            });
        }

        public virtual string ToJson(IDictionary parameters)
        {
            var camel = new CamelCasePropertyNamesContractResolver();
            var cSerialize = new JsonSerializer
            {
                ContractResolver = camel
            };

            var serializer = new JsonSerializerSettings
            {
                ContractResolver = camel,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                DefaultValueHandling = DefaultValueHandling.Populate,
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Include,
                MissingMemberHandling = MissingMemberHandling.Error
            };

            var job = JObject.FromObject(this, cSerialize);

            string[] keys = parameters.Keys.Cast<string>().ToArray();
            for (int i = 0; i < keys.Length; i++)
            {
                string key = keys[i];
                job.Add(key, JToken.FromObject(parameters[key], cSerialize));
            }

            return JsonConvert.SerializeObject(job, serializer);
        }
    }
}
