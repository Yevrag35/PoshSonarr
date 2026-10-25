[CmdletBinding()]
param (
	[Parameter()]
	[string] $LibraryName = 'MG.Sonarr.Next.Shell',

	[Parameter()]
	[string] $ModuleName = 'PoshSonarr',

	[Parameter(Mandatory = $true)]
	[string] $OutputPath
)

$OutputPath = $OutputPath.Trim('"')

$dllPath = "$OutputPath\$LibraryName.dll"
if (-not (Test-Path -Path $dllPath -PathType Leaf)) {
	throw "The specified module DLL was not found -> `"$dllPath`""
}

Import-Module $dllPath -ErrorAction Stop
$fileInfo = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($dllPath)
$productVers = [version]::new($fileInfo.ProductMajorPart, $fileInfo.ProductMinorPart, $fileInfo.ProductBuildPart)

$typesAndFormats = [MG.Sonarr.Next.Shell.Build.Module]::GetFormatsAndTypePaths($OutputPath)

$info = [MG.Sonarr.Next.Shell.Build.Module]::ReadAllAssemblyCmdlets()
$manifestArgs = @{
	Path                 = "$OutputPath\$($ModuleName).psd1"
	Guid                 = '0cd92751-7c76-4b31-ae4a-48d344f9b786'
	Author               = 'Mike Garvey'
	ModuleVersion        = $productVers
	CompanyName          = $fileInfo.CompanyName
	Description          = 'A PowerShell module for querying and managing Sonarr PVR through its APIs.'
	Copyright            = $fileInfo.LegalCopyright.Replace("$([char]169)", '(c)')
	PowerShellVersion    = '7.6'
	CompatiblePSEditions = 'Core'
	LicenseUri           = 'https://raw.githubusercontent.com/Yevrag35/PoshSonarr/master/LICENSE'
	ProjectUri           = 'https://github.com/Yevrag35/PoshSonarr'
	IconUri              = 'https://raw.githubusercontent.com/Yevrag35/PoshSonarr/refs/heads/master/.img/PoshSonarr.png'
	HelpInfoURI          = 'https://github.com/Yevrag35/PoshSonarr/issues'
	RootModule           = "$($LibraryName).dll"
	CmdletsToExport      = $info.Cmdlets
	AliasesToExport      = $info.Aliases
	FunctionsToExport    = @()
	VariablesToExport    = @()
	ReleaseNotes		 = "GA release of PoshSonarr v2-NEXT"
	TypesToProcess		 = $typesAndFormats.Types
	FormatsToProcess     = $typesAndFormats.Formats
	Tags                 = @('Anime', 'Api', 'Backup', 'Calendar', 'Connect', 'dll', 'Episode', 'Json', '.NET',
							 'Manage', 'PVR', 'Quality', 'Rss', 'Series', 'Sonarr',
							 'Status', 'Sync', 'Website', 'automation')
}

New-ModuleManifest @manifestArgs
