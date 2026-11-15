#Requires -Version 7.6
#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '6.2.0'; MaximumVersion = '6.99.99' }

# Episodes, the calendar, and the missing episodes, on the seeded series. Sandbox Ended Show's episodes are S1E1 to S1E3
# and S2E1 to S2E3, with no specials and no absolute numbers, and they aired in 2020. Sandbox Continuing Show's first
# season aired in 2021, and its second airs in January 2099, so the missing episodes are the ones that aired, and the
# calendar for that month holds the rest. A read test compares what a cmdlet returns with what the API returns for the
# same request, by ID, and the -EpisodeIdentifier tests compare the episodes that Get-SonarrEpisode keeps with the ones
# that the API returns for Sandbox Ended Show. The Update-SonarrEpisode test turns the monitoring of one of its episodes
# off, and AfterAll turns it back on through the API. That test comes last, so the read tests run on the baseline.

BeforeAll {
	. "$PSScriptRoot/setup.ps1"

	$ended = $Fixtures.SeededSeries[0]
	$endedId = [int] ((Invoke-SandboxApi series -ExpectStatus 200).Body | Where-Object tvdbId -EQ $ended.TvdbId).id
	$apiEpisodes = @((Invoke-SandboxApi "episode?seriesId=$endedId" -ExpectStatus 200).Body)

	# The IDs of Sandbox Ended Show's episodes, by a label such as S1E2, so that a test names the episodes that it
	# expects.
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

	# Checks an object that a cmdlet returned for an episode against the episode that the API returned: its key fields,
	# and the class and the tag that the module gives it. The air time compares as an instant, since the module reads it
	# as a DateTimeOffset and ConvertFrom-Json as a UTC DateTime.
	function Assert-Episode {
		param (
			[Parameter(Mandatory = $true)]
			[object] $Actual,

			# The episode as the API returned it.
			[Parameter(Mandatory = $true)]
			[object] $Expected,

			# The model class, whose name is the object's first type name too.
			[Parameter(Mandatory = $false)]
			[type] $Type = [MG.Sonarr.Next.Models.Episodes.EpisodeObject],

			[Parameter(Mandatory = $false)]
			[string] $Tag = '#episode'
		)

		$Actual | Should -BeOfType $Type
		$Actual.psobject.TypeNames[0] | Should -BeExactly $Type.FullName
		$Actual.MetadataTag.Value | Should -BeExactly $Tag
		$Actual.SeriesId | Should -Be $Expected.seriesId
		$Actual.SeasonNumber | Should -Be $Expected.seasonNumber
		$Actual.EpisodeNumber | Should -Be $Expected.episodeNumber
		$Actual.Title | Should -BeExactly $Expected.title
		$Actual.IsMonitored | Should -Be $Expected.monitored
		$Actual.HasFile | Should -Be $Expected.hasFile
		$Actual.AirDateUtc | Should -Be ([datetimeoffset] $Expected.airDateUtc)
	}

	# The error that Get-SonarrEpisode stops with when an identifier isn't valid.
	$invalidIdErrorId = 'MG.Sonarr.Next.Exceptions.InvalidEpisodeIdFormatException,MG.Sonarr.Next.Shell.Cmdlets.Episodes.GetSonarrEpisodeCmdlet'
}

Describe 'Get-SonarrEpisode' {
	It 'returns the episodes that GET episode?seriesId= returns for each piped series, and names their series by its title' {
		$apiSeries = @((Invoke-SandboxApi series -ExpectStatus 200).Body)
		$episodes = @(Get-SonarrSeries -ErrorAction Stop | Get-SonarrEpisode -ErrorAction Stop)

		$expectedIds = foreach ($show in $Fixtures.SeededSeries) {
			$id = [int] ($apiSeries | Where-Object tvdbId -EQ $show.TvdbId).id
			$expected = @((Invoke-SandboxApi "episode?seriesId=$id" -ExpectStatus 200).Body)
			$expected | Should -HaveCount $show.EpisodeCount -Because "$($show.Title) has $($show.EpisodeCount) episodes"

			$found = @($episodes | Where-Object SeriesId -EQ $id)
			$found.Id | Sort-Object | Should -Be ($expected.id | Sort-Object)
			foreach ($episode in $found) {
				Assert-Episode $episode ($expected | Where-Object id -EQ $episode.Id)
				$episode.psobject.Series | Should -BeExactly $show.Title -Because 'an episode of a piped series names the series by its title'
			}
			$expected.id
		}
		$episodes.Id | Sort-Object | Should -Be ($expectedIds | Sort-Object)
	}

	It 'returns the episode that an ID names, with the series that Sonarr nests in it' {
		$id = Get-ExpectedEpisodeId 'S1E2'
		$expected = (Invoke-SandboxApi "episode/$id" -ExpectStatus 200).Body
		$episodes = @(Get-SonarrEpisode -Id $id -ErrorAction Stop)

		$episodes | Should -HaveCount 1
		Assert-Episode $episodes[0] $expected
		$episodes[0].Series | Should -BeOfType ([MG.Sonarr.Next.Models.Series.SeriesObject])
		$episodes[0].Series.MetadataTag.Value | Should -BeExactly '#series'
		$episodes[0].Series.Title | Should -BeExactly $ended.Title
		$episodes[0].psobject.Series | Should -BeExactly $ended.Title -Because 'an episode names its series by the title of the series nested in it'
	}

	It 'returns the episodes that it finds, and writes no error for an ID that Sonarr doesn''t have' {
		# Sonarr answers an ID that no episode has with a 404, which the cmdlet doesn't write as an error.
		$id = Get-ExpectedEpisodeId 'S1E2'
		$episodes = @(Get-SonarrEpisode -Id $id, ([int]::MaxValue) -ErrorAction SilentlyContinue -ErrorVariable getErrors)

		$getErrors.Count | Should -Be 0
		$episodes.Id | Should -Be $id
	}

	It 'returns the episode of each piped calendar entry' {
		$entries = @(Get-SonarrCalendar -StartDate $Fixtures.Calendar.Start -EndDate $Fixtures.Calendar.End -ErrorAction Stop)
		$entries | Should -HaveCount $Fixtures.Calendar.EpisodeCount

		$episodes = @($entries | Get-SonarrEpisode -ErrorAction Stop)
		$episodes.Id | Sort-Object | Should -Be ($entries.Id | Sort-Object)
		foreach ($episode in $episodes) {
			$episode | Should -BeOfType ([MG.Sonarr.Next.Models.Episodes.EpisodeObject])
		}
	}

	Context '-EpisodeIdentifier' {
		It 'keeps every episode when it is left out, $null, or empty' {
			$episodeIds.Count | Should -Be $ended.EpisodeCount
			$all = @($episodeIds.Values | Sort-Object)

			@(Get-SonarrEpisode -SeriesId $endedId -ErrorAction Stop).Id | Sort-Object | Should -Be $all
			@(Get-SonarrEpisode -SeriesId $endedId -EpisodeIdentifier $null -ErrorAction Stop).Id | Sort-Object | Should -Be $all
			@(Get-SonarrEpisode -SeriesId $endedId -EpisodeIdentifier @() -ErrorAction Stop).Id | Sort-Object | Should -Be $all
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
			$ids = @(Get-SonarrEpisode -SeriesId $endedId -EpisodeIdentifier $Identifier -ErrorAction Stop).Id
			$ids | Sort-Object | Should -Be (Get-ExpectedEpisodeId $Expected)
		}

		It 'keeps each episode that any of several identifiers names, once' {
			$ids = @(Get-SonarrEpisode -SeriesId $endedId -EpisodeIdentifier 'S01E01', 'S02', 'S01E01-02' -ErrorAction Stop).Id
			$ids.Count | Should -Be 5
			$ids | Sort-Object | Should -Be (Get-ExpectedEpisodeId 'S1E1', 'S1E2', 'S2E1', 'S2E2', 'S2E3')
		}

		It 'takes identifiers by position after a piped series' {
			$ids = @(Get-SonarrSeries -Id $endedId -ErrorAction Stop | Get-SonarrEpisode 'S01E02-E03', 'S02E01' -ErrorAction Stop).Id
			$ids | Sort-Object | Should -Be (Get-ExpectedEpisodeId 'S1E2', 'S1E3', 'S2E1')
		}

		It 'keeps nothing for <Name>' -ForEach @(
			@{ Name = "'S00', since the series has no specials"; Identifier = 'S00' }
			@{ Name = "'S03', a season that the series lacks"; Identifier = 'S03' }
			@{ Name = "'S01E00', episode 0, which the series lacks"; Identifier = 'S01E00' }
			@{ Name = "'S01E04-09', episodes that the series lacks"; Identifier = 'S01E04-09' }
		) {
			Get-SonarrEpisode -SeriesId $endedId -EpisodeIdentifier $Identifier -ErrorAction Stop | Should -BeNullOrEmpty
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
			Get-SonarrEpisode -SeriesId $endedId -EpisodeIdentifier $Identifier -ErrorAction Stop | Should -BeNullOrEmpty
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
			$record = { Get-SonarrEpisode -SeriesId $endedId -EpisodeIdentifier $Identifier -ErrorAction Stop } |
				Should -Throw -ExceptionType ([MG.Sonarr.Next.Exceptions.InvalidEpisodeIdFormatException]) -ErrorId $invalidIdErrorId -PassThru
			$record.CategoryInfo.Category | Should -Be ([System.Management.Automation.ErrorCategory]::InvalidArgument)
			$targets = @($record.TargetObject | ForEach-Object ToString)
			$targets.Count | Should -Be 1
			$targets[0] | Should -BeExactly "$Identifier"
			$record.Exception.Message | Should -BeLike "The episode identifier '$Identifier' isn't valid. *"
		}

		It 'names only the identifiers that are not valid, and keeps no episodes' {
			$output = [System.Collections.Generic.List[object]]::new()
			$record = { Get-SonarrEpisode -SeriesId $endedId -EpisodeIdentifier 'S01', 'S01Ex', 'S01E02', 'S01E03-02' -ErrorAction Stop | ForEach-Object { $output.Add($_) } } |
				Should -Throw -ExceptionType ([MG.Sonarr.Next.Exceptions.InvalidEpisodeIdFormatException]) -ErrorId $invalidIdErrorId -PassThru
			$output.Count | Should -Be 0
			@($record.TargetObject | ForEach-Object ToString) | Should -Be @('S01Ex', 'S01E03-02')
			$record.Exception.Message | Should -BeLike "The episode identifiers 'S01Ex' and 'S01E03-02' aren't valid. *"
		}

		It 'lists three identifiers that are not valid with commas' {
			$record = { Get-SonarrEpisode -SeriesId $endedId -EpisodeIdentifier 'a', 'S01', 'b', 'c' -ErrorAction Stop } |
				Should -Throw -ExceptionType ([MG.Sonarr.Next.Exceptions.InvalidEpisodeIdFormatException]) -ErrorId $invalidIdErrorId -PassThru
			$record.Exception.Message | Should -BeLike "The episode identifiers 'a', 'b', and 'c' aren't valid. *"
		}

		It 'writes one error and no episodes when -ErrorAction lets the error go on' {
			$episodes = Get-SonarrEpisode -SeriesId $endedId -EpisodeIdentifier 'S01', 'S01Ex' -ErrorAction SilentlyContinue -ErrorVariable errors
			$episodes | Should -BeNullOrEmpty
			$errors.Count | Should -Be 1
			$errors[0].FullyQualifiedErrorId | Should -BeExactly $invalidIdErrorId
		}
	}
}

Describe 'Get-SonarrCalendar' {
	BeforeAll {
		$range = $Fixtures.Calendar
		$calendar = @((Invoke-SandboxApi "calendar?start=$($range.Start)&end=$($range.End)" -ExpectStatus 200).Body)

		# A range that holds every episode of the seeded series, which aired from 2020 on and air until 2099, for the
		# tests of -Tags.
		$wide = @{ Start = '2000-01-01'; End = '2099-12-31' }
		$baselineTagId = [int] ((Invoke-SandboxApi tag -ExpectStatus 200).Body | Where-Object label -EQ $Fixtures.Tag).id
	}

	It 'returns the entries that GET calendar returns for a range of dates, as CalendarObjects with the #calendar tag' {
		$calendar | Should -HaveCount $range.EpisodeCount -Because "the calendar from $($range.Start) to $($range.End) holds $($range.EpisodeCount) episodes"

		$entries = @(Get-SonarrCalendar -StartDate $range.Start -EndDate $range.End -ErrorAction Stop)
		$entries.Id | Sort-Object | Should -Be ($calendar.id | Sort-Object)
		foreach ($entry in $entries) {
			Assert-Episode $entry ($calendar | Where-Object id -EQ $entry.Id) -Type ([MG.Sonarr.Next.Models.Calendar.CalendarObject]) -Tag '#calendar'
		}
	}

	It 'keeps only the entries that air on a day that -DayOfWeek names, in local time' {
		# Sonarr returns the entries of every day, and the cmdlet removes those of the other days by the day that each
		# entry airs on in the local time zone, which the test works out the same way.
		$days = @($calendar | ForEach-Object { ([datetimeoffset] $_.airDateUtc).ToLocalTime().DayOfWeek } | Sort-Object -Unique)
		$days | Should -HaveCount 1 -Because 'the episodes in the range air weekly'
		$otherDays = @([System.DayOfWeek].GetEnumValues() | Where-Object { $_ -ne $days[0] })

		$entries = @(Get-SonarrCalendar -StartDate $range.Start -EndDate $range.End -DayOfWeek $days[0] -ErrorAction Stop)
		$entries.Id | Sort-Object | Should -Be ($calendar.id | Sort-Object)
		Get-SonarrCalendar -StartDate $range.Start -EndDate $range.End -DayOfWeek $otherDays -ErrorAction Stop | Should -BeNullOrEmpty
	}

	It 'returns only the entries of the series that carry the tag that <Case> names' -ForEach @(
		@{ Case = 'its label'; ById = $false }
		@{ Case = 'its ID'; ById = $true }
	) {
		$all = @((Invoke-SandboxApi "calendar?start=$($wide.Start)&end=$($wide.End)" -ExpectStatus 200).Body)
		$tagged = @((Invoke-SandboxApi "calendar?start=$($wide.Start)&end=$($wide.End)&tags=$baselineTagId" -ExpectStatus 200).Body)
		$tagged | Should -Not -BeNullOrEmpty -Because 'a seeded series with the tag has episodes in the range'
		$tagged.Count | Should -BeLessThan $all.Count -Because 'a seeded series without the tag has episodes in the range'

		$tag = if ($ById) { $baselineTagId } else { $Fixtures.Tag }
		$entries = @(Get-SonarrCalendar -StartDate $wide.Start -EndDate $wide.End -Tags $tag -ErrorAction Stop)
		$entries.Id | Sort-Object | Should -Be ($tagged.id | Sort-Object)
	}

	It 'writes a warning and returns no entries when no tag matches a name' {
		# Without the tag that the name would have given, the request would return the entries of every series.
		$name = New-TestName
		$entries = Get-SonarrCalendar -StartDate $wide.Start -EndDate $wide.End -Tags $name -ErrorAction Stop -WarningAction SilentlyContinue -WarningVariable calendarWarnings

		$entries | Should -BeNullOrEmpty
		$calendarWarnings.Count | Should -Be 1
		$calendarWarnings[0].Message | Should -BeLike "*'$name'*"
	}
}

Describe 'Get-SonarrWantedMissing' {
	BeforeAll {
		# Every missing episode, on one page that holds as many as the fixtures give.
		$missing = (Invoke-SandboxApi "wanted/missing?pageSize=$($Fixtures.MissingCount)" -ExpectStatus 200).Body
	}

	It 'returns the missing episodes that GET wanted/missing returns, as EpisodeObjects with the #episode tag' {
		# Without a page number or size, Sonarr's defaults apply, and its default page holds every missing episode.
		$page = (Invoke-SandboxApi wanted/missing -ExpectStatus 200).Body
		$page.totalRecords | Should -Be $Fixtures.MissingCount
		$page.records | Should -HaveCount $Fixtures.MissingCount -Because 'the default page holds every missing episode'

		$episodes = @(Get-SonarrWantedMissing -ErrorAction Stop)
		$episodes.Id | Sort-Object | Should -Be ($page.records.id | Sort-Object)
		foreach ($episode in $episodes) {
			Assert-Episode $episode ($page.records | Where-Object id -EQ $episode.Id)
		}
	}

	It 'returns the same missing episodes a page at a time, with a smaller page size' {
		$missing.totalRecords | Should -Be $Fixtures.MissingCount

		# A page size that splits the missing episodes into full pages and a last page that isn't full.
		$pageSize = 4
		$pageCount = [int] [System.Math]::Ceiling($Fixtures.MissingCount / $pageSize)
		$ids = foreach ($number in 1..$pageCount) {
			$apiPage = (Invoke-SandboxApi "wanted/missing?page=$number&pageSize=$pageSize" -ExpectStatus 200).Body
			$episodes = @(Get-SonarrWantedMissing -PageNumber $number -PageSize $pageSize -ErrorAction Stop)
			$episodes.Id | Sort-Object | Should -Be ($apiPage.records.id | Sort-Object)
			$episodes.Id
		}

		$ids | Sort-Object | Should -Be ($missing.records.id | Sort-Object)
		Get-SonarrWantedMissing -PageNumber ($pageCount + 1) -PageSize $pageSize -ErrorAction Stop | Should -BeNullOrEmpty
	}

	It 'returns every missing episode with -All' {
		$missing.totalRecords | Should -Be $Fixtures.MissingCount
		@(Get-SonarrWantedMissing -All -ErrorAction Stop).Id | Sort-Object | Should -Be ($missing.records.id | Sort-Object)
	}
}

Describe 'Update-SonarrEpisode' {
	BeforeAll {
		$episodeId = Get-ExpectedEpisodeId 'S1E1'
		$original = (Invoke-SandboxApi "episode/$episodeId" -ExpectStatus 200).Body
	}
	AfterAll {
		# PUT episode/{id} saves only the episode's monitoring, so the body that GET returned before the test puts back
		# what it changed.
		$stored = (Invoke-SandboxApi "episode/$episodeId" -ExpectStatus 200).Body
		if ($stored.monitored -ne $original.monitored) {
			$null = Invoke-SandboxApi "episode/$episodeId" -Method Put -Body $original -ExpectStatus 202
		}
	}

	It 'saves the piped episode with its monitoring turned off, and takes out what serializing it added' {
		$original.monitored | Should -BeTrue -Because 'the episodes of the seeded series are monitored'
		$original.airDate | Should -Not -BeNullOrEmpty -Because 'the episode has an air date'
		$original.series.firstAired | Should -Not -BeNullOrEmpty -Because "$($ended.Title) has a first air date"

		# An episode that comes by its ID nests its series, so the request body carries the series too, which Sonarr reads
		# with the rest of the body.
		$episode = Get-SonarrEpisode -Id $episodeId -ErrorAction Stop
		$episode.psobject.Properties['AirDate'] | Should -BeNullOrEmpty -Because 'the episode holds AirDate back from PowerShell'
		$episode.Series.psobject.Properties['FirstAired'] | Should -BeNullOrEmpty -Because 'the nested series holds FirstAired back from PowerShell'

		$episode.IsMonitored = $false
		$episode | Update-SonarrEpisode -ErrorAction Stop | Should -BeNullOrEmpty

		(Invoke-SandboxApi "episode/$episodeId" -ExpectStatus 200).Body.monitored | Should -BeFalse
		$episode.psobject.Properties['AirDate'] | Should -BeNullOrEmpty -Because 'the commit removes the AirDate property that serializing the episode added'
		$episode.Series.psobject.Properties['FirstAired'] | Should -BeNullOrEmpty -Because 'the commit removes the FirstAired property that serializing the nested series added'
	}
}
