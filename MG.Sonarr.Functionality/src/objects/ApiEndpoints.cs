using System;
using System.Collections.Generic;
using System.Text;

namespace MG.Sonarr.Functionality
{
    public static class ApiEndpoint
    {
        internal const string CONFIG = "/config";

        public const string Backup = System + "/backup";
        public const string Calendar = "/calendar";
        public const string Command = "/command";
        public const string Diskspace = "/diskspace";
        public const string HostConfig = CONFIG + "/host";
        public const string DelayProfile = "/delayprofile";
        public const string DownloadClient = "/downloadclient";
        public const string Episode = "/episode";
        public const string EpisodeFile = "/episodefile";
        public const string FileSystem = "/filesystem";
        public const string History = "/history";
        public const string Log = "/log";
        public const string LogFile = Log + "/file";
        public const string Mapping = "/remotepathmapping";
        public const string MediaManagement = CONFIG + "/mediamanagement";
        public const string Metadata = "/metadata";
        public const string Notification = "/notification";
        public const string Profile = "/profile";
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
    }
}
