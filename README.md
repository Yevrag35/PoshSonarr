# PoshSonarr

[![version](https://img.shields.io/powershellgallery/v/PoshSonarr.svg?include_prereleases)](https://www.powershellgallery.com/packages/PoshSonarr)
[![downloads](https://img.shields.io/powershellgallery/dt/PoshSonarr.svg?label=downloads)](https://www.powershellgallery.com/stats/packages/PoshSonarr?groupby=Version)

This will eventually be a complete PowerShell module for quickly issuing [Sonarr PVR API](https://github.com/Sonarr/Sonarr/wiki/API) calls. The module was completely redesigned from the ground up in PowerShell Core, and I'm happy to say it works (has been tested) in Ubuntu 16.04 and Ubuntu 18.04 as well as Windows.

# CHANGELOG

## 1.3.0 (Release)

* Includes all the changes from the prerelease versions.
* Fixed these issues:
   * [Issue#12](https://github.com/Yevrag35/PoshSonarr/issues/12) - Get-SonarrEpisode -SeriesId invalid parameter binding
   * [Issue#13](https://github.com/Yevrag35/PoshSonarr/issues/13) - Get-SonarrCalendar -Today displays episode airing at midnight tomorrow

## 1.3.0-gamma

_(3/18/2020)_

* Fix major bug with <code>Connect-SonarrInstance</code> that has existed since 'beta'.  When <code>-NoApiPrefix</code> is not used, the TagManager object creation during connection no longer shows an error and tag management will work again.

## 1.3.0-delta

_(3/16/2020)_

* Fixed mandatory parameter requirement for <code>Get-SonarrCalendar</code>.  DayOfWeek is no longer required (as it should never have been...).
* <code>Get-SonarrTag</code> no longer returns all tags when used in the pipeline.
* <code>Get-SonarrSize</code> has been removed.  (It was no longer needed)
* NEW CMDLET:
   * __Update-SonarrQualityProfile__ _(Set-SonarrQualityProfile/Update-SonarrProfile/Set-SonarrProfile)_

## 1.3.0-beta

_(3/10/2020)_

* __Dropping support for PowerShell 6 in favor of 7__ - v1.3.0 will be the last version to include the binaries for running on PowerShell 6.x.x versions.  To use the newer versions going forward, upgrade to [PowerShell 7](https://github.com/PowerShell/PowerShell/releases/latest).
* NEW CMDLETS:
   * __Get-SonarrSize__ _(Returns the total file size of a Series, Season, or EpisodeFile)_
   * __New-SonarrDelayProfile__
   * __New-SonarrPlexNotification__ _(New-PlexConnection)_
   * __Restart-SonarrInstance__ _(Restart-Sonarr)_
   * __Set-SonarrDelayProfile__
   * __Update-SonarrDownloadClient__

* TAGS (Better Tag Management)
   * DelayProfile, Notification (Connection), Restriction, and Series objects can have their tags queried, added to, changed, or removed now very easily with:
      * <code>Add-SonarrTag</code>
      * <code>Get-SonarrTag</code>
      * <code>Remove-SonarrTag</code>
   ```powershell
   Get-SonarrSeries "brooklyn*" | Add-SonarrTag -Tag "my favorite"
   Get-SonarrSeries "*Agents of S*" | Remove-SonarrTag -ClearAll
   ```

* <code>Add-SonarrSeries</code> now accepts a <code>[SearchSeries]</code> object from the pipeline, instead of properties by name.  It also adds a parameter, <code>-Type</code> to denote the type of series ("Anime", "Daily", "Standard").
* Added <code>-Today</code> and <code>-Tomorrow</code> switch parameters to <code>Get-SonarrCalendar</code>.
* Series, Episode, and EpisodeFile ID's are now treated as <code>[int]</code> value types.
* <code>Get-SonarrEpisode</code> has new switches for filtering results: <code>-Downloaded</code> and <code>-HasAired</code>.
* <code>Get-SonarrEpisodeFile</code> is now piped from an <code>[EpisodeResult]</code> instead of by property name.
* <code>Get-SonarrLog</code> has added lots of new parameters for custom filtering log entries.
* Changed <code>Get-SonarrQualityProfile</code>'s <code>-Id</code> parameter to be <code>-ProfileId</code> to allow for ValueFromPipelineByPropertyName to work properly.
* Changed <code>Remove-SonarrSeries</code> to accept a <code>[SeriesResult]</code> from the pipeline as opposed to using values from properties.
* <code>Search-SonarrDirectory</code> has a new switch parameter: <code>-ExcludeFiles</code>.  This tells the query to only return directories.
* PowerShell format file changes.

## 1.3.0-alpha

_(1/13/2020)_

Version 1.3.0 will be a major update (even though it's a minor revision :P) as the entire backend for issuing the API calls was overhauled.  The biggest deal was that now every command will have a WriteDebug part prior to issuing __any__ API request.  It also simplified the requesting process and did away the need for explicit type casting on the responses.

Another big error of improvement was the handling of exceptions that may occur on the server end.  Before, exceptions may have been cryptic/vague rendering them utterly useless.  Now, exceptions are either thrown directly, or the exception response(s) from the API request are directly translated to PowerShell ErrorRecord(s).

In addition to the backend changes, improvements were made to the cmdlets as well as introducing new cmdlets.  There are a total of __18 new cmdlets__ in this release (see __Cmdlets__ section for the complete list).

## Using

To get started, connect to Sonarr with "Connect-SonarrInstance" _(Connect-Sonarr)_ cmdlet:

```powershell
# By Server and Port -- (8989 is the default port)
Connect-Sonarr -Server "MEDIASERVER" -ApiKey "xxxxxxxxxxxxxxxx" -PassThru

   Version Authentication Url                        StartupPath
   ------- -------------- ---                        -----------
2.0.0.5322      forms      http://MEDIASERVER:8989/   C:\ProgramData\NzbDrone\bin

# By explicit URL
Connect-Sonarr -Url "https://sonarr-api.cloud.com" -ApiKey "xxxxxxxxxxxx" -PassThru

# If you have a reverse proxy that strips away the "/api" path, use the "-NoApiPrefix" switch.

# If you have a reverse proxy URL base set, include that in the -SonarrUrl parameter or use the new "-ReverseProxyUrlBase" parameter when combined with a HostName.

Connect-Sonarr -Url "http://sonarr:8989/sonarr" ...
# or
Connect-Sonarr -HostName "mediaserver" -ReverseProxyUrlBase "sonarr" ...
```

[See the wiki](https://github.com/Yevrag35/PoshSonarr/wiki) for more information about reverse proxy situations.

An example of some commands in action:

```powershell
# Set Season 3 of a series to 'Not Monitored'
$series = Get-SonarrSeries -Name 24
$series.Seasons[2].Monitored = $false
$series | Update-SonarrSeries

# Search for a particular series and add it to Sonarr
Search-SonarrSeries "The X-Files" | Add-SonarrSeries -RootFolderPath "\\NAS\Shows" -IgnoreEpisodesWithFiles -SearchForMissingEpisodes -UseSeasonFolders
```

---

## Cmdlets

As of 1/13/20, I'm still missing the following commands:

1. DownloadedEpisodesScan
1. MissingEpisodeSearch
1. RenameFiles
1. RenameSeries

The following is the list of the working cmdlets:

* _Commands in __bold__ are new in __v1.3.0___
* _Aliases and notes are italicized_

1. Add-SonarrRelease
1. Add-SonarrSeries
1. __Add-SonarrTag__ - _* (still working on this one)_
1. __Clear-SonarrLog__
1. Connect-SonarrInstance - _(Connect-Sonarr)_
1. Get-SonarrBackup
1. Get-SonarrCalendar
1. Get-SonarrCommand - _(Get-SonarrJob)_
1. __Get-SonarrDelayProfile__
1. Get-SonarrDiskspace
1. Get-SonarrDownloadClient
1. Get-SonarrEpisode
1. Get-SonarrEpisodeFile
1. Get-SonarrHistory
1. __Get-SonarrHostConfig__
1. Get-SonarrLog
1. Get-SonarrLogFile
1. Get-SonarrMapping
1. __Get-SonarrMediaManagement__
1. __Get-SonarrMetadata__
1. __Get-SonarrNotification__ - _(Get-SonarrConnection)_
1. Get-SonarrQualityProfile - _(Get-SonarrProfile)_
1. Get-SonarrQueue
1. Get-SonarrRelease - * _renamed to Search-SonarrRelease (left behind as alias...)_
1. Get-SonarrRestriction
1. Get-SonarrRootFolder
1. Get-SonarrSeries
1. Get-SonarrStatus
1. Get-SonarrTag
1. Get-SonarrUpdate
1. Get-SonarrWantedMissing
1. Invoke-SonarrBackup - _(Backup-Sonarr)_
1. Invoke-SonarrEpisodeSearch
1. Invoke-SonarrRssSync
1. Invoke-SonarrSeasonSearch
1. Invoke-SonarrSeriesRefresh
1. Invoke-SonarrSeriesRescan
1. Invoke-SonarrSeriesSearch
1. __New-SonarrDelayProfile__
1. __New-SonarrMapping__
1. __New-SonarrPlexNotification__ - _(New-PlexNotification)_
1. __New-SonarrQualityProfile__ - _(New-SonarrProfile)_
1. New-SonarrReleasePush
1. New-SonarrRestriction
1. New-SonarrTag
1. __Register-SonarrRootFolder__ _(New-SonarrRootFolder)_
1. Remove-SonarrEpisodeFile
1. __Remove-SonarrMapping__
1. __Remove-SonarrQualityProfile__ - _(Remove-SonarrProfile)_
1. __Remove-SonarrQueueItem__
1. Remove-SonarrRestriction
1. __Remove-SonarrRootFolder__
1. Remove-SonarrSeries
1. Remove-SonarrTag
1. Rename-SonarrTag - _* (was Set-SonarrTag)_
1. __Restart-SonarrInstance__ - _(Restart-Sonarr)_
1. Save-SonarrLogFile
1. Search-SonarrDirectory
1. Search-SonarrRelease
1. Search-SonarrSeries
1. __Set-SonarrDelayProfile__
1. Set-SonarrEpisode
1. __Set-SonarrHostConfig__
1. __Set-SonarrMapping__
1. Set-SonarrRestriction
1. Set-SonarrSeries - _* (was Update-SonarrSeries)_
1. Set-SonarrTag - _* (renamed to Rename-SonarrTag; left behind as alias)_
1. __Update-SonarrDownloadClient__
1. __Update-SonarrMediaManagement__
1. __Update-SonarrMetadata__
1. Update-SonarrRestriction
1. Update-SonarrSeries - _* (renamed to Set-SonarrSeries; left behind as alias)_
