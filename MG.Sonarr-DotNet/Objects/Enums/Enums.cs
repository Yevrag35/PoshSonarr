using System;

namespace MG.Sonarr
{
    public enum AuthenticationType
    {
        None,
        Basic,
        Forms
    }

    public enum BackupType
    {
        Scheduled,
        Manual,
        Update
    }

    public enum CertificateValidationType
    {
        Enabled,
        DisabledForLocalAddresses,
        Disabled
    }

    public enum CommandPriority
    {
        Low = -1,
        Normal = 0,
        High = 1
    }

    public enum CommandStatus
    {
        Queued,
        Started,
        Completed,
        Failed,
        Aborted,
        Cancelled,
        Orphaned
    }

    public enum CommandTrigger
    {
        Unspecified,
        Manual,
        Scheduled
    }

    public enum CoverType
    {
        Banner,
        Fanart,
        Poster
    }

    public enum DownloadProtocol
    {
        Unknown,
        Usenet,
        Torrent
    }

    public enum EpisodeTitleRequiredType
    {
        Always,
        BulkSeasonReleases,
        Never
    }

    public enum FileDataType
    {
        None,
        LocalAirDate,
        UtcAirDate
    }

    public enum HealthCheckResult
    {
        Ok,
        Warning,
        Error
    }

    public enum HistoryEventType
    {
        Unknown,
        Grabbed,
        SeriesFolderImported,
        DownloadFolderImported,
        DownloadFailed,
        EpisodeFileDeleted,
        EpisodeFileRenamed
    }

    public enum ProperDownloadType
    {
        PreferAndUpgrade,
        DoNotUpgrade,
        DoNotPreferj
    }

    public enum ProxyType
    {
        Http,
        Socks4,
        Socks5
    }

    public enum RescanAfterRefreshType
    {
        Always,
        AfterManual,
        Never
    }

    public enum SeriesStatusType
    {
        Continuing,
        Ended
    }

    public enum SeriesType
    {
        Standard,
        Daily,
        Anime
    }

    public enum UpdateMechanism
    {
        BuiltIn = 0,
        Script = 1,
        External = 10,
        Apt = 11,
        Docker = 12
    }
}
