using MG.Sonarr.Results;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace MG.Sonarr
{
    /// <summary>
    /// A static class providing conversion methods from <see cref="JToken"/> and <see cref="string"/> objects to objects that inherit from <see cref="ISonarrResult"/>.
    /// </summary>
    public static class SonarrHttp
    {
        [Obsolete]
        private const string API_PREFIX = "/api";

        internal static readonly JsonSerializerSettings Serializer = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateParseHandling = DateParseHandling.DateTime,
            DateTimeZoneHandling = DateTimeZoneHandling.Local,
            DefaultValueHandling = DefaultValueHandling.Populate,
            FloatParseHandling = FloatParseHandling.Decimal,
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize
        };

        [Obsolete]
        public static void AddSonarrApiKey(this HttpClient client, ApiKey apiKey)
        {
            KeyValuePair<string, string> kvp = apiKey.AsKeyValuePair();
            client.DefaultRequestHeaders.Add(kvp.Key, kvp.Value);
        }

        public static SeriesResult ConvertToSeriesResult(string jsonResult, bool fromSearch = false)
        {
            var jtok = JToken.Parse(jsonResult);
            return ConvertToSeriesResult(jtok, fromSearch);
        }
        public static SeriesResult ConvertToSeriesResult(JToken jtoken, bool fromSearch = false)
        {
            SeriesResult series = JsonConvert.DeserializeObject<SeriesResult>(JsonConvert.SerializeObject(jtoken, Serializer), Serializer);
            return series;
        }

        public static List<SeriesResult> ConvertToSeriesResults(string jsonResult, bool fromSearch = true)
        {
            var jar = JArray.Parse(jsonResult);
            var list = new List<SeriesResult>(jar.Count);
            for (int i = 0; i < jar.Count; i++)
            {
                JToken jtok = jar[i];
                list.Add(ConvertToSeriesResult(jtok, fromSearch));
            }

            return list;
        }

        public static T ConvertToSonarrResult<T>(string jsonResult) where T : ISonarrResult
        {
            return (T)JsonConvert.DeserializeObject(jsonResult, typeof(T), Serializer);
        }

        public static List<T> ConvertToSonarrResults<T>(string jsonResult, out bool isSingleObject) where T : ISonarrResult
        {
            var list = new List<T>();
            if (IsJsonArray(jsonResult))
            {
                list.AddRange(JsonConvert.DeserializeObject<List<T>>(jsonResult, Serializer));
            }
            else
            {
                list.Add(JsonConvert.DeserializeObject<T>(jsonResult, Serializer));
            }

            isSingleObject = list.Count == 1;
            return list;
        }

        public static bool IsJsonArray(string jsonStr)
        {
            var load = new JsonLoadSettings
            {
                CommentHandling = CommentHandling.Ignore,
                DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Replace,
                LineInfoHandling = LineInfoHandling.Ignore
            };
            var jtok = JToken.Parse(jsonStr, load);

            return jtok is JArray jar
                ? true
                : false;
        }

        #region OBSOLETE API METHODS

        [Obsolete]
        public static string SonarrGet(this HttpClient client, string sonarrEndpoint)
        {
            if (!Context.NoApiPrefix)
                sonarrEndpoint = API_PREFIX + sonarrEndpoint;

            Task<HttpResponseMessage> task = client.GetAsync(sonarrEndpoint, HttpCompletionOption.ResponseContentRead);
            task.Wait();
            string res = null;
            if (!task.IsFaulted && !task.IsCanceled)
            {
                using (HttpResponseMessage resp = task.Result.EnsureSuccessStatusCode())
                {
                    using (var content = resp.Content)
                    {
                        Task<string> strTask = content.ReadAsStringAsync();
                        strTask.Wait();
                        res = strTask.Result;
                    }
                }
            }

            return res;
        }

        [Obsolete]
        public static string SonarrPost(this HttpClient client, string endpoint, string jsonBody)
        {
            if (!Context.NoApiPrefix)
                endpoint = API_PREFIX + endpoint;

            StringContent sc = null;
            if (!string.IsNullOrEmpty(jsonBody))
                sc = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            Task<HttpResponseMessage> call = client.PostAsync(endpoint, sc);
            call.Wait();

            string res = null;
            if (!call.IsFaulted && !call.IsCanceled)
            {
                using (HttpResponseMessage resp = call.Result.EnsureSuccessStatusCode())
                {
                    using (var content = resp.Content)
                    {
                        Task<string> strTask = content.ReadAsStringAsync();
                        strTask.Wait();
                        res = strTask.Result;
                    }
                }
            }
            return res;
        }

        [Obsolete]
        public static void SonarrDelete(this HttpClient client, string endpoint)
        {
            if (!Context.NoApiPrefix)
                endpoint = API_PREFIX + endpoint;

            //endpoint = "/sonarr" + endpoint;

            Task<HttpResponseMessage> call = client.DeleteAsync(endpoint);
            call.Wait();

            using (var res = call.Result)
            {
                res.EnsureSuccessStatusCode();
            }
        }

        [Obsolete]
        public static string SonarrPut(this HttpClient client, string endpoint, string jsonBody)
        {
            if (!Context.NoApiPrefix)
                endpoint = API_PREFIX + endpoint;

            StringContent sc = null;
            if (!string.IsNullOrEmpty(jsonBody))
                sc = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            Task<HttpResponseMessage> call = client.PutAsync(endpoint, sc);
            call.Wait();

            string res = null;
            if (!call.IsFaulted && !call.IsCanceled)
            {
                using (HttpResponseMessage resp = call.Result.EnsureSuccessStatusCode())
                {
                    using (var content = resp.Content)
                    {
                        Task<string> strTask = content.ReadAsStringAsync();
                        strTask.Wait();
                        res = strTask.Result;
                    }
                }
            }
            return res;
        }

        #endregion
    }
}
