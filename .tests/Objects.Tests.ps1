#Requires -Version 7.6
#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '6.2.0'; MaximumVersion = '6.99.99' }

# The cmdlets that work on what other cmdlets return rather than on Sonarr: Get-SonarrObjectProperty, which lists an
# object's properties, and Show-SonarrException, which shows the errors that a failed response held. The tests list the
# seeded series' properties, and show the error of an add that Sonarr turns away: Sandbox Lookup Show's lookup result,
# given the TVDB ID that the mock metadata service doesn't have.

BeforeAll {
	. "$PSScriptRoot/setup.ps1"

	# Returns property names in the order that Get-SonarrObjectProperty sorts them in: ordinally, ignoring case.
	function Get-SortedName {
		param (
			[Parameter(Mandatory = $true)]
			[string[]] $Name
		)

		[string[]] $sorted = @($Name)
		[System.Array]::Sort($sorted, [System.StringComparer]::OrdinalIgnoreCase)
		$sorted
	}
}

Describe 'Get-SonarrObjectProperty' {
	BeforeAll {
		$apiSeries = @((Invoke-SandboxApi series -ExpectStatus 200).Body)
		$endedId = [int] ($apiSeries | Where-Object tvdbId -EQ $Fixtures.SeededSeries[0].TvdbId).id
		$continuingId = [int] ($apiSeries | Where-Object tvdbId -EQ $Fixtures.SeededSeries[1].TvdbId).id
		$series = Get-SonarrSeries -Id $endedId -ErrorAction Stop
	}

	It 'lists the properties of a piped series that can be set, sorted by name, each with its value, its type, and the series'' class' {
		$properties = @($series | Get-SonarrObjectProperty -ErrorAction Stop)
		$settable = @($series.psobject.Properties | Where-Object IsSettable)
		$settable | Should -Not -BeNullOrEmpty

		$properties.Name | Should -BeExactly (Get-SortedName $settable.Name)
		foreach ($item in $properties) {
			$item | Should -BeOfType ([MG.Sonarr.Next.Shell.Components.SonarrProperty])
			$item.ObjectType | Should -BeExactly 'MG.Sonarr.Next.Models.Series.SeriesObject'
			$item.IsReadOnly | Should -BeFalse
			$property = $series.psobject.Properties[$item.Name]
			$item.Type | Should -BeExactly $property.TypeNameOfValue
			ConvertTo-Json -InputObject $item.CurrentValue -Depth 10 -Compress |
				Should -BeExactly (ConvertTo-Json -InputObject $property.Value -Depth 10 -Compress)
		}
	}

	It 'lists the read-only properties too under -IncludeReadOnly, such as the ID and the metadata tag' {
		$properties = @($series | Get-SonarrObjectProperty -IncludeReadOnly -ErrorAction Stop)

		$properties.Name | Should -BeExactly (Get-SortedName $series.psobject.Properties.Name)
		foreach ($item in $properties) {
			$item.IsReadOnly | Should -Be (-not $series.psobject.Properties[$item.Name].IsSettable)
		}
		($properties | Where-Object Name -EQ 'Id').IsReadOnly | Should -BeTrue
		($properties | Where-Object Name -EQ 'MetadataTag').IsReadOnly | Should -BeTrue
	}

	It 'lists a property for each field that GET series/{id} returns, with the value that Sonarr returned' {
		$apiItem = (Invoke-SandboxApi "series/$endedId" -ExpectStatus 200).Body
		$properties = @($series | Get-SonarrObjectProperty -IncludeReadOnly -ErrorAction Stop)
		$values = @{}
		foreach ($item in $properties) {
			$values[$item.Name] = $item.CurrentValue
		}

		# The model renames monitored and seasonFolder, and holds firstAired back for the bodies that it sends.
		$renames = @{ monitored = 'IsMonitored'; seasonFolder = 'UseSeasonFolders' }
		foreach ($field in $apiItem.psobject.Properties.Name) {
			if ($field -ceq 'firstAired') {
				continue
			}
			$name = $renames.ContainsKey($field) ? $renames[$field] : $field
			$properties.Name | Should -Contain $name -Because "GET series/{id} returns $field"
		}
		$values['Title'] | Should -BeExactly $apiItem.title
		$values['Path'] | Should -BeExactly $apiItem.path
		$values['TvdbId'] | Should -Be $apiItem.tvdbId
		$values['QualityProfileId'] | Should -Be $apiItem.qualityProfileId
		$values['IsMonitored'] | Should -Be $apiItem.monitored
		$values['UseSeasonFolders'] | Should -Be $apiItem.seasonFolder
	}

	It 'lists each property of two piped series once, with the value of the first' {
		$both = @(
			$series
			Get-SonarrSeries -Id $continuingId -ErrorAction Stop
		)
		$properties = @($both | Get-SonarrObjectProperty -IncludeReadOnly -ErrorAction Stop)

		$names = @($both[0].psobject.Properties.Name; $both[1].psobject.Properties.Name) | Sort-Object -Unique
		$properties.Name | Should -BeExactly (Get-SortedName $names)
		($properties | Where-Object Name -EQ 'Title').CurrentValue | Should -BeExactly $both[0].Title
		($properties | Where-Object Name -EQ 'Id').CurrentValue | Should -Be $both[0].Id
	}
}

Describe 'Show-SonarrException' {
	BeforeAll {
		$lookupShow = $Fixtures.LookupSeries
		$anyProfileId = [int] ((Invoke-SandboxApi qualityprofile -ExpectStatus 200).Body | Where-Object name -EQ 'Any').id

		# Adds Sandbox Lookup Show's lookup result with the TVDB ID that the mock metadata service doesn't have, which Sonarr
		# turns away with a 400 that holds one validation error, and returns the add's error records. No search finds a show
		# that the mock lacks, so the lookup result gets the TVDB ID.
		function Add-UnknownShow {
			$lookup = Search-SonarrSeries "tvdb:$($lookupShow.TvdbId)" -ErrorAction Stop
			$lookup.TvdbId = $Fixtures.UnknownTvdbId
			$null = $lookup | Add-SonarrSeries -RootFolderPath $Fixtures.RootFolder -QualityProfileId $anyProfileId -ErrorAction SilentlyContinue -ErrorVariable addErrors
			$addErrors
		}

		$addErrors = @(Add-UnknownShow)
		$exception = $addErrors[0].Exception
	}
	AfterAll {
		# Sonarr adds nothing, but a broken add could have added Sandbox Lookup Show, so each series with its TVDB ID or the
		# unknown one is deleted through the API, once Sonarr has finished adding it.
		$added = @((Invoke-SandboxApi series -ExpectStatus 200).Body | Where-Object tvdbId -In $lookupShow.TvdbId, $Fixtures.UnknownTvdbId)
		foreach ($item in $added) {
			try {
				Wait-SandboxSeries -Series ([pscustomobject]@{ Id = $item.id; Title = $lookupShow.Title; EpisodeCount = $lookupShow.EpisodeCount })
			}
			finally {
				$null = Invoke-SandboxApi "series/$($item.id)?deleteFiles=true" -Method Delete -ExpectStatus 200
			}
		}
	}

	It 'writes the validation error that the add''s 400 held, from a piped exception, with the request''s URL' {
		$addErrors | Should -HaveCount 1
		$exception | Should -BeOfType ([MG.Sonarr.Next.Exceptions.SonarrHttpException])
		$exception.StatusCode | Should -Be ([System.Net.HttpStatusCode]::BadRequest)

		$shown = @($exception | Show-SonarrException -ErrorAction Stop)
		$shown | Should -HaveCount 1
		$shown[0].psobject.TypeNames[0] | Should -BeExactly 'MG.Sonarr.Next.Models.Errors.SonarrServerError'
		$shown[0].PropertyName | Should -BeExactly 'TvdbId'
		$shown[0].AttemptedValue | Should -Be $Fixtures.UnknownTvdbId
		$shown[0].RequestUri | Should -BeExactly "$($SandboxSettings.BaseUrl)/api/v3/series"
		# The cmdlet writes a copy, so the exception's own error gets no RequestUri.
		@($exception.ExtendedInfo)[0].psobject.Properties['RequestUri'] | Should -BeNullOrEmpty
	}

	It 'takes the exception in -InputObject, by position, and as the Exception of a piped error record' {
		@(Show-SonarrException -InputObject $exception -ErrorAction Stop).PropertyName | Should -BeExactly 'TvdbId'
		@(Show-SonarrException $exception -ErrorAction Stop).PropertyName | Should -BeExactly 'TvdbId'
		@($addErrors[0] | Show-SonarrException -ErrorAction Stop).PropertyName | Should -BeExactly 'TvdbId'
	}

	It 'writes the errors of the most recent Sonarr error in $Error when no exception is given' {
		# The add's error goes into $Error, where the error from the add in BeforeAll is too, so a cmdlet that wrote
		# every Sonarr error that $Error holds would write more than one.
		$latest = @(Add-UnknownShow)
		$latest | Should -HaveCount 1

		$shown = @(Show-SonarrException -ErrorAction Stop)
		$shown | Should -HaveCount 1
		$shown[0].PropertyName | Should -BeExactly 'TvdbId'
		$shown[0].RequestUri | Should -BeExactly $latest[0].Exception.RequestUri
	}

	It 'writes only a verbose message when $Error holds no Sonarr error' {
		# A new PowerShell process starts with $Error empty. A verbose record that reaches the child's host goes only to
		# its console, so the child writes each record that the cmdlet wrote as text.
		$lines = @(Invoke-TestProcess @'
foreach ($record in @(Show-SonarrException -Verbose 4>&1)) {
	'{0}: {1}' -f $record.GetType().Name, $record
}
'@)

		$lines | Should -BeExactly 'VerboseRecord: No readable exceptions have been recorded.'
	}
}
