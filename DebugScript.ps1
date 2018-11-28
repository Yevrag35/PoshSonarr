[CmdletBinding()]
param()
$name = "PoshSonarr"
$curDir = Split-Path -Parent $MyInvocation.MyCommand.Definition;
$mod = "$curDir\$name.psd1";
Import-Module $mod;

Connect-Sonarr -Url 'http://garvmedia:8989' -ApiKey '29b223248cbf4317bc1605927bca638d';
