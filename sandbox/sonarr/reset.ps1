#Requires -Version 7.6

<#
.SYNOPSIS
	Return the Sonarr sandbox to its baseline: remove its containers and volumes, then start it fresh.

.DESCRIPTION
	Runs down --volumes --remove-orphans on the poshsonarr-sandbox Compose project, which removes the
	project's containers, its network, and its config and tv volumes, and with them everything
	Sonarr stored. Nothing outside the project is touched, and the images stay for the next build to
	reuse. Then it runs start.ps1, which builds if needed, starts, waits, checks, seeds the baseline
	and the series, and prints the connection values.

	When something fails, it exits 1, and a failure while start.ps1 starts the sandbox or adds the
	seeded series first shows the containers' state and the last 200 lines of each one's log.

.EXAMPLE
	pwsh -NoProfile -File sandbox/sonarr/reset.ps1
#>
[CmdletBinding()]
param (
	# How long to wait for Sonarr's health check once the container has started, in seconds. Building
	# the image doesn't count against it.
	[Parameter(Mandatory = $false)]
	[ValidateRange(1, [int]::MaxValue)]
	[int] $TimeoutSeconds = 120
)

$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'sandbox.psm1') -Force

try {
	Invoke-SandboxCompose down --volumes --remove-orphans
}
catch {
	Write-Error -ErrorRecord $_ -ErrorAction Continue
	exit 1
}

# start.ps1 catches its own failures and exits 1, which ends only that script and leaves the code in
# $LASTEXITCODE here.
& (Join-Path $PSScriptRoot 'start.ps1') -TimeoutSeconds $TimeoutSeconds
exit $LASTEXITCODE
