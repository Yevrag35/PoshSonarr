#Requires -Version 7.6
#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '6.2.0'; MaximumVersion = '6.99.99' }

# The history of what Sonarr has done with episodes, such as grabbing, importing, renaming, or deleting their files. The
# sandbox has no enabled indexers, no download clients, and no media files, so nothing adds a record, and the history
# stays empty. The tests check that each kind of request that the cmdlet sends returns no records and writes no error.

BeforeAll {
	. "$PSScriptRoot/setup.ps1"
}

Describe 'Get-SonarrHistory' {
	It 'returns no records, and writes no error, <Case>' -ForEach @(
		@{ Case = 'for the first page'; Arguments = @{} }
		@{ Case = 'for the page that -PageNumber names'; Arguments = @{ PageNumber = 2 } }
		@{ Case = 'with a page size'; Arguments = @{ PageSize = 5 } }
		@{ Case = 'for the kind of event that -EventType names'; Arguments = @{ EventType = 'Grabbed' } }
		@{ Case = 'since the date that -Since gives'; Arguments = @{ Since = [datetime]::Now.AddDays(-30) } }
	) {
		(Invoke-SandboxApi history -ExpectStatus 200).Body.totalRecords | Should -Be 0 -Because 'the history is empty'

		Get-SonarrHistory @Arguments -ErrorAction Stop | Should -BeNullOrEmpty
	}

	It 'returns no records, and writes no error, for the seeded series that -SeriesId names' {
		$seriesIds = @((Invoke-SandboxApi series -ExpectStatus 200).Body.id)
		(Invoke-SandboxApi history -ExpectStatus 200).Body.totalRecords | Should -Be 0 -Because 'the history is empty'

		# The cmdlet gets each series' records with a request of its own.
		Get-SonarrHistory -SeriesId $seriesIds -ErrorAction Stop | Should -BeNullOrEmpty
	}

	It 'writes a warning, and no error, for a -Since date in the future' {
		Get-SonarrHistory -Since ([datetime]::Now.AddDays(1)) -ErrorAction Stop -WarningAction SilentlyContinue -WarningVariable historyWarnings |
			Should -BeNullOrEmpty

		$historyWarnings.Count | Should -Be 1
		$historyWarnings[0].Message | Should -BeLike '*Since*future*'
	}
}
