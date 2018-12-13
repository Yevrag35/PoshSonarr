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
        //internal protected void MatchResultsToProperties(IDictionary result)
        //{
        //    if (result != null)
        //    {
        //        string[] keys = result.Keys.Cast<string>().ToArray();
        //        Type t = GetType();
        //        FieldInfo[] pi = t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);

        //        for (int i1 = 0; i1 < keys.Length; i1++)
        //        {
        //            var key = keys[i1];
        //            for (int i2 = 0; i2 < pi.Length; i2++)
        //            {
        //                var p = pi[i2];

        //                if (p.Name.Equals("_" + key, StringComparison.OrdinalIgnoreCase))
        //                {
        //                    MethodInfo castMethod = t.GetMethod(
        //                        "Cast", BindingFlags.Instance | BindingFlags.NonPublic).MakeGenericMethod(p.FieldType);
        //                    object obj = castMethod.Invoke(this, new object[] { result[key] });
        //                    p.SetValue(this, obj);
        //                }
        //            }
        //        }
        //    }
        //}

        //internal protected DateTime? ToLocalTime(DateTime? dateTime) =>
        //    dateTime.HasValue ? (DateTime?)dateTime.Value.ToLocalTime() : null;

        //internal protected string ToLocalTime(string stringDate) =>
        //    stringDate != null ? DateTime.Parse(stringDate).ToLocalTime().ToString("h:mm tt") : null;

        //internal protected IList JArrayConvert(JArray jar)
        //{
        //    var list = new List<Dictionary<object, object>>();
        //    for (int i = 0; i < jar.Count; i++)
        //    {
        //        var job = (JObject)jar[i];
        //        var dict = JsonConvert.DeserializeObject<Dictionary<object, object>>(JsonConvert.SerializeObject(job));
        //        list.Add(dict);
        //    }
        //    return list;
        //}

        //internal protected T Cast<T>(dynamic o) => (T)o;

        public override string ToJson(Formatting asFormat) => JsonConvert.SerializeObject(this, Formatting.Indented);
    }
}
