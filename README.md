# PoshSonarr

[![version](https://img.shields.io/powershellgallery/v/PoshSonarr.svg)](https://www.powershellgallery.com/packages/PoshSonarr)
[![downloads](https://img.shields.io/powershellgallery/dt/PoshSonarr.svg?label=downloads)](https://www.powershellgallery.com/stats/packages/PoshSonarr?groupby=Version)

The module was completely redesigned from the ground up in PowerShell Core, and I'm happy to say it works (has been tested) in Ubuntu 16.04 and Ubuntu 18.04 as well as Windows.

This will eventually be a complete PowerShell module for quickly issuing [Sonarr PVR API](https://github.com/Sonarr/Sonarr/wiki/API) calls.  As of 7/25/19, I'm missing only the following commands:

1. DownloadedEpisodesScan
1. MissingEpisodeSearch
1. RenameFiles
1. RenameSeries

The following is the list of the working cmdlets:

1. Add-SonarrRelease
1. Add-SonarrSeries
1. Connect-SonarrInstance (Connect-Sonarr)
1. Get-SonarrBackup
1. Get-SonarrCalendar
1. Get-SonarrCommand
1. Get-SonarrDiskspace
1. Get-SonarrEpisode
1. Get-SonarrEpisodeFile
1. Get-SonarrHistory
1. Get-SonarrQualityProfile
1. Get-SonarrQueue
1. Get-SonarrRelease
1. Get-SonarrRootFolder
1. Get-SonarrSeries
1. Get-SonarrStatus
1. Get-SonarrTag
1. Get-SonarrWantedMissing
1. Invoke-SonarrBackup (Backup-Sonarr)
1. Invoke-SonarrEpisodeSearch
1. Invoke-SonarrRssSync
1. Invoke-SonarrSeasonSearch
1. Invoke-SonarrSeriesRefresh
1. Invoke-SonarrSeriesRescan
1. Invoke-SonarrSeriesSearch
1. New-SonarrReleasePush
1. New-SonarrTag
1. Remove-SonarrEpisodeFile
1. Remove-SonarrSeries
1. Remove-SonarrTag
1. Search-SonarrSeries
1. Set-SonarrEpisode
1. Set-SonarrTag
1. Update-SonarrSeries