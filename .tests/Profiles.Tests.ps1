#Requires -Version 7.6
#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '6.2.0'; MaximumVersion = '6.99.99' }

# Quality profiles and definitions, the language profile, delay profiles, and release profiles. The read tests run on
# what Sonarr creates for itself: its quality profiles, Any among them, its quality definitions, its one language
# profile, and its built-in delay profile, which comes last, with the largest order. The baseline has no release
# profiles, so the release profile read tests run on one that they create through the API. A test that creates,
# changes, or deletes a profile works on a profile of its own, which it creates through the API unless it tests a New-
# cmdlet, so a broken cmdlet fails only its own tests.
#
# Sonarr requires a tag of every delay profile but the built-in one, and turns away a tag that another delay profile
# has, so each delay profile test creates a tag of its own. It also requires a required or ignored term of every release
# profile, so each one gets a required term that starts with its pester- name. Afterward, each test's profiles are
# deleted through the API, if any are left, and then its tag, since Sonarr doesn't delete a tag that a delay profile
# carries.

BeforeAll {
	. "$PSScriptRoot/setup.ps1"

	# Creates a tag through the API, labeled by New-TestName, and returns it as the API returned it.
	function New-TestTag {
		(Invoke-SandboxApi tag -Method Post -Body @{ label = (New-TestName) } -ExpectStatus 201).Body
	}

	# Creates a delay profile through the API for a tag, and returns it as the API returned it. The profile allows both
	# protocols, prefers Usenet, and has no delays.
	function New-TestDelayProfile {
		param (
			[Parameter(Mandatory = $true)]
			[int] $TagId
		)

		$body = @{
			enableUsenet           = $true
			enableTorrent          = $true
			preferredProtocol      = 'usenet'
			usenetDelay            = 0
			torrentDelay           = 0
			bypassIfHighestQuality = $false
			order                  = 1
			tags                   = @($TagId)
		}
		(Invoke-SandboxApi delayprofile -Method Post -Body $body -ExpectStatus 201).Body
	}

	# Deletes through the API each delay profile that carries a test's tag, and then the tag, if any are left. The
	# profiles come first, since Sonarr doesn't delete a tag that a delay profile carries.
	function Remove-TestDelayProfile {
		param (
			[Parameter(Mandatory = $true)]
			[int] $TagId
		)

		foreach ($item in (Invoke-SandboxApi delayprofile -ExpectStatus 200).Body) {
			if (@($item.tags) -contains $TagId) {
				$null = Invoke-SandboxApi "delayprofile/$($item.id)" -Method Delete -ExpectStatus 200
			}
		}
		if ((Invoke-SandboxApi "tag/$TagId").StatusCode -eq 200) {
			$null = Invoke-SandboxApi "tag/$TagId" -Method Delete -ExpectStatus 200
		}
	}

	# Creates a disabled release profile through the API, named by New-TestName, with a required term that starts with
	# its name, and returns it as the API returned it.
	function New-TestReleaseProfile {
		$name = New-TestName
		$body = @{
			name      = $name
			enabled   = $false
			required  = @("$name-term")
			ignored   = @()
			indexerId = 0
			tags      = @()
		}
		(Invoke-SandboxApi releaseprofile -Method Post -Body $body -ExpectStatus 201).Body
	}

	# Deletes a test's release profile through the API, if it's left.
	function Remove-TestReleaseProfile {
		param (
			[Parameter(Mandatory = $true)]
			[int] $Id
		)

		if ((Invoke-SandboxApi "releaseprofile/$Id").StatusCode -eq 200) {
			$null = Invoke-SandboxApi "releaseprofile/$Id" -Method Delete -ExpectStatus 200
		}
	}

	# Returns the names of the fields whose values differ between two copies of a profile, as the API returned them, so
	# that a test can check that a request changed one field and no other.
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

Describe 'Get-SonarrQualityProfile' {
	BeforeAll {
		$apiProfiles = @((Invoke-SandboxApi qualityprofile -ExpectStatus 200).Body)
		# Any is one of the quality profiles that Sonarr creates for itself.
		$anyProfileId = [int] ($apiProfiles | Where-Object name -CEQ 'Any').id
		$ended = (Invoke-SandboxApi series -ExpectStatus 200).Body | Where-Object tvdbId -EQ $Fixtures.SeededSeries[0].TvdbId
	}

	It 'returns every quality profile that GET qualityprofile returns, as QualityProfileObjects with the #quality_profile tag' {
		$profiles = @(Get-SonarrQualityProfile -ErrorAction Stop)

		$profiles.Id | Sort-Object | Should -Be ($apiProfiles.id | Sort-Object)
		foreach ($item in $profiles) {
			$item | Should -BeOfType ([MG.Sonarr.Next.Models.Qualities.QualityProfileObject])
			$item.psobject.TypeNames[0] | Should -BeExactly 'MG.Sonarr.Next.Models.Qualities.QualityProfileObject'
			$item.MetadataTag.Value | Should -BeExactly '#quality_profile'
			$item.Name | Should -BeExactly ($apiProfiles | Where-Object id -EQ $item.Id).name
		}
	}

	It 'finds Any by its name, given by position or in -Name, ignoring case' {
		foreach ($name in 'Any', 'ANY') {
			@(Get-SonarrQualityProfile $name -ErrorAction Stop).Id | Should -Be $anyProfileId
			@(Get-SonarrQualityProfile -Name $name -ErrorAction Stop).Id | Should -Be $anyProfileId
		}
	}

	It 'finds Any by its ID, in -Id or given by position' {
		@(Get-SonarrQualityProfile -Id $anyProfileId -ErrorAction Stop).Name | Should -BeExactly 'Any'
		@(Get-SonarrQualityProfile $anyProfileId -ErrorAction Stop).Name | Should -BeExactly 'Any'
	}

	It 'returns the quality profile of a piped series' {
		$profiles = @(Get-SonarrSeries -Id $ended.id -ErrorAction Stop | Get-SonarrQualityProfile -ErrorAction Stop)

		$profiles.Id | Should -Be $ended.qualityProfileId
	}
}

Describe 'Get-SonarrQualityDefinition' {
	BeforeAll {
		$apiDefinitions = @((Invoke-SandboxApi qualitydefinition -ExpectStatus 200).Body)
		# Any definition would do for a lookup, so the lookup tests take the last one that the API returns.
		$definition = $apiDefinitions[-1]
	}

	It 'returns every quality definition that GET qualitydefinition returns, as QualityDefinitionObjects with the #quality_definition tag' {
		$definitions = @(Get-SonarrQualityDefinition -ErrorAction Stop)

		$definitions.Id | Sort-Object | Should -Be ($apiDefinitions.id | Sort-Object)
		foreach ($item in $definitions) {
			$item | Should -BeOfType ([MG.Sonarr.Next.Models.Qualities.QualityDefinitionObject])
			$item.psobject.TypeNames[0] | Should -BeExactly 'MG.Sonarr.Next.Models.Qualities.QualityDefinitionObject'
			$item.MetadataTag.Value | Should -BeExactly '#quality_definition'
			$expected = $apiDefinitions | Where-Object id -EQ $item.Id
			$item.Title | Should -BeExactly $expected.title
			# The model reads the nested quality as a model too.
			$item.Quality | Should -BeOfType ([MG.Sonarr.Next.Models.Qualities.QualityObject])
			$item.Quality.Name | Should -BeExactly $expected.quality.name
		}
	}

	It 'finds a definition by its title, given by position or in -Name, ignoring case' {
		foreach ($title in $definition.title, $definition.title.ToUpperInvariant()) {
			@(Get-SonarrQualityDefinition $title -ErrorAction Stop).Id | Should -Be $definition.id
			@(Get-SonarrQualityDefinition -Name $title -ErrorAction Stop).Id | Should -Be $definition.id
		}
	}

	It 'finds a definition by its ID, in -Id or given by position' {
		@(Get-SonarrQualityDefinition -Id $definition.id -ErrorAction Stop).Title | Should -BeExactly $definition.title
		@(Get-SonarrQualityDefinition $definition.id -ErrorAction Stop).Title | Should -BeExactly $definition.title
	}
}

Describe 'Get-SonarrLanguageProfile' {
	It 'returns the one language profile that GET languageprofile returns, and warns that the cmdlet is obsolete' {
		$apiProfiles = @((Invoke-SandboxApi languageprofile -ExpectStatus 200).Body)
		$apiProfiles | Should -HaveCount 1 -Because 'Sonarr 4 keeps a single language profile'

		# PowerShell writes the warning for a cmdlet marked obsolete before -WarningVariable starts collecting, so the
		# test reads it from the output, where 3>&1 sends it.
		$output = @(Get-SonarrLanguageProfile -ErrorAction Stop 3>&1)
		$warnings = @($output | Where-Object { $_ -is [System.Management.Automation.WarningRecord] })
		$profiles = @($output | Where-Object { $_ -isnot [System.Management.Automation.WarningRecord] })

		$profiles | Should -HaveCount 1
		$profiles[0] | Should -BeOfType ([MG.Sonarr.Next.Models.Profiles.LanguageProfileObject])
		$profiles[0].psobject.TypeNames[0] | Should -BeExactly 'MG.Sonarr.Next.Models.Profiles.LanguageProfileObject'
		$profiles[0].MetadataTag.Value | Should -BeExactly '#language_profile'
		$profiles[0].Id | Should -Be $apiProfiles[0].id
		$profiles[0].Name | Should -BeExactly $apiProfiles[0].name
		$warnings | Should -HaveCount 1
		$warnings[0].FullyQualifiedWarningId | Should -BeExactly 'CommandObsolete'
		$warnings[0].Message | Should -BeLike "*'Get-SonarrLanguageProfile' is obsolete*"
	}
}

Describe 'Get-SonarrDelayProfile' {
	It 'returns every delay profile that GET delayprofile returns, the built-in one included, as DelayProfileObjects with the #delay_profile tag' {
		$apiProfiles = @((Invoke-SandboxApi delayprofile -ExpectStatus 200).Body)
		$apiProfiles.order | Should -Contain ([int]::MaxValue) -Because 'Sonarr''s built-in delay profile comes last, with the largest order'

		$profiles = @(Get-SonarrDelayProfile -ErrorAction Stop)
		$profiles.Id | Sort-Object | Should -Be ($apiProfiles.id | Sort-Object)
		foreach ($item in $profiles) {
			$item | Should -BeOfType ([MG.Sonarr.Next.Models.Profiles.DelayProfileObject])
			$item.psobject.TypeNames[0] | Should -BeExactly 'MG.Sonarr.Next.Models.Profiles.DelayProfileObject'
			$item.MetadataTag.Value | Should -BeExactly '#delay_profile'
			$expected = $apiProfiles | Where-Object id -EQ $item.Id
			$item.Order | Should -Be $expected.order
			# The model renames Sonarr's delays, which are in minutes.
			$item.UsenetDelayInMins | Should -Be $expected.usenetDelay
			$item.TorrentDelayInMins | Should -Be $expected.torrentDelay
		}
	}
}

Describe 'New-SonarrDelayProfile' {
	BeforeAll {
		$tag = New-TestTag
	}
	AfterAll {
		Remove-TestDelayProfile $tag.id
	}

	It 'creates a delay profile for a tag given by its label, and returns it as a DelayProfileObject with the #delay_profile tag' {
		$newArgs = @{
			Order        = 1
			Protocol     = 'PreferTorrent'
			UsenetDelay  = (New-TimeSpan -Minutes 10)
			TorrentDelay = (New-TimeSpan -Minutes 5 -Seconds 30)
			Tags         = $tag.label
		}
		$profiles = @(New-SonarrDelayProfile @newArgs -ErrorAction Stop)

		$profiles | Should -HaveCount 1
		$profiles[0] | Should -BeOfType ([MG.Sonarr.Next.Models.Profiles.DelayProfileObject])
		$profiles[0].psobject.TypeNames[0] | Should -BeExactly 'MG.Sonarr.Next.Models.Profiles.DelayProfileObject'
		$profiles[0].MetadataTag.Value | Should -BeExactly '#delay_profile'

		# PreferTorrent allows both protocols and prefers torrents, and each delay goes to Sonarr in whole minutes.
		$stored = (Invoke-SandboxApi "delayprofile/$($profiles[0].Id)" -ExpectStatus 200).Body
		@($stored.tags) | Should -Be @($tag.id)
		$stored.enableUsenet | Should -BeTrue
		$stored.enableTorrent | Should -BeTrue
		$stored.preferredProtocol | Should -BeExactly 'torrent'
		$stored.usenetDelay | Should -Be 10
		$stored.torrentDelay | Should -Be 5
		$stored.order | Should -Be 1
	}
}

Describe 'Update-SonarrDelayProfile' {
	BeforeAll {
		$tag = New-TestTag
		$delayProfile = New-TestDelayProfile $tag.id
	}
	AfterAll {
		Remove-TestDelayProfile $tag.id
	}

	It 'saves the changed torrent delay of a piped profile, and changes nothing else' {
		$before = (Invoke-SandboxApi "delayprofile/$($delayProfile.id)" -ExpectStatus 200).Body
		$item = Get-SonarrDelayProfile -Id $delayProfile.id -ErrorAction Stop
		$item.TorrentDelayInMins = $before.torrentDelay + 15
		$item | Update-SonarrDelayProfile -ErrorAction Stop | Should -BeNullOrEmpty

		# The request writes TorrentDelayInMins back under Sonarr's name, torrentDelay, and Sonarr reads the preferred
		# protocol, which the model shows with an uppercase first letter, as it was, so the delay is the one field that
		# changes.
		$stored = (Invoke-SandboxApi "delayprofile/$($delayProfile.id)" -ExpectStatus 200).Body
		$stored.torrentDelay | Should -Be ($before.torrentDelay + 15)
		Get-ChangedField $before $stored | Should -Be 'torrentDelay'
	}
}

Describe 'Remove-SonarrDelayProfile' {
	BeforeAll {
		# Sonarr's built-in delay profile comes last, with the largest order.
		$builtInId = [int] ((Invoke-SandboxApi delayprofile -ExpectStatus 200).Body | Where-Object order -EQ ([int]::MaxValue)).id
	}
	BeforeEach {
		$tag = New-TestTag
		$delayProfile = New-TestDelayProfile $tag.id
	}
	AfterEach {
		Remove-TestDelayProfile $tag.id
	}

	It 'deletes the profile that -Id names, <Case>' -ForEach @(
		@{ Case = 'under -Confirm:$false'; Force = $false }
		@{ Case = 'with -Force, without asking'; Force = $true }
	) {
		$removeArgs = if ($Force) { @{ Force = $true } } else { @{ Confirm = $false } }
		Remove-SonarrDelayProfile -Id $delayProfile.id @removeArgs -ErrorAction Stop | Should -BeNullOrEmpty

		(Invoke-SandboxApi "delayprofile/$($delayProfile.id)").StatusCode | Should -Be 404
	}

	It 'writes an error for the built-in profile, and deletes the other profiles piped with it' {
		Get-SonarrDelayProfile -Id $builtInId, $delayProfile.id -ErrorAction Stop |
			Remove-SonarrDelayProfile -Force -ErrorAction SilentlyContinue -ErrorVariable removeErrors

		$removeErrors.Count | Should -Be 1
		$removeErrors[0].Exception | Should -BeOfType ([MG.Sonarr.Next.Exceptions.Shell.SonarrParameterException])
		$removeErrors[0].CategoryInfo.Category | Should -Be ([System.Management.Automation.ErrorCategory]::InvalidArgument)
		$removeErrors[0].FullyQualifiedErrorId | Should -BeExactly 'SonarrParameterException.DeletingRestrictedId,MG.Sonarr.Next.Shell.Cmdlets.Profiles.Delay.RemoveSonarrDelayProfileCmdlet'
		(Invoke-SandboxApi "delayprofile/$builtInId").StatusCode | Should -Be 200
		(Invoke-SandboxApi "delayprofile/$($delayProfile.id)").StatusCode | Should -Be 404
	}
}

Describe 'Get-SonarrReleaseProfile' {
	BeforeAll {
		$releaseProfile = New-TestReleaseProfile
	}
	AfterAll {
		Remove-TestReleaseProfile $releaseProfile.id
	}

	It 'returns every release profile that GET releaseprofile returns, as ReleaseProfileObjects with the #release_profile tag' {
		$apiProfiles = @((Invoke-SandboxApi releaseprofile -ExpectStatus 200).Body)
		$apiProfiles.id | Should -Contain $releaseProfile.id -Because 'the test created a release profile'

		$profiles = @(Get-SonarrReleaseProfile -ErrorAction Stop)
		$profiles.Id | Sort-Object | Should -Be ($apiProfiles.id | Sort-Object)
		foreach ($item in $profiles) {
			$item | Should -BeOfType ([MG.Sonarr.Next.Models.Profiles.ReleaseProfileObject])
			$item.psobject.TypeNames[0] | Should -BeExactly 'MG.Sonarr.Next.Models.Profiles.ReleaseProfileObject'
			$item.MetadataTag.Value | Should -BeExactly '#release_profile'
			$item.Name | Should -BeExactly ($apiProfiles | Where-Object id -EQ $item.Id).name
		}
		@(($profiles | Where-Object Id -EQ $releaseProfile.id).Required) | Should -BeExactly @($releaseProfile.required)
	}

	It 'finds the profile by its name, given by position or in -Name, ignoring case' {
		foreach ($name in $releaseProfile.name, $releaseProfile.name.ToUpperInvariant()) {
			@(Get-SonarrReleaseProfile $name -ErrorAction Stop).Id | Should -Be $releaseProfile.id
			@(Get-SonarrReleaseProfile -Name $name -ErrorAction Stop).Id | Should -Be $releaseProfile.id
		}
	}

	It 'finds the profile by its ID, in -Id or given by position' {
		@(Get-SonarrReleaseProfile -Id $releaseProfile.id -ErrorAction Stop).Name | Should -BeExactly $releaseProfile.name
		@(Get-SonarrReleaseProfile $releaseProfile.id -ErrorAction Stop).Name | Should -BeExactly $releaseProfile.name
	}
}

Describe 'New-SonarrReleaseProfile' {
	BeforeAll {
		$name = New-TestName
	}
	AfterAll {
		$profiles = (Invoke-SandboxApi releaseprofile -ExpectStatus 200).Body | Where-Object name -EQ $name
		foreach ($item in $profiles) {
			$null = Invoke-SandboxApi "releaseprofile/$($item.id)" -Method Delete -ExpectStatus 200
		}
	}

	It 'creates a release profile with the name and required term that it is given, and returns it as a ReleaseProfileObject with the #release_profile tag' {
		$term = "$name-term"
		$profiles = @(New-SonarrReleaseProfile -Name $name -RequiredTerms $term -ErrorAction Stop)

		$profiles | Should -HaveCount 1
		$profiles[0] | Should -BeOfType ([MG.Sonarr.Next.Models.Profiles.ReleaseProfileObject])
		$profiles[0].psobject.TypeNames[0] | Should -BeExactly 'MG.Sonarr.Next.Models.Profiles.ReleaseProfileObject'
		$profiles[0].MetadataTag.Value | Should -BeExactly '#release_profile'
		$profiles[0].Name | Should -BeExactly $name

		# Without -Enabled, -Indexer, or -Tags, the profile is disabled and applies to every indexer and every series.
		$stored = (Invoke-SandboxApi "releaseprofile/$($profiles[0].Id)" -ExpectStatus 200).Body
		$stored.name | Should -BeExactly $name
		@($stored.required) | Should -BeExactly @($term)
		$stored.ignored | Should -BeNullOrEmpty
		$stored.enabled | Should -BeFalse
		$stored.indexerId | Should -Be 0
		$stored.tags | Should -BeNullOrEmpty
	}
}

Describe 'Update-SonarrReleaseProfile' {
	BeforeAll {
		$releaseProfile = New-TestReleaseProfile
	}
	AfterAll {
		Remove-TestReleaseProfile $releaseProfile.id
	}

	It 'saves a piped profile with Enabled turned on, and changes nothing else' {
		$before = (Invoke-SandboxApi "releaseprofile/$($releaseProfile.id)" -ExpectStatus 200).Body
		$before.enabled | Should -BeFalse -Because 'the test created the profile disabled'

		$item = Get-SonarrReleaseProfile -Id $releaseProfile.id -ErrorAction Stop
		$item.Enabled = $true
		$item | Update-SonarrReleaseProfile -ErrorAction Stop | Should -BeNullOrEmpty

		# The request sends the whole profile back, its terms included, so Enabled is the one field that changes.
		$stored = (Invoke-SandboxApi "releaseprofile/$($releaseProfile.id)" -ExpectStatus 200).Body
		$stored.enabled | Should -BeTrue
		Get-ChangedField $before $stored | Should -Be 'enabled'
	}
}

Describe 'Remove-SonarrReleaseProfile' {
	BeforeEach {
		$releaseProfile = New-TestReleaseProfile
	}
	AfterEach {
		Remove-TestReleaseProfile $releaseProfile.id
	}

	It 'deletes the profile that -Id names, <Case>' -ForEach @(
		@{ Case = 'under -Confirm:$false'; Force = $false }
		@{ Case = 'with -Force, without asking'; Force = $true }
	) {
		$removeArgs = if ($Force) { @{ Force = $true } } else { @{ Confirm = $false } }
		Remove-SonarrReleaseProfile -Id $releaseProfile.id @removeArgs -ErrorAction Stop | Should -BeNullOrEmpty

		(Invoke-SandboxApi "releaseprofile/$($releaseProfile.id)").StatusCode | Should -Be 404
	}
}
