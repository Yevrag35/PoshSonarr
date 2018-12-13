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
        //private string _audioCodec;
        //private dynamic _audioChannels;
        //private string _videoCodec;

        public dynamic AudioChannels { get; internal set; }
        public string AudioCodec { get; internal set; }
        public string VideoCodec { get; internal set; }

        //private MediaInfo(JObject job)
        //{
        //    if (job != null)
        //    {
        //        _audioChannels = job["audioChannels"];
        //        _audioCodec = (string)job["audioCodec"];
        //        _videoCodec = (string)job["videoCodec"];
        //    }
        //}

        //public static explicit operator MediaInfo(JObject job) =>
        //    job != null ? new MediaInfo(job) : null;
    }
}
