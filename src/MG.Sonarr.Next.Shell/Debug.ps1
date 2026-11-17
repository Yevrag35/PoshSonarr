[CmdletBinding(SupportsShouldProcess=$true, PositionalBinding = $false)]
param  (
	[Parameter()]
	[string] $LibraryName = 'MG.Sonarr.Next.Shell',

	[Parameter()]
	[string] $ApiKey,

	[Parameter()]
	[string] $SonarrUrl,

	[Parameter()]
	[switch] $NoApiInPath
)

function readEnvFile([string]$path) {
	$settings = @{}
	foreach ($line in Get-Content -Path $path) {
		$name, $value = $line.Trim() -split '=', 2
		if ($null -eq $value -or $name.StartsWith("#")) {
			continue
		}

		# Drop the quotes around a quoted value.
		$value = $value.Trim()
		if ($value -match '^(["''])(.*)\1$') {
			$value = $Matches[2]
		}

		$settings[$name.Trim()] = $value
	}

	return $settings
}

# The script runs from the Shell project's build output, so this walks up to the folder that holds the solution.
function findRepoRoot([string]$path) {
	for ($dir = $path; $dir; $dir = Split-Path -Path $dir -Parent) {
		if (Test-Path -Path "$dir\PoshSonarr.slnx" -PathType Leaf) {
			return $dir
		}
	}
}

# The .env file at the repo root supplies each connection setting that isn't passed as a parameter.
$repoRoot = findRepoRoot $PSScriptRoot
if ($repoRoot -and (Test-Path -Path "$repoRoot\.env" -PathType Leaf)) {

	$settings = readEnvFile "$repoRoot\.env"
	if (-not $PSBoundParameters.ContainsKey("SonarrUrl")) {
		$SonarrUrl = $settings["SONARR_URL"]
	}
	if (-not $PSBoundParameters.ContainsKey("ApiKey")) {
		$ApiKey = $settings["SONARR_API_KEY"]
	}
	if (-not $PSBoundParameters.ContainsKey("NoApiInPath")) {
		$NoApiInPath = $settings["SONARR_NO_API_IN_PATH"] -eq "true"
	}
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
if ([string]::IsNullOrWhiteSpace($SonarrUrl) -or [string]::IsNullOrWhiteSpace($ApiKey)) {
	Write-Warning "Not connecting to Sonarr. Set SONARR_URL and SONARR_API_KEY in the .env file at the repo root, or pass -SonarrUrl and -ApiKey."
}
else {
	Connect-SonarrInstance -Url $SonarrUrl -ApiKey $ApiKey -NoApiInPath:$NoApiInPath -PassThru
}