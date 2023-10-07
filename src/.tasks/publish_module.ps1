[CmdletBinding()]
param (
	[Parameter()]
	[string] $LibraryName = 'MG.Sonarr.Next.Shell',

	[Parameter()]
	[string] $ModuleName = "PoshSonarr",

	[Parameter()]
	[string] $RuntimeTarget,

    [Parameter(Mandatory = $true)]
    [string] $OutputPath,

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

$OutputPath = $OutputPath.Trim('"')

if (-not [System.IO.Path]::IsPathFullyQualified($BuildDependenciesJson)) {

	$BuildDependenciesJson = "$OutputPath\$BuildDependenciesJson"
}

[string[]] $CopyToOutput = Get-Content -Path $BuildDependenciesJson -Raw | ConvertFrom-Json -Depth 10 | % -MemberName CopyToOutput

$depFile = "$OutputPath\$LibraryName.deps.json"
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
		if (-not (Test-Path -Path "$OutputPath\$fileName" -PathType Leaf))
		{
			$file = "$NugetDirectory\$name\$version\$($mem.Name)"
			try { 
				Copy-Item -Path $file -Destination "$OutputPath" -ErrorAction Stop
				Write-Host "Copied file -> $("$OutputPath\$fileName")" -ForegroundColor Yellow
			}
			catch {
				Write-Warning "Unable to copy file -> $file"
				Write-Warning $_.Exception.Message
			}
		}
	}
}

$dllPath = "$OutputPath\$LibraryName.dll"
if (-not (Test-Path -Path $dllPath -PathType Leaf)) {
	throw "The specified module DLL was not found -> `"$dllPath`""
}

Import-Module $dllPath -ErrorAction Stop
$fileInfo = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($dllPath)
$productVers = [version]::new($fileInfo.ProductMajorPart, $fileInfo.ProductMinorPart, $fileInfo.ProductBuildPart)

$info = [MG.Sonarr.Next.Shell.Build.Module]::GetAllAssemblyCmdlets()
$manifestArgs = @{
	Path = "$OutputPath\$($ModuleName).psd1"
	Guid = '0cd92751-7c76-4b31-ae4a-48d344f9b786'
	Author = 'Mike Garvey'
	ModuleVersion = $productVers
	CompanyName = $fileInfo.CompanyName
	Description = "A PowerShell module for querying and managing Sonarr PVR through its APIs."
	Copyright = $fileInfo.LegalCopyright.Replace("$([char]169)", "(c)")
	PowerShellVersion = '7.3'
	CompatiblePSEditions = "Core"
	LicenseUri = 'https://raw.githubusercontent.com/Yevrag35/PoshSonarr/master/LICENSE'
	ProjectUri = 'https://github.com/Yevrag35/PoshSonarr'
	IconUri = 'https://images.yevrag35.com/icons/sonarr_powershell.png'
	HelpInfoURI = 'https://github.com/Yevrag35/PoshSonarr/issues'
	RootModule = $LibraryName
	CmdletsToExport = $info.Cmdlets
	AliasesToExport = $info.Aliases
	FunctionsToExport = @()
	VariablesToExport = @()
	Tags = @('Api','Backup','Calendar','Connect','dll','Episode','Json','.NET',
            'Manage','PVR','Quality','Rss','Series','Sonarr',
            'Status','Sync','Website')
}

New-ModuleManifest @manifestArgs