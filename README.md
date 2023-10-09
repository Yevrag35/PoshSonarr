# <img height="30px" src="./.img/PoshSonarr.png" alt="PoshSonarr"></img> PoshSonarr-NEXT

[![version](https://img.shields.io/powershellgallery/v/PoshSonarr.svg?include_prereleases)](https://www.powershellgallery.com/packages/PoshSonarr)
[![downloads](https://img.shields.io/powershellgallery/dt/PoshSonarr.svg?label=downloads)](https://www.powershellgallery.com/stats/packages/PoshSonarr?groupby=Version)
[![Codacy Badge](https://app.codacy.com/project/badge/Grade/607129c30165413e816aa0cdad1845d0)](https://app.codacy.com/gh/Yevrag35/PoshSonarr/dashboard?utm_source=gh&utm_medium=referral&utm_content=&utm_campaign=Badge_grade)

Introducing a completely, redesigned, overhauled version of PoshSonarr.  In order to become the flexible module that I envisioned, I had to take a completely new approach.  Gone are the static types and the needlessly complex, rigid architecture of the previous cmdlets.  The NEXT cmdlets are lighter, more error-resistant, and more flexible in accounting for the changes the [Sonarr PVR API](https://sonarr.tv/docs/api) brings in every update.

## COMPATIBILITY

In addition to this, I am making the executive decision to only write the NEXT versions of the module for [PowerShell 7](https://github.com/PowerShell/PowerShell/releases/latest) and up (previously known as PowerShell Core).  Doing so will allow me to focus on making the library easier to develop and to take advantage of some key functionality that PowerShell 7's SDK brings to the table.

## Using

To get started, connect to Sonarr with "Connect-SonarrInstance" _(Connect-Sonarr)_ cmdlet:

```powershell
# By Server and Port -- (8989 is the default port)
Connect-Sonarr -Server "MEDIASERVER" -ApiKey "xxxxxxxxxxxxxxxx"

   Version Authentication Url                        StartupPath
   ------- -------------- ---                        -----------
2.0.0.5322      forms      http://MEDIASERVER:8989/   C:\ProgramData\NzbDrone\bin

# By explicit URL
Connect-Sonarr -Url "https://sonarr-api.cloud.com" -ApiKey "xxxxxxxxxxxx"

# If you have a reverse proxy that strips away the "/api" path, use the "-NoApiPrefix" switch.

Connect-Sonarr -Url "http://sonarr:8989/sonarr" ...
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

The following is the list of the working cmdlets:

1. Add-SonarrRelease
1. Add-SonarrSeries
1. Add-SonarrTag
1. Clear-SonarrLog
1. [Connect-SonarrInstance](https://github.com/Yevrag35/PoshSonarr/wiki/Connect-SonarrInstance) - _(Connect-Sonarr)_
1. [Get-SonarrBackup](https://github.com/Yevrag35/PoshSonarr/wiki/Get-SonarrBackup)
1. Get-SonarrCalendar
1. Get-SonarrCommand - _(Get-SonarrJob)_
1. Get-SonarrDelayProfile
1. Get-SonarrDiskspace
1. Get-SonarrDownloadClient
1. Get-SonarrEpisode
1. Get-SonarrEpisodeFile
1. Get-SonarrHistory
1. Get-SonarrHostConfig
1. Get-SonarrLog
1. Get-SonarrLogFile
1. Get-SonarrMapping
1. Get-SonarrMediaManagement
1. Get-SonarrMetadata
1. Get-SonarrNotification - _(Get-SonarrConnection)_
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
1. New-SonarrDelayProfile
1. New-SonarrMapping
1. New-SonarrPlexNotification - _(New-PlexNotification)_
1. New-SonarrQualityProfile - _(New-SonarrProfile)_
1. New-SonarrReleasePush
1. New-SonarrRestriction
1. New-SonarrTag
1. Register-SonarrRootFolder _(New-SonarrRootFolder)_
1. Remove-SonarrEpisodeFile
1. Remove-SonarrMapping
1. Remove-SonarrQualityProfile - _(Remove-SonarrProfile)_
1. Remove-SonarrQueueItem
1. Remove-SonarrRestriction
1. Remove-SonarrRootFolder
1. Remove-SonarrSeries
1. Remove-SonarrTag
1. Rename-SonarrTag - _* (was Set-SonarrTag)_
1. Restart-SonarrInstance - _(Restart-Sonarr)_
1. Save-SonarrLogFile
1. Search-SonarrDirectory
1. Search-SonarrRelease
1. Search-SonarrSeries
1. Set-SonarrDelayProfile
1. Set-SonarrEpisode
1. Set-SonarrHostConfig
1. Set-SonarrMapping
1. Set-SonarrRestriction
1. Set-SonarrSeries - _* (was Update-SonarrSeries)_
1. Set-SonarrTag - _* (renamed to Rename-SonarrTag; left behind as alias)_
1. Update-SonarrDownloadClient
1. Update-SonarrMediaManagement
1. Update-SonarrMetadata
1. Update-SonarrQualityProfile _(Set-SonarrQualityProfile/Update-SonarrProfile/Set-SonarrProfile)_
1. Update-SonarrRestriction
1. Update-SonarrSeries - _* (renamed to Set-SonarrSeries; left behind as alias)_
