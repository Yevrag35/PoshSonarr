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

# Shows the project's containers and the last 200 lines of its logs, for a failure to point at. A
# failure in here is reported and skipped, so it can't take the place of the one that asked for it.
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

# Builds the image when the Dockerfile or compose.yaml changed, starts the project, and waits for
# Sonarr's health check. up reuses an existing image even after either file changes, so --build goes
# on every call, and the layer cache makes an unchanged rebuild quick. --wait exits nonzero when the
# container exits or turns unhealthy, and --wait-timeout bounds the wait.
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

Export-ModuleMember -Function Invoke-TestbedCompose, Get-TestbedSettings, Invoke-TestbedApi, Start-Testbed, Assert-TestbedReady, Initialize-TestbedBaseline
