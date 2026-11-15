# <img height="30px" src="./.img/PoshSonarr.png" alt="PoshSonarr"></img> PoshSonarr-NEXT

[![version](https://img.shields.io/powershellgallery/v/PoshSonarr.svg?include_prereleases)](https://www.powershellgallery.com/packages/PoshSonarr)
[![downloads](https://img.shields.io/powershellgallery/dt/PoshSonarr.svg?label=downloads)](https://www.powershellgallery.com/stats/packages/PoshSonarr?groupby=Version)
[![Codacy Badge](https://app.codacy.com/project/badge/Grade/607129c30165413e816aa0cdad1845d0)](https://app.codacy.com/gh/Yevrag35/PoshSonarr/dashboard?utm_source=gh&utm_medium=referral&utm_content=&utm_campaign=Badge_grade)
[![DeepWiki](https://img.shields.io/badge/DeepWiki-Yevrag35%2FPoshSonarr-blue.svg?logo=data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAACwAAAAyCAYAAAAnWDnqAAAAAXNSR0IArs4c6QAAA05JREFUaEPtmUtyEzEQhtWTQyQLHNak2AB7ZnyXZMEjXMGeK/AIi+QuHrMnbChYY7MIh8g01fJoopFb0uhhEqqcbWTp06/uv1saEDv4O3n3dV60RfP947Mm9/SQc0ICFQgzfc4CYZoTPAswgSJCCUJUnAAoRHOAUOcATwbmVLWdGoH//PB8mnKqScAhsD0kYP3j/Yt5LPQe2KvcXmGvRHcDnpxfL2zOYJ1mFwrryWTz0advv1Ut4CJgf5uhDuDj5eUcAUoahrdY/56ebRWeraTjMt/00Sh3UDtjgHtQNHwcRGOC98BJEAEymycmYcWwOprTgcB6VZ5JK5TAJ+fXGLBm3FDAmn6oPPjR4rKCAoJCal2eAiQp2x0vxTPB3ALO2CRkwmDy5WohzBDwSEFKRwPbknEggCPB/imwrycgxX2NzoMCHhPkDwqYMr9tRcP5qNrMZHkVnOjRMWwLCcr8ohBVb1OMjxLwGCvjTikrsBOiA6fNyCrm8V1rP93iVPpwaE+gO0SsWmPiXB+jikdf6SizrT5qKasx5j8ABbHpFTx+vFXp9EnYQmLx02h1QTTrl6eDqxLnGjporxl3NL3agEvXdT0WmEost648sQOYAeJS9Q7bfUVoMGnjo4AZdUMQku50McDcMWcBPvr0SzbTAFDfvJqwLzgxwATnCgnp4wDl6Aa+Ax283gghmj+vj7feE2KBBRMW3FzOpLOADl0Isb5587h/U4gGvkt5v60Z1VLG8BhYjbzRwyQZemwAd6cCR5/XFWLYZRIMpX39AR0tjaGGiGzLVyhse5C9RKC6ai42ppWPKiBagOvaYk8lO7DajerabOZP46Lby5wKjw1HCRx7p9sVMOWGzb/vA1hwiWc6jm3MvQDTogQkiqIhJV0nBQBTU+3okKCFDy9WwferkHjtxib7t3xIUQtHxnIwtx4mpg26/HfwVNVDb4oI9RHmx5WGelRVlrtiw43zboCLaxv46AZeB3IlTkwouebTr1y2NjSpHz68WNFjHvupy3q8TFn3Hos2IAk4Ju5dCo8B3wP7VPr/FGaKiG+T+v+TQqIrOqMTL1VdWV1DdmcbO8KXBz6esmYWYKPwDL5b5FA1a0hwapHiom0r/cKaoqr+27/XcrS5UwSMbQAAAABJRU5ErkJggg==)](https://deepwiki.com/Yevrag35/PoshSonarr)

Introducing a completely, redesigned, overhauled version of PoshSonarr.  In order to become the flexible module that I envisioned, I had to take a completely new approach.  Gone are the static types and the needlessly complex, rigid architecture of the previous cmdlets.  The NEXT cmdlets are lighter, more error-resistant, and more flexible in accounting for the changes the [Sonarr PVR API](https://sonarr.tv/docs/api) brings in every update.

## COMPATIBILITY

In addition to this, I am making the executive decision to only write the NEXT versions of the module for [PowerShell 7](https://github.com/PowerShell/PowerShell/releases/latest) and up (previously known as PowerShell Core).  Doing so will allow me to focus on making the library easier to develop and to take advantage of some key functionality that PowerShell 7's SDK brings to the table.

## Using

To get started, connect to Sonarr with "Connect-SonarrInstance" _(Connect-Sonarr)_ cmdlet:

```powershell
# By URL -- (Sonarr's default port is 8989)
Connect-Sonarr -Url "http://MEDIASERVER:8989" -ApiKey "xxxxxxxxxxxxxxxx"

# The cmdlet writes nothing unless you add "-PassThru", which writes the server's status.
Connect-Sonarr -Url "https://sonarr-api.cloud.com" -ApiKey "xxxxxxxxxxxx" -PassThru

# If you have a reverse proxy that strips away the "/api" path, use the "-NoApiInPath" switch (alias "-NoApiPrefix").
Connect-Sonarr -Url "http://sonarr:8989/sonarr" -ApiKey "xxxxxxxxxxxx" -NoApiInPath
```

[See the wiki](https://github.com/Yevrag35/PoshSonarr/wiki) for more information about reverse proxy situations.

An example of some commands in action:

```powershell
# Set Season 3 of a series to 'Not Monitored'
$series = Get-SonarrSeries -Name 24
$series.Seasons[2].Monitored = $false
$series | Update-SonarrSeries

# Search for a particular series and add the best match to Sonarr
$qualityProfile = Get-SonarrQualityProfile -Name "HD-1080p"
Search-SonarrSeries "The X-Files" |
	Select-Object -First 1 |
	Add-SonarrSeries -RootFolderPath "\\NAS\Shows" -QualityProfileId $qualityProfile.Id -SearchForMissingEpisodes IgnoreWithEpisodes -UseSeasonFolders
```

---

## Cmdlets

The following is the list of the working cmdlets:

1. Add-SonarrRelease
1. Add-SonarrSeries
1. Add-SonarrTag
1. Clear-SonarrTag
1. [Connect-SonarrInstance](https://github.com/Yevrag35/PoshSonarr/wiki/Connect-SonarrInstance) - _(Connect-Sonarr)_
1. Disconnect-SonarrInstance - _(Disconnect-Sonarr)_
1. Edit-SonarrManualImport
1. [Get-SonarrBackup](https://github.com/Yevrag35/PoshSonarr/wiki/Get-SonarrBackup)
1. Get-SonarrCalendar
1. Get-SonarrCommand
1. Get-SonarrCommandHistory
1. Get-SonarrDelayProfile
1. Get-SonarrDiskspace
1. Get-SonarrDownloadClient
1. Get-SonarrDownloadClientConfig
1. Get-SonarrDownloadClientSchema
1. Get-SonarrEpisode
1. Get-SonarrEpisodeFile
1. Get-SonarrHistory
1. Get-SonarrHostConfig - _(Get-SonarrHost)_
1. Get-SonarrIndexer
1. Get-SonarrLanguageProfile
1. Get-SonarrLog
1. Get-SonarrLogFile
1. Get-SonarrManualImport
1. Get-SonarrMediaManagement
1. Get-SonarrNamingConfig
1. Get-SonarrNotification
1. Get-SonarrNotificationSchema
1. Get-SonarrObjectProperty - _(Get-SonarrObjectProperties/Get-SonarrProperties)_
1. Get-SonarrQualityDefinition - _(Get-SonarrQuality)_
1. Get-SonarrQualityProfile - _(Get-SonarrProfile)_
1. Get-SonarrReleaseProfile
1. Get-SonarrRemotePathMapping - _(Get-SonarrRemoteMapping/Get-SonarrRemotePath)_
1. Get-SonarrRootFolder
1. Get-SonarrSeries
1. Get-SonarrStatus
1. Get-SonarrTag
1. Get-SonarrWantedMissing
1. Invoke-SonarrManualImport
1. Invoke-SonarrRename - _(Rename-SonarrEpisodeFile)_
1. Invoke-SonarrRssSync
1. New-SonarrDelayProfile
1. New-SonarrDownloadClient
1. New-SonarrReleaseProfile
1. New-SonarrRemotePathMapping - _(New-SonarrRemoteMapping/New-SonarrRemotePath)_
1. New-SonarrRootFolder
1. New-SonarrTag
1. Remove-SonarrBackup - _(Delete-SonarrBackup)_
1. Remove-SonarrDelayProfile
1. Remove-SonarrDownloadClient
1. Remove-SonarrEpisodeFile - _(Delete-SonarrEpisodeFile)_
1. Remove-SonarrIndexer
1. Remove-SonarrNotification
1. Remove-SonarrReleaseProfile
1. Remove-SonarrRemotePathMapping
1. Remove-SonarrRootFolder
1. Remove-SonarrSeries - _(Delete-SonarrSeries)_
1. Remove-SonarrTag - _(Delete-SonarrTag)_
1. Rename-SonarrTag
1. Restart-SonarrInstance - _(Restart-Sonarr)_
1. Save-SonarrBackup - _(Download-SonarrBackup)_
1. Save-SonarrLogFile - _(Download-SonarrLogFile)_
1. Search-SonarrRelease
1. Search-SonarrSeries
1. Show-SonarrException - _(Show-SonarrError)_
1. Test-SonarrHost - _(Test-Sonarr)_
1. Test-SonarrRename
1. Test-SonarrResource
1. Update-SonarrDelayProfile
1. Update-SonarrDownloadClient
1. Update-SonarrEpisode
1. Update-SonarrHostConfig - _(Set-SonarrHost/Set-SonarrHostConfig/Update-SonarrHost)_
1. Update-SonarrIndexer - _(Set-SonarrIndexer)_
1. Update-SonarrMediaManagement
1. Update-SonarrNamingConfig
1. Update-SonarrNotification
1. Update-SonarrReleaseProfile
1. Update-SonarrRemotePathMapping - _(Set-SonarrRemoteMapping/Set-SonarrRemotePathMapping/Update-SonarrRemoteMapping)_
1. Update-SonarrSeries - _(Set-SonarrSeries)_
