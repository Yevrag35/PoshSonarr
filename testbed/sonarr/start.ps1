#Requires -Version 7.6

<#
.SYNOPSIS
	Start the Sonarr testbed, wait until it's ready, and seed its baseline and series.

.DESCRIPTION
	Builds the images whose inputs changed, Sonarr's and the mock metadata service's, starts the
	poshsonarr-testbed Compose project, and waits for Sonarr's health check. Then it makes one status
	call from the host, through the published port, and goes on only when Sonarr answers with the
	pinned version and the testbed's instance name. Next, it creates whatever the baseline lacks, a
	tag labeled testbed-baseline and a root folder at /tv. Then it makes sure Sonarr accepts the
	mock's certificate, adds whichever of the two seeded shows Sonarr lacks, and waits until Sonarr
	has finished adding them, episodes included. Last, it prints the connection values. Running it
	again creates nothing.

	When something fails, it exits 1. A failure while starting or seeding the series first shows the
	containers' state and the last 200 lines of each one's log.

.EXAMPLE
	pwsh -NoProfile -File testbed/sonarr/start.ps1
.EXAMPLE
	pwsh -NoProfile -File testbed/sonarr/start.ps1 -TimeoutSeconds 300
	# a slow machine
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
Import-Module (Join-Path $PSScriptRoot 'testbed.psm1') -Force

try {

	Start-Testbed -TimeoutSeconds $TimeoutSeconds
	Assert-TestbedReady
	$baseline = Initialize-TestbedBaseline
	Initialize-TestbedSeries -BaselineTagId $baseline.TagId

	$settings = Get-TestbedSettings
	Write-Host ''
	Write-Host 'The Sonarr testbed is ready.' -ForegroundColor Green
	Write-Host "  URL      $($settings.BaseUrl)"
	Write-Host "  API key  $($settings.ApiKey)"
	Write-Host "  Connect  Connect-SonarrInstance -Url $($settings.BaseUrl) -ApiKey $($settings.ApiKey)"
}
catch {
	Write-Error -ErrorRecord $_ -ErrorAction Continue
	exit 1
}
exit 0
