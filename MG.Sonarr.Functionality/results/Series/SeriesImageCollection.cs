using MG.Sonarr.Functionality.Collections;
using MG.Sonarr.Functionality.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace MG.Sonarr.Results
{
    public sealed class SeriesImageCollection : SortedListBase<SeriesImage>, IReadOnlyList<SeriesImage>
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
    }
}
