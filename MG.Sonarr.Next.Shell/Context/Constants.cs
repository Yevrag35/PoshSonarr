using MG.Sonarr.Next.Services.Metadata;
using System.CodeDom;

namespace MG.Sonarr.Next.Shell.Context
{
    public static class Constants
    {
        public const string META_PROPERTY_NAME = MetadataResolver.META_PROPERTY_NAME;
        public const char META_PREFIX = MetadataResolver.META_PREFIX;

        public const string LABEL = "Label";
        public const string NAME = "Name";
        public const string TITLE = "Title";

        internal const string CONFIG = "/config";
        internal const string BY_ID = "/{0}";

        public const string BACKUP = SYSTEM + "/backup";
        public const string CALENDAR = "/calendar";
        public const string COMMAND = "/command";
        public const string DISKSPACE = "/diskspace";
        public const string HOST_CONFIG = CONFIG + "/host";
        public const string DELAY_PROFILE = "/delayprofile";
        public const string DOWNLOAD_CLIENT = "/downloadclient";
        public const string DOWNLOAD_CLIENT_BYID = DOWNLOAD_CLIENT + BY_ID;
        public const string EPISODE = "/episode";
        public const string EPISODEFILE = "/episodefile";
        public const string FILESYSTEM = "/filesystem";
        public const string HISTORY = "/history";
        public const string INDEXER = "/indexer";
        public const string INDEXER_BY_ID = INDEXER + BY_ID;
        public const string INDEXER_OPTIONS = CONFIG + INDEXER;
        public const string INDEXER_SCHEMA = INDEXER + "/schema";
        public const string LOG = "/log";
        public const string LOGFILE = LOG + "/file";
        public const string MANUALIMPORT = "/manualimport";
        public const string MAPPING = "/remotepathmapping";
        public const string MEDIAMANAGEMENT = CONFIG + "/mediamanagement";
        public const string METADATA = "/metadata";
        public const string NOTIFICATION = "/notification";
        public const string PROFILE = "/profile";
        public const string QUALITY_DEFINITIONS = "/qualitydefinition";
        public const string QUEUE = "/queue";
        public const string RELEASE = "/release";
        public const string RESTART = SYSTEM + "/restart";
        public const string RESTRICTION = "/restriction";
        public const string ROOTFOLDER = "/rootfolder";
        public const string SERIES = "/series";
        public const string STATUS = SYSTEM + "/status";
        public const string SYSTEM = "/system";
        public const string TAG = "/tag";
        public const string UPDATE = "/update";
        public const string WANTEDMISSING = "/wanted/missing";

        public const string DEBUG = "Debug";
        public const string VERBOSE = "Verbose";
        public const string PREFERENCE = "Preference";
        public const string DEBUG_PREFERENCE = DEBUG + PREFERENCE;
        public const string VERBOSE_PREFERENCE = VERBOSE + PREFERENCE;

        
    }

    public static class Meta
    {
        public const string SERIES = "#series";
        public const string EPISODE = "#episode";
        public const string EPISODEFILE = "#episodefile";
        public const string INDEXER = "#indexer";
        public const string TAG = "#tag";
    }
}
