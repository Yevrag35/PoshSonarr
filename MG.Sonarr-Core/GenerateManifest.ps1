param
(
    [parameter(Mandatory = $true, Position = 0)]
    [string] $DebugDirectory,

    [parameter(Mandatory = $true, Position = 1)]
    [string] $ModuleFileDirectory,

    [parameter(Mandatory = $true, Position = 3)]
    [string] $TargetFileName
)

$curDir = Split-Path -Parent $MyInvocation.MyCommand.Definition;

## Clear out files
Get-ChildItem -Path $DebugDirectory -Include *.ps1xml -Recurse | Remove-Item -Force;

## Get Module Version from project.assets.json
$json = Get-Content "$curDir\obj\project.assets.json" | ConvertFrom-Json;
$vers = $json.project.version;

$allFiles = Get-ChildItem $ModuleFileDirectory -Include * -Exclude *.old -Recurse;
$References = Join-Path "$ModuleFileDirectory\.." "Assemblies";

[string[]]$verbs = Get-Verb | Select-Object -ExpandProperty Verb;
$patFormat = '^({0})(\S{{1,}})\.cs';
$pattern = $patFormat -f ($verbs -join '|');
$cmdletFormat = "{0}-{1}";

$baseCmdletDir = Join-Path "$ModuleFileDirectory\.." "Cmdlets";
[string[]]$folders = [System.IO.Directory]::EnumerateDirectories($baseCmdletDir, "*", [System.IO.SearchOption]::TopDirectoryOnly) | Where-Object { -not $_.EndsWith('Bases') };

$aliasPat = '\[alias\(\"(.{1,})\"\)\]'
$csFiles = @(Get-ChildItem -Path $folders *.cs -File);
$Cmdlets = New-Object System.Collections.Generic.List[string] $csFiles.Count;
$Aliases = New-Object System.Collections.Generic.List[string];
foreach ($cs in $csFiles)
{
	$match = [regex]::Match($cs.Name, $pattern)
    $Cmdlets.Add(($cmdletFormat -f $match.Groups[1].Value, $match.Groups[2].Value));
    $content = Get-Content -Path $cs -Raw;
    $aliasMatch = [regex]::Match($content, $aliasPat, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase);
    if ($aliasMatch.Success)
    {
        $Aliases.Add($aliasMatch.Groups[1].Value);
    }
}

[string[]]$allDlls = Get-ChildItem $References -Include *.dll -Exclude 'System.Management.Automation.dll' -Recurse | Select-Object -ExpandProperty Name;
[string[]]$allFormats = $allFiles | Where-Object -FilterScript { $_.Extension -eq ".ps1xml" } | Select-Object -ExpandProperty Name;

$manifestFile = "PoshSonarr.psd1"

$allFiles | Copy-Item -Destination $DebugDirectory -Force;
$modPath = Join-Path $DebugDirectory $manifestFile;

$manifest = @{
    Path                   = $modPath
    Guid                   = '0cd92751-7c76-4b31-ae4a-48d344f9b786';
    Description            = 'A PowerShell module for querying and managing Sonarr PVR through its API''s.'
    Author                 = 'Mike Garvey'
    CompanyName            = 'Yevrag35, LLC.'
    Copyright              = '(c) 2019 Yevrag35, LLC.  All rights reserved.'
    ModuleVersion          = $($vers.Trim() -split '\.' | Select-Object -First 3) -join '.'
    PowerShellVersion      = '5.1'
    DotNetFrameworkVersion = '4.7'
    RootModule             = $TargetFileName
    DefaultCommandPrefix   = "Sonarr"
    RequiredAssemblies     = $allDlls
	CmdletsToExport		   = $Cmdlets
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
Update-ModuleManifest -Path $modPath -Prerelease 'alpha' -FunctionsToExport '';