#Requires -Version 7.6
#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '6.2.0'; MaximumVersion = '6.99.99' }

# The episodes of the seeded series. The -EpisodeIdentifier tests run Get-SonarrEpisode on Sandbox Ended Show, whose
# episodes are S1E1 to S1E3 and S2E1 to S2E3, with no specials and no absolute numbers, and compare the episodes that
# it keeps with the ones that the API returns for the series, by ID. Nothing in the file changes the sandbox.

BeforeAll {
	. "$PSScriptRoot/setup.ps1"

	$ended = $Fixtures.SeededSeries[0]
	$seriesId = [int] ((Invoke-SandboxApi series -ExpectStatus 200).Body | Where-Object tvdbId -EQ $ended.TvdbId).id
	$apiEpisodes = @((Invoke-SandboxApi "episode?seriesId=$seriesId" -ExpectStatus 200).Body)

	# The IDs of the series' episodes, by a label such as S1E2, so that a test names the episodes that it expects.
	$episodeIds = @{}
	foreach ($episode in $apiEpisodes) {
		$episodeIds['S{0}E{1}' -f $episode.seasonNumber, $episode.episodeNumber] = [int] $episode.id
	}

	# Returns the sorted IDs of the episodes that the labels name, and fails the test when the series lacks one, so a
	# change to the fixtures can't make a test pass by expecting nothing.
	function Get-ExpectedEpisodeId {
		param (
			[Parameter(Mandatory = $true)]
			[string[]] $Label
		)

		$ids = foreach ($item in $Label) {
			$episodeIds.ContainsKey($item) | Should -BeTrue -Because "$($ended.Title) has the episode $item"
			$episodeIds[$item]
		}
		$ids | Sort-Object
	}

	# The error that Get-SonarrEpisode stops with when an identifier isn't valid.
	$invalidIdErrorId = 'MG.Sonarr.Next.Exceptions.InvalidEpisodeIdFormatException,MG.Sonarr.Next.Shell.Cmdlets.Episodes.GetSonarrEpisodeCmdlet'
}

Describe 'Get-SonarrEpisode' {
	Context '-EpisodeIdentifier' {
		It 'keeps every episode when it is left out, $null, or empty' {
			$episodeIds.Count | Should -Be $ended.EpisodeCount
			$all = @($episodeIds.Values | Sort-Object)

			@(Get-SonarrEpisode -SeriesId $seriesId -ErrorAction Stop).Id | Sort-Object | Should -Be $all
			@(Get-SonarrEpisode -SeriesId $seriesId -EpisodeIdentifier $null -ErrorAction Stop).Id | Sort-Object | Should -Be $all
			@(Get-SonarrEpisode -SeriesId $seriesId -EpisodeIdentifier @() -ErrorAction Stop).Id | Sort-Object | Should -Be $all
		}

		It "keeps the episodes that '<Identifier>' names" -ForEach @(
			@{ Identifier = 'S01E02'; Expected = @('S1E2') }
			@{ Identifier = 's01e02'; Expected = @('S1E2') }
			@{ Identifier = 'S1E2'; Expected = @('S1E2') }
			@{ Identifier = ' S01E02 '; Expected = @('S1E2') }
			@{ Identifier = 'S01E02-03'; Expected = @('S1E2', 'S1E3') }
			@{ Identifier = 'S01E02-E03'; Expected = @('S1E2', 'S1E3') }
			@{ Identifier = 's02e01-e02'; Expected = @('S2E1', 'S2E2') }
			@{ Identifier = 'S01E03-03'; Expected = @('S1E3') }
			@{ Identifier = 'S02E02-99'; Expected = @('S2E2', 'S2E3') }
			@{ Identifier = 'S01E00-02'; Expected = @('S1E1', 'S1E2') }
			@{ Identifier = 'S01'; Expected = @('S1E1', 'S1E2', 'S1E3') }
			@{ Identifier = 'S02'; Expected = @('S2E1', 'S2E2', 'S2E3') }
		) {
			$ids = @(Get-SonarrEpisode -SeriesId $seriesId -EpisodeIdentifier $Identifier -ErrorAction Stop).Id
			$ids | Sort-Object | Should -Be (Get-ExpectedEpisodeId $Expected)
		}

		It 'keeps each episode that any of several identifiers names, once' {
			$ids = @(Get-SonarrEpisode -SeriesId $seriesId -EpisodeIdentifier 'S01E01', 'S02', 'S01E01-02' -ErrorAction Stop).Id
			$ids.Count | Should -Be 5
			$ids | Sort-Object | Should -Be (Get-ExpectedEpisodeId 'S1E1', 'S1E2', 'S2E1', 'S2E2', 'S2E3')
		}

		It 'takes identifiers by position after a piped series' {
			$ids = @(Get-SonarrSeries -Id $seriesId -ErrorAction Stop | Get-SonarrEpisode 'S01E02-E03', 'S02E01' -ErrorAction Stop).Id
			$ids | Sort-Object | Should -Be (Get-ExpectedEpisodeId 'S1E2', 'S1E3', 'S2E1')
		}

		It 'keeps nothing for <Name>' -ForEach @(
			@{ Name = "'S00', since the series has no specials"; Identifier = 'S00' }
			@{ Name = "'S03', a season that the series lacks"; Identifier = 'S03' }
			@{ Name = "'S01E00', episode 0, which the series lacks"; Identifier = 'S01E00' }
			@{ Name = "'S01E04-09', episodes that the series lacks"; Identifier = 'S01E04-09' }
		) {
			Get-SonarrEpisode -SeriesId $seriesId -EpisodeIdentifier $Identifier -ErrorAction Stop | Should -BeNullOrEmpty
		}

		It 'keeps nothing for the absolute identifier <Name>, since the series has no absolute numbers' -ForEach @(
			@{ Name = "'E1'"; Identifier = 'E1' }
			@{ Name = "'1'"; Identifier = '1' }
			@{ Name = '[int] 1'; Identifier = 1 }
			@{ Name = '[int] 0'; Identifier = 0 }
			@{ Name = "'E1-3'"; Identifier = 'E1-3' }
			@{ Name = "'E1-E3'"; Identifier = 'E1-E3' }
		) {
			$apiEpisodes | Where-Object { $null -ne $_.absoluteEpisodeNumber } | Should -BeNullOrEmpty -Because "$($ended.Title) has no absolute numbers"
			Get-SonarrEpisode -SeriesId $seriesId -EpisodeIdentifier $Identifier -ErrorAction Stop | Should -BeNullOrEmpty
		}

		It 'stops with an error that names <Name>' -ForEach @(
			@{ Name = "'S01Ex'"; Identifier = 'S01Ex' }
			@{ Name = "'S01E'"; Identifier = 'S01E' }
			@{ Name = "'S01E02-'"; Identifier = 'S01E02-' }
			@{ Name = "'S01E02-E'"; Identifier = 'S01E02-E' }
			@{ Name = "'S01E-2'"; Identifier = 'S01E-2' }
			@{ Name = "'S01E02E03'"; Identifier = 'S01E02E03' }
			@{ Name = "'S01E02-S01E03'"; Identifier = 'S01E02-S01E03' }
			@{ Name = "'S01E02-EE03'"; Identifier = 'S01E02-EE03' }
			@{ Name = "'S01E02--1'"; Identifier = 'S01E02--1' }
			@{ Name = "'S01E02-03-04'"; Identifier = 'S01E02-03-04' }
			@{ Name = "'xS01E02'"; Identifier = 'xS01E02' }
			@{ Name = "'S01 E02'"; Identifier = 'S01 E02' }
			@{ Name = "'S01E99999999999'"; Identifier = 'S01E99999999999' }
			@{ Name = "'25-28'"; Identifier = '25-28' }
			@{ Name = "''"; Identifier = '' }
			@{ Name = "'S01E03-02', a range that ends before it starts"; Identifier = 'S01E03-02' }
			@{ Name = "'S01E03-E02', a range that ends before it starts"; Identifier = 'S01E03-E02' }
			@{ Name = "'E3-2', a range that ends before it starts"; Identifier = 'E3-2' }
			@{ Name = 'the negative [int] -1'; Identifier = -1 }
		) {
			$record = { Get-SonarrEpisode -SeriesId $seriesId -EpisodeIdentifier $Identifier -ErrorAction Stop } |
				Should -Throw -ExceptionType ([MG.Sonarr.Next.Exceptions.InvalidEpisodeIdFormatException]) -ErrorId $invalidIdErrorId -PassThru
			$record.CategoryInfo.Category | Should -Be ([System.Management.Automation.ErrorCategory]::InvalidArgument)
			$targets = @($record.TargetObject | ForEach-Object ToString)
			$targets.Count | Should -Be 1
			$targets[0] | Should -BeExactly "$Identifier"
			$record.Exception.Message | Should -BeLike "The episode identifier '$Identifier' isn't valid. *"
		}

		It 'names only the identifiers that are not valid, and keeps no episodes' {
			$output = [System.Collections.Generic.List[object]]::new()
			$record = { Get-SonarrEpisode -SeriesId $seriesId -EpisodeIdentifier 'S01', 'S01Ex', 'S01E02', 'S01E03-02' -ErrorAction Stop | ForEach-Object { $output.Add($_) } } |
				Should -Throw -ExceptionType ([MG.Sonarr.Next.Exceptions.InvalidEpisodeIdFormatException]) -ErrorId $invalidIdErrorId -PassThru
			$output.Count | Should -Be 0
			@($record.TargetObject | ForEach-Object ToString) | Should -Be @('S01Ex', 'S01E03-02')
			$record.Exception.Message | Should -BeLike "The episode identifiers 'S01Ex' and 'S01E03-02' aren't valid. *"
		}

		It 'lists three identifiers that are not valid with commas' {
			$record = { Get-SonarrEpisode -SeriesId $seriesId -EpisodeIdentifier 'a', 'S01', 'b', 'c' -ErrorAction Stop } |
				Should -Throw -ExceptionType ([MG.Sonarr.Next.Exceptions.InvalidEpisodeIdFormatException]) -ErrorId $invalidIdErrorId -PassThru
			$record.Exception.Message | Should -BeLike "The episode identifiers 'a', 'b', and 'c' aren't valid. *"
		}

		It 'writes one error and no episodes when -ErrorAction lets the error go on' {
			$episodes = Get-SonarrEpisode -SeriesId $seriesId -EpisodeIdentifier 'S01', 'S01Ex' -ErrorAction SilentlyContinue -ErrorVariable errors
			$episodes | Should -BeNullOrEmpty
			$errors.Count | Should -Be 1
			$errors[0].FullyQualifiedErrorId | Should -BeExactly $invalidIdErrorId
		}
	}
}
