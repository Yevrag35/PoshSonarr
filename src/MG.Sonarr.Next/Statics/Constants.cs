using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next;

/// <summary>
/// Provides shared constant values for Sonarr property names and API routes.
/// </summary>
public static class Constants
{
	/// <summary>
	/// Initializes the static fields that depend on the system route prefix.
	/// </summary>
	static Constants()
	{
		SYSTEM = "/system";
		BACKUP = SYSTEM + "/backup";
		RESTART = SYSTEM + "/restart";
		STATUS = SYSTEM + "/status";
	}

	/// <summary>
	/// The metadata property name used for model metadata lookup.
	/// </summary>
	public static readonly string META_PROPERTY_NAME = MetadataResolver.META_PROPERTY_NAME;

	/// <summary>
	/// The metadata prefix character used by metadata tokens.
	/// </summary>
	public const char META_PREFIX = MetadataResolver.META_PREFIX;

	/// <summary>
	/// The API key property name.
	/// </summary>
	public const string API_KEY = "ApiKey";

	/// <summary>
	/// The episode identifier property name.
	/// </summary>
	public const string EPISODE_ID = "episodeId";

	/// <summary>
	/// The identifier property name.
	/// </summary>
	public const string ID = "Id";

	/// <summary>
	/// The lowercase identifier property name.
	/// </summary>
	public const string ID_LOWERCASE = "id";

	/// <summary>
	/// The label property name.
	/// </summary>
	public const string LABEL = "Label";

	/// <summary>
	/// The language profile identifier property name.
	/// </summary>
	public const string LANG_PROFILE_ID = "LanguageProfileId";

	/// <summary>
	/// The name property name.
	/// </summary>
	public const string NAME = "Name";

	/// <summary>
	/// The lowercase name property name.
	/// </summary>
	public const string NAME_LOWERCASE = "name";

	/// <summary>
	/// The password property name.
	/// </summary>
	public const string PASSWORD = "Password";

	/// <summary>
	/// The profile identifier property name.
	/// </summary>
	public const string PROFILE_ID = "ProfileId";

	/// <summary>
	/// The internal data property name.
	/// </summary>
	internal const string PROPERTY_DATA = "Data";

	/// <summary>
	/// The internal episode property name.
	/// </summary>
	internal const string PROPERTY_EPISODE = "Episode";

	/// <summary>
	/// The internal episode file property name.
	/// </summary>
	internal const string PROPERTY_EPISODE_FILE = "EpisodeFile";

	/// <summary>
	/// The internal short overview property name.
	/// </summary>
	internal const string PROPERTY_SHORT_OVERVIEW = "ShortOverview";

	/// <summary>
	/// The internal quality property name.
	/// </summary>
	internal const string PROPERTY_QUALITY = "Quality";

	/// <summary>
	/// The internal quality revision property name.
	/// </summary>
	internal const string PROPERTY_REVISION = "Revision";

	/// <summary>
	/// The internal series property name.
	/// </summary>
	internal const string PROPERTY_SERIES = "Series";

	/// <summary>
	/// The proxy password property name.
	/// </summary>
	public const string PROXY_PASSWORD = "ProxyPassword";

	/// <summary>
	/// The quality profile identifier property name.
	/// </summary>
	public const string QUALITY_PROFILE_ID = "QualityProfileId";

	/// <summary>
	/// The seasons property name.
	/// </summary>
	public const string SEASONS = "Seasons";

	/// <summary>
	/// The season number property name.
	/// </summary>
	public const string SEASON_NUMBER = "seasonNumber";

	/// <summary>
	/// The series identifier property name.
	/// </summary>
	public const string SERIES_ID = "SeriesId";

	/// <summary>
	/// The lowercase series identifier property name.
	/// </summary>
	public const string SERIES_ID_LOWERCASE = "seriesId";

	/// <summary>
	/// The series type property name.
	/// </summary>
	public const string SERIES_TYPE = "SeriesType";

	/// <summary>
	/// The tags property name.
	/// </summary>
	public const string TAGS = "Tags";

	/// <summary>
	/// The title property name.
	/// </summary>
	public const string TITLE = "Title";

	/// <summary>
	/// The season-folder usage property name.
	/// </summary>
	public const string USE_SEASON_FOLDER = "UseSeasonFolders";

	/// <summary>
	/// The route template suffix for resource-by-id endpoints.
	/// </summary>
	internal const string BY_ID = "/{0}";

	/// <summary>
	/// The shared config route segment.
	/// </summary>
	internal const string CONFIG = "/config";

	/// <summary>
	/// The shared schema route segment.
	/// </summary>
	internal const string SCHEMA = "/schema";

	/// <summary>
	/// The backup endpoint route.
	/// </summary>
	public static readonly string BACKUP;

	/// <summary>
	/// The calendar endpoint route.
	/// </summary>
	public static readonly string CALENDAR = "/calendar";

	/// <summary>
	/// The command endpoint route.
	/// </summary>
	public static readonly string COMMAND = "/command";

	/// <summary>
	/// The disk space endpoint route.
	/// </summary>
	public static readonly string DISKSPACE = "/diskspace";

	/// <summary>
	/// The delay profile endpoint route.
	/// </summary>
	public static readonly string DELAY_PROFILE = "/delayprofile";

	/// <summary>
	/// The download client endpoint route.
	/// </summary>
	public static readonly string DOWNLOAD_CLIENT = "/downloadclient";

	/// <summary>
	/// The download client by-id endpoint route template.
	/// </summary>
	public static readonly string DOWNLOAD_CLIENT_BYID = DOWNLOAD_CLIENT + BY_ID;

	/// <summary>
	/// The download client config endpoint route.
	/// </summary>
	public static readonly string DOWNLOAD_CLIENT_CONFIG = CONFIG + DOWNLOAD_CLIENT;

	/// <summary>
	/// The download client schema endpoint route.
	/// </summary>
	public static readonly string DOWNLOAD_CLIENT_SCHEMA = DOWNLOAD_CLIENT + SCHEMA;

	/// <summary>
	/// The episode endpoint route.
	/// </summary>
	public static readonly string EPISODE = "/episode";

	/// <summary>
	/// The episode file endpoint route.
	/// </summary>
	public static readonly string EPISODEFILE = "/episodefile";

	/// <summary>
	/// The filesystem endpoint route.
	/// </summary>
	public static readonly string FILESYSTEM = "/filesystem";

	/// <summary>
	/// The history endpoint route.
	/// </summary>
	public static readonly string HISTORY = "/history";

	/// <summary>
	/// The history-by-series endpoint route.
	/// </summary>
	public static readonly string HISTORY_BY_SERIES = HISTORY + "/series";

	/// <summary>
	/// The history-since endpoint route.
	/// </summary>
	public static readonly string HISTORY_SINCE = HISTORY + "/since";

	/// <summary>
	/// The host config endpoint route.
	/// </summary>
	public static readonly string HOST = CONFIG + "/host";

	/// <summary>
	/// The indexer endpoint route.
	/// </summary>
	public static readonly string INDEXER = "/indexer";

	/// <summary>
	/// The indexer by-id endpoint route template.
	/// </summary>
	public static readonly string INDEXER_BY_ID = INDEXER + BY_ID;

	/// <summary>
	/// The indexer options endpoint route.
	/// </summary>
	public static readonly string INDEXER_OPTIONS = CONFIG + INDEXER;

	/// <summary>
	/// The indexer schema endpoint route.
	/// </summary>
	public static readonly string INDEXER_SCHEMA = INDEXER + "/schema";

	/// <summary>
	/// The language profile endpoint route.
	/// </summary>
	public static readonly string LANGUAGE_PROFILE = "/languageprofile";

	/// <summary>
	/// The language endpoint route.
	/// </summary>
	public static readonly string LANGUAGE = "/language";

	/// <summary>
	/// The log endpoint route.
	/// </summary>
	public static readonly string LOG = "/log";

	/// <summary>
	/// The log file endpoint route.
	/// </summary>
	public static readonly string LOGFILE = LOG + "/file";

	/// <summary>
	/// The manual import endpoint route.
	/// </summary>
	public static readonly string MANUAL_IMPORT = "/manualimport";

	/// <summary>
	/// The remote path mapping endpoint route.
	/// </summary>
	public static readonly string MAPPING = "/remotepathmapping";

	/// <summary>
	/// The media management config endpoint route.
	/// </summary>
	public static readonly string MEDIA_MANAGEMENT = CONFIG + "/mediamanagement";

	/// <summary>
	/// The metadata endpoint route.
	/// </summary>
	public static readonly string METADATA = "/metadata";

	/// <summary>
	/// The naming config endpoint route.
	/// </summary>
	public static readonly string NAMING_CONFIG = CONFIG + "/naming";

	/// <summary>
	/// The notification endpoint route.
	/// </summary>
	public static readonly string NOTIFICATION = "/notification";

	/// <summary>
	/// The notification schema endpoint route.
	/// </summary>
	public static readonly string NOTIFICATION_SCHEMA = NOTIFICATION + SCHEMA;

	/// <summary>
	/// The quality profile endpoint route.
	/// </summary>
	public static readonly string PROFILE = "/qualityprofile";

	/// <summary>
	/// The quality definition endpoint route.
	/// </summary>
	public static readonly string QUALITY_DEFINITIONS = "/qualitydefinition";

	/// <summary>
	/// The queue endpoint route.
	/// </summary>
	public static readonly string QUEUE = "/queue";

	/// <summary>
	/// The release endpoint route.
	/// </summary>
	public static readonly string RELEASE = "/release";

	/// <summary>
	/// The release profile endpoint route.
	/// </summary>
	public static readonly string RELEASE_PROFILE = RELEASE + "profile";

	/// <summary>
	/// The remote path mapping endpoint route.
	/// </summary>
	public static readonly string REMOTE_PATH = "/remotepathmapping";

	/// <summary>
	/// The rename endpoint route.
	/// </summary>
	public static readonly string RENAME = "/rename";

	/// <summary>
	/// The restart endpoint route.
	/// </summary>
	public static readonly string RESTART;

	/// <summary>
	/// The restriction endpoint route.
	/// </summary>
	public static readonly string RESTRICTION = "/restriction";

	/// <summary>
	/// The root folder endpoint route.
	/// </summary>
	public static readonly string ROOTFOLDER = "/rootfolder";

	/// <summary>
	/// The series endpoint route.
	/// </summary>
	public static readonly string SERIES = "/series";

	/// <summary>
	/// The series lookup endpoint route.
	/// </summary>
	public static readonly string SERIES_LOOKUP = SERIES + "/lookup";

	/// <summary>
	/// The system status endpoint route.
	/// </summary>
	public static readonly string STATUS;

	/// <summary>
	/// The system endpoint route.
	/// </summary>
	public static readonly string SYSTEM;

	/// <summary>
	/// The tag endpoint route.
	/// </summary>
	public static readonly string TAG = "/tag";

	/// <summary>
	/// The update endpoint route.
	/// </summary>
	public static readonly string UPDATE = "/update";

	/// <summary>
	/// The wanted missing endpoint route.
	/// </summary>
	public static readonly string WANTEDMISSING = "/wanted/missing";

	/// <summary>
	/// The calendar date-time format string used by Sonarr APIs.
	/// </summary>
	public static readonly string CALENDAR_DT_FORMAT = "yyyy-MM-ddTHH:mm:ss";
}

/// <summary>
/// Provides metadata key constants that identify Sonarr resource metadata types.
/// </summary>
public static class Meta
{
	/// <summary>
	/// The metadata key for backup resources.
	/// </summary>
	public const string BACKUP = "#backup";

	/// <summary>
	/// The metadata key for calendar resources.
	/// </summary>
	public const string CALENDAR = "#calendar";

	/// <summary>
	/// The metadata key for command resources.
	/// </summary>
	public const string COMMAND = "#command";

	/// <summary>
	/// The metadata key for delay profile resources.
	/// </summary>
	public const string DELAY_PROFILE = "#delay_profile";

	/// <summary>
	/// The metadata key for disk resources.
	/// </summary>
	public const string DISK = "#disk";

	/// <summary>
	/// The metadata key for download client resources.
	/// </summary>
	public const string DOWNLOAD_CLIENT = "#download_client";

	/// <summary>
	/// The metadata key for download client config resources.
	/// </summary>
	public const string DOWNLOAD_CLIENT_CONFIG = "#download_client_config";

	/// <summary>
	/// The metadata key for download client schema resources.
	/// </summary>
	public const string DOWNLOAD_CLIENT_SCHEMA = "#download_client_schema";

	/// <summary>
	/// The metadata key for episode resources.
	/// </summary>
	public const string EPISODE = "#episode";

	/// <summary>
	/// The metadata key for episode file resources.
	/// </summary>
	public const string EPISODE_FILE = "#episode_file";

	/// <summary>
	/// The metadata key for history item resources.
	/// </summary>
	public const string HISTORY = "#history_item";

	/// <summary>
	/// The metadata key for history-since resources.
	/// </summary>
	public const string HISTORY_SINCE = "#history_since";

	/// <summary>
	/// The metadata key for host config resources.
	/// </summary>
	public const string HOST = "#host_config";

	/// <summary>
	/// The metadata key for indexer resources.
	/// </summary>
	public const string INDEXER = "#indexer";

	/// <summary>
	/// The metadata key for language profile resources.
	/// </summary>
	public const string LANGUAGE = "#language_profile";

	/// <summary>
	/// The metadata key for log file resources.
	/// </summary>
	public const string LOG_FILE = "#log_file";

	/// <summary>
	/// The metadata key for log item resources.
	/// </summary>
	public const string LOG_ITEM = "#log_item";

	/// <summary>
	/// The metadata key for manual import resources.
	/// </summary>
	public const string MANUAL_IMPORT = "#manual_import";

	/// <summary>
	/// The metadata key for media management resources.
	/// </summary>
	public const string MEDIA_MANGEMENT = "#media_management";

	/// <summary>
	/// The metadata key for naming config resources.
	/// </summary>
	public const string NAMING_CONFIG = "#naming_config";

	/// <summary>
	/// The metadata key for notification resources.
	/// </summary>
	public const string NOTIFICATION = "#notification";

	/// <summary>
	/// The metadata key for quality resources.
	/// </summary>
	public const string QUALITY = "#quality";

	/// <summary>
	/// The metadata key for quality definition resources.
	/// </summary>
	public const string QUALITY_DEFINITION = "#quality_definition";

	/// <summary>
	/// The metadata key for quality profile resources.
	/// </summary>
	public const string QUALITY_PROFILE = "#quality_profile";

	/// <summary>
	/// The metadata key for release resources.
	/// </summary>
	public const string RELEASE = "#release";

	/// <summary>
	/// The metadata key for release profile resources.
	/// </summary>
	public const string RELEASE_PROFILE = "#release_profile";

	/// <summary>
	/// The metadata key for remote path mapping resources.
	/// </summary>
	public const string REMOTE_PATH_MAPPING = "#remote_path_mapping";

	/// <summary>
	/// The metadata key for renamable file resources.
	/// </summary>
	public const string RENAMABLE = "#rename_file";

	/// <summary>
	/// The metadata key for quality revision resources.
	/// </summary>
	public const string REVISION = "#quality_revision";

	/// <summary>
	/// The metadata key for root folder resources.
	/// </summary>
	public const string ROOT_FOLDER = "#root_folder";

	/// <summary>
	/// The metadata key for series resources.
	/// </summary>
	public const string SERIES = "#series";

	/// <summary>
	/// The metadata key for add-series resources.
	/// </summary>
	public const string SERIES_ADD = "#add_series";

	/// <summary>
	/// The metadata key for series history resources.
	/// </summary>
	public const string SERIES_HISTORY = "#history_series";

	/// <summary>
	/// The metadata key for system status resources.
	/// </summary>
	public const string STATUS = "#system_status";

	/// <summary>
	/// The metadata key for tag resources.
	/// </summary>
	public const string TAG = "#tag";
}
