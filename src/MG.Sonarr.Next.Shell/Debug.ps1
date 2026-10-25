[CmdletBinding(SupportsShouldProcess=$true, PositionalBinding = $false, DefaultParameterSetName = "ByExplicitApiKeyAndUrl")]
param  (
	[Parameter()]
	[string] $LibraryName = 'MG.Sonarr.Next.Shell',

	[Parameter(Mandatory = $true, ParameterSetName = "ByConfigFile")]
	[ValidateNotNullOrEmpty()]
	[string] $ConfigJson,

	[Parameter(Mandatory = $false, ParameterSetName = "ByExplicitApiKeyAndUrl")]
	[string] $ApiKey,

	[Parameter(Mandatory = $false, ParameterSetName = "ByExplicitApiKeyAndUrl")]
	[string] $SonarrUrl,

	[Parameter(Mandatory = $false, ParameterSetName = "ByExplicitApiKeyAndUrl")]
	[switch] $NoApiInPath
)

if (-not [string]::IsNullOrWhitespace($ConfigJson)) {

	if (-not [System.IO.Path]::IsPathFullyQualified($ConfigJson)) {
		$ConfigJson = "$PSScriptRoot\$ConfigJson"
	}

	$config = Get-Content -Path $ConfigJson | ConvertFrom-Json -Depth 10 -AsHashtable
	if ($config.ContainsKey("Instance")) {

		$config = $config["Instance"]
	}

	$SonarrUrl = $config["Url"] -as [string]
	$ApiKey = $config["ApiKey"] -as [string]
	$NoApiInPath = $config["NoApiInPath"] -as [bool]
}

function resolveVariables([string]$text) {
	$varName = $text -replace '\{\s*?(\w+)\s*\}', '$1'
	try { return (Get-Variable -Name $varName -ErrorAction Stop).Value -as [string] }
	catch { return $text }
}

$myDesktop = [System.Environment]::GetFolderPath("Desktop")
$dllPath = "$PSScriptRoot\$LibraryName.dll"
if (-not (Test-Path -Path $dllPath -PathType Leaf)) {
	throw "The specified module DLL was not found -> `"$dllPath`""
}

if ($PSCmdlet.ShouldProcess($dllPath, "Importing Module")) {

	Import-Module $dllPath -ErrorAction Stop
	Push-Location $myDesktop
}

Get-ChildItem -Path "$PSScriptRoot\PSFormats" -File *.ps1xml -Recurse -ea Stop | %{ Update-FormatData -PrependPath $_.FullName }
Get-ChildItem -Path "$PSScriptRoot\PSTypes" -File *.ps1xml -Recurse -ea Stop | %{ Update-TypeData -AppendPath $_.FullName }

Write-Host ""
Write-Host "Debugging PoshSonarr PowerShell Module" -ForegroundColor Cyan
Write-Host "`n"
#$VerbosePreference = "Continue"
$debugSettings = Get-Content -Path "$PSScriptRoot\debugging.json" | ConvertFrom-Json -AsHashtable
$instance = $debugSettings.Instance

#Connect-SonarrInstance -Url (resolveVariables $instance.Url) -ApiKey (resolveVariables $instance.ApiKey) -PassThru