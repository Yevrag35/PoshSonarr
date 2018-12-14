using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Sonarr.Api.Results
{
    public class MediaInfo : SonarrResult
    {
        internal override string[] SkipThese => null;

        public dynamic AudioChannels { get; internal set; }
        public string AudioCodec { get; internal set; }
        public string VideoCodec { get; internal set; }
        
        public MediaInfo() : base() { }

        public static explicit operator MediaInfo(JObject job) =>
            FromJObject<MediaInfo>(job);
    }
}
