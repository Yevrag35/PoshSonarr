# PoshSonarr

This will eventually be a complete PowerShell module for quickly issuing [Sonarr PVR API](https://github.com/Sonarr/Sonarr/wiki/API) calls.

The module can be found in the [PoshSonarr](https://github.com/Yevrag35/PoshSonarr/tree/master/PoshSonarr) folder and contains the following commands:

1. Backup-Sonarr
1. Connect-Sonarr
1. Get-SonarrBackup
1. Get-SonarrCalendar
1. Get-SonarrCommandResult -- __\*NEW\*__
1. Get-SonarrDisk -- __\*NEW\*__
1. Get-SonarrEpisode
1. Get-SonarrEpisodeFile -- __\*NEW\*__
1. Get-SonarrSeries
1. Get-SonarrStatus
1. Get-SonarrWantedMissing -- __\*NEW\*__
1. Remove-SonarrEpisodeFile -- __\*NEW\*__
1. Rename-SonarrSeriesFiles -- __\*NEW\*__
1. Start-SonarrRefresh
1. Start-SonarrRescan
1. Start-SonarrRssSync
1. Start-SonarrSearch -- __\*NEW\*__

---

## What I'm working on...

I have re-architected my MG.Api assembly to allow for easier JSON object translation, with the result being that I should be able
to crank out cmdlets at a higher clip than before.

The following 'object' results have been __created__.  While their commands and endpoints are still in progress, the corresponding object has been finished:

* Season -- _available from 'Series'_
  * SeasonStatistics
* EpisodeFile -- _available from 'Episode' and 'CalendarEntry'_
  * EpisodeQuality
  * MediaInfo

Below is the list of endpoints:
_* Striken text signifies completion. *_

* ~~Calendar~~
* Command
  * ~~RefreshSeries~~
  * ~~RescanSeries~~
  * ~~EpisodeSearch~~
  * ~~SeasonSearch~~
  * ~~SeriesSearch~~
  * DownloadedEpisodesScan
  * ~~RssSync~~
  * RenameFiles  _-- (having api issues)_
  * ~~RenameSeries~~
  * ~~Backup~~
* ~~Diskspace~~
* ~~Episode~~
* ~~EpisodeFile~~
* History
* Images       -- _(object is done)_
* ~~Wanted-Missing~~
* Queue
* Parse
* Profile
* Release
* Release Push
* Rootfolder
* ~~Series~~
* Series Lookup
* ~~System-Status~~
* ~~System-Backup~~
