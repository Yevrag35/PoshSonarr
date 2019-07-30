using System;

namespace MG.Sonarr.Results
{
    /// <summary>
    /// The class that defines a response from the "/MediaCover" endpoint.
    /// </summary>
    public class SeriesImage : BaseResult
    {
        public CoverType CoverType { get; set; }
        public Uri Url { get; set; }
    }
}
