#Requires -Version 7.6

# Helpers that the Sonarr testbed's scripts share. Each one works on the poshsonarr-testbed Compose
# project that compose.yaml, beside this module, defines, and the testbed's URL, API key, Sonarr
# version, and instance name come from that file through docker compose config, so no script
# repeats them.

# A module's functions see the module's preference variables rather than the calling script's, so
# the module sets its own: a failing cmdlet or a docker call that exits nonzero throws, and the web
# cmdlets draw no progress bar.
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true
$ProgressPreference = 'SilentlyContinue'

$composeFile = Join-Path $PSScriptRoot 'compose.yaml'

# The project name goes on every call as well as in compose.yaml, because COMPOSE_PROJECT_NAME,
# from the environment or a .env file, outranks the file's own name: and would point the call at
# another project.
$projectName = 'poshsonarr-testbed'

# Get-TestbedSettings runs docker compose config, so the API helpers read it once and keep it here.
$settings = $null

# The mock metadata service's show fixtures, one file per TVDB ID.
$showFixtures = Join-Path $PSScriptRoot 'skyhook/v1/tvdb/shows/en'

# The mock's shows that seeding adds, by TVDB ID, and whether each gets the testbed-baseline tag. The
# third show, 9000003, stays on the mock alone, for tests to look up, add, and delete.
$seededShows = @(
	[pscustomobject]@{ TvdbId = 9000001; Tagged = $true }
	[pscustomobject]@{ TvdbId = 9000002; Tagged = $false }
)

# Runs docker compose on the testbed's project with the arguments it's given, and throws when it
# exits nonzero. This is a simple function rather than an advanced one so that docker's own flags
# pass through: an advanced function would bind -d to -Debug and -v to -Verbose.
function Invoke-TestbedCompose {
	docker compose --project-name $projectName --file $composeFile @args
}

# The testbed's URL, API key, Sonarr version, and instance name, as Compose reads them from
# compose.yaml after applying SONARR_TESTBED_PORT. Compose reports the published port as a string.
function Get-TestbedSettings {

	$sonarr = (Invoke-TestbedCompose config --format json | ConvertFrom-Json).services.sonarr
	return [pscustomobject]@{
		BaseUrl      = 'http://127.0.0.1:{0}' -f $sonarr.ports[0].published
		ApiKey       = $sonarr.environment.SONARR__AUTH__APIKEY
		Version      = $sonarr.build.args.SONARR_VERSION
		InstanceName = $sonarr.environment.SONARR__APP__INSTANCENAME
	}
}

# Shows the project's containers and the last 200 lines of each one's log, for a failure to point at.
# A failure in here is reported and skipped, so it can't take the place of the one that asked for it.
function Show-TestbedDiagnostics {

	Write-Host 'Testbed containers:' -ForegroundColor Yellow
	try {
		Invoke-TestbedCompose ps --all | Out-Host
	}
	catch {
		Write-Warning "Couldn't list the testbed's containers: $($_.Exception.Message)"
	}
	Write-Host 'Testbed logs, last 200 lines:' -ForegroundColor Yellow
	try {
		Invoke-TestbedCompose logs --tail 200 | Out-Host
	}
	catch {
		Write-Warning "Couldn't read the testbed's logs: $($_.Exception.Message)"
	}
}

# Sends one request to the testbed's API and returns its status code, its body parsed as JSON
# ($null when it's empty or isn't JSON), and the body's text. -SkipHttpErrorCheck makes an error
# status a response rather than an exception, so a caller can check for one, and -ExpectStatus
# throws unless the status is one that it lists. The testbed's key goes in the X-Api-Key header
# unless -ApiKey sends another or -NoApiKey sends none.
function Invoke-TestbedApi {
	[CmdletBinding(DefaultParameterSetName = 'TestbedKey')]
	param (
		# The path after /api/v3/, query string included, such as 'tag/1' or 'series/lookup?term=testbed'.
		[Parameter(Mandatory = $true, Position = 0)]
		[string] $Path,

		[Parameter(Mandatory = $false)]
		[Microsoft.PowerShell.Commands.WebRequestMethod] $Method = 'Get',

		# Sent as JSON.
		[Parameter(Mandatory = $false)]
		[object] $Body,

		# The status codes that count as success; any other throws. Omit it to accept every status.
		[Parameter(Mandatory = $false)]
		[int[]] $ExpectStatus,

		[Parameter(Mandatory = $true, ParameterSetName = 'OtherKey')]
		[string] $ApiKey,

		[Parameter(Mandatory = $true, ParameterSetName = 'NoKey')]
		[switch] $NoApiKey
	)

	$script:settings ??= Get-TestbedSettings
	$request = @{
		Uri                      = '{0}/api/v3/{1}' -f $script:settings.BaseUrl, $Path
		Method                   = $Method
		Headers                  = @{}
		SkipHttpErrorCheck       = $true
		ConnectionTimeoutSeconds = 30
	}
	switch ($PSCmdlet.ParameterSetName) {
		'TestbedKey' { $request.Headers['X-Api-Key'] = $script:settings.ApiKey }
		'OtherKey' { $request.Headers['X-Api-Key'] = $ApiKey }
	}
	if ($PSBoundParameters.ContainsKey('Body')) {

		$request.Body = ConvertTo-Json -InputObject $Body -Depth 20 -Compress
		$request.ContentType = 'application/json'
	}

	$response = Invoke-WebRequest @request

	# Content is a string for a text media type and a byte array otherwise, as for a body that comes
	# without a Content-Type.
	$text = ($response.Content -is [byte[]]) ? [System.Text.Encoding]::UTF8.GetString($response.Content) : [string] $response.Content
	$json = $null
	if (-not [string]::IsNullOrWhiteSpace($text)) {

		try {
			# -NoEnumerate keeps an array an array. Without it, an empty one would come back as $null
			# and a one-item one as its item.
			$json = ConvertFrom-Json -InputObject $text -NoEnumerate
		}
		catch {
			$json = $null
		}
	}

	$result = [pscustomobject]@{
		StatusCode = [int] $response.StatusCode
		Body       = $json
		Content    = $text
	}
	if ($ExpectStatus -and $result.StatusCode -notin $ExpectStatus) {

		$verb = $Method.ToString().ToUpperInvariant()
		throw ('{0} {1} returned {2} instead of {3}: {4}' -f $verb, $Path, $result.StatusCode, ($ExpectStatus -join ' or '), $text)
	}
	return $result
}

# Builds the images whose inputs changed, Sonarr's and the mock metadata service's, starts the
# project, and waits for Sonarr's health check. up reuses an existing image even after its
# Dockerfile, the files it copies, or its build arguments change, so --build goes on every call, and
# the layer cache makes an unchanged rebuild quick. --wait exits nonzero when a container exits or
# turns unhealthy, and --wait-timeout bounds the wait.
function Start-Testbed {
	param (
		[Parameter(Mandatory = $false)]
		[int] $TimeoutSeconds = 120
	)

	try {
		Invoke-TestbedCompose up --detach --build --wait --wait-timeout $TimeoutSeconds
	}
	catch [System.Management.Automation.NativeCommandExitException] {

		Show-TestbedDiagnostics
		throw "docker compose up exited with code $($_.Exception.ExitCode), so the testbed isn't ready. Compose's own message comes before the container's state and logs above."
	}
}

# Makes one status call from the host, through the published port, and throws unless it gets a 200
# with JSON that names the pinned version and the testbed's instance name. The scripts seed nothing
# until this passes, so they never write to some other server that answers on the port.
function Assert-TestbedReady {

	$script:settings ??= Get-TestbedSettings
	try {

		$status = Invoke-TestbedApi system/status
		$problem = if ($status.StatusCode -ne 200 -or $null -eq $status.Body) {
			"system/status returned $($status.StatusCode): $($status.Content)"
		}
		elseif ($status.Body.version -ne $script:settings.Version) {
			"Sonarr reports version $($status.Body.version) instead of $($script:settings.Version)"
		}
		elseif ($status.Body.instanceName -ne $script:settings.InstanceName) {
			"Sonarr reports the instance name '$($status.Body.instanceName)' instead of '$($script:settings.InstanceName)'"
		}
	}
	catch {
		$problem = "system/status failed: $($_.Exception.Message)"
	}
	if ($problem) {

		Show-TestbedDiagnostics
		throw "$($script:settings.BaseUrl) isn't answering as the testbed. $problem"
	}
}

# Makes sure the baseline exists: a tag labeled testbed-baseline and a root folder at /tv. Each is
# matched by its label or path in what the API lists, and only a missing one gets created, so a
# second run changes nothing. The folder has to be looked up first, because Sonarr answers a second
# root folder at the same path with a 400. Returns both IDs as the API reports them.
function Initialize-TestbedBaseline {

	$tag = (Invoke-TestbedApi tag -ExpectStatus 200).Body | Where-Object label -eq 'testbed-baseline'
	if ($null -eq $tag) {

		$tag = (Invoke-TestbedApi tag -Method Post -Body @{ label = 'testbed-baseline' } -ExpectStatus 201).Body
		Write-Host "Created the testbed-baseline tag, ID $($tag.id)."
	}
	$folder = (Invoke-TestbedApi rootfolder -ExpectStatus 200).Body | Where-Object { $_.path.TrimEnd('/') -eq '/tv' }
	if ($null -eq $folder) {

		$folder = (Invoke-TestbedApi rootfolder -Method Post -Body @{ path = '/tv' } -ExpectStatus 201).Body
		Write-Host "Created the /tv root folder, ID $($folder.id)."
	}
	return [pscustomobject]@{
		TagId        = $tag.id
		RootFolderId = $folder.id
	}
}

# Waits until Sonarr has finished adding each series it's given. Sonarr answers an add before the
# series has episodes, because it fetches them in a refresh that it queues. After storing them, the
# refresh runs the post-add actions, which set which episodes are monitored, save the series again,
# and last clear its addOptions. So a series counts as added once it has as many episodes as it
# should and no addOptions, and from then on a request that changes it can't race Sonarr's own save.
# -TimeoutSeconds bounds the wait for all of them together, and running out throws.
function Wait-TestbedSeries {
	[CmdletBinding()]
	param (
		# Each series to wait for, as an object with its Id, its Title, and the EpisodeCount it should
		# reach.
		[Parameter(Mandatory = $true)]
		[object[]] $Series,

		# How long to wait for all of them, in seconds.
		[Parameter(Mandatory = $false)]
		[ValidateRange(1, [int]::MaxValue)]
		[int] $TimeoutSeconds = 60
	)

	$deadline = [datetime]::UtcNow.AddSeconds($TimeoutSeconds)
	$pending = $Series
	while ($true) {

		$pending = @(foreach ($item in $pending) {

			$stored = (Invoke-TestbedApi "series/$($item.Id)" -ExpectStatus 200).Body
			$episodes = (Invoke-TestbedApi "episode?seriesId=$($item.Id)" -ExpectStatus 200).Body
			if ($null -ne $stored.addOptions -or $episodes.Count -lt $item.EpisodeCount) {
				$item
			}
		})
		if ($pending.Count -eq 0) {
			return
		}
		if ([datetime]::UtcNow -ge $deadline) {

			$states = foreach ($item in $pending) {

				$episodes = (Invoke-TestbedApi "episode?seriesId=$($item.Id)" -ExpectStatus 200).Body
				'{0} ({1} of {2} episodes)' -f $item.Title, $episodes.Count, $item.EpisodeCount
			}
			throw ("Sonarr didn't finish adding {0} within {1} seconds." -f ($states -join ' and '), $TimeoutSeconds)
		}
		Start-Sleep -Milliseconds 500
	}
}

# Makes sure Sonarr has the seeded shows from the mock metadata service: Testbed Ended Show, tagged
# testbed-baseline, and Testbed Continuing Show. First it makes sure that certificateValidation is
# disabledForLocalAddresses, so that Sonarr accepts the mock's self-signed certificate. The setting
# lives in the database, which a reset wipes, so every run checks it, and because PUT config/host
# saves every field it's sent, the whole resource that GET returned goes back. Then each show that GET
# series doesn't list by TVDB ID gets added: monitored, in /tv, with season folders and all its
# episodes monitored, and with no searches. Last, it waits until Sonarr has finished adding the new
# shows, each with as many episodes as its fixture, for at most -TimeoutSeconds. A failure shows the
# containers and both services' logs.
function Initialize-TestbedSeries {
	[CmdletBinding()]
	param (
		# The testbed-baseline tag's ID, as Initialize-TestbedBaseline returns it.
		[Parameter(Mandatory = $true)]
		[int] $BaselineTagId,

		# How long to wait for Sonarr to finish adding the new shows, in seconds.
		[Parameter(Mandatory = $false)]
		[ValidateRange(1, [int]::MaxValue)]
		[int] $TimeoutSeconds = 60
	)

	try {

		$hostConfig = (Invoke-TestbedApi config/host -ExpectStatus 200).Body
		if ($hostConfig.certificateValidation -ne 'disabledForLocalAddresses') {

			$hostConfig.certificateValidation = 'disabledForLocalAddresses'
			$null = Invoke-TestbedApi config/host -Method Put -Body $hostConfig -ExpectStatus 202
			Write-Host 'Set certificateValidation to disabledForLocalAddresses.'
		}

		$existing = (Invoke-TestbedApi series -ExpectStatus 200).Body
		$missing = @($seededShows | Where-Object TvdbId -notin $existing.tvdbId)
		if ($missing.Count -eq 0) {
			return
		}

		$qualityProfile = (Invoke-TestbedApi qualityprofile -ExpectStatus 200).Body | Where-Object name -eq 'Any'
		if ($null -eq $qualityProfile) {
			throw 'Sonarr has no quality profile named Any.'
		}
		$added = foreach ($show in $missing) {

			$fixture = Get-Content -Raw -LiteralPath (Join-Path $showFixtures "$($show.TvdbId).json") | ConvertFrom-Json
			$body = @{
				tvdbId           = $show.TvdbId
				title            = $fixture.title
				qualityProfileId = $qualityProfile.id
				rootFolderPath   = '/tv'
				monitored        = $true
				seasonFolder     = $true
				seriesType       = 'standard'
				tags             = @(if ($show.Tagged) { $BaselineTagId })
				addOptions       = @{
					monitor                      = 'all'
					searchForMissingEpisodes     = $false
					searchForCutoffUnmetEpisodes = $false
				}
			}
			$series = (Invoke-TestbedApi series -Method Post -Body $body -ExpectStatus 201).Body
			Write-Host "Added $($series.title), series ID $($series.id)."
			[pscustomobject]@{
				Id           = $series.id
				Title        = $series.title
				EpisodeCount = $fixture.episodes.Count
			}
		}
		Wait-TestbedSeries -Series $added -TimeoutSeconds $TimeoutSeconds
	}
	catch {

		Show-TestbedDiagnostics
		throw
	}
}

Export-ModuleMember -Function Invoke-TestbedCompose, Get-TestbedSettings, Invoke-TestbedApi, Start-Testbed, Assert-TestbedReady, Initialize-TestbedBaseline, Wait-TestbedSeries, Initialize-TestbedSeries
