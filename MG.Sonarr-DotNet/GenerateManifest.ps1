param
(
    [parameter(Mandatory = $true, Position = 0)]
    [string] $DebugDirectory,

    [parameter(Mandatory = $true, Position = 1)]
    [string] $ModuleFileDirectory,

	[parameter(Mandatory = $true, Position = 2)]
	[string] $AssemblyInfo,

    [parameter(Mandatory = $true, Position = 3)]
    [string] $TargetFileName,

	[Parameter(Mandatory = $true, Position = 4)]
	[string] $Configuration
)

$curDir = Split-Path -Parent $MyInvocation.MyCommand.Definition;

## Clear out files
Get-ChildItem -Path $DebugDirectory -Include *.ps1xml,*.psd1 -Recurse | Remove-Item -Force;

## Get Module Version from project.assets.json - PowerShell Core
#$json = Get-Content "$curDir\obj\project.assets.json" | ConvertFrom-Json;
#$vers = $json.project.version;

## Get Module Version from Assembly.cs - WindowsPowerShell
$assInfo = Get-Content -Path $AssemblyInfo;
foreach ($line in $assInfo)
{
    if ($line -like "*AssemblyFileVersion(*")
    {
        $vers = $line -replace '^\s*\[assembly\:\sAssemblyFileVersion\(\"(.*?)\"\)\]$', '$1';
    }
}

$allFiles = Get-ChildItem $ModuleFileDirectory -Include * -Exclude *.old -Recurse;
$References = Join-Path "$ModuleFileDirectory\.." "Assemblies";

[string[]]$verbs = Get-Verb | Select-Object -ExpandProperty Verb;
$patFormat = '^({0})(?:([a-zA-Z]{{1,}})|([a-zA-Z]{{1,}})\-Core)\.cs';
$pattern = $patFormat -f ($verbs -join '|');
$cmdletFormat = "{0}-{1}";

$baseCmdletDir = Join-Path "$ModuleFileDirectory\.." "Cmdlets";
[string[]]$folders = [System.IO.Directory]::EnumerateDirectories($baseCmdletDir, "*", [System.IO.SearchOption]::TopDirectoryOnly) | `
	Where-Object { -not $_.EndsWith('Bases') -and -not $_.EndsWith("Exclude") };

$aliasPat = '\[(?:A|a)lias\(\"(\S+)\"\)\]'
$csFiles = @(Get-ChildItem -Path $folders *.cs -File);
$Cmdlets = New-Object System.Collections.Generic.List[string] $csFiles.Count;
$Aliases = New-Object System.Collections.Generic.List[string];
foreach ($cs in $csFiles)
{
	$match = [regex]::Match($cs.Name, $pattern)
	if ($match.Success)
	{
		if ([string]::IsNullOrEmpty($match.Groups[3]))
		{
			$name = $cmdletFormat -f $match.Groups[1].Value, $match.Groups[2].Value;
		}
		else
		{
			$name = $cmdletFormat -f $match.Groups[1].Value, $match.Groups[3].Value
		}
		$Cmdlets.Add($name);
	}
	<#
    $content = Get-Content -Path $cs.FullName -Raw -ErrorAction SilentlyContinue
	if ($null -ne $content)
	{
		$aliasMatch = [regex]::Match($content, $aliasPat, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase);
		if ($aliasMatch.Success)
		{
			$Aliases.Add($aliasMatch.Groups[1].Value);
		}
	}
	#>
}

[string[]]$Cmdlets = $Cmdlets | Select-Object -Unique;

[string[]]$allDlls = Get-ChildItem $DebugDirectory -Include *.dll -Exclude 'System.Management.Automation.dll', $TargetFileName -Recurse | Select-Object -ExpandProperty Name;
[string[]]$allFormats = $allFiles | Where-Object -FilterScript { $_.Extension -eq ".ps1xml" } | `
	Select-Object @{L="FormatPath";E={"TypeFormats\{0}" -f$_.Name}} | Select-Object -ExpandProperty FormatPath
	#Select-Object -ExpandProperty Name

if ($Configuration -eq "Debug")
{
	$manifestFile = "PoshSonarr-Beta.psd1"
}
else
{
	$manifestFile = "PoshSonarr.psd1"
}

$formatsPath = "$DebugDirectory\TypeFormats"
if (-not (Test-Path -Path $formatsPath -PathType Container))
{
	New-Item -Path $DebugDirectory -Name "TypeFormats" -ItemType Directory | Out-Null
}
$allFiles | Copy-Item -Destination "$DebugDirectory\TypeFormats" -Force;
$modPath = Join-Path $DebugDirectory $manifestFile;

#Write-Warning $($Aliases | Out-String)

$manifest = @{
    Path                   = $modPath
    Guid                   = '0cd92751-7c76-4b31-ae4a-48d344f9b786';
    Description            = 'A PowerShell module for querying and managing Sonarr PVR through its API''s.'
    Author                 = 'Mike Garvey'
    CompanyName            = 'Yevrag35, LLC.'
    Copyright              = '(c) 2019-2020 Yevrag35, LLC.  All rights reserved.'
    ModuleVersion          = $($vers.Trim() -split '\.' | Select-Object -First 3) -join '.'
    PowerShellVersion      = '5.1'
    DotNetFrameworkVersion = '4.8'
    RootModule             = $TargetFileName
    DefaultCommandPrefix   = "Sonarr"
    RequiredAssemblies     = $allDlls
	CmdletsToExport		   = $Cmdlets
#	AliasesToExport		   = $Aliases
	CompatiblePSEditions   = "Core", "Desktop"
    FormatsToProcess       = if ($allFormats.Length -gt 0) { $allFormats } else { @() };
    ProjectUri             = 'https://github.com/Yevrag35/PoshSonarr'
	LicenseUri			   = 'https://raw.githubusercontent.com/Yevrag35/PoshSonarr/master/LICENSE'
	HelpInfoUri			   = 'https://github.com/Yevrag35/PoshSonarr/issues'
    Tags                   = @('Sonarr', 'PVR', 'Api', 'Manage', 'Website', 'Newtonsoft', 'Json', 'dll',
							'Backup', 'Series', 'Episode', 'Rss', 'Sync', 'Calendar', 'Refresh', 'Rescan',
							'Status', 'Connect')
};

New-ModuleManifest @manifest;
#Update-ModuleManifest -Path $modPath -Prerelease 'alpha' -FunctionsToExport '';