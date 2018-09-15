# PoshSonarr

This will eventually be a PowerShell module for quickly issuing [Sonarr PVR API](https://github.com/Sonarr/Sonarr/wiki/API) calls.

The project can now be built into a '.dll' module, and 6 commands are available:

1. Backup-Sonarr -- _*complete*_
1. Connect-Sonarr _*complete* __(reuqires ApiKey)___
1. Get-SonarrSeries -- _*complete*_
1. Get-SonarrCalendar -- _*complete*_
1. Start-SonarrRefresh -- _*complete*_
1. Start-SonarrRescan -- _*complete*_

## What I'm working on...

Currently, I'm making the framework for each api endpoint.  I believe I have the API calling class squared away.  Once the framework of the code is complete, then can begin the writing of a PowerShell module.

The following 'object' results have been __created__.  While their commands and endpoints are still in progress, the result object is finished:

* Season -- _available from 'Series'_
  * SeasonStatistics
* EpisodeFile -- _available from 'CalendarEntry'_
  * EpisodeQuality
  * MediaInfo

Below is the list of endpoints:
_* Striken text signifies completion. *_

* ~~Calendar~~
* Command
  * ~~RefreshSeries~~
  * ~~RescanSeries~~
  * EpisodeSearch
  * SeasonSearch
  * SeriesSearch
  * DownloadedEpisodesScan
  * RssSync
  * RenameFiles
  * RenameSeries
  * ~~Backup~~
* Diskspace
* Episode
* EpisodeFile  -- _(object is done)_
* History
* Images       -- _(object is done)_
* Wanted-Missing
* Queue
* Parse
* Profile
* Release
* Release Push
* Rootfolder
* ~~Series~~
* Series Lookup
* System-Status
* System-Backup
