using System;
using System.Collections.Generic;

namespace MG.Sonarr.Results
{
    public class SonarrBackupResult : ISonarrResult
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public BackupType Type { get; set; }
        public DateTime? Time { get; set; }

        public SonarrBackupResult() { }
    }
}
