using MG.Api.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Functionality.Strings
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class JsonStringSet : SortedSet<string>, IReadOnlyList<string>, IJsonObject
    {
        private const string COMMA = ",";
        private static readonly string[] COMMA_ARR = new string[1] { COMMA };

        public string this[int index]
        {
            get
            {
                if (index < 0)
                    index = this.Count + index;

                return this.ElementAtOrDefault(index);
            }
        }

        public JsonStringSet() : base(SonarrFactory.NewIgnoreCaseComparer()) { }
        public JsonStringSet(string singleString) : base(ConvertToStrings(singleString), SonarrFactory.NewIgnoreCaseComparer()) { }

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
