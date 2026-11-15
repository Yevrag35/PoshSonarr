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
- PoshSonarr now requires PowerShell 7.7 or later, which runs on .NET 11. Version 2.0.0 required 7.5, and 3.0.0 doesn't import on PowerShell 7.6 or earlier.
- Test-SonarrHost checks the URL and API key with the same test as Connect-SonarrInstance, and without -Quiet it writes the server's status instead of the /ping result. It also takes -NoApiInPath, -SkipCertificateCheck, and -Timeout.
- Get-SonarrDiskspace returns FreeSpace and TotalSpace as byte counts, and FreeSpaceInMB, FreeSpaceInGB, FreeSpaceInTB, TotalSpaceInMB, TotalSpaceInGB, and TotalSpaceInTB give the rounded sizes. The Size type, with its InMB, InGB, InTB, and TotalBytes properties, is gone.
- The naming settings lose the SeparatorLength property, which was always 0.

Fixes
- Connect-SonarrInstance replaces the current connection, so a wrong URL or key fails while connected too, and -PassThru fails in the same way. Disconnect-SonarrInstance does nothing when there's no connection.
- -Force skips only the confirmation prompt, so -WhatIf works with -Force, and Invoke-SonarrRename, Invoke-SonarrRssSync, Save-SonarrBackup, and Remove-SonarrBackup honor -WhatIf and -Confirm.
- Get-SonarrTag with no arguments lists every tag, and Add-SonarrTag and Clear-SonarrTag take -Id with piped objects.
- Get-SonarrWantedMissing sends -PageNumber.
- Get-SonarrEpisodeFile and Remove-SonarrEpisodeFile use a piped episode's file ID, and warn about an episode without a file.
- Episode identifiers accept S00 for specials and absolute ranges such as E25-28.
- A cmdlet goes on with the piped inputs that bound when another input fails to bind.
- New-SonarrRootFolder sends a path that's absolute on Linux, macOS, or Windows as it's given.
- Remove-SonarrSeries warns, instead of failing, when Sonarr doesn't have the series.
- Save-SonarrBackup and Save-SonarrLogFile keep a DOMAIN\user name whole, and a download that lands on Sonarr's login page fails with a 401 instead of saving the page as the file.
- Stopping a pipeline, such as with Ctrl+C, cancels its request.
- A string that looks like a number stays a string, and a value that doesn't read as its expected type keeps what Sonarr sent.
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
