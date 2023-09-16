using MG.Sonarr.Functionality;
using MG.Sonarr.Results.Collections;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace MG.Sonarr.Results
{
    public sealed class SeriesImageCollection : ResultListBase<SeriesImage>
    {
        //public SeriesImage this[string coverType] => Enum.TryParse(coverType, true, out CoverType result) ? this[result] : null;
        public SeriesImage this[string coverType] => base.Find(x => 
            StringComparer.InvariantCultureIgnoreCase.Equals(x.CoverType, coverType));

        [JsonConstructor]
        internal SeriesImageCollection(IEnumerable<SeriesImage> images) : base(images) { }
    }
}
