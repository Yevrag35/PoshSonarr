#Requires -Version 7.6
#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '6.2.0'; MaximumVersion = '6.99.99' }

# The series in Sonarr's library, and the lookups that add them. The read tests run on the two seeded series. A test
# that adds, changes, or removes a series works on a show that the mock metadata service serves and the baseline lacks:
# Sandbox Lookup Show, or Sandbox Wild*Card Show for the wildcard tests. It adds the show itself, through the API unless
# it tests Add-SonarrSeries, and deletes it through the API afterward, once Sonarr has finished adding it, so a broken
# cmdlet can't leave the show behind. The tests of a failed update change Sandbox Continuing Show in a way that Sonarr
# turns away, and AfterAll puts the series back through the API if a request went through anyway.

BeforeAll {
	. "$PSScriptRoot/setup.ps1"

	$anyProfileId = [int] ((Invoke-SandboxApi qualityprofile -ExpectStatus 200).Body | Where-Object name -EQ 'Any').id

	# Adds a show that the mock serves through the API, as the sandbox's seeding adds one: monitored, under the root
	# folder, with the Any quality profile and season folders, and with no searches. Then it waits until Sonarr has
	# finished adding the show, so that a test can change it without racing Sonarr's own save, and returns its ID.
	function Add-FixtureShow {
		param (
			# The show, as $Fixtures holds it.
			[Parameter(Mandatory = $true)]
			[hashtable] $Show
		)

		$body = @{
			tvdbId           = $Show.TvdbId
			title            = $Show.Title
			qualityProfileId = $anyProfileId
			rootFolderPath   = $Fixtures.RootFolder
			monitored        = $true
			seasonFolder     = $true
			seriesType       = 'standard'
			tags             = @()
			addOptions       = @{
				monitor                      = 'all'
				searchForMissingEpisodes     = $false
				searchForCutoffUnmetEpisodes = $false
			}
		}
		$series = (Invoke-SandboxApi series -Method Post -Body $body -ExpectStatus 201).Body
		Wait-SandboxSeries -Series ([pscustomobject]@{ Id = $series.id; Title = $Show.Title; EpisodeCount = $Show.EpisodeCount })
		[int] $series.id
	}

	# Deletes every series with the show's TVDB ID through the API. Sonarr fetches a show's episodes after it answers the
	# add, so each series is deleted once Sonarr has finished adding it, or once the wait gives up.
	function Remove-FixtureShow {
		param (
			# The show, as $Fixtures holds it.
			[Parameter(Mandatory = $true)]
			[hashtable] $Show
		)

		$added = @((Invoke-SandboxApi series -ExpectStatus 200).Body | Where-Object tvdbId -EQ $Show.TvdbId)
		foreach ($series in $added) {
			try {
				Wait-SandboxSeries -Series ([pscustomobject]@{ Id = $series.id; Title = $Show.Title; EpisodeCount = $Show.EpisodeCount })
			}
			finally {
				$null = Invoke-SandboxApi "series/$($series.id)?deleteFiles=true" -Method Delete -ExpectStatus 200
			}
		}
	}

	# Returns tag IDs as one string, so that sets with no tags compare too. The IDs are enumerated here, since an
	# [object[]] parameter takes a model's SortedSet[int] as a single element.
	function Join-TagId {
		param (
			[Parameter(Mandatory = $true)]
			[AllowNull()]
			[object] $Tags
		)

		@(foreach ($id in $Tags) { $id }) -join ','
	}
}

Describe 'Get-SonarrSeries' {
	BeforeAll {
		$apiSeries = @((Invoke-SandboxApi series -ExpectStatus 200).Body)
		$ended = $Fixtures.SeededSeries[0]
		$endedId = [int] ($apiSeries | Where-Object tvdbId -EQ $ended.TvdbId).id
	}

	It 'returns every series that GET series returns, which are the seeded series alone' {
		$series = @(Get-SonarrSeries -ErrorAction Stop)

		$series.TvdbId | Sort-Object | Should -Be ($Fixtures.SeededSeries.TvdbId | Sort-Object)
		$series.Id | Sort-Object | Should -Be ($apiSeries.id | Sort-Object)
		foreach ($item in $series) {
			$expected = $apiSeries | Where-Object id -EQ $item.Id
			$item.TvdbId | Should -Be $expected.tvdbId
			$item.Title | Should -BeExactly $expected.title
			$item.Path | Should -BeExactly $expected.path
			$item.IsMonitored | Should -Be $expected.monitored
			$item.QualityProfileId | Should -Be $expected.qualityProfileId
			Join-TagId $item.Tags | Should -BeExactly (Join-TagId $expected.tags)
		}
	}

	It 'returns each series as a SeriesObject with the #series tag' {
		$series = @(Get-SonarrSeries -ErrorAction Stop)
		$series | Should -Not -BeNullOrEmpty
		foreach ($item in $series) {
			$item | Should -BeOfType ([MG.Sonarr.Next.Models.Series.SeriesObject])
			$item.psobject.TypeNames[0] | Should -BeExactly 'MG.Sonarr.Next.Models.Series.SeriesObject'
			$item.MetadataTag.Value | Should -BeExactly '#series'
		}
	}

	It 'finds a series by its title, <Case>' -ForEach @(
		@{ Case = 'as Sonarr writes it'; Lowercase = $false }
		@{ Case = 'ignoring case'; Lowercase = $true }
	) {
		$name = if ($Lowercase) { $ended.Title.ToLowerInvariant() } else { $ended.Title }
		$found = @(Get-SonarrSeries -Name $name -ErrorAction Stop)
		$found | Should -HaveCount 1
		$found[0].Id | Should -Be $endedId
	}

	It "finds both seeded series by 'Sandbox*'" {
		$found = @(Get-SonarrSeries 'Sandbox*' -ErrorAction Stop)
		$found.Id | Sort-Object | Should -Be ($apiSeries.id | Sort-Object)
	}

	It 'finds a series by an ID given by position' {
		$found = @(Get-SonarrSeries $endedId -ErrorAction Stop)
		$found | Should -HaveCount 1
		$found[0].Id | Should -Be $endedId
		$found[0].Title | Should -BeExactly $ended.Title
	}

	Context 'a title that holds a * of its own' {
		BeforeAll {
			$wildcardId = Add-FixtureShow $Fixtures.WildcardSeries
		}
		AfterAll {
			Remove-FixtureShow $Fixtures.WildcardSeries
		}

		It "finds the series by '<Name>', whose * is a wildcard wherever it stands" -ForEach @(
			@{ Name = 'Sandbox Wild*Show' }
			@{ Name = '*Wild*' }
		) {
			$found = @(Get-SonarrSeries -Name $Name -ErrorAction Stop)
			$found | Should -HaveCount 1
			$found[0].Id | Should -Be $wildcardId
			$found[0].Title | Should -BeExactly $Fixtures.WildcardSeries.Title
		}
	}
}

Describe 'Search-SonarrSeries' {
	BeforeAll {
		# The mock's four shows, by TVDB ID.
		$shows = @{}
		foreach ($show in @($Fixtures.SeededSeries; $Fixtures.LookupSeries; $Fixtures.WildcardSeries)) {
			$shows[$show.TvdbId] = $show
		}
	}

	It 'returns the shows that GET series/lookup finds for a term, as lookup results with the #add_series tag' {
		$api = @((Invoke-SandboxApi "series/lookup?term=$($Fixtures.SearchTerm)" -ExpectStatus 200).Body)
		$api.tvdbId | Sort-Object | Should -Be ($shows.Keys | Sort-Object) -Because 'the mock finds all four shows for any term but a TVDB ID'

		$results = @(Search-SonarrSeries $Fixtures.SearchTerm -ErrorAction Stop)
		$results.TvdbId | Sort-Object | Should -Be ($api.tvdbId | Sort-Object)
		foreach ($result in $results) {
			$result | Should -BeOfType ([MG.Sonarr.Next.Models.Series.AddSeriesObject])
			$result.psobject.TypeNames[0] | Should -BeExactly 'MG.Sonarr.Next.Models.Series.AddSeriesObject'
			$result.MetadataTag.Value | Should -BeExactly '#add_series'
			$result.Title | Should -BeExactly $shows[$result.TvdbId].Title
		}
	}

	It 'returns Sandbox Lookup Show alone for <Case>' -ForEach @(
		@{ Case = "its TVDB ID after 'tvdb:'"; UseTvdbId = $false }
		@{ Case = 'its TVDB ID in -TVDbId'; UseTvdbId = $true }
	) {
		$lookupShow = $Fixtures.LookupSeries
		$arguments = if ($UseTvdbId) { @{ TVDbId = $lookupShow.TvdbId } } else { @{ Name = "tvdb:$($lookupShow.TvdbId)" } }
		$results = @(Search-SonarrSeries @arguments -ErrorAction Stop)

		$results | Should -HaveCount 1
		$results[0].TvdbId | Should -Be $lookupShow.TvdbId
		$results[0].Title | Should -BeExactly $lookupShow.Title
	}

	It "keeps only the results whose titles fit '<Name>' under -Strict" -ForEach @(
		@{ Name = 'Sandbox Lookup'; Expected = 'LookupSeries' }
		@{ Name = 'sandbox wild*show'; Expected = 'WildcardSeries' }
	) {
		@(Search-SonarrSeries $Name -ErrorAction Stop) | Should -HaveCount $shows.Count -Because 'the mock finds all four shows for any term but a TVDB ID'

		$results = @(Search-SonarrSeries $Name -Strict -ErrorAction Stop)
		$results | Should -HaveCount 1
		$results[0].Title | Should -BeExactly $Fixtures[$Expected].Title
	}
}

Describe 'Add-SonarrSeries' {
	BeforeAll {
		$lookupShow = $Fixtures.LookupSeries

		# The properties that an add writes into a lookup result for its request body, and takes out again afterward.
		$requestProperties = 'Path', 'RootFolderPath', 'AddOptions'
	}
	AfterEach {
		Remove-FixtureShow $lookupShow
	}

	It 'adds the show that a lookup found <Name>, and returns the new series' -ForEach @(
		@{ Name = 'under -RootFolderPath'; FullPath = $false }
		@{ Name = 'at -AbsolutePath'; FullPath = $true }
	) {
		$lookup = Search-SonarrSeries "tvdb:$($lookupShow.TvdbId)" -ErrorAction Stop
		if ($FullPath) {
			$pathArgs = @{ AbsolutePath = "$($Fixtures.RootFolder)/$(New-TestName)" }
			$expected = $pathArgs.AbsolutePath
		}
		else {
			$pathArgs = @{ RootFolderPath = $Fixtures.RootFolder }
			$expected = "$($Fixtures.RootFolder)/$($lookup.Folder)"
		}
		$added = @($lookup | Add-SonarrSeries @pathArgs -QualityProfileId $anyProfileId -ErrorAction Stop)

		$added | Should -HaveCount 1
		$added[0] | Should -BeOfType ([MG.Sonarr.Next.Models.Series.SeriesObject])
		$added[0].psobject.TypeNames[0] | Should -BeExactly 'MG.Sonarr.Next.Models.Series.SeriesObject'
		$added[0].MetadataTag.Value | Should -BeExactly '#series'
		$added[0].TvdbId | Should -Be $lookupShow.TvdbId

		$stored = (Invoke-SandboxApi "series/$($added[0].Id)" -ExpectStatus 200).Body
		$stored.title | Should -BeExactly $lookupShow.Title
		$stored.path | Should -BeExactly $expected
		$stored.qualityProfileId | Should -Be $anyProfileId
		foreach ($property in $requestProperties) {
			$lookup.psobject.Properties[$property] | Should -BeNullOrEmpty -Because "the add's commit takes the $property property out again"
		}

		# Sonarr answers the add before it has the show's episodes, so the add has finished once they exist.
		Wait-SandboxSeries -Series ([pscustomobject]@{ Id = $added[0].Id; Title = $lookupShow.Title; EpisodeCount = $lookupShow.EpisodeCount })
	}

	It 'writes the error that Sonarr returns for a show that the metadata service lacks, and adds nothing' {
		# No search finds a show that the mock lacks, so the test gives Sandbox Lookup Show's lookup result the TVDB ID
		# that the mock doesn't have.
		$lookup = Search-SonarrSeries "tvdb:$($lookupShow.TvdbId)" -ErrorAction Stop
		$lookup.TvdbId = $Fixtures.UnknownTvdbId
		$added = $lookup | Add-SonarrSeries -RootFolderPath $Fixtures.RootFolder -QualityProfileId $anyProfileId -ErrorAction SilentlyContinue -ErrorVariable addErrors

		$added | Should -BeNullOrEmpty
		$addErrors.Count | Should -Be 1
		$addErrors[0].Exception | Should -BeOfType ([MG.Sonarr.Next.Exceptions.SonarrHttpException])
		$addErrors[0].Exception.StatusCode | Should -Be ([System.Net.HttpStatusCode]::BadRequest)
		$addErrors[0].CategoryInfo.Category | Should -Be ([System.Management.Automation.ErrorCategory]::InvalidData)
		$addErrors[0].TargetObject | Should -BeExactly '/series'
		$addErrors[0].Exception.Message | Should -BeLike '*A series with this ID was not found*'
		foreach ($property in $requestProperties) {
			$lookup.psobject.Properties[$property] | Should -BeNullOrEmpty -Because "the reset after the failed add takes the $property property out again"
		}

		@((Invoke-SandboxApi series -ExpectStatus 200).Body.tvdbId) | Sort-Object | Should -Be ($Fixtures.SeededSeries.TvdbId | Sort-Object)
	}

	# An add serializes the lookup result, which writes its path property, and the add's commit or reset takes the
	# property out again. These tests do to the lookup result what an add does when it stops between the two, as one
	# does when its debug prompt is halted, and check that a second add puts the show where it says, not at the stale
	# path, and that the lookup result looks as it did before.
	Context 'after an add that serialized the lookup result and stopped before its commit or reset' {
		It 'adds the show <Name>, and leaves the lookup result without a path or add options' -ForEach @(
			@{ Name = 'under -RootFolderPath after a stopped -AbsolutePath add'; StoppedFullPath = $true; FullPath = $false }
			@{ Name = 'at -AbsolutePath after a stopped -RootFolderPath add'; StoppedFullPath = $false; FullPath = $true }
			@{ Name = 'under -RootFolderPath after a stopped -RootFolderPath add'; StoppedFullPath = $false; FullPath = $false }
			@{ Name = 'at -AbsolutePath after a stopped -AbsolutePath add'; StoppedFullPath = $true; FullPath = $true }
		) {
			$lookup = Search-SonarrSeries "tvdb:$($lookupShow.TvdbId)" -ErrorAction Stop
			foreach ($property in $requestProperties) {
				$lookup.psobject.Properties[$property] | Should -BeNullOrEmpty -Because "a lookup result for a show that isn't in the library has no $property property"
			}

			# What an add does to the lookup result before it sends it, through the C# members that .psobject reaches: it
			# sets the path and serializes the object, which writes the path property. The stopped add goes no further.
			$lookup.psobject.Path = if ($StoppedFullPath) { "$($Fixtures.RootFolder)/$(New-TestName)" } else { $Fixtures.RootFolder }
			$lookup.psobject.IsFullPath = $StoppedFullPath
			$lookup.psobject.OnSerializing()

			if ($FullPath) {
				$pathArgs = @{ AbsolutePath = "$($Fixtures.RootFolder)/$(New-TestName)" }
				$expected = $pathArgs.AbsolutePath
			}
			else {
				$pathArgs = @{ RootFolderPath = $Fixtures.RootFolder }
				$expected = "$($Fixtures.RootFolder)/$($lookup.Folder)"
			}
			$added = $lookup | Add-SonarrSeries @pathArgs -QualityProfileId $anyProfileId -ErrorAction Stop

			(Invoke-SandboxApi "series/$($added.Id)" -ExpectStatus 200).Body.path | Should -BeExactly $expected
			foreach ($property in $requestProperties) {
				$lookup.psobject.Properties[$property] | Should -BeNullOrEmpty -Because "the add's commit takes the $property property out again"
			}
		}
	}
}

Describe 'Update-SonarrSeries' {
	BeforeAll {
		$continuing = $Fixtures.SeededSeries[1]
		$original = (Invoke-SandboxApi series -ExpectStatus 200).Body | Where-Object tvdbId -EQ $continuing.TvdbId
		$baselineTagId = [int] ((Invoke-SandboxApi tag -ExpectStatus 200).Body | Where-Object label -EQ $Fixtures.Tag).id

		# Gets the series and changes it in two ways: it adds the baseline tag, which a reset takes off again, and sets
		# the quality profile ID to 0, which Sonarr turns away. The checks first make sure that the series has a first
		# air date, so that the FirstAired checks after the request aren't empty.
		function Get-ChangedSeries {
			$original.firstAired | Should -Not -BeNullOrEmpty -Because "$($continuing.Title) has a first air date"
			$series = Get-SonarrSeries -Id $original.id -ErrorAction Stop
			$series.psobject.Properties['FirstAired'] | Should -BeNullOrEmpty -Because 'the series holds FirstAired back from PowerShell'

			$null = $series.Tags.Add($baselineTagId)
			$series.QualityProfileId = 0
			Join-TagId $series.Tags | Should -Not -BeExactly (Join-TagId $original.tags) -Because 'the tag was added to the series'
			$series
		}

		# Checks that the series object is back as it was before the request, and that Sonarr still stores the series
		# as it was.
		function Assert-SeriesReset {
			param (
				[Parameter(Mandatory = $true)]
				[object] $Series
			)

			$Series.psobject.Properties['FirstAired'] | Should -BeNullOrEmpty -Because 'a reset removes the FirstAired property that serializing the series added'
			Join-TagId $Series.Tags | Should -BeExactly (Join-TagId $original.tags) -Because "a reset puts back the tags that $($continuing.Title) had"

			$stored = (Invoke-SandboxApi "series/$($original.id)" -ExpectStatus 200).Body
			$stored.qualityProfileId | Should -Be $original.qualityProfileId
			Join-TagId $stored.tags | Should -BeExactly (Join-TagId $original.tags)
		}
	}
	AfterAll {
		$stored = (Invoke-SandboxApi "series/$($original.id)" -ExpectStatus 200).Body
		if ($stored.qualityProfileId -ne $original.qualityProfileId -or (Join-TagId $stored.tags) -cne (Join-TagId $original.tags)) {
			$null = Invoke-SandboxApi "series/$($original.id)" -Method Put -Body $original -ExpectStatus 202
		}
	}

	It 'resets the series and writes the error after a request that fails' {
		$series = Get-ChangedSeries
		$series | Update-SonarrSeries -ErrorAction SilentlyContinue -ErrorVariable updateErrors

		$updateErrors.Count | Should -Be 1
		$updateErrors[0].Exception | Should -BeOfType ([MG.Sonarr.Next.Exceptions.SonarrHttpException])
		$updateErrors[0].Exception.StatusCode | Should -Be ([System.Net.HttpStatusCode]::BadRequest)
		$updateErrors[0].CategoryInfo.Category | Should -Be ([System.Management.Automation.ErrorCategory]::InvalidData)
		Assert-SeriesReset $series
	}

	It 'resets the series before the error ends the statement under -ErrorAction Stop' {
		$series = Get-ChangedSeries
		$record = { $series | Update-SonarrSeries -ErrorAction Stop } |
			Should -Throw -ExceptionType ([MG.Sonarr.Next.Exceptions.SonarrHttpException]) -PassThru

		$record.Exception.StatusCode | Should -Be ([System.Net.HttpStatusCode]::BadRequest)
		$record.CategoryInfo.Category | Should -Be ([System.Management.Automation.ErrorCategory]::InvalidData)
		Assert-SeriesReset $series
	}

	Context 'a change that Sonarr saves' {
		BeforeAll {
			$lookupShow = $Fixtures.LookupSeries
			$seriesId = Add-FixtureShow $lookupShow
		}
		AfterAll {
			Remove-FixtureShow $Fixtures.LookupSeries
		}

		It 'saves the piped series with its monitoring turned off, and no other field changes' {
			$before = (Invoke-SandboxApi "series/$seriesId" -ExpectStatus 200).Body
			$before.monitored | Should -BeTrue -Because 'the show was added monitored'
			$before.firstAired | Should -Not -BeNullOrEmpty -Because "$($lookupShow.Title) has a first air date"

			$series = Get-SonarrSeries -Id $seriesId -ErrorAction Stop
			$series.IsMonitored = $false
			$series | Update-SonarrSeries -ErrorAction Stop | Should -BeNullOrEmpty

			# The update sends back the first air date that the series holds back from PowerShell, and the fields that the
			# model renames under their own names, so the one field that changes is the one that the test changed.
			$stored = (Invoke-SandboxApi "series/$seriesId" -ExpectStatus 200).Body
			$stored.monitored | Should -BeFalse
			$changed = foreach ($field in $before.psobject.Properties) {
				if ((ConvertTo-Json $stored.($field.Name) -Depth 10 -Compress) -cne (ConvertTo-Json $field.Value -Depth 10 -Compress)) {
					$field.Name
				}
			}
			$changed | Should -Be 'monitored'
			$series.psobject.Properties['FirstAired'] | Should -BeNullOrEmpty -Because 'the commit removes the FirstAired property that serializing the series added'
		}
	}
}

Describe 'Remove-SonarrSeries' {
	BeforeEach {
		$seriesId = Add-FixtureShow $Fixtures.LookupSeries
	}
	AfterEach {
		Remove-FixtureShow $Fixtures.LookupSeries
	}

	It 'removes the series with -Force, without asking' {
		Remove-SonarrSeries -Id $seriesId -Force -ErrorAction Stop | Should -BeNullOrEmpty
		(Invoke-SandboxApi "series/$seriesId").StatusCode | Should -Be 404
	}

	It 'asks a question of its own under -Confirm:$false, and removes nothing when the question fails' {
		# -Confirm:$false skips the prompt that ShouldProcess shows, but not the cmdlet's own question, which only -Force
		# skips. The runner runs Pester under -NonInteractive, where a prompt throws instead of waiting for an answer.
		Remove-SonarrSeries -Id $seriesId -Confirm:$false -ErrorAction SilentlyContinue -ErrorVariable removeErrors

		$removeErrors.Count | Should -Be 1
		$removeErrors[0].Exception | Should -BeOfType ([System.Management.Automation.PSInvalidOperationException])
		(Invoke-SandboxApi "series/$seriesId").StatusCode | Should -Be 200
	}

	It 'writes a warning that names the series, and no error, for a series that Sonarr doesn''t have, <Case>' -ForEach @(
		@{ Case = 'given by its ID'; Piped = $false }
		@{ Case = 'piped with its title'; Piped = $true }
	) {
		# Sonarr 4 answers the delete of a series that it doesn't have with a 500, not a 404, so the warning depends on the
		# cmdlet asking Sonarr for the series after a 500.
		$series = Get-SonarrSeries -Id $seriesId -ErrorAction Stop
		$null = Invoke-SandboxApi "series/$($seriesId)?deleteFiles=true" -Method Delete -ExpectStatus 200

		$removeArgs = @{
			Force           = $true
			ErrorAction     = 'SilentlyContinue'
			ErrorVariable   = 'removeErrors'
			WarningAction   = 'SilentlyContinue'
			WarningVariable = 'removeWarnings'
		}
		if ($Piped) {
			$series | Remove-SonarrSeries @removeArgs
			$name = $Fixtures.LookupSeries.Title
		}
		else {
			Remove-SonarrSeries -Id $seriesId @removeArgs
			$name = $seriesId
		}

		$removeErrors.Count | Should -Be 0
		$removeWarnings.Count | Should -Be 1
		$removeWarnings[0].Message | Should -BeLike "*$name*"
	}
}
