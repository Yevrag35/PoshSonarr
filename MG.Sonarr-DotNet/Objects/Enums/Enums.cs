﻿using MG.Sonarr.Results;
using System;

namespace MG.Sonarr
{
    /// <summary>
    /// The set authentication being used by the Sonarr instance.
    /// </summary>
    public enum AuthenticationType
    {
        /// <summary>
        /// No authentication is being used.
        /// </summary>
        None,

        /// <summary>
        /// Basic authentication (username/password)
        /// </summary>
        Basic,

        /// <summary>
        /// Web page authentication (forms-based)
        /// </summary>
        Forms
    }

    /// <summary>
    /// The type of backup job that was performed.
    /// </summary>
    public enum BackupType
    {
        /// <summary>
        /// The backup job was performed from a schedule.
        /// </summary>
        Scheduled,

        /// <summary>
        /// The backup job was performed manually.
        /// </summary>
        Manual,

        /// <summary>
        /// The backup job was performed after installing an update.
        /// </summary>
        Update
    }

    public enum CertificateValidationType
    {
        Enabled,
        DisabledForLocalAddresses,
        Disabled
    }

    /// <summary>
    /// Specifies the priority of the command when submitted.
    /// </summary>
    public enum CommandPriority
    {
        /// <summary>
        /// Command executed with LOW priority.
        /// </summary>
        Low = -1,
        /// <summary>
        /// Command executed with NORMAL priority.
        /// </summary>
        Normal = 0,

        /// <summary>
        /// Command executed with HIGH priority.
        /// </summary>
        High = 1
    }

    /// <summary>
    /// The current status of a given command/job.
    /// </summary>
    public enum CommandStatus
    {
        /// <summary>
        /// The job is in queue, and will be executed when threads are freed.
        /// </summary>
        Queued,

        /// <summary>
        /// The job is currently running.
        /// </summary>
        Started,

        /// <summary>
        /// The job has completed successfully.
        /// </summary>
        Completed,

        /// <summary>
        /// The job has completed unsuccessfully.
        /// </summary>
        Failed,

        /// <summary>
        /// The job was stopped prematurely.
        /// </summary>
        Aborted,

        /// <summary>
        /// The job was stopped prematurely by a user.
        /// </summary>
        Cancelled,

        /// <summary>
        /// I don't fucking know...
        /// </summary>
        Orphaned
    }

    /// <summary>
    /// Describes how the command/job was started.
    /// </summary>
    public enum CommandTrigger
    {
        /// <summary>
        /// No execution history was found.
        /// </summary>
        Unspecified,

        /// <summary>
        /// The command was started manually.
        /// </summary>
        Manual,

        /// <summary>
        /// The command was started from a set schedule.
        /// </summary>
        Scheduled
    }

    /// <summary>
    /// The graphic cover type for a given <see cref="SeriesImage"/>.
    /// </summary>
    public enum CoverType
    {
        /// <summary>
        /// A banner image.
        /// </summary>
        Banner,

        /// <summary>
        /// The graphic's source is not from the studio.
        /// </summary>
        Fanart,

        /// <summary>
        /// A poster image.
        /// </summary>
        Poster
    }

    /// <summary>
    /// Determines the method of how a <see cref="DownloadClient"/> procures shows.
    /// </summary>
    public enum DownloadProtocol
    {
        /// <summary>
        /// Just that...
        /// </summary>
        Unknown,

        /// <summary>
        /// <see cref="DownloadClient"/> downloads from a Usenet source.
        /// </summary>
        Usenet,

        /// <summary>
        /// <see cref="DownloadClient"/> downloads from a torrent client.
        /// </summary>
        Torrent
    }

    public enum EpisodeTitleRequiredType
    {
        Always,
        BulkSeasonReleases,
        Never
    }

    /// <summary>
    /// The type of field from a given <see cref="DownloadClientSetting"/>.
    /// </summary>
    public enum FieldType
    {
        /// <summary>
        /// An obfuscated text box for entering sensitive information.
        /// </summary>
        Password,

        /// <summary>
        /// A multiple choice selector. 
        /// </summary>
        Select,

        /// <summary>
        /// A normal text box for entering text.
        /// </summary>
        TextBox
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

    /// <summary>
    /// The severity of a given log entry to filter by.
    /// </summary>
    public enum LogLevel
    {
        Fatal,
        Error,
        Warn,
        Info,
        Debug,
        Trace
    }

    public enum ProperDownloadType
    {
        PreferAndUpgrade,
        DoNotUpgrade,
        DoNotPrefer
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

    /// <summary>
    /// The current airing status for a given series.
    /// </summary>
    public enum SeriesStatusType
    {
        /// <summary>
        /// The series is on-going with episodes debuting in the near future.
        /// </summary>
        Continuing,

        /// <summary>
        /// The series has concluded with no new episodes debuting.
        /// </summary>
        Ended
    }

    /// <summary>
    /// The type classification for the given series.
    /// </summary>
    public enum SeriesType
    {
        /// <summary>
        /// Default.  The series airs with a weekly episode cadence.
        /// </summary>
        Standard,

        /// <summary>
        /// The series airs with a daily cadence.
        /// </summary>
        Daily,

        /// <summary>
        /// The series follows a regimen synonymous with Japanese anime.
        /// </summary>
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
