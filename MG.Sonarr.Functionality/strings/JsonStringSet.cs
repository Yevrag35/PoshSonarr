using MG.Api.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;

namespace MG.Sonarr.Functionality.Strings
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class JsonStringSet : HashSet<string>, IJsonObject
    {
        private const string COMMA = ",";
        private static readonly string[] COMMA_ARR = new string[1] { COMMA };

        public JsonStringSet() : base(SonarrFactory.NewIgnoreCase()) { }
        public JsonStringSet(string singleString) : base(ConvertToStrings(singleString), SonarrFactory.NewIgnoreCase()) { }

        private static string[] ConvertToStrings(string oneStr)
        {
            return oneStr.Split(COMMA_ARR, StringSplitOptions.RemoveEmptyEntries);
        }
        public string ToJson()
        {
            string oneString = string.Join(",", this);
            return oneString;
        }
        public string ToJson(IDictionary parameters) => this.ToJson();
    }
}
