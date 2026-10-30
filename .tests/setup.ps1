#Requires -Version 7.6

# Every test file's top-level BeforeAll block dot-sources this script. It imports the module that .tests/run.ps1 built
# and staged, checks that the testbed answers as the testbed and is at its baseline, and connects the module to it. It
# leaves each file $TestbedSettings, $Fixtures, and the functions below, and through testbed.psm1, Invoke-TestbedApi,
# Wait-TestbedSeries, and Invoke-TestbedCompose.

# The runner names the staged manifest. Without it, a run that Invoke-Pester started directly could test a stale build,
# or a copy of PoshSonarr installed from the PowerShell Gallery, which an import by name could find first.
if ([string]::IsNullOrEmpty($env:POSHSONARR_TEST_MANIFEST)) {
	throw "PoshSonarr's tests run only through the runner, which builds and stages the module and names its manifest in POSHSONARR_TEST_MANIFEST: pwsh -NoProfile -File .tests/run.ps1"
}
Import-Module -Name $env:POSHSONARR_TEST_MANIFEST -ErrorAction Stop

# The testbed's URL, key, version, and instance name come from its compose.yaml through this module, so they live in
# one place, and a SONARR_TESTBED_PORT override reaches the suite too.
Import-Module -Name (Join-Path $PSScriptRoot '../testbed/sonarr/testbed.psm1') -ErrorAction Stop

# Nothing goes to the port until it has answered with the pinned version and the testbed's instance name.
Assert-TestbedReady
$TestbedSettings = Get-TestbedSettings

# The fixtures' facts, which the mock metadata service's shows and their fixed air dates make the same on every run.
# testbed/sonarr/test.ps1 keeps its own copy of them, so a change to the fixtures updates both.
$Fixtures = @{
	Tag           = 'testbed-baseline'
	RootFolder    = '/tv'
	SeededSeries  = @(
		@{ TvdbId = 9000001; Title = 'Testbed Ended Show'; EpisodeCount = 6; Tags = @('testbed-baseline') }
		@{ TvdbId = 9000002; Title = 'Testbed Continuing Show'; EpisodeCount = 6; Tags = @() }
	)
	LookupSeries  = @{ TvdbId = 9000003; Title = 'Testbed Lookup Show'; EpisodeCount = 2 }
	SearchTerm    = 'testbed'
	MissingCount  = 9
	Calendar      = @{ Start = '2099-01-01'; End = '2099-01-31'; EpisodeCount = 3 }
	UnknownTvdbId = 9009999
}

# Tests rely on the baseline that reset.ps1 restores, so a file stops before its tests when the testbed has drifted from
# it, as when an earlier test couldn't remove what it created. The check runs in a script block of its own, so its
# variables don't reach the test files.
& {
	$format = { param ($items, $noun) if ($items.Count -eq 0) { "no $noun" } else { "$noun " + (($items | ForEach-Object { "'$_'" }) -join ', ') } }

	$drift = @(
		$tags = @((Invoke-TestbedApi tag -ExpectStatus 200).Body.label | Sort-Object)
		if (($tags -join ',') -cne $Fixtures.Tag) {
			'it has {0} instead of the tag {1}' -f (& $format $tags 'tags'), "'$($Fixtures.Tag)'"
		}

		$tvdbIds = @((Invoke-TestbedApi series -ExpectStatus 200).Body.tvdbId | Sort-Object)
		$seeded = @($Fixtures.SeededSeries.TvdbId | Sort-Object)
		if (($tvdbIds -join ',') -ne ($seeded -join ',')) {
			'it has {0} instead of the series {1}' -f (& $format $tvdbIds 'series with TVDB IDs'), ($seeded -join ' and ')
		}

		$folders = @((Invoke-TestbedApi rootfolder -ExpectStatus 200).Body.path | ForEach-Object { $_.TrimEnd('/') } | Sort-Object)
		if (($folders -join ',') -cne $Fixtures.RootFolder) {
			'it has {0} instead of the root folder {1}' -f (& $format $folders 'root folders'), "'$($Fixtures.RootFolder)'"
		}
	)
	if ($drift.Count -gt 0) {
		throw "The testbed isn't at its baseline: $($drift -join '; '). Run the suite with -Reset, which resets the testbed first: pwsh -NoProfile -File .tests/run.ps1 -Reset"
	}
}

# Returns a name for whatever a test creates: pester- and 8 random hex digits, so that no two runs share one and a
# leftover is easy to spot.
function New-TestName {
	return 'pester-' + [System.Security.Cryptography.RandomNumberGenerator]::GetHexString(8, $true)
}

# Runs a script in a new PowerShell process that has imported the module under test, with no connection, and returns
# the lines that it writes, its errors included, as strings. Pester runs every test inside a try block, where an error
# that ends its statement and one that ends the script both jump to the catch, so a test of how far an error reaches
# runs the command here, and checks for a line that a statement after it writes. The script goes to the process as an
# encoded command with text output: under -OutputFormat xml, which PowerShell uses when it passes a script block to
# pwsh, the process stops at the first error that ends a statement.
function Invoke-TestProcess {
	param (
		[Parameter(Mandatory = $true)]
		[string] $Script
	)

	$text = "`$PSStyle.OutputRendering = 'PlainText'`nImport-Module -Name `$env:POSHSONARR_TEST_MANIFEST -ErrorAction Stop`n$Script"
	$encoded = [System.Convert]::ToBase64String([System.Text.Encoding]::Unicode.GetBytes($text))
	& ([System.Environment]::ProcessPath) -NoProfile -NonInteractive -EncodedCommand $encoded 2>&1 | ForEach-Object { "$_" }
}

Connect-SonarrInstance -Url $TestbedSettings.BaseUrl -ApiKey $TestbedSettings.ApiKey -ErrorAction Stop
