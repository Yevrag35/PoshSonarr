#Requires -Version 7.6
#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '6.2.0'; MaximumVersion = '6.99.99' }

# Series. The Update-SonarrSeries tests send Sandbox Continuing Show with a quality profile ID of 0, which Sonarr turns
# away with a 400, and check that the series object comes back as it was before the request: with the tags that it had,
# and without the FirstAired property that serializing it adds. AfterAll puts the series back through the API if a
# request went through anyway.

BeforeAll {
	. "$PSScriptRoot/setup.ps1"
}

Describe 'Update-SonarrSeries' {
	BeforeAll {
		$continuing = $Fixtures.SeededSeries[1]
		$original = (Invoke-SandboxApi series -ExpectStatus 200).Body | Where-Object tvdbId -EQ $continuing.TvdbId
		$baselineTagId = [int] ((Invoke-SandboxApi tag -ExpectStatus 200).Body | Where-Object label -EQ $Fixtures.Tag).id

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
}
