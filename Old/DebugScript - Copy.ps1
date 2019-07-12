[CmdletBinding()]
param()

$loadThis = "PoshSonarr.psd1";
$url = "https://sonarr-api.yevrag35.com";
$key = '126e559bbe654278aa1736a898b32dd6';
$curDir = Split-Path -Parent $MyInvocation.MyCommand.Definition;
Import-Module "$curDir\$loadThis" -ea Stop;

Connect-Sonarr -Url $url -ApiKey $key -NoApiPrefix;