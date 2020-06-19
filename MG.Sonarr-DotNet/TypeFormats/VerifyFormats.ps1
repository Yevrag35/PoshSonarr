[CmdletBinding()]
param
(
    [Parameter(Mandatory=$true)]
    [string] $ReleaseDirectory = "..\bin\Release"
)

$dll = Get-ChildItem -Path $ReleaseDirectory -Filter "MG.Sonarr.dll" -File
Import-Module $dll.FullName -Force -ErrorAction Stop

$allFormats = Get-ChildItem -Path $PSScriptRoot -Filter *.ps1xml -File
foreach ($formatFile in $allFormats)
{
    $xml = new-object xml
    $xml.Load($formatFile.FullName)
    $view = $xml.Configuration.ViewDefinitions.View
    $type = $view.ViewSelectedBy.TypeName
    $control = $view | Get-Member -MemberType Properties | ? Name -like "*Control" | % Name
    [string[]] $propNames = switch ($control)
    {
        "TableControl"
        {
            $item = $view.TableControl.TableRowEntries.TableRowEntry.TableColumnItems.TableColumnItem
            $hasProps = @($item | Get-Member -MemberType Properties | ? Name -eq 'PropertyName').Count -gt 0
            if ($hasProps)
            {
                $item.PropertyName
            }
        }
        "ListControl"
        {

        }
    }

    [type]::GetType('MG.Sonarr.Results.AlternateTitle')

}