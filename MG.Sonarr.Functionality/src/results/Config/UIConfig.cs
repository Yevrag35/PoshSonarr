using MG.Sonarr.Functionality;
using MG.Sonarr.Functionality.Converters;
using MG.Sonarr.Functionality.DateFormats;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Results
{
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class UIConfig : BaseResult, IEquatable<UIConfig>
    {
        [JsonProperty("id")]
        private int _id;

        #region PROPERTIES
        [JsonProperty("calendarWeekColumnHeader")]
        [JsonConverter(typeof(WeekColumnHeaderConverter))]
        public WeekColumnHeader CalendarWeekColumnHeader { get; private set; }

        [JsonProperty("enableColorImpairedMode")]
        public bool EnableColorImpairedMode { get; set; }

        [JsonProperty("firstDayOfWeek")]
        public FirstDayOfWeek FirstDayOfWeek { get; set; }

        [JsonProperty("longDateFormat")]
        public string LongDateFormat { get; private set; }

        [JsonProperty("shortDateFormat")]
        public string ShortDateFormat { get; private set; }

        [JsonProperty("showRelativeDates")]
        public bool ShowRelativeDates { get; set; }

        [JsonProperty("timeFormat")]
        public string TimeFormat { get; private set; }

        public bool Equals(UIConfig other) => _id.Equals(other._id);

        public override string ToJson()
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
            return JsonConvert.SerializeObject(this, converter);
        }

        #endregion
    }
}