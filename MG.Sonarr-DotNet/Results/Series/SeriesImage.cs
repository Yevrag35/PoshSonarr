using System;

namespace MG.Sonarr.Results
{
    public class SeriesImage : BaseResult
    {
        public CoverType CoverType { get; set; }
        public Uri Url { get; set; }
    }
}
