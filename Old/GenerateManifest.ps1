param
(
    [parameter(Mandatory=$true, Position=0)]
    [string] $DebugDirectory,

    [parameter(Mandatory=$true, Position=2)]
    [string] $AssemblyInfo,

    [parameter(Mandatory=$true, Position=3)]
    [string] $TargetFileName
)

Get-ChildItem $DebugDirectory *.ps1xml -File | Remove-Item -Force;
$manifestFile = "PoshSonarr.psd1";
if ((Test-Path "$DebugDirectory\$manifestFile"))
{
	Remove-Item "$DebugDirectory\$manifestFile" -Force;
	Get-ChildItem "$DebugDirectory" *.ps1xml -File | Remove-Item -Force;
}

## Get Module Version
$assInfo = Get-Content $AssemblyInfo;
foreach ($line in $assInfo)
{
    if ($line -like "*AssemblyFileVersion(*")
    {
        $vers = $line -replace '^\s*\[assembly\:\sAssemblyFileVersion\(\"(.*?)\"\)\]$', '$1';
    }
}

$References = Join-Path "$DebugDirectory\..\.." "ReferenceAssemblies";
$Formats = Join-Path "$DebugDirectory\..\.." "TypeFormats";
$dlls = Get-ChildItem $References -Include *.dll -Exclude 'System.Management.Automation.dll' -Recurse;
$formatFiles = Get-ChildItem $Formats -Include *.ps1xml -Recurse;
[string[]]$allDlls = $dlls.Name;
[string[]]$allFormats = $formatFiles.Name;
@($dlls, $formatFiles) | Copy-Item -Destination $DebugDirectory -Force;

$manifest = @{
    Path               = $(Join-Path $DebugDirectory $manifestFile)
    Guid               = '0cd92751-7c76-4b31-ae4a-48d344f9b786';
    Description        = 'A set of PowerShell cmdlets for use against a Sonarr PVR instance.'
    Author             = 'Mike Garvey'
    CompanyName        = 'Yevrag35, LLC.'
    Copyright          = '(c) 2018 Yevrag35, LLC.  All rights reserved.'
    ModuleVersion      = $vers.Trim()
    PowerShellVersion  = '5.1'
	DotNetFrameworkVersion = '4.6.2'
    RootModule         = $TargetFileName
	RequiredAssemblies = $allDlls
	FormatsToProcess   = $allFormats
    AliasesToExport    = ''
    CmdletsToExport    = '*'
    FunctionsToExport  = @()
    VariablesToExport  = ''
	ProjectUri         = 'https://github.com/Yevrag35/PoshSonarr'
	Tags               = @('Sonarr', 'PVR', 'Api', 'Manage', 'Website', 'Newtonsoft', 'Json', 'dll',
							'Backup', 'Series', 'Episode', 'Rss', 'Sync', 'Calendar', 'Refresh', 'Rescan',
							'Status', 'Connect')
};

New-ModuleManifest @manifest;