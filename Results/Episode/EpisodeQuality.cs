﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Sonarr.Api.Results
{
    public class EpisodeQuality : SonarrResult
    {
        //#pragma warning disable IDE0044 // Add readonly modifier
        //        private long _id;
        //        private string _name;
        //        private string _source;
        //        private long _resolution;
        //        private long _version;
        //        private long _real;
        //#pragma warning restore IDE0044 // Add readonly modifier

        public long Id { get; internal set; }
        public string Name { get; internal set; }
        public string Source { get; internal set; }
        public long Resolution { get; internal set; }
        public long Version { get; internal set; }
        public long Real { get; internal set; }

        //private EpisodeQuality(IDictionary dict) => MatchResultsToProperties(dict);

        //public static explicit operator EpisodeQuality(JObject job)
        //{
        //    if (job != null)
        //    {
        //        var quat = (JObject)job["quality"];
        //        var rev = (JObject)job["revision"];
        //        var dictQ = JsonConvert.DeserializeObject<Dictionary<object, object>>(JsonConvert.SerializeObject(quat));
        //        var dictRev = JsonConvert.DeserializeObject<Dictionary<object, object>>(JsonConvert.SerializeObject(rev));
        //        object[] keys = dictRev.Keys.Cast<object>().ToArray();
        //        for (int i = 0; i < keys.Length; i++)
        //        {
        //            var key = keys[i];
        //            dictQ.Add(key, dictRev[key]);
        //        }
        //        return new EpisodeQuality(dictQ);
        //    }
        //    else
        //    {
        //        return null;
        //    }
        //}
    }
}
