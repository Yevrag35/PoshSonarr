using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Sonarr.Api
{
    public class RequestParameters : Dictionary<object, object>
    {
        //private readonly string _val;
        public string JsonContent => Count > 0 ? JsonConvert.SerializeObject(this, Formatting.Indented) : null;

        public RequestParameters() { }
        public RequestParameters(IDictionary body)
        {
            var keys = body.Keys.Cast<object>().ToArray();
            for (int i = 0; i < keys.Length; i++)
            {
                object k = keys.GetValue(i);
                this.Add(k, body[k]);
            }
        }
        public RequestParameters(JObject body)
        {
            var dict = body.ToObject<Dictionary<object, object>>();
            var keys = dict.Keys.Cast<object>().ToArray();
            for (int i = 0; i < keys.Length; i++)
            {
                var k = keys[i];
                this.Add(k, dict[k]);
            }
        }

        public static implicit operator RequestParameters(Hashtable hashtable) => new RequestParameters(hashtable);
        public static implicit operator RequestParameters(JObject job) => new RequestParameters(job);
        public static implicit operator string(RequestParameters jsonBody) => jsonBody.JsonContent;

        public override string ToString() => this.JsonContent;
    }
}
