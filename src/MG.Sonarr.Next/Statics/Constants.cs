using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next
{
    public static class Constants
    {
        static Constants()
        {
            SYSTEM = "/system";
            BACKUP = SYSTEM + "/backup";
            RESTART = SYSTEM + "/restart";
            STATUS = SYSTEM + "/status";
        }

        public static readonly string META_PROPERTY_NAME = MetadataResolver.META_PROPERTY_NAME;
        public const char META_PREFIX = MetadataResolver.META_PREFIX;

        public static readonly string API_KEY = "ApiKey";
        public static readonly string ID = "Id";
        public static readonly string LABEL = "Label";
        public static readonly string LANG_PROFILE_ID = "LanguageProfileId";
        public static readonly string NAME = "Name";
        public static readonly string PASSWORD = "Password";
        public static readonly string PROFILE_ID = "ProfileId";
        internal const string PROPERTY_DATA = "Data";
        internal const string PROPERTY_EPISODE = "Episode";
        internal const string PROPERTY_EPISODE_FILE = "EpisodeFile";
        internal const string PROPERTY_SHORT_OVERVIEW = "ShortOverview";
        internal const string PROPERTY_QUALITY = "Quality";
        internal const string PROPERTY_REVISION = "Revision";
        internal const string PROPERTY_SERIES = "Series";
        public static readonly string PROXY_PASSWORD = "ProxyPassword";
        public static readonly string QUALITY_PROFILE_ID = "QualityProfileId";
        public static readonly string SEASONS = "Seasons";
        public static readonly string SERIES_ID = "seriesId";
        public static readonly string SERIES_TYPE = "SeriesType";
        public static readonly string TAGS = "Tags";
        public static readonly string TITLE = "Title";
        public static readonly string USE_SEASON_FOLDER = "UseSeasonFolders";

        internal const string BY_ID = "/{0}";
        internal static readonly string CONFIG = "/config";
        internal const string SCHEMA = "/schema";

        public static readonly string BACKUP;
        public static readonly string CALENDAR = "/calendar";
        public static readonly string COMMAND = "/command";
        public static readonly string DISKSPACE = "/diskspace";
        public static readonly string DELAY_PROFILE = "/delayprofile";
        public static readonly string DOWNLOAD_CLIENT = "/downloadclient";
        public static readonly string DOWNLOAD_CLIENT_BYID = DOWNLOAD_CLIENT + BY_ID;
        public static readonly string DOWNLOAD_CLIENT_CONFIG = CONFIG + DOWNLOAD_CLIENT;
        public static readonly string EPISODE = "/episode";
        public static readonly string EPISODEFILE = "/episodefile";
        public static readonly string FILESYSTEM = "/filesystem";
        public static readonly string HISTORY = "/history";
        public static readonly string HISTORY_BY_SERIES = HISTORY + "/series";
        public static readonly string HISTORY_SINCE = HISTORY + "/since";
        public static readonly string HOST = CONFIG + "/host";
        public static readonly string INDEXER = "/indexer";
        public static readonly string INDEXER_BY_ID = INDEXER + BY_ID;
        public static readonly string INDEXER_OPTIONS = CONFIG + INDEXER;
        public static readonly string INDEXER_SCHEMA = INDEXER + "/schema";
        public static readonly string LANGUAGE_PROFILE = "/languageprofile";
        public static readonly string LANGUAGE = "/language";
        public static readonly string LOG = "/log";
        public static readonly string LOGFILE = LOG + "/file";
        public static readonly string MANUAL_IMPORT = "/manualimport";
        public static readonly string MAPPING = "/remotepathmapping";
        public static readonly string MEDIA_MANAGEMENT = CONFIG + "/mediamanagement";
        public static readonly string METADATA = "/metadata";
        public static readonly string NAMING_CONFIG = CONFIG + "/naming";
        public static readonly string NOTIFICATION = "/notification";
        public static readonly string NOTIFICATION_SCHEMA = NOTIFICATION + SCHEMA;
        public static readonly string PROFILE = "/qualityprofile";
        public static readonly string QUALITY_DEFINITIONS = "/qualitydefinition";
        public static readonly string QUEUE = "/queue";
        public static readonly string RELEASE = "/release";
        public static readonly string RELEASE_PROFILE = RELEASE + "profile";
        public static readonly string REMOTE_PATH = "/remotepathmapping";
        public static readonly string RENAME = "/rename";
        public static readonly string RESTART;
        public static readonly string RESTRICTION = "/restriction";
        public static readonly string ROOTFOLDER = "/rootfolder";
        public static readonly string SERIES = "/series";
        public static readonly string SERIES_LOOKUP = SERIES + "/lookup";
        public static readonly string STATUS;
        public static readonly string SYSTEM;
        public static readonly string TAG = "/tag";
        public static readonly string UPDATE = "/update";
        public static readonly string WANTEDMISSING = "/wanted/missing";

        public static readonly string CALENDAR_DT_FORMAT = "yyyy-MM-ddTHH:mm:ss";
    }

    public static class Meta
    {
        public const string BACKUP = "#backup";
        public const string CALENDAR = "#calendar";
        public const string COMMAND = "#command";
        public const string DELAY_PROFILE = "#delay_profile";
        public const string DISK = "#disk";
        public const string DOWNLOAD_CLIENT = "#download_client";
        public const string DOWNLOAD_CLIENT_CONFIG = "#download_client_config";
        public const string EPISODE = "#episode";
        public const string EPISODE_FILE = "#episode_file";
        public const string HISTORY = "#history_item";
        public const string HISTORY_SINCE = "#history_since";
        public const string HOST = "#host_config";
        public const string INDEXER = "#indexer";
        public const string LANGUAGE = "#language_profile";
        public const string LOG_FILE = "#log_file";
        public const string LOG_ITEM = "#log_item";
        public const string MANUAL_IMPORT = "#manual_import";
        public const string MEDIA_MANGEMENT = "#media_management";
        public const string NAMING_CONFIG = "#naming_config";
        public const string NOTIFICATION = "#notification";
        public const string QUALITY = "#quality";
        public const string QUALITY_DEFINITION = "#quality_definition";
        public const string QUALITY_PROFILE = "#quality_profile";
        public const string RELEASE = "#release";
        public const string RELEASE_PROFILE = "#release_profile";
        public const string REMOTE_PATH_MAPPING = "#remote_path_mapping";
        public const string RENAMABLE = "#rename_file";
        public const string REVISION = "#quality_revision";
        public const string ROOT_FOLDER = "#root_folder";
        public const string SERIES = "#series";
        public const string SERIES_ADD = "#add_series";
        public const string SERIES_HISTORY = "#history_series";
        public const string STATUS = "#system_status";
        public const string TAG = "#tag";
    }
}
