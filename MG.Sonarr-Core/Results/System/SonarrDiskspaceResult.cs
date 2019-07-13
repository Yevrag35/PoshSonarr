using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;

namespace MG.Sonarr.Results
{
    public class SonarrDiskspaceResult : ISonarrResult
    {
        public long? FreeSpace { get; set; }
        public string Label { get; set; }
        public string Path { get; set; }
        public decimal? PercentUsed
        {
            get
            {
                if (this.FreeSpace.HasValue && this.TotalSpace.HasValue)
                {
                    decimal spaceUsed = this.TotalSpace.Value - FreeSpace.Value;
                    decimal percent = Math.Round(spaceUsed / this.TotalSpace.Value * 100.00M, 2);
                    return percent;
                }
                else
                    return null;
            }
        }
        public long? TotalSpace { get; set; }
        
        public SonarrDiskspaceResult() { }
    }
}
