using MG.Sonarr.Results;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MG.Sonarr
{
    /// <summary>
    /// A static class providing conversion methods from <see cref="JToken"/> and <see cref="string"/> objects to objects that inherit from <see cref="IJsonResult"/>.
    /// </summary>
    public static class SonarrHttp
    {
        internal static readonly JsonSerializerSettings Serializer = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateParseHandling = DateParseHandling.DateTime,
            DateTimeZoneHandling = DateTimeZoneHandling.Local,
            DefaultValueHandling = DefaultValueHandling.Populate,
            FloatParseHandling = FloatParseHandling.Decimal,
            Formatting = Formatting.Indented,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize
        };

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

        public static T ConvertToSonarrResult<T>(string jsonResult) where T : IJsonResult
        {
            return JsonConvert.DeserializeObject<T>(jsonResult, Serializer);
        }

        public static List<T> ConvertToSonarrResults<T>(string jsonResult) where T : IJsonResult
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

            return list;
        }

        internal static List<T> ConvertToSonarrResults<T>(string jsonResult, out bool isSingleObject) where T : IJsonResult
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

#if DEBUG
        [Obsolete]
        public static object ConvertToSonarrResult(string jsonResult, Type convertTo)
        {
            var currentMethod = MethodBase.GetCurrentMethod();
            if (!convertTo.GetInterfaces().Contains(typeof(IJsonResult)))
                throw new InvalidCastException("The parameter for \"convertTo\" does not inherit the interface \"ISonarrResult\".");

            MethodInfo otherMeth = typeof(SonarrHttp).GetMethod(currentMethod.Name, new Type[1] { typeof(string) });
            MethodInfo genMeth = otherMeth.MakeGenericMethod(convertTo);
            return genMeth.Invoke(null, new object[1] { jsonResult });
        }

        [Obsolete]
        public static List<object> ConvertToSonarrResults(string jsonResult, Type convertTo)
        {
            var currentMethod = MethodBase.GetCurrentMethod();
            if (!convertTo.GetInterfaces().Contains(typeof(IJsonResult)))
                throw new InvalidCastException("The parameter for \"convertTo\" does not inherit the interface \"ISonarrResult\".");

            MethodInfo otherMeth = typeof(SonarrHttp).GetMethod(currentMethod.Name, new Type[1] { typeof(string) });
            MethodInfo genMeth = otherMeth.MakeGenericMethod(convertTo);
            object result = genMeth.Invoke(null, new object[1] { jsonResult });
            if (result is IEnumerable ienum)
            {
                var list = ienum.Cast<object>().ToList();
                return list;
            }
            else
                return null;
        }
#endif

        public static bool IsJsonArray(string jsonStr)
        {
            var load = new JsonLoadSettings
            {
                CommentHandling = CommentHandling.Ignore,
                DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Replace,
                LineInfoHandling = LineInfoHandling.Ignore
            };
            var jtok = JToken.Parse(jsonStr, load);

            return jtok is JArray
                ? true
                : false;
        }
    }
}
