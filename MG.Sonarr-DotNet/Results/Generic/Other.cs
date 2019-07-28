using System;
using System.Collections.Generic;

namespace MG.Sonarr.Results
{
    public class MediaInfo : BaseResult
    {
        public decimal AudioChannels { get; set; }
        public string AudioCodec { get; set; }
        public string VideoCodec { get; set; }
    }

    public class QualityDetails : BaseResult
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Source { get; set; }
        public int Resolution { get; set; }
    }

    public class Ratings : BaseResult
    {
        public float Value { get; set; }
        public int Votes { get; set; }
    }

    public class Revision : BaseResult
    {
        public int Real { get; set; }
        public int Version { get; set; }
    }
}
