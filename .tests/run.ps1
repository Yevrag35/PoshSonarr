#Requires -Version 7.6

<#
.SYNOPSIS
	Build PoshSonarr, stage it, and run its Pester suite against the Sonarr testbed.

.DESCRIPTION
	Runs the Pester suite in .tests/ against a Debug build of the module, imported by a manifest written for it, in a new
	PowerShell process that connects to the Docker testbed and nothing else. In order, it:

	1. Checks that Pester 6.2.0 or a later 6.x is installed, and stops with the command that installs it when it isn't.
	2. Builds the solution in Debug, unless -NoBuild reuses the last build.
	3. Replaces .temp/pester/PoshSonarr/ with a copy of the Shell project's Debug output, without Debug.ps1. When a
	   process holds a file in the folder, such as a PowerShell session that imported the module from it, it stops
	   without deleting anything.
	4. Writes the copy's manifest, PoshSonarr.psd1, with .tasks/publish_module.ps1, the script that a Release build runs.
	5. Resets the testbed with testbed/sonarr/reset.ps1 when -Reset asks, and otherwise checks that the testbed answers
	   with its pinned version and instance name.
	6. Runs Pester in a new process, in which POSHSONARR_TEST_MANIFEST names the staged manifest. Pester runs the files
	   one after another, leaves out the tests tagged Restart unless -Tag names Restart, writes detailed output, and saves
	   NUnit XML results to .temp/pester/results.xml.

	It exits 0 when every selected test passes, and 1 when a test fails or a step before the tests does. It builds every
	path from its own location, so it runs the same from any directory.

.EXAMPLE
	pwsh -NoProfile -File .tests/run.ps1
.EXAMPLE
	pwsh -NoProfile -File .tests/run.ps1 -Reset
	# resets the testbed to its baseline first
.EXAMPLE
	pwsh -NoProfile -File .tests/run.ps1 -NoBuild -Path .tests/Connection.Tests.ps1
	# one file, on the last build
.EXAMPLE
	pwsh -NoProfile -File .tests/run.ps1 -Tag Restart
	# only the tests tagged Restart
#>
[CmdletBinding()]
param (
	# The test files to run; relative paths resolve against the current directory. Omit it to run every *.Tests.ps1
	# file in .tests/. Under -File, separate several with commas, or pass them first, without -Path.
	[Parameter(Mandatory = $false, Position = 0, ValueFromRemainingArguments = $true)]
	[string[]] $Path,

	# Runs only the tests with one of these tags. The tests tagged Restart run only when it names Restart. Under -File,
	# separate several with commas.
	[Parameter(Mandatory = $false)]
	[string[]] $Tag,

	# Tests the last Debug build instead of building the solution first.
	[Parameter(Mandatory = $false)]
	[switch] $NoBuild,

	# Resets the testbed to its baseline before the tests run.
	[Parameter(Mandatory = $false)]
	[switch] $Reset
)

$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$buildOutput = Join-Path $repoRoot 'src/MG.Sonarr.Next.Shell/bin/Debug/net10.0'
$stage = Join-Path $repoRoot '.temp/pester/PoshSonarr'
$manifest = Join-Path $stage 'PoshSonarr.psd1'
$results = Join-Path $repoRoot '.temp/pester/results.xml'

# The same PowerShell that runs this script runs the steps' scripts and the tests.
$pwsh = [System.Environment]::ProcessPath

try {

	$pester = Get-Module -ListAvailable -Name Pester | Where-Object { $_.Version -ge [version] '6.2.0' -and $_.Version.Major -eq 6 }
	if ($null -eq $pester) {

		$found = @(Get-Module -ListAvailable -Name Pester | ForEach-Object { $_.Version.ToString() })
		throw ("The suite needs Pester 6.2.0 or a later 6.x, and this machine has {0}. Install it with: Install-PSResource -Name Pester -Version '[6.2.0,7.0.0)' -Scope CurrentUser -TrustRepository" -f ($found ? "only Pester $($found -join ', ')" : 'no Pester'))
	}

	# Under -File, a list arrives as one string, so each value splits on its commas.
	$files = if ($Path) {
		@($Path -split ',' | Where-Object { $_ } | ForEach-Object { (Resolve-Path -LiteralPath $_.Trim()).ProviderPath })
	}
	else {
		@(Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.Tests.ps1' -File | Sort-Object Name | ForEach-Object FullName)
	}
	$tags = @($Tag -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ })
	$excludeTags = @(if ('Restart' -notin $tags) { 'Restart' })

	if (-not $NoBuild) {

		# An incremental build compiles a project only when one of its files is newer than its output, so it would miss
		# a file put back with its old timestamp, as a copy keeps it, and the tests would run on stale code.
		Write-Host 'Building the solution in Debug.' -ForegroundColor Cyan
		dotnet build (Join-Path $repoRoot 'PoshSonarr.slnx') -c Debug --nologo -v q --no-incremental
		if ($LASTEXITCODE -ne 0) {
			throw "The build failed with exit code $LASTEXITCODE."
		}
	}
	if (-not (Test-Path -LiteralPath (Join-Path $buildOutput 'MG.Sonarr.Next.Shell.dll') -PathType Leaf)) {
		throw "There's no Debug build in $buildOutput to stage. Run the suite without -NoBuild."
	}

	Write-Host "Staging the module in $stage." -ForegroundColor Cyan
	if (Test-Path -LiteralPath $stage) {

		# Remove-Item deletes what it can before it stops at a file that a process holds, so every file is checked first,
		# and a folder in use stays whole. A process that loaded the module, such as a PowerShell session that imported it
		# from the folder, holds its assemblies until it exits.
		$held = foreach ($file in Get-ChildItem -LiteralPath $stage -Recurse -File) {
			try {
				[System.IO.File]::Open($file.FullName, 'Open', 'ReadWrite', 'None').Dispose()
			}
			catch [System.IO.IOException] {
				$file.Name
			}
		}
		if ($held) {
			throw "Can't replace $stage, because another process holds $($held -join ', '). Close the PowerShell session that imported the module from that folder, and run the suite again."
		}
		Remove-Item -LiteralPath $stage -Recurse -Force
	}
	$null = New-Item -ItemType Directory -Path $stage -Force
	Copy-Item -Path (Join-Path $buildOutput '*') -Destination $stage -Recurse -Exclude 'Debug.ps1'

	# The script imports the module to read its cmdlets, so it runs in a process of its own, which exits before the tests
	# import the module.
	& $pwsh -NoProfile -NonInteractive -File (Join-Path $repoRoot '.tasks/publish_module.ps1') -OutputPath $stage
	if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $manifest -PathType Leaf)) {
		throw ".tasks/publish_module.ps1 didn't write $manifest."
	}

	if ($Reset) {

		Write-Host 'Resetting the testbed.' -ForegroundColor Cyan
		& $pwsh -NoProfile -NonInteractive -File (Join-Path $repoRoot 'testbed/sonarr/reset.ps1')
		if ($LASTEXITCODE -ne 0) {
			throw "testbed/sonarr/reset.ps1 failed with exit code $LASTEXITCODE."
		}
	}
	else {

		Import-Module (Join-Path $repoRoot 'testbed/sonarr/testbed.psm1') -Force
		Assert-TestbedReady
	}

	# Pester's output reaches this console through the new process, so this console decides whether it gets colors: ANSI
	# colors for a terminal, and plain text for redirected output, such as a log or an agent's shell.
	$renderMode = ($null -eq $env:NO_COLOR -and $Host.UI.SupportsVirtualTerminal -and -not [Console]::IsOutputRedirected) ? 'Ansi' : 'Plaintext'
	if (Test-Path -LiteralPath $results) {
		Remove-Item -LiteralPath $results -Force
	}

	# A script block passes its arguments to the new process without changing this one's environment. The process runs
	# with -NonInteractive, so a confirmation prompt fails at once instead of waiting for an answer.
	& $pwsh -NoProfile -NonInteractive -Command {
		param ($Manifest, $RepoRoot, $Files, $Tags, $ExcludeTags, $ResultsPath, $RenderMode)

		$env:POSHSONARR_TEST_MANIFEST = $Manifest
		Import-Module -Name Pester -MinimumVersion 6.2.0 -MaximumVersion 6.99.99 -ErrorAction Stop

		$configuration = New-PesterConfiguration
		$configuration.Run.Path = [string[]] $Files
		$configuration.Run.Exit = $true
		if ($Tags) {
			$configuration.Filter.Tag = [string[]] $Tags
		}
		if ($ExcludeTags) {
			$configuration.Filter.ExcludeTag = [string[]] $ExcludeTags
		}
		$configuration.Output.Verbosity = 'Detailed'
		$configuration.Output.RenderMode = $RenderMode
		$configuration.TestResult.Enabled = $true
		$configuration.TestResult.OutputFormat = 'NUnitXml'
		$configuration.TestResult.OutputPath = $ResultsPath

		# Before each test file, Pester runs any Pester.BeforeContainer.ps1 between the repository root, which it finds
		# from the current directory, and the file's folder, so the run starts in the repository from any directory.
		Set-Location -LiteralPath $RepoRoot
		Invoke-Pester -Configuration $configuration
	} -args $manifest, $repoRoot, $files, $tags, $excludeTags, $results, $renderMode

	# Pester exits with the number of tests, blocks, and files that failed, or -1 when the run itself failed.
	if ($LASTEXITCODE -ne 0) {
		exit 1
	}
}
catch {
	Write-Error -ErrorRecord $_ -ErrorAction Continue
	exit 1
}
exit 0
