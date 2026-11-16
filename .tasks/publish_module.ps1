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

Breaking Changes
- Raise the minimum PowerShell version from 7.5 to 7.7, the first release on .NET 11
- Write each error when it happens instead of only the last one when the cmdlet ends, so -ErrorAction Stop ends the statement at the step that failed
- Stop a cmdlet whose setup fails instead of going on to its input
- Move CmdletScopeNotReadyException, ContextNotSetException, InvalidApiKeyException, and SonarrParameterException from MG.Sonarr.Next.Shell.Exceptions to MG.Sonarr.Next.Exceptions.Shell
- Move InvalidEpisodeIdFormatException from MG.Sonarr.Next.Shell.Exceptions to MG.Sonarr.Next.Exceptions
- Make Get-SonarrHistory -EventType take an event type's name or number, and reject one it doesn't know when it binds instead of returning every event
- Return a history record's Data as the strings that Sonarr sends for the event, instead of a release object
- Change the type name of Get-SonarrDiskspace's objects from MG.Sonarr.Next.Models.System.Diskspace to MG.Sonarr.Next.Models.System.DiskspaceObject
- Remove the naming settings' SeparatorLength property, which was always 0

Connection Updates and Fixes
- Add Test-SonarrHost (Test-Sonarr) to check a URL and API key the way Connect-SonarrInstance does, without changing the current connection
- Write the server's status from Test-SonarrHost, or $true or $false with -Quiet, and support -NoApiInPath, -SkipCertificateCheck, and -Timeout
- Replace the current connection in Connect-SonarrInstance, so a new URL or key takes effect and a wrong one fails even while connected
- Report a wrong API key as a 401 error, with or without -PassThru, instead of a JSON parse error
- Fix the URL base of a -Url such as https://host/sonarr being left out of requests, the forms login, and downloads
- Fix a SecureString API key not working on Linux and macOS
- Validate Connect-SonarrInstance -Timeout when it binds, allowing more than zero and up to about 24.8 days
- Fix Disconnect-SonarrInstance throwing a NullReferenceException when there's no connection

General Cmdlet Updates and Fixes
- Make -Force skip only confirmation prompts, so -WhatIf always wins
- Fix -Force -WhatIf running Remove-SonarrDelayProfile, Remove-SonarrDownloadClient, Remove-SonarrIndexer, Remove-SonarrNotification, Remove-SonarrReleaseProfile, Remove-SonarrRemotePathMapping, Remove-SonarrRootFolder, Remove-SonarrTag, Restart-SonarrInstance, and Update-SonarrMediaManagement
- Support -WhatIf and -Confirm in Invoke-SonarrRename with -SeriesId or -EpisodeFileId, Invoke-SonarrRssSync, and Save-SonarrBackup
- Report a request that can't reach the server as ConnectionError, with the URL in the message, and one that times out as OperationTimeout, from every cmdlet
- Cancel a request in flight when the pipeline stops, such as with Ctrl+C
- Show Sonarr's messages for its validation errors instead of a JSON error or no detail
- Fix search terms and paths that contain &, #, or +, such as Search-SonarrSeries 'Law & Order' searching for 'Law '
- Honor preference variables that a script or function sets to a string, such as $ErrorActionPreference = 'Stop'
- Show the body of every request that has one with -Debug
- Fix requests deadlocking in a host whose pipeline thread has a single-threaded synchronization context
- Make Show-SonarrException find the newest Sonarr error in $Error, including one that ended a statement under -ErrorAction Stop
- Fix Add-SonarrTag, Clear-SonarrTag, Get-SonarrEpisode, Get-SonarrEpisodeFile, Get-SonarrQualityProfile, Get-SonarrTag, and Remove-SonarrTag skipping every piped input when one fails to bind or another command writes an error

Series and Episode Updates and Fixes
- Fix Get-SonarrSeries writing earlier piped objects' series again with each new object
- Fix Add-SonarrSeries -SearchForMissingEpisodes IgnoreWithoutEpisodes also leaving episodes that have files unmonitored
- Fix Remove-SonarrSeries -DeleteFiles not deleting the series' files
- Write a warning that names the series, instead of an error, when Remove-SonarrSeries can't find it in Sonarr 4
- Accept S00 for specials, ranges of specials such as S00E01-02, and absolute ranges such as E25-28 in episode identifiers
- Reject an episode identifier that can't be read, such as S01Ex or 10E05, with an error that names it, instead of taking it for the whole season or another episode
- Continue to the next piped series after a failed request in Get-SonarrEpisode and Get-SonarrEpisodeFile
- Fix Get-SonarrEpisodeFile sending its request only the first time it runs
- Use a piped episode's file ID in Get-SonarrEpisodeFile and Remove-SonarrEpisodeFile, and warn about an episode or calendar entry that has no file
- Fix Update-SonarrEpisode sending an air date of 0001-01-01 for an episode that has none
- Fix Get-SonarrCalendar returning every entry when -Tags matches no tag
- Use the local air day instead of the UTC one for Get-SonarrCalendar -DayOfWeek
- Fix Get-SonarrWantedMissing ignoring -PageNumber
- Fix Add-SonarrRelease writing an error when it succeeds and Sonarr's response when it fails
- Fix Add-SonarrRelease sending an empty guid for a release whose guid looks like a number or a GUID
- Fix Edit-SonarrManualImport not binding an ID or an object to -Series, -Episode, and -Quality
- Fix Get-SonarrManualImport changing a path that's absolute on Linux, macOS, or Windows

Tag, Profile, and Notification Updates and Fixes
- Allow -Id with piped objects in Add-SonarrTag and Clear-SonarrTag
- Accept series lookup results and schemas in Add-SonarrTag, Clear-SonarrTag, and Get-SonarrTag
- Fix Add-SonarrTag changing a notification's tags only in PowerShell instead of saving them to Sonarr
- Fix Get-SonarrTag listing no tags when given no arguments
- Fix New-SonarrDelayProfile -Protocol PreferUsenet or PreferTorrent enabling only the preferred protocol
- Stop New-SonarrDelayProfile and New-SonarrReleaseProfile with an error when a tag name matches no tag, instead of creating the profile without it
- Stop New-SonarrReleaseProfile with an error when -Indexer matches no indexer, instead of creating the profile for every indexer
- Accept (Any) in New-SonarrReleaseProfile -Indexer for any indexer
- Fix Get-SonarrReleaseProfile ignoring the names it's given along with IDs
- Fix Update-SonarrNotification failing on every run
- Make a notification's IsEnabled true when any of its triggers is on, including triggers that later Sonarr versions add

System and Settings Updates and Fixes
- Fix Save-SonarrBackup and Invoke-SonarrRssSync failing before sending a request
- Fix Save-SonarrBackup and Save-SonarrLogFile logging in with -Credential only after Connect-SonarrInstance -PassThru
- Keep a DOMAIN\user or user@host name whole when Save-SonarrBackup and Save-SonarrLogFile log in
- Log in again when Sonarr ends the session during a download
- Fail with an error, instead of saving the page as the file, when Sonarr answers a download with an error or its login page
- Write a download's connection and file errors as errors instead of throwing them
- Honor -Verbose and -Debug in Save-SonarrBackup and Save-SonarrLogFile
- Count a failed or aborted command as finished in Get-SonarrCommandHistory
- Fix New-SonarrRootFolder changing a path that's absolute on Linux or Windows, such as /tv becoming C:\tv from Windows
- Write the new root folder object from New-SonarrRootFolder
- Accept a local path for the host localhost in New-SonarrRemotePathMapping without checking that it exists on your computer
- Fix Update-SonarrRemotePathMapping dropping the values that you leave out
- Fix Update-SonarrMediaManagement adding a backslash to the recycle bin's path, which broke Linux paths

Value Handling Fixes
- Keep names and typed text that look like numbers as strings, such as a download client named 2024, a tag's label, a provider's setting, a host name, or a release's guid
- Fix wildcard name filters missing such text, such as -Name '10*' not finding a tag labeled 1080
- Fix the 400 error that Sonarr returns for a setting whose text looks like a number
- Read numbers and dates, and build queries, the same way under every culture setting
- Keep text with group separators, such as 1,000, as text instead of a number
- Read a string as a date only in ISO 8601 form
- Keep the text that Sonarr sent for a date that doesn't read as one, instead of 1/1/0001
- Fix an error when setting a model's number or date property to a value that came from a command
- Treat every * in a name filter as a wildcard, even where a name holds a literal *
- Ignore case for letters outside ASCII in name filters
- Read a lone % in a name filter as a name instead of every name
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
