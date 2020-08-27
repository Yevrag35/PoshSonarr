using MG.Api.Json;
using MG.Sonarr.Functionality.Collections;
using MG.Sonarr.Functionality.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;

namespace MG.Sonarr.Results
{
    public sealed class SeriesImageCollection : SortedStringList<SeriesImage>, IReadOnlyList<SeriesImage>, IJsonObject
    {
        public SeriesImage this[int index]
        {
            get
            {
                int posIndex = this.GetPositiveIndex(index);
                return posIndex > -1 ? base.InnerList.Values[posIndex] : null;
            }
        }

        [JsonConstructor]
        internal SeriesImageCollection(IEnumerable<SeriesImage> images)
            : base(images, x => x.CoverType.ToString())
        {
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(base.InnerList.Values, Formatting.Indented);
        }
        public string ToJson(IDictionary parameters)
        {
            return this.ToJson();
        }
    }
}
