using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Sonarr.Api.Results
{
    public class DiskspaceResult : SonarrResult
    {
        internal override string[] SkipThese => new string[1]
        {
            "PercentUsed"
        };

        public string Path { get; internal set; }
        public string Label { get; internal set; }
        public long? FreeSpace { get; internal set; }
        public long? TotalSpace { get; internal set; }
        public decimal? PercentUsed
        {
            get
            {
                if (FreeSpace.HasValue && TotalSpace.HasValue)
                {
                    decimal spaceUsed = TotalSpace.Value - FreeSpace.Value;
                    decimal percent = Math.Round(spaceUsed / TotalSpace.Value * 100.00M, 2);
                    return percent;
                }
                else
                    return null;
            }
        }

        public DiskspaceResult() : base() { }

        public static explicit operator DiskspaceResult(JObject job) =>
            FromJObject<DiskspaceResult>(job);
    }
}