using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace MG.Sonarr.Results
{
    /// <summary>
    /// The base class for all PoshSonarr RESTful API responses.
    /// </summary>
    public abstract class BaseResult : IJsonResult
    {
        /// <summary>
        /// Converts the inheriting class to a JSON-formatted string using programmed serializers.
        /// </summary>
        public virtual string ToJson()
        {
            var converter = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                DefaultValueHandling = DefaultValueHandling.Populate,
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Include,
                MissingMemberHandling = MissingMemberHandling.Error
            };
            converter.Converters.Add(new StringEnumConverter(new CamelCaseNamingStrategy()));
            return JsonConvert.SerializeObject(this, converter);
        }

        /// <summary>
        /// Converts the inheriting class to a JSON-formatted string using programmed serializers adding in the contents from the specified generic dictionary.
        /// </summary>
        /// <param name="parameters">The dictionary that will have it contents added to resulting JSON string.</param>
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
            serializer.Converters.Add(new StringEnumConverter(new CamelCaseNamingStrategy()));

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
