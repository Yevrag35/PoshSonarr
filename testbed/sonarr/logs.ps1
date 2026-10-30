#Requires -Version 7.6

<#
.SYNOPSIS
	Show the Sonarr testbed's logs.

.DESCRIPTION
	Shows the last 200 lines of Sonarr's log, or as many as -Tail says. -Service skyhook shows the
	mock metadata service's log instead, which holds a line for each request Sonarr made to it.
	-Follow keeps streaming new lines until Ctrl+C. Exits 1 when Compose fails.

.EXAMPLE
	pwsh -NoProfile -File testbed/sonarr/logs.ps1
.EXAMPLE
	pwsh -NoProfile -File testbed/sonarr/logs.ps1 -Tail 50 -Follow
.EXAMPLE
	pwsh -NoProfile -File testbed/sonarr/logs.ps1 -Service skyhook
#>
[CmdletBinding()]
param (
	# The service whose log to show: sonarr, or skyhook for the mock metadata service.
	[Parameter(Mandatory = $false)]
	[ValidateSet('sonarr', 'skyhook')]
	[string] $Service = 'sonarr',

	# How many of the latest lines to show. With -Follow, 0 shows only lines that arrive afterward.
	[Parameter(Mandatory = $false)]
	[ValidateRange(0, [int]::MaxValue)]
	[int] $Tail = 200,

	# Keep streaming new lines until Ctrl+C.
	[Parameter(Mandatory = $false)]
	[switch] $Follow
)

$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'testbed.psm1') -Force

$arguments = @('logs', '--tail', $Tail)
if ($Follow) {
	$arguments += '--follow'
}
try {
	Invoke-TestbedCompose @arguments $Service
}
catch {
	Write-Error -ErrorRecord $_ -ErrorAction Continue
	exit 1
}
exit 0
