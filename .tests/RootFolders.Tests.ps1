#Requires -Version 7.6
#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '6.2.0'; MaximumVersion = '6.99.99' }

# Root folders. The tests read the baseline's one root folder, /tv, which setup.ps1 checks is there, and change nothing.

BeforeAll {
	. "$PSScriptRoot/setup.ps1"
}

Describe 'Get-SonarrRootFolder' {
	BeforeAll {
		$apiFolders = @((Invoke-SandboxApi rootfolder -ExpectStatus 200).Body)
		$tvFolder = $apiFolders | Where-Object { $_.path.TrimEnd('/') -ceq $Fixtures.RootFolder }
	}

	It 'returns every root folder that GET rootfolder returns, /tv among them, as RootFolderObjects with the #root_folder tag' {
		$folders = @(Get-SonarrRootFolder -ErrorAction Stop)

		$folders.Id | Sort-Object | Should -Be ($apiFolders.id | Sort-Object)
		$folders.Id | Should -Contain $tvFolder.id
		foreach ($item in $folders) {
			$item | Should -BeOfType ([MG.Sonarr.Next.Models.RootFolders.RootFolderObject])
			$item.psobject.TypeNames[0] | Should -BeExactly 'MG.Sonarr.Next.Models.RootFolders.RootFolderObject'
			$item.MetadataTag.Value | Should -BeExactly '#root_folder'
			# Free space changes between two requests, as Sonarr and the container write files, so it isn't compared.
			$expected = $apiFolders | Where-Object id -EQ $item.Id
			$item.Path | Should -BeExactly $expected.path
			$item.Accessible | Should -Be $expected.accessible
		}
	}

	It 'finds /tv by its ID, in -Id or given by position' {
		@(Get-SonarrRootFolder -Id $tvFolder.id -ErrorAction Stop).Path | Should -BeExactly $tvFolder.path
		@(Get-SonarrRootFolder $tvFolder.id -ErrorAction Stop).Path | Should -BeExactly $tvFolder.path
	}
}
