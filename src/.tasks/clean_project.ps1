[CmdletBinding(DefaultParameterSetName = "ByProjectName")]
param (
    [Parameter(Mandatory = $true, ParameterSetName = "WithProjectFolder")]
    [string] $OutputPath,

    [Parameter(Mandatory = $false, ParameterSetName = "ByProjectName")]
    [string] $VSInstallPath,

    [Parameter(Mandatory = $false, ParameterSetName = "ByProjectName")]
    [string] $SolutionFilePath,

    # "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\amd64\Microsoft.Build.dll"
    [Parameter(Mandatory = $true, ParameterSetName = "ByProjectName")]
    [string[]] $ProjectName
)

#region SCRIPT FUNCTIONS
Function Get-CurrentDir() {
	[CmdletBinding()]
	param (
		[Parameter(Mandatory = $true)]
		[string] $ScriptPath
	)

	$private:curDir = $ScriptPath
	if ([string]::IsNullOrWhitespace($private:curDir)) {
		$private:curDir = "."
	}

	Write-Verbose "Current Directory -> $($private:curDir)"
	return $private:curDir
}

Function Find-SolutionFile() {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $false)]
        [string] $ScriptPath = $PSScriptRoot
    )

    $private:curDir = Get-CurrentDir -ScriptPath $ScriptPath
    [array] $files = Get-ChildItem -Path $private:curDir -Filter "*.sln" -ErrorAction Stop
    if ($files.Count -le 0) {
        exit 901
    }

    $files[0].FullName
}

Function Parse-OutputPath() {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true, ValueFromPipeline = $true)]
        [string] $Path
    )
    Process {
        if ([string]::IsNullOrWhiteSpace($Path)) {
            Write-Error "No output path specified."
            exit 99
        }
    
        $Path = $Path.Trim('"')
        
        if (-not [System.IO.Path]::IsPathFullyQualified($Path) -or -not (Test-Path -Path $Path -PathType Container)) {
            Write-Error "Not a directory"
            exit 101
        }
    
        $Path
    }
}

Function Remove-Empty() {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true, ValueFromPipeline = $true)]
        [System.IO.DirectoryInfo] $InputObject
    )
    Begin {
        $options = New-Object 'System.IO.EnumerationOptions' -Property @{
            RecurseSubdirectories = $true
            IgnoreInaccessible = $false
        }
    }
    Process {
        Remove-EmptyDirectory -EnumerationOptions $options -Query ($InputObject.EnumerateDirectories("*", $options))
    }
}

Function Remove-EmptyDirectory() {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [System.Collections.Generic.IEnumerable[System.IO.DirectoryInfo]] $Query,

        [Parameter(Mandatory = $true)]
        [System.IO.EnumerationOptions] $EnumerationOptions
    )
    Process {
        if ($null -eq $InputObject -or $InputObject.Count -le 0) {
            return
        }

        foreach ($dirObj in $Query) {

            if ((Test-AnyFile -Query ($dirObj.EnumerateFiles("*", $EnumerationOptions)))) {
                Write-Host "Skipping $($dirObj.FullName)..."
                continue
            }

            $dirObj.Delete($true)
        }
    }
}

Function Test-AnyFile([System.Collections.Generic.IEnumerable[System.IO.FileInfo]] $Query) {
    try {
        foreach ($fileInfo in $Query) {
            if ($null -ne $fileInfo) {
                return $true
            }
        }
    }
    catch {
        return $false
    }

    return $false
}

#endregion

Write-Host ($PSBoundParameters | Out-String)

if ($PSCmdlet.ParameterSetName -eq "ByProjectName") {

    # if (-not $PSBoundParameters.ContainsKey("SolutionFilePath")) {
    #     $SolutionFilePath = Find-SolutionFile
    # }
    # else {
    #     $SolutionFilePath = $SolutionFilePath.Trim('"')
    # }

    # if (-not $PSBoundParameters.ContainsKey("VSInstallPath")) {
    #     $VSInstallPath = $env:VSInstallPath
    # }

    # # Write-Host $VSInstallPath
    # $dllPath = Join-Path -Path $VSInstallPath.Trim('"') -ChildPath "..\..\MSBuild\Current\Bin\amd64\Microsoft.Build.dll"
    # Import-Module $dllPath -ErrorAction Stop

    # $sln = [Microsoft.Build.Construction.SolutionFile]::Parse($SolutionFilePath)
    # foreach ($project in $sln.ProjectsInOrder.Where({
    #     $p = $_
    #     $ProjectName | Assert-Any { $_ -eq $p.ProjectName }
    # })) {

    #     Get-Item -Path $project.AbsolutePath | Remove-Empty
    # }
}
else {
    Parse-OutputPath -Path $OutputPath | Get-Item | Remove-Empty
}

Write-Host "Directories Cleaned"
