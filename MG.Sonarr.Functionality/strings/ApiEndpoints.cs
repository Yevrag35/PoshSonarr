using System;
using System.Collections.Generic;
using System.Text;

namespace MG.Sonarr.Functionality.Strings
{
    /// <summary>
    /// A static class of <see cref="string"/> constants that denote the various Sonarr endpoint paths used by this assembly.
    /// </summary>
    public static class ApiEndpoints
    {
        internal const string CONFIG = "/config";
        internal const string BY_ID = "/{0}";

        public const string Backup = System + "/backup";
        public const string Calendar = "/calendar";
        public const string Command = "/command";
        public const string Diskspace = "/diskspace";
        public const string HostConfig = CONFIG + "/host";
        public const string DelayProfile = "/delayprofile";
        public const string DownloadClient = "/downloadclient";
        public const string DownloadClient_ById = DownloadClient + BY_ID;
        public const string Episode = "/episode";
        public const string EpisodeFile = "/episodefile";
        public const string FileSystem = "/filesystem";
        public const string History = "/history";
        public const string Indexer = "/indexer";
        public const string Indexer_ById = Indexer + BY_ID;
        public const string IndexerOptions = CONFIG + Indexer;
        public const string IndexerSchema = Indexer + "/schema";
        public const string Log = "/log";
        public const string LogFile = Log + "/file";
        public const string ManualImport = "/manualimport";
        public const string Mapping = "/remotepathmapping";
        public const string MediaManagement = CONFIG + "/mediamanagement";
        public const string Metadata = "/metadata";
        public const string Notification = "/notification";
        public const string Profile = "/profile";
        public const string QualityDefinitions = "/qualitydefinition";
        public const string Queue = "/queue";
        public const string Release = "/release";
        public const string Restart = System + "/restart";
        public const string Restriction = "/restriction";
        public const string RootFolder = "/rootfolder";
        public const string Series = "/series";
        public const string Status = System + "/status";
        public const string System = "/system";
        public const string Tag = "/tag";
        public const string Update = "/update";
        public const string WantedMissing = "/wanted/missing";

        // Calendar - Extra paths
        public const string Calendar_DTFormat = "yyyy-MM-ddTHH:mm:ss";
        public const string Calendar_WithDate = Calendar + "?start={0}&end={1}";

        // Command - Extra paths
        public const string Command_ById = Command + BY_ID;

        // Episode - Extra paths
        public const string Episode_SeriesId = Episode + "?seriesId={0}";
        public const string Episode_ById = Episode + BY_ID;

        // Release - Extra paths
        public const string Release_EpisodeId = Release + "?episodeId={0}";

        // FileSystem - Extra paths
        public const string FileSystem_Path = FileSystem + "?path={0}";
        public const string FileSystem_PathWithFiles = FileSystem_Path + "&includeFiles=true";

        // Series - Extra paths
        public const string Series_ById = Series + BY_ID;
        internal const string Series_LookupFormat = Series + "/lookup?";
        public const string Series_LookupByStr = Series_LookupFormat + "term={0}";
        public const string Series_LookupById = Series_LookupFormat + "term=tvdb:{0}";

        // Tags - Extra paths
        public const string Tag_ById = Tag + BY_ID;
    }
}
