using MG.Sonarr.Functionality;
using System;
using System.Collections.Generic;

namespace MG.Sonarr.Results
{
    /// <summary>
    /// The class that defines a response from the "/system/backup" endpoint.
    /// </summary>
    public class SonarrBackupResult : BaseResult
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public BackupType Type { get; set; }
        public DateTime? Time { get; set; }
    }
}
