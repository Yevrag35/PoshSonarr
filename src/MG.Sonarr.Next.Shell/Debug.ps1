[CmdletBinding(SupportsShouldProcess=$true, PositionalBinding = $false)]
param  (
	[Parameter(Mandatory=$false)]
	[string] $LibraryName = 'MG.Sonarr.Next.Shell',

	[Parameter(Mandatory=$false)]
	[string] $RuntimeTarget,

	[Parameter(Mandatory=$false)]
	[ValidateScript({
		# Path must exist and be a directory
		Test-Path -Path $_ -PathType 'Container'
	})]
	[string] $NugetDirectory = "$env:USERPROFILE\.nuget\packages",

	[Parameter(Mandatory=$false)]
	[string[]] $CopyToOutput = @(
		'MG.Collections',
		'Microsoft.Extensions.Caching.Memory',
		'Microsoft.Extensions.Caching.Abstractions',
		'Microsoft.Extensions.DependencyInjection',
		'Microsoft.Extensions.DependencyInjection.Abstractions',
		'Microsoft.Extensions.Http',
		'Microsoft.Extensions.Logging',
		'Microsoft.Extensions.Logging.Abstractions',
		'Microsoft.Extensions.Options',
		'Microsoft.Extensions.Primitives',
		'OneOf'
	)
)

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
			Copy-Item -Path $file -Destination "$PSScriptRoot"
		}
		else
		{
			Write-Host "`"$fileName`" already copied..." -f Yellow
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

$VerbosePreference = "Continue"
Connect-Sonarr -ApiKey $skey -Url $surl

$s = Get-SonarrSeries 607