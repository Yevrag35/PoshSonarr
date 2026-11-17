#Requires -Version 7.6
#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '6.2.0'; MaximumVersion = '6.99.99' }

# The naming settings, the media management settings, and the download client settings, each a single object that every
# later test sees. The update tests change one field each, the naming tests replaceIllegalCharacters and the media
# management tests recycleBinCleanupDays, by one, and each update's AfterAll puts back the settings that GET returned
# before its tests. To test a failed update, each sends a value that Sonarr turns away with a 400, which saves nothing: a
# multiEpisodeStyle of 6, outside the 0 to 5 that Sonarr allows, and a recycleBinCleanupDays of -1.

BeforeAll {
	. "$PSScriptRoot/setup.ps1"

	# Returns a line for each field of the settings that Sonarr sent, other than the ones that -Exclude names, that the
	# object from the module lacks or holds with another value. Values compare as text, exactly. -Rename maps a field to
	# the property that the module renames it to.
	function Compare-Setting {
		param (
			[Parameter(Mandatory = $true)]
			[object] $Actual,

			[Parameter(Mandatory = $true)]
			[object] $Expected,

			[Parameter(Mandatory = $false)]
			[string[]] $Exclude,

			[Parameter(Mandatory = $false)]
			[hashtable] $Rename = @{}
		)

		foreach ($field in $Expected.psobject.Properties) {
			if ($field.Name -in $Exclude) {
				continue
			}

			$name = $Rename.ContainsKey($field.Name) ? $Rename[$field.Name] : $field.Name
			$property = $Actual.psobject.Properties[$name]
			if ($null -eq $property) {
				"$name is missing"
				continue
			}

			if ("$($property.Value)" -cne "$($field.Value)") {
				"$name is '$($property.Value)' instead of '$($field.Value)'"
			}
		}
	}

	# Returns the names of the fields whose values differ between two copies of the settings, as the API returned them,
	# so that a test can check that a request changed one field and no other.
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

Describe 'Get-SonarrNamingConfig' {
	It 'returns the naming settings that GET config/naming returns, without their ID' {
		$api = (Invoke-SandboxApi config/naming -ExpectStatus 200).Body
		$config = @(Get-SonarrNamingConfig -ErrorAction Stop)

		$config | Should -HaveCount 1
		$config[0] | Should -BeOfType ([MG.Sonarr.Next.Models.Config.NamingConfigObject])
		$config[0].psobject.TypeNames[0] | Should -BeExactly 'MG.Sonarr.Next.Models.Config.NamingConfigObject'
		$config[0].MetadataTag.Value | Should -BeExactly '#naming_config'
		$config[0].psobject.Id | Should -Be $api.id -Because 'the object keeps its ID in C#'
		$config[0].psobject.Properties['Id'] | Should -BeNullOrEmpty -Because 'the object keeps its ID out of PowerShell''s view'
		Compare-Setting $config[0] $api -Exclude 'id' | Should -BeNullOrEmpty
	}
}

Describe 'Update-SonarrNamingConfig' {
	BeforeAll {
		$original = (Invoke-SandboxApi config/naming -ExpectStatus 200).Body
	}
	AfterAll {
		# PUT config/naming saves every field that it's sent, so the body that GET returned before the tests puts back
		# whatever they changed.
		$stored = (Invoke-SandboxApi config/naming -ExpectStatus 200).Body
		if ((ConvertTo-Json $stored -Compress) -cne (ConvertTo-Json $original -Compress)) {
			$null = Invoke-SandboxApi "config/naming/$($original.id)" -Method Put -Body $original -ExpectStatus 202
		}
	}

	It 'saves the piped naming settings with ReplaceIllegalCharacters flipped, and changes nothing else' {
		$before = (Invoke-SandboxApi config/naming -ExpectStatus 200).Body
		$config = Get-SonarrNamingConfig -ErrorAction Stop
		$config.ReplaceIllegalCharacters = -not $before.replaceIllegalCharacters
		$config | Update-SonarrNamingConfig -ErrorAction Stop | Should -BeNullOrEmpty

		$stored = (Invoke-SandboxApi config/naming -ExpectStatus 200).Body
		$stored.replaceIllegalCharacters | Should -Be (-not $before.replaceIllegalCharacters)
		Get-ChangedField $before $stored | Should -Be 'replaceIllegalCharacters'
		# The update sends the ID in the body, and then takes it out of the object again.
		$config.psobject.Properties['Id'] | Should -BeNullOrEmpty -Because 'the update takes Id out of the object again'
	}

	It 'resets the naming settings and writes the error after a request that fails' {
		$before = (Invoke-SandboxApi config/naming -ExpectStatus 200).Body
		$config = Get-SonarrNamingConfig -ErrorAction Stop
		$config.MultiEpisodeStyle = 6
		$config | Update-SonarrNamingConfig -ErrorAction SilentlyContinue -ErrorVariable updateErrors

		$updateErrors.Count | Should -Be 1
		$updateErrors[0].Exception | Should -BeOfType ([MG.Sonarr.Next.Exceptions.SonarrHttpException])
		$updateErrors[0].Exception.StatusCode | Should -Be ([System.Net.HttpStatusCode]::BadRequest)
		$updateErrors[0].CategoryInfo.Category | Should -Be ([System.Management.Automation.ErrorCategory]::InvalidData)
		$updateErrors[0].TargetObject | Should -BeExactly "/config/naming/$($before.id)"
		$config.psobject.Properties['Id'] | Should -BeNullOrEmpty -Because 'the reset takes Id out of the object again'
		$stored = (Invoke-SandboxApi config/naming -ExpectStatus 200).Body
		Get-ChangedField $before $stored | Should -BeNullOrEmpty
	}
}

Describe 'Get-SonarrMediaManagement' {
	It 'returns the media management settings that GET config/mediamanagement returns, with recycleBin as RecycleBinPath and without their ID' {
		$api = (Invoke-SandboxApi config/mediamanagement -ExpectStatus 200).Body
		$settings = @(Get-SonarrMediaManagement -ErrorAction Stop)

		$settings | Should -HaveCount 1
		$settings[0] | Should -BeOfType ([MG.Sonarr.Next.Models.Media.MediaManagementObject])
		$settings[0].psobject.TypeNames[0] | Should -BeExactly 'MG.Sonarr.Next.Models.Media.MediaManagementObject'
		$settings[0].MetadataTag.Value | Should -BeExactly '#media_management'
		$settings[0].psobject.Id | Should -Be $api.id -Because 'the object keeps its ID in C#'
		$settings[0].psobject.Properties['Id'] | Should -BeNullOrEmpty -Because 'the object keeps its ID out of PowerShell''s view'
		Compare-Setting $settings[0] $api -Exclude 'id' -Rename @{ recycleBin = 'RecycleBinPath' } | Should -BeNullOrEmpty
		# A Path alias shows the recycle bin too.
		$settings[0].Path | Should -BeExactly $api.recycleBin
	}
}

Describe 'Update-SonarrMediaManagement' {
	BeforeAll {
		$original = (Invoke-SandboxApi config/mediamanagement -ExpectStatus 200).Body
	}
	AfterAll {
		# PUT config/mediamanagement saves every field that it's sent, so the body that GET returned before the tests
		# puts back whatever they changed.
		$stored = (Invoke-SandboxApi config/mediamanagement -ExpectStatus 200).Body
		if ((ConvertTo-Json $stored -Compress) -cne (ConvertTo-Json $original -Compress)) {
			$null = Invoke-SandboxApi "config/mediamanagement/$($original.id)" -Method Put -Body $original -ExpectStatus 202
		}
	}

	# The cmdlet's confirm impact is High, so it prompts for confirmation unless -Force or -Confirm:$false is given, and a
	# prompt fails in the runner's non-interactive process.
	It 'saves the piped settings with RecycleBinCleanupDays changed by one, and changes nothing else, <Case>' -ForEach @(
		@{ Case = 'under -Confirm:$false'; Force = $false }
		@{ Case = 'with -Force, without asking'; Force = $true }
	) {
		$before = (Invoke-SandboxApi config/mediamanagement -ExpectStatus 200).Body
		$settings = Get-SonarrMediaManagement -ErrorAction Stop
		$settings.RecycleBinCleanupDays = $before.recycleBinCleanupDays + 1
		$updateArgs = if ($Force) { @{ Force = $true } } else { @{ Confirm = $false } }
		$settings | Update-SonarrMediaManagement @updateArgs -ErrorAction Stop | Should -BeNullOrEmpty

		# The update sends RecycleBinPath back under Sonarr's name, recycleBin, so the cleanup days are the one field that
		# changes.
		$stored = (Invoke-SandboxApi config/mediamanagement -ExpectStatus 200).Body
		$stored.recycleBinCleanupDays | Should -Be ($before.recycleBinCleanupDays + 1)
		Get-ChangedField $before $stored | Should -Be 'recycleBinCleanupDays'
		# The update also sends the ID in the body, in place of the Path alias, and then puts the object back as it was.
		$settings.psobject.Properties['Id'] | Should -BeNullOrEmpty -Because 'the update takes Id out of the object again'
		$settings.psobject.Properties['Path'] | Should -Not -BeNullOrEmpty -Because 'the update puts the Path alias back'
	}

	It 'resets the settings and writes the error after a request that fails' {
		$before = (Invoke-SandboxApi config/mediamanagement -ExpectStatus 200).Body
		$settings = Get-SonarrMediaManagement -ErrorAction Stop
		$settings.RecycleBinCleanupDays = -1
		$settings | Update-SonarrMediaManagement -Force -ErrorAction SilentlyContinue -ErrorVariable updateErrors

		$updateErrors.Count | Should -Be 1
		$updateErrors[0].Exception | Should -BeOfType ([MG.Sonarr.Next.Exceptions.SonarrHttpException])
		$updateErrors[0].Exception.StatusCode | Should -Be ([System.Net.HttpStatusCode]::BadRequest)
		$updateErrors[0].CategoryInfo.Category | Should -Be ([System.Management.Automation.ErrorCategory]::InvalidData)
		$updateErrors[0].TargetObject | Should -BeExactly "/config/mediamanagement/$($before.id)"
		$settings.psobject.Properties['Id'] | Should -BeNullOrEmpty -Because 'the reset takes Id out of the object again'
		$settings.psobject.Properties['Path'] | Should -Not -BeNullOrEmpty -Because 'the reset puts the Path alias back'
		$stored = (Invoke-SandboxApi config/mediamanagement -ExpectStatus 200).Body
		Get-ChangedField $before $stored | Should -BeNullOrEmpty
	}
}

Describe 'Get-SonarrDownloadClientConfig' {
	It 'returns the download client settings that GET config/downloadclient returns' {
		$api = (Invoke-SandboxApi config/downloadclient -ExpectStatus 200).Body
		$config = @(Get-SonarrDownloadClientConfig -ErrorAction Stop)

		$config | Should -HaveCount 1
		$config[0] | Should -BeOfType ([MG.Sonarr.Next.Models.Config.DownloadClientConfigObject])
		$config[0].psobject.TypeNames[0] | Should -BeExactly 'MG.Sonarr.Next.Models.Config.DownloadClientConfigObject'
		$config[0].MetadataTag.Value | Should -BeExactly '#download_client_config'
		# Unlike the naming and media management settings, the object shows its ID.
		Compare-Setting $config[0] $api | Should -BeNullOrEmpty
	}
}
