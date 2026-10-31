using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next;

/// <summary>
/// Provides shared constant values for Sonarr property names and API routes.
/// </summary>
internal static class Constants
{
	/// <summary>
	/// The metadata property name used for model metadata lookup.
	/// </summary>
	internal const string MetaPropertyName = MetadataResolver.MetaPropertyName;

	/// <summary>
	/// The API key property name.
	/// </summary>
	internal const string ApiKey = "ApiKey";

	/// <summary>
	/// The episode identifier property name.
	/// </summary>
	internal const string EpisodeId = "episodeId";

	/// <summary>
	/// The identifier property name.
	/// </summary>
	internal const string Id = "Id";

	/// <summary>
	/// The lowercase identifier property name.
	/// </summary>
	internal const string IdLowercase = "id";

	/// <summary>
	/// The label property name.
	/// </summary>
	internal const string Label = "Label";

	/// <summary>
	/// The language profile identifier property name.
	/// </summary>
	internal const string LangProfileId = "LanguageProfileId";

	/// <summary>
	/// The name property name.
	/// </summary>
	internal const string Name = "Name";

	/// <summary>
	/// The lowercase name property name.
	/// </summary>
	internal const string NameLowercase = "name";

	/// <summary>
	/// The password property name.
	/// </summary>
	internal const string Password = "Password";

	/// <summary>
	/// The profile identifier property name.
	/// </summary>
	internal const string ProfileId = "ProfileId";

	/// <summary>
	/// The internal data property name.
	/// </summary>
	internal const string Property_Data = "Data";

	/// <summary>
	/// The internal episode property name.
	/// </summary>
	internal const string Property_Episode = "Episode";

	/// <summary>
	/// The internal episode file property name.
	/// </summary>
	internal const string Property_EpisodeFile = "EpisodeFile";

	/// <summary>
	/// The internal short overview property name.
	/// </summary>
	internal const string Property_ShortOverview = "ShortOverview";

	/// <summary>
	/// The internal quality property name.
	/// </summary>
	internal const string Property_Quality = "Quality";

	/// <summary>
	/// The internal quality revision property name.
	/// </summary>
	internal const string Property_Revision = "Revision";

	/// <summary>
	/// The internal series property name.
	/// </summary>
	internal const string Property_Series = "Series";

	/// <summary>
	/// The proxy password property name.
	/// </summary>
	internal const string ProxyPassword = "ProxyPassword";

	/// <summary>
	/// The quality profile identifier property name.
	/// </summary>
	internal const string QualityProfileId = "QualityProfileId";

	/// <summary>
	/// The seasons property name.
	/// </summary>
	internal const string Seasons = "Seasons";

	/// <summary>
	/// The season number property name.
	/// </summary>
	internal const string SeasonNumber = "seasonNumber";

	/// <summary>
	/// The series identifier property name.
	/// </summary>
	internal const string SeriesId = "SeriesId";

	/// <summary>
	/// The lowercase series identifier property name.
	/// </summary>
	internal const string SeriesIdLowercase = "seriesId";

	/// <summary>
	/// The series type property name.
	/// </summary>
	internal const string SeriesType = "SeriesType";

	/// <summary>
	/// The tags property name.
	/// </summary>
	internal const string Tags = "Tags";

	/// <summary>
	/// The title property name.
	/// </summary>
	internal const string Title = "Title";

	/// <summary>
	/// The season-folder usage property name.
	/// </summary>
	internal const string UseSeasonFolder = "UseSeasonFolders";

	/// <summary>
	/// The route template suffix for resource-by-id endpoints.
	/// </summary>
	internal const string ById = "/{0}";

	/// <summary>
	/// The shared config route segment.
	/// </summary>
	internal const string Config = "/config";

	/// <summary>
	/// The shared schema route segment.
	/// </summary>
	internal const string Schema = "/schema";

	/// <summary>
	/// The backup endpoint route.
	/// </summary>
	internal const string Backup = System + "/backup";

	/// <summary>
	/// The calendar endpoint route.
	/// </summary>
	internal const string Calendar = "/calendar";

	/// <summary>
	/// The command endpoint route.
	/// </summary>
	internal const string Command = "/command";

	/// <summary>
	/// The disk space endpoint route.
	/// </summary>
	internal const string DiskSpace = "/diskspace";

	/// <summary>
	/// The delay profile endpoint route.
	/// </summary>
	internal const string DelayProfile = "/delayprofile";

	/// <summary>
	/// The download client endpoint route.
	/// </summary>
	internal const string DownloadClient = "/downloadclient";

	/// <summary>
	/// The download client by-id endpoint route template.
	/// </summary>
	internal const string DownloadClientById = DownloadClient + ById;

	/// <summary>
	/// The download client config endpoint route.
	/// </summary>
	internal const string DownloadClientConfig = Config + DownloadClient;

	/// <summary>
	/// The download client schema endpoint route.
	/// </summary>
	internal const string DownloadClientSchema = DownloadClient + Schema;

	/// <summary>
	/// The episode endpoint route.
	/// </summary>
	internal const string Episode = "/episode";

	/// <summary>
	/// The episode file endpoint route.
	/// </summary>
	internal const string EpisodeFile = "/episodefile";

	/// <summary>
	/// The filesystem endpoint route.
	/// </summary>
	internal const string FileSystem = "/filesystem";

	/// <summary>
	/// The history endpoint route.
	/// </summary>
	internal const string History = "/history";

	/// <summary>
	/// The history-by-series endpoint route.
	/// </summary>
	internal const string HistoryBySeries = History + "/series";

	/// <summary>
	/// The history-since endpoint route.
	/// </summary>
	internal const string HistorySince = History + "/since";

	/// <summary>
	/// The host config endpoint route.
	/// </summary>
	internal const string Host = Config + "/host";

	/// <summary>
	/// The indexer endpoint route.
	/// </summary>
	internal const string Indexer = "/indexer";

	/// <summary>
	/// The indexer by-id endpoint route template.
	/// </summary>
	internal const string IndexerById = Indexer + ById;

	/// <summary>
	/// The indexer options endpoint route.
	/// </summary>
	internal const string IndexerOptions = Config + Indexer;

	/// <summary>
	/// The indexer schema endpoint route.
	/// </summary>
	internal const string IndexerSchema = Indexer + "/schema";

	/// <summary>
	/// The language profile endpoint route.
	/// </summary>
	internal const string LanguageProfile = "/languageprofile";

	/// <summary>
	/// The language endpoint route.
	/// </summary>
	internal const string Language = "/language";

	/// <summary>
	/// The log endpoint route.
	/// </summary>
	internal const string Log = "/log";

	/// <summary>
	/// The log file endpoint route.
	/// </summary>
	internal const string LogFile = Log + "/file";

	/// <summary>
	/// The manual import endpoint route.
	/// </summary>
	internal const string ManualImport = "/manualimport";

	/// <summary>
	/// The remote path mapping endpoint route.
	/// </summary>
	internal const string RemotePathMapping = "/remotepathmapping";

	/// <summary>
	/// The media management config endpoint route.
	/// </summary>
	internal const string MediaManagement = Config + "/mediamanagement";

	/// <summary>
	/// The metadata endpoint route.
	/// </summary>
	internal const string Metadata = "/metadata";

	/// <summary>
	/// The naming config endpoint route.
	/// </summary>
	internal const string NamingConfig = Config + "/naming";

	/// <summary>
	/// The notification endpoint route.
	/// </summary>
	internal const string Notification = "/notification";

	/// <summary>
	/// The notification schema endpoint route.
	/// </summary>
	internal const string NotificationSchema = Notification + Schema;

	/// <summary>
	/// The quality profile endpoint route.
	/// </summary>
	internal const string QualityProfile = "/qualityprofile";

	/// <summary>
	/// The quality definition endpoint route.
	/// </summary>
	internal const string QualityDefinitions = "/qualitydefinition";

	/// <summary>
	/// The queue endpoint route.
	/// </summary>
	internal const string Queue = "/queue";

	/// <summary>
	/// The release endpoint route.
	/// </summary>
	internal const string Release = "/release";

	/// <summary>
	/// The release profile endpoint route.
	/// </summary>
	internal const string ReleaseProfile = Release + "profile";

	/// <summary>
	/// The remote path mapping endpoint route.
	/// </summary>
	internal const string RemotePath = "/remotepathmapping";

	/// <summary>
	/// The rename endpoint route.
	/// </summary>
	internal const string Rename = "/rename";

	/// <summary>
	/// The restart endpoint route.
	/// </summary>
	internal const string Restart = System + "/restart";

	/// <summary>
	/// The restriction endpoint route.
	/// </summary>
	internal const string Restriction = "/restriction";

	/// <summary>
	/// The root folder endpoint route.
	/// </summary>
	internal const string RootFolder = "/rootfolder";

	/// <summary>
	/// The series endpoint route.
	/// </summary>
	internal const string Series = "/series";

	/// <summary>
	/// The series lookup endpoint route.
	/// </summary>
	internal const string SeriesLookup = Series + "/lookup";

	/// <summary>
	/// The system status endpoint route.
	/// </summary>
	internal const string Status = System + "/status";

	/// <summary>
	/// The system endpoint route.
	/// </summary>
	internal const string System = "/system";

	/// <summary>
	/// The tag endpoint route.
	/// </summary>
	public static readonly string Tag = "/tag";

	/// <summary>
	/// The update endpoint route.
	/// </summary>
	public static readonly string Update = "/update";

	/// <summary>
	/// The wanted missing endpoint route.
	/// </summary>
	public static readonly string WantedMissing = "/wanted/missing";

	/// <summary>
	/// The calendar date-time format string used by Sonarr APIs.
	/// </summary>
	public static readonly string CalendarDateTimeFormat = "yyyy-MM-ddTHH:mm:ss";
}

/// <summary>
/// Provides metadata key constants that identify Sonarr resource metadata types.
/// </summary>
public static class Meta
{
	/// <summary>
	/// The metadata key for backup resources.
	/// </summary>
	internal const string Backup = "#backup";

	/// <summary>
	/// The metadata key for calendar resources.
	/// </summary>
	internal const string Calendar = "#calendar";

	/// <summary>
	/// The metadata key for command resources.
	/// </summary>
	internal const string Command = "#command";

	/// <summary>
	/// The metadata key for delay profile resources.
	/// </summary>
	internal const string DelayProfile = "#delay_profile";

	/// <summary>
	/// The metadata key for disk resources.
	/// </summary>
	internal const string Disk = "#disk";

	/// <summary>
	/// The metadata key for download client resources.
	/// </summary>
	internal const string DownloadClient = "#download_client";

	/// <summary>
	/// The metadata key for download client config resources.
	/// </summary>
	internal const string DownloadClientConfig = "#download_client_config";

	/// <summary>
	/// The metadata key for download client schema resources.
	/// </summary>
	internal const string DownloadClientSchema = "#download_client_schema";

	/// <summary>
	/// The metadata key for episode resources.
	/// </summary>
	internal const string Episode = "#episode";

	/// <summary>
	/// The metadata key for episode file resources.
	/// </summary>
	internal const string EpisodeFile = "#episode_file";

	/// <summary>
	/// The metadata key for history item resources.
	/// </summary>
	internal const string History = "#history_item";

	/// <summary>
	/// The metadata key for history-since resources.
	/// </summary>
	internal const string HistorySince = "#history_since";

	/// <summary>
	/// The metadata key for host config resources.
	/// </summary>
	internal const string HostConfig = "#host_config";

	/// <summary>
	/// The metadata key for indexer resources.
	/// </summary>
	internal const string Indexer = "#indexer";

	/// <summary>
	/// The metadata key for language profile resources.
	/// </summary>
	internal const string LanguageProfile = "#language_profile";

	/// <summary>
	/// The metadata key for log file resources.
	/// </summary>
	internal const string LogFile = "#log_file";

	/// <summary>
	/// The metadata key for log item resources.
	/// </summary>
	internal const string LogItem = "#log_item";

	/// <summary>
	/// The metadata key for manual import resources.
	/// </summary>
	internal const string ManualImport = "#manual_import";

	/// <summary>
	/// The metadata key for media management resources.
	/// </summary>
	internal const string MediaManagement = "#media_management";

	/// <summary>
	/// The metadata key for naming config resources.
	/// </summary>
	internal const string NamingConfig = "#naming_config";

	/// <summary>
	/// The metadata key for notification resources.
	/// </summary>
	internal const string Notification = "#notification";

	/// <summary>
	/// The metadata key for quality resources.
	/// </summary>
	internal const string Quality = "#quality";

	/// <summary>
	/// The metadata key for quality definition resources.
	/// </summary>
	internal const string QualityDefinition = "#quality_definition";

	/// <summary>
	/// The metadata key for quality profile resources.
	/// </summary>
	internal const string QualityProfile = "#quality_profile";

	/// <summary>
	/// The metadata key for release resources.
	/// </summary>
	internal const string Release = "#release";

	/// <summary>
	/// The metadata key for release profile resources.
	/// </summary>
	internal const string ReleaseProfile = "#release_profile";

	/// <summary>
	/// The metadata key for remote path mapping resources.
	/// </summary>
	internal const string RemotePathMapping = "#remote_path_mapping";

	/// <summary>
	/// The metadata key for renamable file resources.
	/// </summary>
	internal const string Renamable = "#rename_file";

	/// <summary>
	/// The metadata key for quality revision resources.
	/// </summary>
	internal const string QualityRevision = "#quality_revision";

	/// <summary>
	/// The metadata key for root folder resources.
	/// </summary>
	internal const string RootFolder = "#root_folder";

	/// <summary>
	/// The metadata key for series resources.
	/// </summary>
	internal const string Series = "#series";

	/// <summary>
	/// The metadata key for add-series resources.
	/// </summary>
	internal const string SeriesAdd = "#add_series";

	/// <summary>
	/// The metadata key for series history resources.
	/// </summary>
	internal const string SeriesHistory = "#history_series";

	/// <summary>
	/// The metadata key for system status resources.
	/// </summary>
	internal const string Status = "#system_status";

	/// <summary>
	/// The metadata key for tag resources.
	/// </summary>
	internal const string Tag = "#tag";
}
