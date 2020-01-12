using MG.Sonarr.Functionality;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Results
{
    [JsonObject(MemberSerialization.OptIn)]
    public class MediaManagement : BaseResult
    {
        #region JSON PROPERTIES
        [JsonProperty("id")]
        private int _backingId;

        [JsonProperty("fileChmod")]
        private string _fileChmod;

        [JsonProperty("folderChmod")]
        private string _folderChmod;

        [JsonProperty("chownUser")]
        private string _chownUser;

        [JsonProperty("chownGroup")]
        private string _chownGroup;

        [JsonProperty("autoDownloadPropers")]
        public bool AutoDownloadPropers { get; set; }

        [JsonProperty("autoUnmonitorPreviouslyDownloadedEpisodes")]
        public bool AutoMonitorPreviouslyDownloadedEpisodes { get; set; }

        [JsonProperty("createEmptySeriesFolders")]
        public bool CreateEmptySeriesFolders { get; set; }

        [JsonProperty("copyUsingHardLinks")]
        public bool CopyUsingHardLinks { get; set; }

        [JsonProperty("deleteEmptyFolders")]
        public bool DeleteEmptyFolders { get; set; }

        [JsonProperty("enableMediaInfo")]
        public bool EnableMediaInfo { get; set; }

        [JsonProperty("extraFileExtensions")]
        public string ExtraFileExtensions { get; private set; }

        [JsonProperty("fileDate")]
        public string FileDate { get; set; }

        [JsonProperty("importExtraFiles")]
        public bool ImportExtraFiles { get; set; }

        [JsonProperty("recycleBin")]
        public string RecycleBinPath { get; set; }

        [JsonProperty("setPermissionsLinux")]
        public bool SetPermissionsLinux { get; set; }

        [JsonProperty("skipFreeSpaceCheckWhenImporting")]
        public bool SkipFreeSpaceCheckWhenImporting { get; set; }

        #endregion
    }
}