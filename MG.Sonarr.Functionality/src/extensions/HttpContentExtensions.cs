using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace MG.Sonarr.Functionality.Extensions
{
    internal static class HttpContentExtensions
    {
        /// <summary>
        /// Reads the specified <see cref="HttpContent"/> and deserializes it into a <see cref="IJsonResult"/> object.
        /// </summary>
        /// <typeparam name="T">The <see cref="IJsonResult"/> type to deserialize the content as.</typeparam>
        /// <param name="content">The content from a <see cref="HttpResponseMessage.Content"/>.</param>
        /// <param name="suppressExceptions">Indicates whether or not to suppress any errors that may occur during deserialization.</param>
        /// <returns></returns>
        internal async static Task<T> ReadAsJsonAsync<T>(this HttpContent content, bool suppressExceptions)
        {
            string rawJson = await content.ReadAsStringAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(rawJson) && !suppressExceptions)
            {
                throw new ParseJsonResponseException(rawJson);
            }
            else
            {
                var settings = new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    NullValueHandling = NullValueHandling.Include,
                    DateFormatHandling = DateFormatHandling.IsoDateFormat,
                    DateParseHandling = DateParseHandling.DateTime,
                    DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                    FloatParseHandling = FloatParseHandling.Decimal
                };
                settings.Converters.Add(new StringEnumConverter(new CamelCaseNamingStrategy()));
                T result = JsonConvert.DeserializeObject<T>(rawJson, settings);
                return result;
            }
        }
    }
}