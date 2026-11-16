[CmdletBinding()]
param (
	[Parameter()]
	[string] $LibraryName = 'MG.Sonarr.Next.Shell',

	[Parameter()]
	[string] $ModuleName = 'PoshSonarr',

	[Parameter(Mandatory = $true)]
	[string] $OutputPath
)

# Every error ends the script, so pwsh exits with 1 and the build that runs the script fails instead of succeeding without
# a manifest.
$ErrorActionPreference = 'Stop'

$OutputPath = $OutputPath.Trim('"')
$manifestPath = "$OutputPath\$ModuleName.psd1"

# No build target writes the manifest, so a rebuild's clean leaves the last one in place. Removing it first means that a
# run that fails leaves no manifest beside the new build.
if (Test-Path -LiteralPath $manifestPath -PathType Leaf) {
	Remove-Item -LiteralPath $manifestPath -Force
}

$dllPath = "$OutputPath\$LibraryName.dll"
if (-not (Test-Path -Path $dllPath -PathType Leaf)) {
	throw "The specified module DLL was not found -> `"$dllPath`""
}

Import-Module $dllPath -ErrorAction Stop
$fileInfo = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($dllPath)
$productVers = [version]::new($fileInfo.ProductMajorPart, $fileInfo.ProductMinorPart, $fileInfo.ProductBuildPart)

$typesAndFormats = [MG.Sonarr.Next.Shell.Build.Module]::GetFormatsAndTypePaths($OutputPath)

$releaseNotes = @'
PoshSonarr 3.0.0

Breaking changes
- PoshSonarr requires PowerShell 7.7 or later, which runs on .NET 11. Version 2.0.0 required 7.5, and 3.0.0 doesn't import on PowerShell 7.6 or earlier.
- A cmdlet writes each error when it happens, and under -ErrorAction Stop the statement ends at the step that failed. 2.0.0 held errors back and wrote only the last one when the cmdlet ended, and a cmdlet whose setup failed still went on to its input.
- CmdletScopeNotReadyException, ContextNotSetException, InvalidApiKeyException, and SonarrParameterException moved from the MG.Sonarr.Next.Shell.Exceptions namespace to MG.Sonarr.Next.Exceptions.Shell, and InvalidEpisodeIdFormatException to MG.Sonarr.Next.Exceptions, so a catch block or an -is test that names one needs its new name.
- Get-SonarrHistory -EventType takes the name or number of a history event type, and tab completion lists the names. A name it doesn't know fails when it binds, where 2.0.0 ignored it and returned every event.
- A history record's Data holds the strings that Sonarr sends for the event. In 2.0.0 it was a release object, with TotalWeight and an Age time span.
- The disk objects that Get-SonarrDiskspace writes have the type name MG.Sonarr.Next.Models.System.DiskspaceObject in place of MG.Sonarr.Next.Models.System.Diskspace.
- The naming settings lose the SeparatorLength property, which was always 0, since Sonarr's naming settings have no separator.

New
- Test-SonarrHost, or Test-Sonarr, tests a URL and an API key with the check that Connect-SonarrInstance runs, and leaves the current connection as it is. It writes the server's status, or $true or $false under -Quiet, and takes -NoApiInPath, -SkipCertificateCheck, and -Timeout.
- Add-SonarrTag and Clear-SonarrTag take -Id along with piped objects. They and Get-SonarrTag also take series lookup results and schemas, which 2.0.0 turned away because they have no ID yet.

Fixes

Cmdlets that failed in 2.0.0
- Save-SonarrBackup and Invoke-SonarrRssSync work. In 2.0.0 they failed before sending anything.
- Update-SonarrNotification works. In 2.0.0 it failed on every run.
- Edit-SonarrManualImport takes an ID or an object for -Series, -Episode, and -Quality, which 2.0.0 never bound.
- Remove-SonarrSeries -DeleteFiles deletes the series' files, which 2.0.0 never asked Sonarr to do.
- Add-SonarrRelease writes Sonarr's response when it succeeds and an error when it fails, where 2.0.0 had the two swapped. It also sends a release whose guid looks like a number or a GUID, where 2.0.0 sent an empty guid.
- Get-SonarrTag with no arguments lists every tag, where 2.0.0 listed none.
- Get-SonarrWantedMissing sends -PageNumber, where 2.0.0 always got the first page.

Connecting
- Connect-SonarrInstance replaces the current connection, so a new URL or key takes effect, and a wrong one fails even while connected. A wrong key fails with a 401 error that carries the status, with or without -PassThru, where 2.0.0 gave a JSON parse error.
- A -Url with a URL base, such as https://host/sonarr, works for every request, the forms login, and downloads. 2.0.0 left the base out.
- An API key given as a SecureString works on Linux and macOS.
- Connect-SonarrInstance checks -Timeout when it binds, and the timeout has to be more than zero and at most about 24.8 days.
- Disconnect-SonarrInstance does nothing when there's no connection, where 2.0.0 threw a NullReferenceException.

-WhatIf, -Confirm, and -Force
- -Force skips only the confirmation prompts, so -WhatIf always wins. With -Force -WhatIf, 2.0.0 really ran Remove-SonarrDownloadClient, Remove-SonarrIndexer, Remove-SonarrTag, Remove-SonarrRootFolder, Remove-SonarrRemotePathMapping, Remove-SonarrDelayProfile, Remove-SonarrReleaseProfile, Remove-SonarrNotification, Update-SonarrMediaManagement, and Restart-SonarrInstance.
- Invoke-SonarrRename with -SeriesId or -EpisodeFileId, Invoke-SonarrRssSync, and Save-SonarrBackup honor -WhatIf and -Confirm.

Requests and errors
- The characters &, #, and + in search terms and paths reach Sonarr as typed. In 2.0.0, Search-SonarrSeries 'Law & Order' searched for "Law ".
- Sonarr's validation errors show Sonarr's messages, where 2.0.0 showed a JSON error or no detail.
- An unreachable server gives the error category ConnectionError, and a timeout OperationTimeout, for every request, with the URL in the message.
- Stopping a pipeline, such as with Ctrl+C, ends a request in flight. 2.0.0 waited until the request finished or timed out.
- A preference variable that a script or function sets to a string, such as $ErrorActionPreference = 'Stop', applies to the module's cmdlets.
- -Debug shows the body of every request that has one.
- Requests no longer deadlock in a host whose pipeline thread has a single-threaded synchronization context.
- Show-SonarrException finds the newest Sonarr error in $Error, including one that ended a statement under -ErrorAction Stop.

Pipeline input and episodes
- Get-SonarrTag, Get-SonarrQualityProfile, Get-SonarrEpisode, Get-SonarrEpisodeFile, Add-SonarrTag, Clear-SonarrTag, and Remove-SonarrTag act on the piped inputs that bound when another input fails to bind or another command writes an error. 2.0.0 skipped all of them.
- Get-SonarrSeries writes each piped object's series once, where 2.0.0 wrote the earlier objects' series again with each one.
- Get-SonarrEpisode and Get-SonarrEpisodeFile go on to the next piped series after a request fails, and Get-SonarrEpisodeFile sends its request on every run, not only the first.
- Get-SonarrEpisodeFile and Remove-SonarrEpisodeFile use a piped episode's file ID, and warn about an episode or a calendar entry that has no file.
- Episode identifiers accept S00 for specials, ranges of specials such as S00E01-02, and absolute ranges such as E25-28. One that can't be read, such as S01Ex or 10E05, fails with an error that names it, where 2.0.0 could take it for the whole season or for another episode.
- Update-SonarrEpisode doesn't send an air date of 0001-01-01 for an episode that has none.

Series, tags, and profiles
- Remove-SonarrSeries writes a warning that names the series, instead of an error, when Sonarr 4 doesn't have it.
- Add-SonarrSeries -SearchForMissingEpisodes IgnoreWithoutEpisodes leaves unmonitored only the episodes that have no file, where 2.0.0 also left unmonitored the ones that have one.
- Add-SonarrTag saves a notification's new tags to Sonarr, where 2.0.0 changed only the object in PowerShell.
- A notification's IsEnabled is true when any of its triggers is on, including a trigger that a later Sonarr adds.
- New-SonarrDelayProfile -Protocol PreferUsenet or PreferTorrent enables both protocols, where 2.0.0 enabled only the preferred one.
- New-SonarrDelayProfile and New-SonarrReleaseProfile stop with an error when a tag name matches no tag, where 2.0.0 left the name out and created the profile.
- New-SonarrReleaseProfile stops with an error when -Indexer matches no indexer, where 2.0.0 created the profile for every indexer, and it takes (Any) for any indexer.
- Get-SonarrReleaseProfile returns the profiles that match -Id and those that match -Name, where 2.0.0 ignored the names when it had IDs.

Settings, paths, and downloads
- New-SonarrRootFolder sends a path that's absolute on Linux or Windows as it's given, so /tv no longer becomes C:\tv from Windows, and it writes a root folder object.
- Get-SonarrManualImport sends a path that's absolute on Linux, macOS, or Windows as it's given. New-SonarrRemotePathMapping takes a local path for the host localhost as it's given, without checking that it exists on your computer.
- Update-SonarrMediaManagement ends the recycle bin's path with the server's separator, where 2.0.0 added a backslash, which broke Linux paths.
- Update-SonarrRemotePathMapping keeps the values that you leave out, where 2.0.0 sent only the ones you gave.
- Save-SonarrBackup and Save-SonarrLogFile log in with -Credential after any Connect-SonarrInstance, where 2.0.0 logged in only after one with -PassThru. They keep a DOMAIN\user or user@host name whole, log in again when Sonarr ends the session, and fail with an error, instead of saving the page as the file, when Sonarr answers with an error or its login page. They write a connection or file error as an error instead of throwing it, and honor -Verbose and -Debug.
- Get-SonarrCalendar returns nothing when -Tags matches no tag, where 2.0.0 returned every entry, and -DayOfWeek uses the local air day instead of the UTC one.
- Get-SonarrCommandHistory counts a failed or aborted command as finished.

Values from Sonarr
- Names and text that people type stay strings even when they look like numbers, such as a download client named 2024, a tag's label, a provider's setting, a host name, or a release's guid. A tag labeled 1080 is found by -Name '10*', and settings that hold such text go back to Sonarr without a 400.
- Numbers and dates read the same way, and queries are built the same way, under every culture setting, where 2.0.0 used the current culture.
- Text with group separators, such as 1,000, stays text instead of becoming a number, and a string becomes a date only in ISO 8601 form. A date that doesn't read as one keeps the text that Sonarr sent, where 2.0.0 gave 1/1/0001.
- Setting a model's number or date property to a value that came from a command works, where 2.0.0 threw.
- Name filters treat every * as a wildcard, even where a name holds a literal *, ignore case for letters outside ASCII, and read a lone % as a name instead of as every name.
'@

$info = [MG.Sonarr.Next.Shell.Build.Module]::ReadAllAssemblyCmdlets()
$manifestArgs = @{
	Path                 = $manifestPath
	Guid                 = '0cd92751-7c76-4b31-ae4a-48d344f9b786'
	Author               = 'Mike Garvey'
	ModuleVersion        = $productVers
	CompanyName          = $fileInfo.CompanyName
	Description          = 'A PowerShell module for querying and managing Sonarr PVR through its APIs.'
	Copyright            = $fileInfo.LegalCopyright.Replace("$([char]169)", '(c)')
	PowerShellVersion    = '7.7'
	CompatiblePSEditions = 'Core'
	LicenseUri           = 'https://raw.githubusercontent.com/Yevrag35/PoshSonarr/master/LICENSE'
	ProjectUri           = 'https://github.com/Yevrag35/PoshSonarr'
	IconUri              = 'https://raw.githubusercontent.com/Yevrag35/PoshSonarr/refs/heads/master/.img/PoshSonarr.png'
	RootModule           = "$($LibraryName).dll"
	CmdletsToExport      = $info.Cmdlets
	AliasesToExport      = $info.Aliases
	FunctionsToExport    = @()
	VariablesToExport    = @()
	ReleaseNotes		 = $releaseNotes
	TypesToProcess		 = $typesAndFormats.Types
	FormatsToProcess     = $typesAndFormats.Formats
	Tags                 = @('Anime', 'Api', 'Backup', 'Calendar', 'Connect', 'dll', 'Episode', 'Json', '.NET',
							 'Manage', 'PVR', 'Quality', 'Rss', 'Series', 'Sonarr',
							 'Status', 'Sync', 'Website', 'automation')
}

New-ModuleManifest @manifestArgs
