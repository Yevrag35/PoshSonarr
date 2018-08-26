# PoshSonarr

This will eventually be a PowerShell module for quickly issuing [Sonarr PVR API](https://github.com/Sonarr/Sonarr/wiki/API) calls.

## What I'm working on...

Currently, I'm making the framework for each api endpoint.  I believe I have the API calling class squared away.  Once the framework of the code is complete, then can begin the writing of a PowerShell module.

Below is the list of endpoints:
_* Striken text signifies completion. *_

* ~~Calendar~~
* Command
  * RefreshSeries
  * RescanSeries
  * EpisodeSearch
  * SeasonSearch
  * SeriesSearch
  * DownloadedEpisodesScan
  * RssSync
  * RenameFiles
  * RenameSeries
  * Backup
* Diskspace
* Episode
* EpisodeFile
* History
* Images
* Wanted-Missing
* Queue
* Parse
* Profile
* Release
* Release Push
* Rootfolder
* Series
* Series Lookup
* System-Status
* System-Backup