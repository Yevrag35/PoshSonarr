#Requires -Version 7.6
#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '6.2.0'; MaximumVersion = '6.99.99' }

# Tags, and the cmdlets that put them on series and take them off. The read tests run on the baseline, whose one tag,
# sandbox-baseline, Sandbox Ended Show carries and Sandbox Continuing Show doesn't. A test that creates, renames, or
# deletes a tag, or puts one on a series or takes one off, works on a tag of its own, which it creates through the API
# unless it tests New-SonarrTag, so a broken cmdlet fails only its own tests. The tests that put a tag on a series or
# take one off use Sandbox Continuing Show, which has no tags on the baseline, so Clear-SonarrTag can't take
# sandbox-baseline off a series. Afterward, each test's tag comes off the show and is deleted through the API, if either
# is left, in that order, since Sonarr doesn't delete a tag that a series carries.

BeforeAll {
	. "$PSScriptRoot/setup.ps1"

	$baselineTagId = [int] ((Invoke-SandboxApi tag -ExpectStatus 200).Body | Where-Object label -CEQ $Fixtures.Tag).id
	$apiSeries = @((Invoke-SandboxApi series -ExpectStatus 200).Body)
	$ended = $Fixtures.SeededSeries[0]
	$endedId = [int] ($apiSeries | Where-Object tvdbId -EQ $ended.TvdbId).id
	$continuing = $Fixtures.SeededSeries[1]
	$continuingId = [int] ($apiSeries | Where-Object tvdbId -EQ $continuing.TvdbId).id

	# Sandbox Continuing Show as the baseline has it, which puts the show back after a test that changes it.
	$continuingBaseline = (Invoke-SandboxApi "series/$continuingId" -ExpectStatus 200).Body

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

	# Creates a tag through the API, labeled by New-TestName, and returns it as the API returned it.
	function New-TestTag {
		(Invoke-SandboxApi tag -Method Post -Body @{ label = (New-TestName) } -ExpectStatus 201).Body
	}

	# Puts a tag on Sandbox Continuing Show through the API, beside the tags that the show has.
	function Add-ContinuingTag {
		param (
			[Parameter(Mandatory = $true)]
			[int] $TagId
		)

		$series = (Invoke-SandboxApi "series/$continuingId" -ExpectStatus 200).Body
		$series.tags = @(@($series.tags) + $TagId)
		$null = Invoke-SandboxApi "series/$continuingId" -Method Put -Body $series -ExpectStatus 202
	}

	# Puts Sandbox Continuing Show back through the API when its tags differ from the baseline's.
	function Restore-ContinuingShow {
		$stored = (Invoke-SandboxApi "series/$continuingId" -ExpectStatus 200).Body
		if ((Join-TagId $stored.tags) -cne (Join-TagId $continuingBaseline.tags)) {
			$null = Invoke-SandboxApi "series/$continuingId" -Method Put -Body $continuingBaseline -ExpectStatus 202
		}
	}

	# Takes a test's tag off Sandbox Continuing Show and deletes it through the API, if either is left. The show comes
	# first, since Sonarr doesn't delete a tag that a series carries.
	function Remove-TestTag {
		param (
			[Parameter(Mandatory = $true)]
			[int] $TagId
		)

		Restore-ContinuingShow
		if ((Invoke-SandboxApi "tag/$TagId").StatusCode -eq 200) {
			$null = Invoke-SandboxApi "tag/$TagId" -Method Delete -ExpectStatus 200
		}
	}

	# Returns the names of the fields whose values differ between two copies of a series, as GET series/{id} returned
	# them, so that a test can check that a request changed one field and no other.
	function Get-ChangedField {
		param (
			[Parameter(Mandatory = $true)]
			[object] $Before,

			[Parameter(Mandatory = $true)]
			[object] $After
		)

		$names = @($Before.psobject.Properties.Name; $After.psobject.Properties.Name) | Sort-Object -Unique
		foreach ($name in $names) {
			if ((ConvertTo-Json $After.$name -Depth 10 -Compress) -cne (ConvertTo-Json $Before.$name -Depth 10 -Compress)) {
				$name
			}
		}
	}
}

Describe 'Get-SonarrTag' {
	It 'returns every tag that GET tag returns, as TagObjects with the #tag tag' {
		$apiTags = @((Invoke-SandboxApi tag -ExpectStatus 200).Body)
		$apiTags.label | Should -BeExactly $Fixtures.Tag -Because 'the baseline has one tag'

		$tags = @(Get-SonarrTag -ErrorAction Stop)
		$tags.Id | Sort-Object | Should -Be ($apiTags.id | Sort-Object)
		foreach ($tag in $tags) {
			$tag | Should -BeOfType ([MG.Sonarr.Next.Models.Tags.TagObject])
			$tag.psobject.TypeNames[0] | Should -BeExactly 'MG.Sonarr.Next.Models.Tags.TagObject'
			$tag.MetadataTag.Value | Should -BeExactly '#tag'
			$tag.Label | Should -BeExactly ($apiTags | Where-Object id -EQ $tag.Id).label
		}
	}

	It 'finds the baseline tag by its label, given by position or in -Name, ignoring case' {
		foreach ($label in $Fixtures.Tag, $Fixtures.Tag.ToUpperInvariant()) {
			@(Get-SonarrTag $label -ErrorAction Stop).Id | Should -Be $baselineTagId
			@(Get-SonarrTag -Name $label -ErrorAction Stop).Id | Should -Be $baselineTagId
		}
	}

	It "finds the baseline tag by the wildcard '<Pattern>'" -ForEach @(
		@{ Pattern = 'sandbox-*' }
		@{ Pattern = '*BASE*' }
	) {
		@(Get-SonarrTag $Pattern -ErrorAction Stop).Id | Should -Be $baselineTagId
	}

	It 'finds the baseline tag by its ID, in -Id or given by position' {
		@(Get-SonarrTag -Id $baselineTagId -ErrorAction Stop).Label | Should -BeExactly $Fixtures.Tag
		@(Get-SonarrTag $baselineTagId -ErrorAction Stop).Label | Should -BeExactly $Fixtures.Tag
	}

	It 'reads a number in -Name as a label, so the baseline tag''s ID there finds nothing' {
		(Invoke-SandboxApi tag -ExpectStatus 200).Body.label | Should -Not -Contain "$baselineTagId" -Because 'no tag has the ID as its label'
		Get-SonarrTag -Name $baselineTagId -ErrorAction Stop | Should -BeNullOrEmpty
	}

	It 'returns the tags that it finds, and writes no error for an ID that Sonarr doesn''t have' {
		# Sonarr answers an ID that no tag has with a 404, which the cmdlet doesn't write as an error.
		$tags = @(Get-SonarrTag -Id $baselineTagId, ([int]::MaxValue) -ErrorAction SilentlyContinue -ErrorVariable getErrors)

		$getErrors.Count | Should -Be 0
		$tags.Id | Should -Be $baselineTagId
	}

	It 'returns the tags that a piped series carries' {
		$expected = ($apiSeries | Where-Object id -EQ $endedId).tags
		$tags = @(Get-SonarrSeries -Id $endedId -ErrorAction Stop | Get-SonarrTag -ErrorAction Stop)

		$tags.Id | Sort-Object | Should -Be ($expected | Sort-Object)
		$tags.Label | Sort-Object | Should -BeExactly ($ended.Tags | Sort-Object)
	}

	It 'returns nothing for a piped series that carries no tags' {
		($apiSeries | Where-Object id -EQ $continuingId).tags | Should -BeNullOrEmpty -Because "$($continuing.Title) has no tags"
		$series = @(Get-SonarrSeries -Id $continuingId -ErrorAction Stop)
		$series | Should -HaveCount 1

		$series | Get-SonarrTag -ErrorAction Stop | Should -BeNullOrEmpty
	}
}

Describe 'New-SonarrTag' {
	BeforeAll {
		$label = New-TestName
	}
	AfterAll {
		$tags = (Invoke-SandboxApi tag -ExpectStatus 200).Body | Where-Object label -EQ $label
		foreach ($tag in $tags) {
			$null = Invoke-SandboxApi "tag/$($tag.id)" -Method Delete -ExpectStatus 200
		}
	}

	It 'creates a tag with the label that it is given, and returns it as a TagObject with the #tag tag' {
		$tags = @(New-SonarrTag -Label $label -ErrorAction Stop)

		$tags | Should -HaveCount 1
		$tags[0] | Should -BeOfType ([MG.Sonarr.Next.Models.Tags.TagObject])
		$tags[0].psobject.TypeNames[0] | Should -BeExactly 'MG.Sonarr.Next.Models.Tags.TagObject'
		$tags[0].MetadataTag.Value | Should -BeExactly '#tag'
		$tags[0].Label | Should -BeExactly $label
		(Invoke-SandboxApi "tag/$($tags[0].Id)" -ExpectStatus 200).Body.label | Should -BeExactly $label
	}
}

Describe 'Rename-SonarrTag' {
	BeforeAll {
		$tag = New-TestTag
	}
	AfterAll {
		Remove-TestTag $tag.id
	}

	It 'gives the tag that -Id names the new label' {
		$newLabel = New-TestName
		Rename-SonarrTag -Id $tag.id -NewName $newLabel -ErrorAction Stop | Should -BeNullOrEmpty

		(Invoke-SandboxApi "tag/$($tag.id)" -ExpectStatus 200).Body.label | Should -BeExactly $newLabel
	}

	It 'writes the error that Sonarr returns for a label that another tag has, and keeps the label' {
		# Sonarr's database holds each label once, so Sonarr turns away a rename to another tag's label with a 409.
		$label = (Invoke-SandboxApi "tag/$($tag.id)" -ExpectStatus 200).Body.label
		Rename-SonarrTag -Id $tag.id -NewName $Fixtures.Tag -ErrorAction SilentlyContinue -ErrorVariable renameErrors

		$renameErrors.Count | Should -Be 1
		$renameErrors[0].Exception | Should -BeOfType ([MG.Sonarr.Next.Exceptions.SonarrHttpException])
		$renameErrors[0].Exception.StatusCode | Should -Be ([System.Net.HttpStatusCode]::Conflict)
		$renameErrors[0].CategoryInfo.Category | Should -Be ([System.Management.Automation.ErrorCategory]::NotSpecified)
		$renameErrors[0].TargetObject | Should -BeExactly "/tag/$($tag.id)"
		(Invoke-SandboxApi "tag/$($tag.id)" -ExpectStatus 200).Body.label | Should -BeExactly $label
	}
}

Describe 'Add-SonarrTag' {
	BeforeAll {
		$tag = New-TestTag
	}
	AfterEach {
		Restore-ContinuingShow
	}
	AfterAll {
		Remove-TestTag $tag.id
	}

	It 'puts a tag on a piped series by <Case>, and saves the series with no other change' -ForEach @(
		@{ Case = 'its ID in -Id'; ByLabel = $false }
		@{ Case = 'its label in -Name'; ByLabel = $true }
	) {
		$before = (Invoke-SandboxApi "series/$continuingId" -ExpectStatus 200).Body
		$before.tags | Should -BeNullOrEmpty -Because "$($continuing.Title) has no tags"
		$before.firstAired | Should -Not -BeNullOrEmpty -Because "$($continuing.Title) has a first air date"

		$series = Get-SonarrSeries -Id $continuingId -ErrorAction Stop
		$tagArgs = if ($ByLabel) { @{ Name = $tag.label } } else { @{ Id = $tag.id } }
		$series | Add-SonarrTag @tagArgs -ErrorAction Stop | Should -BeNullOrEmpty

		# The request sends the whole series back, with the first air date that the series holds back from PowerShell,
		# so the tags are the one field that changes.
		$stored = (Invoke-SandboxApi "series/$continuingId" -ExpectStatus 200).Body
		Join-TagId $stored.tags | Should -BeExactly "$($tag.id)"
		Get-ChangedField $before $stored | Should -Be 'tags'
		Join-TagId $series.Tags | Should -BeExactly "$($tag.id)" -Because 'the commit keeps the tags that the request saved'
		$series.psobject.Properties['FirstAired'] | Should -BeNullOrEmpty -Because 'the commit removes the FirstAired property that serializing the series added'
	}
}

Describe 'Clear-SonarrTag' {
	BeforeAll {
		$tag = New-TestTag
	}
	BeforeEach {
		Add-ContinuingTag $tag.id
	}
	AfterEach {
		Restore-ContinuingShow
	}
	AfterAll {
		Remove-TestTag $tag.id
	}

	It 'takes a tag off a piped series by <Case>, and saves the series with no other change' -ForEach @(
		@{ Case = 'its ID in -Id'; ByLabel = $false }
		@{ Case = 'its label in -Name'; ByLabel = $true }
	) {
		$before = (Invoke-SandboxApi "series/$continuingId" -ExpectStatus 200).Body
		Join-TagId $before.tags | Should -BeExactly "$($tag.id)" -Because 'the test put its tag on the series'
		$before.firstAired | Should -Not -BeNullOrEmpty -Because "$($continuing.Title) has a first air date"

		$series = Get-SonarrSeries -Id $continuingId -ErrorAction Stop
		$tagArgs = if ($ByLabel) { @{ Name = $tag.label } } else { @{ Id = $tag.id } }
		$series | Clear-SonarrTag @tagArgs -ErrorAction Stop | Should -BeNullOrEmpty

		# The request sends the whole series back, with the first air date that the series holds back from PowerShell,
		# so the tags are the one field that changes.
		$stored = (Invoke-SandboxApi "series/$continuingId" -ExpectStatus 200).Body
		$stored.tags | Should -BeNullOrEmpty
		Get-ChangedField $before $stored | Should -Be 'tags'
		Join-TagId $series.Tags | Should -BeNullOrEmpty -Because 'the commit keeps the tags that the request saved'
		$series.psobject.Properties['FirstAired'] | Should -BeNullOrEmpty -Because 'the commit removes the FirstAired property that serializing the series added'
	}
}

Describe 'Remove-SonarrTag' {
	BeforeEach {
		$tag = New-TestTag
	}
	AfterEach {
		Remove-TestTag $tag.id
	}

	It 'deletes the tag that -Id names, <Case>' -ForEach @(
		@{ Case = 'under -Confirm:$false'; Force = $false }
		@{ Case = 'with -Force, without asking'; Force = $true }
	) {
		$removeArgs = if ($Force) { @{ Force = $true } } else { @{ Confirm = $false } }
		Remove-SonarrTag -Id $tag.id @removeArgs -ErrorAction Stop | Should -BeNullOrEmpty

		(Invoke-SandboxApi "tag/$($tag.id)").StatusCode | Should -Be 404
	}

	It 'writes the error that Sonarr returns for a tag that a series carries, and deletes nothing' {
		# Sonarr turns away the delete of a tag that's still in use with a 409.
		Add-ContinuingTag $tag.id
		Remove-SonarrTag -Id $tag.id -Force -ErrorAction SilentlyContinue -ErrorVariable removeErrors

		$removeErrors.Count | Should -Be 1
		$removeErrors[0].Exception | Should -BeOfType ([MG.Sonarr.Next.Exceptions.SonarrHttpException])
		$removeErrors[0].Exception.StatusCode | Should -Be ([System.Net.HttpStatusCode]::Conflict)
		$removeErrors[0].CategoryInfo.Category | Should -Be ([System.Management.Automation.ErrorCategory]::NotSpecified)
		$removeErrors[0].TargetObject | Should -BeExactly "/tag/$($tag.id)"
		(Invoke-SandboxApi "tag/$($tag.id)").StatusCode | Should -Be 200
		Join-TagId (Invoke-SandboxApi "series/$continuingId" -ExpectStatus 200).Body.tags | Should -BeExactly "$($tag.id)"
	}
}
