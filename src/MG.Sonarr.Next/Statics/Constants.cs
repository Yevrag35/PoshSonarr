using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next
{
    public static class Constants
    {
        static Constants()
        {
            CONFIG = "/config";
            SYSTEM = "/system";
        }

        public static readonly string META_PROPERTY_NAME = MetadataResolver.META_PROPERTY_NAME;
        public const char META_PREFIX = MetadataResolver.META_PREFIX;

        public static readonly string ID = "Id";
        public static readonly string LABEL = "Label";
        public static readonly string LANG_PROFILE_ID = "LanguageProfileId";
        public static readonly string NAME = "Name";
        public static readonly string PROFILE_ID = "ProfileId";
        public static readonly string QUALITY_PROFILE_ID = "QualityProfileId";
        public static readonly string SEASONS = "Seasons";
        public static readonly string SERIES_ID = "seriesId";
        public static readonly string SERIES_TYPE = "SeriesType";
        public static readonly string TAGS = "Tags";
        public static readonly string TITLE = "Title";
        public static readonly string USE_SEASON_FOLDER = "UseSeasonFolders";

        internal const string BY_ID = "/{0}";
        internal static readonly string CONFIG;

        public static readonly string BACKUP = SYSTEM + "/backup";
        public static readonly string CALENDAR = "/calendar";
        public static readonly string COMMAND = "/command";
        public static readonly string DISKSPACE = "/diskspace";
        public static readonly string HOST_CONFIG = CONFIG + "/host";
        public static readonly string DELAY_PROFILE = "/delayprofile";
        public static readonly string DOWNLOAD_CLIENT = "/downloadclient";
        public static readonly string DOWNLOAD_CLIENT_BYID = DOWNLOAD_CLIENT + BY_ID;
        public static readonly string EPISODE = "/episode";
        public static readonly string EPISODEFILE = "/episodefile";
        public static readonly string FILESYSTEM = "/filesystem";
        public static readonly string HISTORY = "/history";
        public static readonly string INDEXER = "/indexer";
        public static readonly string INDEXER_BY_ID = INDEXER + BY_ID;
        public static readonly string INDEXER_OPTIONS = CONFIG + INDEXER;
        public static readonly string INDEXER_SCHEMA = INDEXER + "/schema";
        public static readonly string LANGUAGE_PROFILE = "/languageprofile";
        public static readonly string LANGUAGE = "/language";
        public static readonly string LOG = "/log";
        public static readonly string LOGFILE = LOG + "/file";
        public static readonly string MANUALIMPORT = "/manualimport";
        public static readonly string MAPPING = "/remotepathmapping";
        public static readonly string MEDIAMANAGEMENT = CONFIG + "/mediamanagement";
        public static readonly string METADATA = "/metadata";
        public static readonly string NOTIFICATION = "/notification";
        public static readonly string PROFILE = "/qualityprofile";
        public static readonly string QUALITY_DEFINITIONS = "/qualitydefinition";
        public static readonly string QUEUE = "/queue";
        public static readonly string RELEASE = "/release";
        public static readonly string RELEASE_PROFILE = RELEASE + "profile";
        public static readonly string RESTART = SYSTEM + "/restart";
        public static readonly string RESTRICTION = "/restriction";
        public static readonly string ROOTFOLDER = "/rootfolder";
        public static readonly string SERIES = "/series";
        public static readonly string STATUS = SYSTEM + "/status";
        public static readonly string SYSTEM;
        public static readonly string TAG = "/tag";
        public static readonly string UPDATE = "/update";
        public static readonly string WANTEDMISSING = "/wanted/missing";

        public static readonly string CALENDAR_DT_FORMAT = "yyyy-MM-ddTHH:mm:ss";

        public static readonly string DEBUG = "Debug";
        public static readonly string VERBOSE = "Verbose";
        public static readonly string PREFERENCE = "Preference";
        public static readonly string DEBUG_PREFERENCE = DEBUG + PREFERENCE;
        public static readonly string VERBOSE_PREFERENCE = VERBOSE + PREFERENCE;
        public static readonly string ERROR_ACTION = "ErrorAction";
        public static readonly string ERROR_ACTION_PREFERENCE = ERROR_ACTION + PREFERENCE;
    }

    public static class Meta
    {
        public static readonly string BACKUP = "#backup";
        public static readonly string CALENDAR = "#calendar";
        public static readonly string DELAY_PROFILE = "#delay_profile";
        public static readonly string EPISODE = "#episode";
        public static readonly string EPISODE_FILE = "#episode_file";
        public static readonly string INDEXER = "#indexer";
        public static readonly string LANGUAGE = "#language_profile";
        public static readonly string LOG_FILE = "#log_file";
        public static readonly string LOG_ITEM = "#log_item";
        public static readonly string QUALITY_DEFINITION = "#quality_definition";
        public static readonly string QUALITY_PROFILE = "#quality_profile";
        public static readonly string RELEASE = "#release";
        public static readonly string RELEASE_PROFILE = "#release_profile";
        public static readonly string ROOT_FOLDER = "#root_folder";
        public static readonly string SERIES = "#series";
        public static readonly string SERIES_ADD = "#add_series";
        public static readonly string TAG = "#tag";
    }
}
