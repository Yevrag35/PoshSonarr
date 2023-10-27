[CmdletBinding(SupportsShouldProcess=$true, PositionalBinding = $false, DefaultParameterSetName = "ByExplicitApiKeyAndUrl")]
param  (
	[Parameter()]
	[string] $LibraryName = 'MG.Sonarr.Next.Shell',

	[Parameter()]
	[string] $RuntimeTarget,

	[Parameter(Mandatory = $true, ParameterSetName = "ByConfigFile")]
	[ValidateNotNullOrEmpty()]
	[string] $ConfigJson,

	[Parameter(Mandatory = $false, ParameterSetName = "ByExplicitApiKeyAndUrl")]
	[string] $ApiKey = $skey,

	[Parameter(Mandatory = $false, ParameterSetName = "ByExplicitApiKeyAndUrl")]
	[string] $SonarrUrl = $surl,

	[Parameter(Mandatory = $false, ParameterSetName = "ByExplicitApiKeyAndUrl")]
	[switch] $NoApiInPath,

	[Parameter()]
	[ValidateScript({
		# Path must exist and be a directory
		Test-Path -Path $_ -PathType 'Container'
	})]
	[string] $NugetDirectory = "$env:USERPROFILE\.nuget\packages",

	[Parameter()]
	[ValidateNotNullOrEmpty()]
	[string] $BuildDependenciesJson = "build_dependencies.json"
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

if (-not [System.IO.Path]::IsPathFullyQualified($BuildDependenciesJson)) {

	$BuildDependenciesJson = "$PSScriptRoot\$BuildDependenciesJson"
}

[string[]] $CopyToOutput = Get-Content -Path $BuildDependenciesJson -Raw | ConvertFrom-Json -Depth 10 | % -MemberName CopyToOutput

$depFile = "$PSScriptRoot\$LibraryName.deps.json"
$json = Get-Content -Path $depFile -Raw | ConvertFrom-Json

if (-not $PSBoundParameters.ContainsKey("RuntimeTarget")) {

	$RuntimeTarget = $json.runtimeTarget.name

	if ([string]::IsNullOrEmpty($RuntimeTarget)) {

		throw "No RuntimeTarget supplied or detected."
	}
}

$targets = $json.targets."$RuntimeTarget"
if ($null -eq $targets) {
	Write-Debug "JSON:`n$($json | Out-String)"
	throw "`$targets is NULL"
}

foreach ($toCopy in $CopyToOutput)
{
	$dependency = $targets.psobject.Properties.Where({$_.Name -like "$toCopy*"}) | Select -First 1

	if ($null -eq $dependency) {
		Write-Warning "'$toCopy' was not found in the list of dependencies."
		continue
	}

	$name, $version = $dependency.Name -split '\/'
	if ([string]::IsNullOrEmpty($name) -or [string]::IsNullOrEmpty($version)) {

		Write-Warning "Unable to parse name and version from '$toCopy'."
		continue
	}

	$pso = $targets."$($dependency.Name)".runtime
	if ($null -eq $pso) {

		Write-Warning "No runtime target was found in '$($dependency.Name)'."
		continue;
	}

	$mems = $pso | Get-Member -MemberType NoteProperty | Where { $_.Name -clike "lib/*" }
	foreach ($mem in $mems)
	{
		$fileName = [System.IO.Path]::GetFileName($mem.Name)
		if (-not (Test-Path -Path "$PSScriptRoot\$fileName" -PathType Leaf))
		{
			$file = "$NugetDirectory\$name\$version\$($mem.Name)"
			try { 
				Copy-Item -Path $file -Destination "$PSScriptRoot" -ErrorAction Stop
				Write-Host "Copied file -> $("$PSScriptRoot\$fileName")" -ForegroundColor Yellow
			}
			catch {
				Write-Warning "Unable to copy file -> $file"
				Write-Warning $_.Exception.Message
			}
		}
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

Write-Host ""
Write-Host "Debugging PoshSonarr PowerShell Module" -ForegroundColor Cyan
Write-Host "`n"
$VerbosePreference = "Continue"
if (-not ([string]::IsNullOrWhitespace($SonarrUrl) -or [string]::IsNullOrWhitespace($ApiKey))) {

	$status = Connect-SonarrInstance -Url $SonarrUrl -ApiKey $ApiKey -PassThru
	$status
	#$s = Get-SonarrSeries asdfm*
}