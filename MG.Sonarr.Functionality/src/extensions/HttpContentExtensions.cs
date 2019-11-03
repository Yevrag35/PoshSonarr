using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace MG.Sonarr.Functionality.Extensions
{
    public static class HttpContentExtensions
    {
        public async static Task<T> ReadAsJsonAsync<T>(this HttpContent content)
        {
            var serializer = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Local,
                DefaultValueHandling = DefaultValueHandling.Populate,
                FloatParseHandling = FloatParseHandling.Decimal,
                Formatting = Formatting.Indented,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                NullValueHandling = NullValueHandling.Include
            };
            return await ReadAsJsonAsync<T>(content, serializer);
        }
        public async static Task<T> ReadAsJsonAsync<T>(this HttpContent content, JsonSerializerSettings serializerSettings)
        {
            string rawString = await content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(rawString, serializerSettings);
        }
    }
}
