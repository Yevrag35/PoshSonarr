using System;
using System.Collections;
using System.Collections.Generic;

namespace Sonarr.Api.Results
{
    public class BackupResult : SonarrResult
    {
        internal override string[] SkipThese => null;

        public string Name { get; internal set; }
        public string Path { get; internal set; }
        public string Type { get; internal set; }
        public DateTime? Time { get; internal set; }
        public long Id { get; internal set; }

        public BackupResult() : base() { }
    }
}
