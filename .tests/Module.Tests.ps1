#Requires -Version 7.6
#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '6.2.0'; MaximumVersion = '6.99.99' }

BeforeAll {
	. "$PSScriptRoot/setup.ps1"

	$module = Get-Module -Name PoshSonarr
	$moduleFolder = [System.IO.Path]::GetFullPath((Split-Path -Parent $env:POSHSONARR_TEST_MANIFEST))
	$manifest = Import-PowerShellDataFile -LiteralPath $env:POSHSONARR_TEST_MANIFEST
}

Describe 'PoshSonarr' {
	It 'is loaded from the staged folder' {
		@($module) | Should -HaveCount 1
		$module.ModuleBase | Should -Be $moduleFolder
		$module.ModuleType | Should -Be ([System.Management.Automation.ModuleType]::Binary)

		# The cmdlets come from the staged copy of the root assembly, not from a build folder or an installed module.
		[MG.Sonarr.Next.Shell.Build.Module].Assembly.Location | Should -Be (Join-Path $moduleFolder $manifest.RootModule)
	}

	It 'exports the cmdlets that its manifest lists' {
		$exported = @($module.ExportedCmdlets.Keys)
		$manifest.CmdletsToExport | Should -Not -BeNullOrEmpty
		$manifest.CmdletsToExport | Where-Object { $_ -notin $exported } | Should -BeNullOrEmpty -Because 'every cmdlet that the manifest lists is exported'
		$exported | Where-Object { $_ -notin $manifest.CmdletsToExport } | Should -BeNullOrEmpty -Because 'only the cmdlets that the manifest lists are exported'
	}

	It 'exports the aliases that its manifest lists' {
		$exported = @($module.ExportedAliases.Keys)
		$manifest.AliasesToExport | Should -Not -BeNullOrEmpty
		$manifest.AliasesToExport | Where-Object { $_ -notin $exported } | Should -BeNullOrEmpty -Because 'every alias that the manifest lists is exported'
		$exported | Where-Object { $_ -notin $manifest.AliasesToExport } | Should -BeNullOrEmpty -Because 'only the aliases that the manifest lists are exported'
	}

	It 'resolves each alias to one of its exported cmdlets' {
		$unresolved = foreach ($alias in $module.ExportedAliases.Values) {
			$command = $alias.ResolvedCommand
			if ($command -isnot [System.Management.Automation.CmdletInfo] -or $command.Name -notin $module.ExportedCmdlets.Keys -or $command.ModuleName -ne $module.Name) {
				'{0} -> {1}' -f $alias.Name, $alias.Definition
			}
		}
		$unresolved | Should -BeNullOrEmpty
	}

	It 'loads every view in its format files' {
		$manifest.FormatsToProcess | Should -Not -BeNullOrEmpty
		$missing = foreach ($file in $manifest.FormatsToProcess) {
			$xml = [xml] (Get-Content -Raw -LiteralPath (Join-Path $moduleFolder $file))
			foreach ($view in $xml.SelectNodes('/Configuration/ViewDefinitions/View')) {
				$viewName = $view.SelectSingleNode('Name').InnerText
				foreach ($typeName in $view.SelectNodes('ViewSelectedBy/TypeName')) {
					$loaded = @((Get-FormatData -TypeName $typeName.InnerText).FormatViewDefinition.Name)
					if ($viewName -cnotin $loaded) {
						"the view '$viewName' for $($typeName.InnerText), in $file"
					}
				}
			}
		}
		$missing | Should -BeNullOrEmpty
	}

	It 'loads the type data in its type files' {
		$manifest.TypesToProcess | Should -Not -BeNullOrEmpty
		$missing = foreach ($file in $manifest.TypesToProcess) {
			$xml = [xml] (Get-Content -Raw -LiteralPath (Join-Path $moduleFolder $file))
			foreach ($type in $xml.SelectNodes('/Types/Type')) {
				$typeName = $type.SelectSingleNode('Name').InnerText
				$typeData = Get-TypeData -TypeName $typeName
				if ($null -eq $typeData) {
					"the type data for $typeName, in $file"
					continue
				}
				foreach ($member in $type.SelectNodes('Members/*/Name')) {
					if ($member.InnerText -cnotin $typeData.Members.Keys) {
						"the member $typeName.$($member.InnerText), in $file"
					}
				}
			}
		}
		$missing | Should -BeNullOrEmpty
	}

	It 'requires PowerShell 7.6 and the Core edition' {
		$module.PowerShellVersion | Should -Be ([version] '7.6')
		$module.CompatiblePSEditions | Should -Be @('Core')
	}
}
