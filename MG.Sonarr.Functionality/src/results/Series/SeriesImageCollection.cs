using MG.Sonarr.Functionality;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;

namespace MG.Sonarr.Results
{
    public sealed class SeriesImageCollection : ResultCollectionBase<SeriesImage>
    {
        public SeriesImage this[int index] => base.InnerList[index];
        public SeriesImage this[string imageType]
        {
            get => base.Find(x => Enum.TryParse<CoverType>(imageType, true, out CoverType ct) && x.CoverType.Equals(ct));
        }

        [JsonConstructor]
        internal SeriesImageCollection(IEnumerable<SeriesImage> images) : base(images) { }
    }
}
