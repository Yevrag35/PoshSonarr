using MG.Sonarr.Next.Services.Metadata;

namespace MG.Sonarr.Next
{
    public static class Constants
    {
        public const string META_PROPERTY_NAME = MetadataResolver.META_PROPERTY_NAME;
        public const char META_PREFIX = MetadataResolver.META_PREFIX;

        public const string ID = "Id";
        public const string LABEL = "Label";
        public const string LANG_PROFILE_ID = "LanguageProfileId";
        public const string NAME = "Name";
        public const string PROFILE_ID = "ProfileId";
        public const string QUALITY_PROFILE_ID = "QualityProfileId";
        public const string SEASONS = "Seasons";
        public const string SERIES_TYPE = "SeriesType";
        public const string TAGS = "Tags";
        public const string TITLE = "Title";
        public const string USE_SEASON_FOLDER = "UseSeasonFolders";

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
        public const string EPISODE = "#episode";
        public const string EPISODE_FILE = "#episode_file";
        public const string INDEXER = "#indexer";
        public const string ROOT_FOLDER = "#root_folder";
        public const string SERIES = "#series";
        public const string SERIES_ADD = "#add_series";
        public const string TAG = "#tag";
    }
}
