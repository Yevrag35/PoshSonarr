#Requires -Version 7.6

<#
.SYNOPSIS
	Show the Sonarr testbed's logs.

.DESCRIPTION
	Shows the last 200 lines of Sonarr's log, or as many as -Tail says. -Follow keeps streaming new
	lines until Ctrl+C. Exits 1 when Compose fails.

.EXAMPLE
	pwsh -NoProfile -File testbed/sonarr/logs.ps1
.EXAMPLE
	pwsh -NoProfile -File testbed/sonarr/logs.ps1 -Tail 50 -Follow
#>
[CmdletBinding()]
param (
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
	Invoke-TestbedCompose @arguments sonarr
}
catch {
	Write-Error -ErrorRecord $_ -ErrorAction Continue
	exit 1
}
exit 0
