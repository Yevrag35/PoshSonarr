#Requires -Version 7.6

<#
.SYNOPSIS
	Run the smoke tests against the running Sonarr testbed.

.DESCRIPTION
	Checks that the running testbed is the one that README.md describes, through raw HTTP requests to
	its API rather than through PoshSonarr. In order, it checks Sonarr's identity, that a request
	without the key or with a wrong one gets a 401, the update lock, the baseline tag and root
	folder, the seeded series and their episodes, lookups through the mock metadata service,
	wanted/missing and the calendar, and that adding a show the mock lacks fails. Then it takes a tag
	and Testbed Lookup Show through a round trip each, adding, reading, changing, and deleting them,
	and last it checks the user that Sonarr runs as and the address that its port is published on.

	It expects the testbed running at its baseline, as start.ps1 and reset.ps1 leave it, and first
	makes the same status call that they make, so it sends nothing to another server that answers on
	the port. A round trip deletes what it added even when one of its checks fails, so a run leaves
	the testbed as it found it.

	It prints a PASS or FAIL line for each check, with what the testbed answered for a failed one, and
	exits 1 when any check fails or the testbed isn't answering.

.EXAMPLE
	pwsh -NoProfile -File testbed/sonarr/test.ps1
#>
[CmdletBinding()]
param ()

$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'testbed.psm1') -Force

$checkCount = 0
$failureCount = 0
$group = ''

# The seeded shows, with the number of episodes in each one's fixture.
$seededShows = @(
	[pscustomobject]@{ TvdbId = 9000001; Title = 'Testbed Ended Show'; EpisodeCount = 6 }
	[pscustomobject]@{ TvdbId = 9000002; Title = 'Testbed Continuing Show'; EpisodeCount = 6 }
)

# Prints one check's result, as a PASS or FAIL line under the current group, and counts it. A failed
# check also shows what the testbed answered, when the caller passes it.
function Write-CheckResult {
	param (
		[Parameter(Mandatory = $true)]
		[string] $Name,

		[Parameter(Mandatory = $true)]
		[bool] $Passed,

		# What the testbed answered instead.
		[Parameter(Mandatory = $false)]
		[string] $Actual
	)

	$script:checkCount++
	$line = '{0}: {1}' -f $script:group, $Name
	if (-not $Passed) {

		$script:failureCount++
		if ($PSBoundParameters.ContainsKey('Actual')) {
			$line += ' (got {0})' -f ([string]::IsNullOrWhiteSpace($Actual) ? 'nothing' : $Actual)
		}
	}
	# A response body can span lines, and each check gets one.
	Write-Host ($Passed ? 'PASS  ' : 'FAIL  ') -ForegroundColor ($Passed ? 'Green' : 'Red') -NoNewline
	Write-Host ($line -replace '\s+', ' ')
}

# Runs one group of checks. An exception ends the group as one more failed check, so the checks after
# it don't run and can't count as passing.
function Invoke-CheckGroup {
	param (
		[Parameter(Mandatory = $true)]
		[string] $Name,

		[Parameter(Mandatory = $true)]
		[scriptblock] $Checks
	)

	$script:group = $Name
	try {
		& $Checks
	}
	catch {
		Write-CheckResult "stopped early. $($_.Exception.Message)" $false
	}
}

# A response's status code and its body, for a failed check to show.
function Format-Response {
	param (
		[Parameter(Mandatory = $true)]
		[object] $Response
	)

	if ([string]::IsNullOrWhiteSpace($Response.Content)) {
		return [string] $Response.StatusCode
	}
	return '{0}: {1}' -f $Response.StatusCode, $Response.Content
}

# The body of a request that adds a show the way seeding adds its shows: monitored, in /tv with the
# Any quality profile, with season folders and all its episodes monitored, and with no searches.
function New-SeriesBody {
	param (
		[Parameter(Mandatory = $true)]
		[int] $TvdbId,

		[Parameter(Mandatory = $true)]
		[string] $Title
	)

	$qualityProfile = (Invoke-TestbedApi qualityprofile -ExpectStatus 200).Body | Where-Object name -eq 'Any'
	if ($null -eq $qualityProfile) {
		throw 'Sonarr has no quality profile named Any.'
	}
	return @{
		tvdbId           = $TvdbId
		title            = $Title
		qualityProfileId = $qualityProfile.id
		rootFolderPath   = '/tv'
		monitored        = $true
		seasonFolder     = $true
		seriesType       = 'standard'
		addOptions       = @{
			monitor                      = 'all'
			searchForMissingEpisodes     = $false
			searchForCutoffUnmetEpisodes = $false
		}
	}
}

try {

	Assert-TestbedReady
	$settings = Get-TestbedSettings
}
catch {
	Write-Error -ErrorRecord $_ -ErrorAction Continue
	exit 1
}

Invoke-CheckGroup 'Identity' {

	$status = Invoke-TestbedApi system/status
	Write-CheckResult 'system/status returns 200' ($status.StatusCode -eq 200) (Format-Response $status)
	Write-CheckResult "version is $($settings.Version)" ($status.Body.version -eq $settings.Version) $status.Body.version
	Write-CheckResult "instanceName is $($settings.InstanceName)" ($status.Body.instanceName -eq $settings.InstanceName) $status.Body.instanceName
	Write-CheckResult 'isDocker is true' ($status.Body.isDocker -eq $true) $status.Body.isDocker
}

Invoke-CheckGroup 'Keys' {

	# A key in the right form that isn't the testbed's.
	$wrongKey = 'fedcba9876543210fedcba9876543210'
	foreach ($path in 'system/status', 'tag') {

		$response = Invoke-TestbedApi $path -NoApiKey
		Write-CheckResult "$path without a key returns 401" ($response.StatusCode -eq 401) (Format-Response $response)
		$response = Invoke-TestbedApi $path -ApiKey $wrongKey
		Write-CheckResult "$path with a wrong key returns 401" ($response.StatusCode -eq 401) (Format-Response $response)
	}
}

Invoke-CheckGroup 'Update lock' {

	$status = (Invoke-TestbedApi system/status -ExpectStatus 200).Body
	Write-CheckResult 'packageUpdateMechanism is docker' ($status.packageUpdateMechanism -eq 'docker') $status.packageUpdateMechanism
	$hostConfig = (Invoke-TestbedApi config/host -ExpectStatus 200).Body
	Write-CheckResult 'updateAutomatically is false' ($hostConfig.updateAutomatically -eq $false) $hostConfig.updateAutomatically
	Write-CheckResult 'updateMechanism is docker' ($hostConfig.updateMechanism -eq 'docker') $hostConfig.updateMechanism
}

Invoke-CheckGroup 'Baseline' {

	$labels = @((Invoke-TestbedApi tag -ExpectStatus 200).Body.label)
	Write-CheckResult 'the testbed-baseline tag exists' ('testbed-baseline' -in $labels) ($labels ? "tags $($labels -join ', ')" : 'no tags')
	$folders = @((Invoke-TestbedApi rootfolder -ExpectStatus 200).Body)
	$folder = $folders | Where-Object { $_.path.TrimEnd('/') -eq '/tv' }
	$found = ($null -ne $folder) ? "/tv with accessible $($folder.accessible)" : ($folders ? "root folders $($folders.path -join ', ')" : 'no root folders')
	Write-CheckResult 'the /tv root folder exists and is accessible' ($null -ne $folder -and $folder.accessible -eq $true) $found
}

Invoke-CheckGroup 'Seeded series' {

	$series = (Invoke-TestbedApi series -ExpectStatus 200).Body
	$ids = @($series.tvdbId | Sort-Object)
	Write-CheckResult 'series lists TVDB IDs 9000001 and 9000002' (9000001 -in $ids -and 9000002 -in $ids) "TVDB IDs $($ids -join ', ')"
	foreach ($show in $seededShows) {

		$match = $series | Where-Object tvdbId -eq $show.TvdbId
		if ($null -eq $match) {

			Write-CheckResult "$($show.Title) has $($show.EpisodeCount) episodes" $false 'no series with its TVDB ID'
			continue
		}
		$episodes = (Invoke-TestbedApi "episode?seriesId=$($match.id)" -ExpectStatus 200).Body
		Write-CheckResult "$($show.Title) has $($show.EpisodeCount) episodes" ($episodes.Count -eq $show.EpisodeCount) "$($episodes.Count) episodes"
	}

	$tag = (Invoke-TestbedApi tag -ExpectStatus 200).Body | Where-Object label -eq 'testbed-baseline'
	if ($null -eq $tag) {
		throw 'There is no testbed-baseline tag to look up.'
	}
	$detail = (Invoke-TestbedApi "tag/detail/$($tag.id)" -ExpectStatus 200).Body
	$ended = $series | Where-Object tvdbId -eq 9000001
	Write-CheckResult 'the testbed-baseline tag lists Testbed Ended Show' ($null -ne $ended -and $ended.id -in $detail.seriesIds) "seriesIds $(@($detail.seriesIds) -join ', ')"
}

Invoke-CheckGroup 'Mock lookup' {

	$found = (Invoke-TestbedApi 'series/lookup?term=tvdb:9000003' -ExpectStatus 200).Body
	Write-CheckResult 'tvdb:9000003 finds Testbed Lookup Show alone' ($found.Count -eq 1 -and $found[0].title -eq 'Testbed Lookup Show') "$($found.Count) shows: $($found.title -join ', ')"
	$found = (Invoke-TestbedApi 'series/lookup?term=testbed' -ExpectStatus 200).Body
	$ids = @($found.tvdbId | Sort-Object)
	Write-CheckResult 'testbed finds all three shows' (($ids -join ',') -eq '9000001,9000002,9000003') "TVDB IDs $($ids -join ', ')"
	$hostConfig = (Invoke-TestbedApi config/host -ExpectStatus 200).Body
	Write-CheckResult 'certificateValidation is disabledForLocalAddresses' ($hostConfig.certificateValidation -eq 'disabledForLocalAddresses') $hostConfig.certificateValidation
}

# These run before the round trips, which add Testbed Lookup Show and its two aired episodes for a
# while.
Invoke-CheckGroup 'Wanted and calendar' {

	$missing = (Invoke-TestbedApi wanted/missing -ExpectStatus 200).Body
	Write-CheckResult 'wanted/missing reports 9 episodes' ($missing.totalRecords -eq 9) "totalRecords $($missing.totalRecords)"
	$calendar = (Invoke-TestbedApi 'calendar?start=2099-01-01&end=2099-01-31' -ExpectStatus 200).Body
	Write-CheckResult 'the calendar from 2099-01-01 to 2099-01-31 holds 3 episodes' ($calendar.Count -eq 3) "$($calendar.Count) episodes"
}

Invoke-CheckGroup 'Unknown show' {

	$response = Invoke-TestbedApi series -Method Post -Body (New-SeriesBody -TvdbId 9009999 -Title 'Testbed Unknown Show')
	$notFound = $response.StatusCode -eq 400 -and $response.Content -match 'A series with this ID was not found'
	Write-CheckResult "adding TVDB ID 9009999 returns 400 with 'A series with this ID was not found'" $notFound (Format-Response $response)
}

Invoke-CheckGroup 'Tag round trip' {

	# A label that no other run uses, so that a tag a failed run left behind can't get in the way.
	$label = 'testbed-temp-' + [System.Security.Cryptography.RandomNumberGenerator]::GetHexString(8, $true)
	$renamed = "$label-renamed"
	$response = Invoke-TestbedApi tag -Method Post -Body @{ label = $label }
	$id = $response.Body.id
	Write-CheckResult "POST of $label returns 201 with an ID" ($response.StatusCode -eq 201 -and $null -ne $id) (Format-Response $response)
	if ($null -eq $id) {
		throw 'There is no tag ID for the rest of the round trip.'
	}
	try {

		$response = Invoke-TestbedApi "tag/$id"
		Write-CheckResult 'GET by ID returns the label' ($response.StatusCode -eq 200 -and $response.Body.label -eq $label) (Format-Response $response)
		$response = Invoke-TestbedApi "tag/$id" -Method Put -Body @{ id = $id; label = $renamed }
		Write-CheckResult 'PUT with a new label returns 202' ($response.StatusCode -eq 202) (Format-Response $response)
		$response = Invoke-TestbedApi "tag/$id"
		Write-CheckResult 'GET by ID returns the new label' ($response.StatusCode -eq 200 -and $response.Body.label -eq $renamed) (Format-Response $response)
	}
	finally {

		# The DELETE runs even when a step before it throws, so the round trip leaves no tag behind.
		$response = Invoke-TestbedApi "tag/$id" -Method Delete
		Write-CheckResult 'DELETE returns 200' ($response.StatusCode -eq 200) (Format-Response $response)
	}
	$response = Invoke-TestbedApi "tag/$id"
	Write-CheckResult 'GET by ID then returns 404' ($response.StatusCode -eq 404) (Format-Response $response)
}

Invoke-CheckGroup 'Series round trip' {

	$response = Invoke-TestbedApi series -Method Post -Body (New-SeriesBody -TvdbId 9000003 -Title 'Testbed Lookup Show')
	$id = $response.Body.id
	Write-CheckResult 'POST of Testbed Lookup Show returns 201 with an ID' ($response.StatusCode -eq 201 -and $null -ne $id) (Format-Response $response)
	if ($null -eq $id) {
		throw 'There is no series ID for the rest of the round trip.'
	}
	try {

		# Sonarr fetches the episodes after it answers the add, and it saves the series again once it
		# has them, so the round trip goes on only when Sonarr has finished adding it.
		Wait-TestbedSeries -Series ([pscustomobject]@{ Id = $id; Title = 'Testbed Lookup Show'; EpisodeCount = 2 })
		$episodes = (Invoke-TestbedApi "episode?seriesId=$id" -ExpectStatus 200).Body
		Write-CheckResult 'Sonarr finishes adding it, with its 2 episodes, within 60 seconds' ($episodes.Count -eq 2) "$($episodes.Count) episodes"
		$response = Invoke-TestbedApi "series/$id"
		Write-CheckResult 'GET by ID returns 200 with the show' ($response.StatusCode -eq 200 -and $response.Body.tvdbId -eq 9000003) (Format-Response $response)
		$series = $response.Body
		$series.monitored = $false
		$response = Invoke-TestbedApi "series/$id" -Method Put -Body $series
		Write-CheckResult 'PUT with monitored false returns 202 with the show unmonitored' ($response.StatusCode -eq 202 -and $response.Body.monitored -eq $false) (Format-Response $response)
	}
	finally {

		# The DELETE runs even when a step before it throws, so the round trip leaves the baseline's
		# series as they were.
		$response = Invoke-TestbedApi "series/$id" -Method Delete
		Write-CheckResult 'DELETE returns 200' ($response.StatusCode -eq 200) (Format-Response $response)
	}
	$response = Invoke-TestbedApi "series/$id"
	Write-CheckResult 'GET by ID then returns 404' ($response.StatusCode -eq 404) (Format-Response $response)
}

Invoke-CheckGroup 'Container' {

	$uid = (Invoke-TestbedCompose exec -T sonarr id -u) -join ' '
	Write-CheckResult 'Sonarr runs as UID 1000' ($uid -eq '1000') $uid
	$address = '127.0.0.1:{0}' -f ([uri] $settings.BaseUrl).Port
	$published = (Invoke-TestbedCompose port sonarr 8989) -join ' '
	Write-CheckResult "port 8989 is published on $address" ($published -eq $address) $published
}

Write-Host ''
if ($failureCount -gt 0) {

	Write-Host "$failureCount of $checkCount checks failed." -ForegroundColor Red
	exit 1
}
Write-Host "All $checkCount checks passed." -ForegroundColor Green
exit 0
