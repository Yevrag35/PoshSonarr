# PoshSonarr

[![version](https://img.shields.io/powershellgallery/v/PoshSonarr.svg)](https://www.powershellgallery.com/packages/PoshSonarr)
[![downloads](https://img.shields.io/powershellgallery/dt/PoshSonarr.svg?label=downloads)](https://www.powershellgallery.com/stats/packages/PoshSonarr?groupby=Version)

This will eventually be a complete PowerShell module for quickly issuing [Sonarr PVR API](https://github.com/Sonarr/Sonarr/wiki/API) calls. The module was completely redesigned from the ground up in PowerShell Core, and I'm happy to say it works (has been tested) in Ubuntu 16.04 and Ubuntu 18.04 as well as Windows.

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

As of 8/20/19, I'm missing only the following commands:

1. DownloadedEpisodesScan
1. MissingEpisodeSearch
1. RenameFiles
1. RenameSeries

The following is the list of the working cmdlets:

* _Commands in __bold__ are new in __v1.2.0___

1. Add-SonarrRelease
1. Add-SonarrSeries
1. Connect-SonarrInstance (Connect-Sonarr)
1. Get-SonarrBackup
1. Get-SonarrCalendar
1. Get-SonarrCommand
1. Get-SonarrDiskspace
1. __Get-SonarrDownloadClient__
1. Get-SonarrEpisode
1. Get-SonarrEpisodeFile
1. Get-SonarrHistory
1. __Get-SonarrLog__
1. __Get-SonarrLogFile__
1. __Get-SonarrMapping__
1. Get-SonarrQualityProfile
1. Get-SonarrQueue
1. Get-SonarrRelease - * _renamed to Search-SonarrRelease (left behind as alias...)_ *
1. __Get-SonarrRestriction__
1. Get-SonarrRootFolder
1. Get-SonarrSeries
1. Get-SonarrStatus
1. Get-SonarrTag
1. __Get-SonarrUpdate__
1. Get-SonarrWantedMissing
1. Invoke-SonarrBackup (Backup-Sonarr)
1. Invoke-SonarrEpisodeSearch
1. Invoke-SonarrRssSync
1. Invoke-SonarrSeasonSearch
1. Invoke-SonarrSeriesRefresh
1. Invoke-SonarrSeriesRescan
1. Invoke-SonarrSeriesSearch
1. New-SonarrReleasePush
1. __New-SonarrRestriction__
1. New-SonarrTag
1. Remove-SonarrEpisodeFile
1. __Remove-SonarrRestriction__
1. Remove-SonarrSeries
1. Remove-SonarrTag
1. __Save-SonarrLogFile__
1. __Search-SonarrDirectory__
1. __Search-SonarrRelease__
1. Search-SonarrSeries
1. Set-SonarrEpisode
1. __Set-SonarrRestriction__
1. Set-SonarrTag
1. __Update-SonarrRestriction__
1. Update-SonarrSeries
