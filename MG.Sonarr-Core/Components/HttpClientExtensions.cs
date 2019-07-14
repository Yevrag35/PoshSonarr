﻿using MG.Sonarr.Results;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace MG.Sonarr
{
    public static class SonarrHttpClient
    {
        private const string API_PREFIX = "/api";
        private const string CONTENT_TYPE = "application/json";

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

        public static void AddSonarrApiKey(this HttpClient client, ApiKey apiKey)
        {
            KeyValuePair<string, string> kvp = apiKey.AsKeyValuePair();
            client.DefaultRequestHeaders.Add(kvp.Key, kvp.Value);
        }

        public static SeriesResult ConvertToSeriesResult(string jsonResult)
        {
            var jtok = JToken.Parse(jsonResult);
            return ConvertToSeriesResult(jtok);
        }
        public static SeriesResult ConvertToSeriesResult(JToken jtoken)
        {
            SeriesResult series = JsonConvert.DeserializeObject<SeriesResult>(JsonConvert.SerializeObject(jtoken, Serializer), Serializer);

            var seasonArray = jtoken["seasons"] as JArray;
            for (int s = 0; s < seasonArray.Count; s++)
            {
                series.AddSeason(seasonArray[s]);
            }
            return series;
        }

        public static List<SeriesResult> ConvertToSeriesResults(string jsonResult)
        {
            var jar = JArray.Parse(jsonResult);
            var list = new List<SeriesResult>(jar.Count);
            for (int i = 0; i < jar.Count; i++)
            {
                JToken jtok = jar[i];
                list.Add(ConvertToSeriesResult(jtok));
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

        public static string SonarrGet(this HttpClient client, string sonarrEndpoint)
        {
            if (!Context.NoApiPrefix)
                sonarrEndpoint = API_PREFIX + sonarrEndpoint;

            Task<HttpResponseMessage> task = client.GetAsync(sonarrEndpoint, HttpCompletionOption.ResponseContentRead);
            task.Wait();
            string res = null;
            if (!task.IsFaulted && !task.IsCanceled)
            {
                using (HttpResponseMessage resp = task.Result)
                {
                    if (resp.IsSuccessStatusCode)
                    {
                        using (HttpContent content = resp.Content)
                        {
                            Task<string> strTask = content.ReadAsStringAsync();
                            strTask.Wait();
                            res = strTask.Result;
                        }
                    }
                }
            }

            return res;
        }

        public static string SonarrPost(this HttpClient client, string endpoint, string jsonBody)
        {
            return null;
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
    }
}
