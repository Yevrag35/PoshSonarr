#Requires -Version 7.6

<#
.SYNOPSIS
	Stop the Sonarr testbed, keeping its containers and volumes.

.DESCRIPTION
	Runs stop on the poshsonarr-testbed Compose project, which stops Sonarr and the mock metadata
	service. Sonarr gets a SIGTERM and shuts down cleanly, and its container keeps the config and tv
	volumes, so the next start.ps1 brings back the same data. Exits 1 when Compose fails.

.EXAMPLE
	pwsh -NoProfile -File testbed/sonarr/stop.ps1
#>
[CmdletBinding()]
param ()

$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'testbed.psm1') -Force

try {
	Invoke-TestbedCompose stop
}
catch {
	Write-Error -ErrorRecord $_ -ErrorAction Continue
	exit 1
}
exit 0
