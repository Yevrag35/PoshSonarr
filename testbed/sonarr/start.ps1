#Requires -Version 7.6

<#
.SYNOPSIS
	Start the Sonarr testbed, wait until it's ready, and seed its baseline.

.DESCRIPTION
	Builds the image when the Dockerfile or compose.yaml changed, starts the poshsonarr-testbed
	Compose project, and waits for Sonarr's health check. Then it makes one status call from the
	host, through the published port, and goes on only when Sonarr answers with the pinned version
	and the testbed's instance name. Last, it creates whatever the baseline lacks, a tag labeled
	testbed-baseline and a root folder at /tv, and prints the connection values. Running it again
	creates nothing.

	When something fails, it shows the container's state and the last 200 lines of its logs, and
	exits 1.

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
	$null = Initialize-TestbedBaseline

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
