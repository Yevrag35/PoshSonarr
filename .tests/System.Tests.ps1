#Requires -Version 7.6
#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '6.2.0'; MaximumVersion = '6.99.99' }

# The server's status, its drives, and its host config. The host config is a single object that every later test sees,
# so the Update-SonarrHostConfig tests change only backupRetention: by one, within the range of 1 to 90 that Sonarr
# enforces, or, to test a failed update, to 0, which Sonarr turns away with a 400. AfterAll puts back the config that
# GET config/host returned before them.

BeforeAll {
	. "$PSScriptRoot/setup.ps1"

	# The properties that a host config from Get-SonarrHostConfig keeps out of PowerShell's view without
	# -IncludeSensitiveInfo: its ID, which the object keeps in C#, and the API key and the passwords, which an update sends
	# back to Sonarr.
	$hiddenProperties = 'Id', 'ApiKey', 'Password', 'ProxyPassword'

	# Returns a line for each field of a host config that Sonarr sent, other than the ones that -Exclude names, that the
	# object lacks or holds with another value. Values compare as text, exactly, except that the object shows the values
	# of a few fields, such as authenticationMethod's external, with an uppercase first letter.
	function Compare-HostConfig {
		param (
			[Parameter(Mandatory = $true)]
			[object] $Config,

			[Parameter(Mandatory = $true)]
			[object] $Expected,

			[Parameter(Mandatory = $false)]
			[string[]] $Exclude
		)

		$capitalized = 'AuthenticationMethod', 'CertificateValidation', 'LogLevel', 'ProxyType', 'UpdateMechanism'
		foreach ($field in $Expected.psobject.Properties) {
			if ($field.Name -in $Exclude) {
				continue
			}

			$property = $Config.psobject.Properties[$field.Name]
			if ($null -eq $property) {
				"$($field.Name) is missing"
				continue
			}

			$text = "$($field.Value)"
			if ($field.Name -in $capitalized -and $text.Length -gt 0) {
				$text = $text.Substring(0, 1).ToUpperInvariant() + $text.Substring(1)
			}
			if ("$($property.Value)" -cne $text) {
				"$($field.Name) is '$($property.Value)' instead of '$text'"
			}
		}
	}
}

Describe 'Get-SonarrStatus' {
	It 'returns the sandbox''s status' {
		$status = @(Get-SonarrStatus -ErrorAction Stop)
		$status | Should -HaveCount 1
		$status[0].psobject.TypeNames[0] | Should -BeExactly 'MG.Sonarr.Next.Models.System.SystemStatusObject'
		$status[0].MetadataTag.Value | Should -BeExactly '#system_status'
		[string] $status[0].Version | Should -BeExactly $SandboxSettings.Version
		$status[0].InstanceName | Should -BeExactly $SandboxSettings.InstanceName
		$status[0].IsDocker | Should -BeTrue
	}
}

Describe 'Get-SonarrDiskspace' {
	It 'returns each drive that GET diskspace lists, with its label and total space' {
		$api = @((Invoke-SandboxApi diskspace -ExpectStatus 200).Body)
		$drives = @(Get-SonarrDiskspace -ErrorAction Stop)

		# Free space changes between two requests, as Sonarr and the container write files, so it isn't compared.
		$api | Should -Not -BeNullOrEmpty -Because 'Sonarr lists the drives that its folders are on'
		$drives.Path | Sort-Object | Should -Be ($api.path | Sort-Object)
		foreach ($drive in $drives) {
			$expected = $api | Where-Object path -CEQ $drive.Path
			$drive.psobject.TypeNames[0] | Should -BeExactly 'MG.Sonarr.Next.Models.System.DiskspaceObject'
			$drive.MetadataTag.Value | Should -BeExactly '#disk'
			$drive.Label | Should -BeExactly "$($expected.label)"
			$drive.TotalSpace | Should -Be $expected.totalSpace
			$drive.TotalSpace | Should -BeGreaterThan 0
		}
	}
}

Describe 'Get-SonarrHostConfig' {
	BeforeAll {
		$api = (Invoke-SandboxApi config/host -ExpectStatus 200).Body
	}

	It 'returns the host config that GET config/host returns, without its ID, API key, or passwords' {
		$config = @(Get-SonarrHostConfig -ErrorAction Stop)
		$config | Should -HaveCount 1
		$config[0].psobject.TypeNames[0] | Should -BeExactly 'MG.Sonarr.Next.Models.System.NoKeyHostObject'
		$config[0].MetadataTag.Value | Should -BeExactly '#host_config'
		$config[0].psobject.Id | Should -Be $api.id -Because 'the object keeps its ID in C#'
		foreach ($name in $hiddenProperties) {
			$config[0].psobject.Properties[$name] | Should -BeNullOrEmpty -Because "the object keeps $name out of PowerShell's view"
		}
		Compare-HostConfig $config[0] $api -Exclude $hiddenProperties | Should -BeNullOrEmpty
	}

	It 'shows the API key and the passwords with -IncludeSensitiveInfo' {
		$config = Get-SonarrHostConfig -IncludeSensitiveInfo -ErrorAction Stop
		$config.psobject.TypeNames[0] | Should -BeExactly 'MG.Sonarr.Next.Models.System.HostObject'
		$config.ApiKey | Should -BeExactly $SandboxSettings.ApiKey
		$config.psobject.Properties['Id'] | Should -BeNullOrEmpty -Because 'the object keeps its ID in C#'
		Compare-HostConfig $config $api -Exclude 'Id' | Should -BeNullOrEmpty
	}
}

Describe 'Update-SonarrHostConfig' {
	BeforeAll {
		$original = (Invoke-SandboxApi config/host -ExpectStatus 200).Body

		# One more than the current retention, or one less at Sonarr's most, 90.
		$newRetention = if ($original.backupRetention -lt 90) { $original.backupRetention + 1 } else { $original.backupRetention - 1 }
	}
	AfterAll {
		# PUT config/host saves every field that it's sent, so the body that GET returned before the tests puts back
		# whatever they changed.
		$stored = (Invoke-SandboxApi config/host -ExpectStatus 200).Body
		if ((ConvertTo-Json $stored -Compress) -cne (ConvertTo-Json $original -Compress)) {
			$null = Invoke-SandboxApi config/host -Method Put -Body $original -ExpectStatus 202
		}
	}

	It 'saves the piped host config, with the API key and the passwords that the object hides' {
		$config = Get-SonarrHostConfig -ErrorAction Stop
		$config.BackupRetention = $newRetention
		$config | Update-SonarrHostConfig -ErrorAction Stop | Should -BeNullOrEmpty

		# The update sends back the values that the object hides, so the one field that it changes is the one that the
		# test changed.
		$stored = (Invoke-SandboxApi config/host -ExpectStatus 200).Body
		$stored.backupRetention | Should -Be $newRetention
		$changed = foreach ($field in $original.psobject.Properties) {
			if ("$($stored.($field.Name))" -cne "$($field.Value)") {
				$field.Name
			}
		}
		$changed | Should -Be 'backupRetention'

		foreach ($name in $hiddenProperties) {
			$config.psobject.Properties[$name] | Should -BeNullOrEmpty -Because "the update takes $name out of the object again"
		}
	}

	It 'resets the host config and writes the error after a request that fails' {
		$before = (Invoke-SandboxApi config/host -ExpectStatus 200).Body
		$config = Get-SonarrHostConfig -ErrorAction Stop
		$config.BackupRetention = 0
		$config | Update-SonarrHostConfig -ErrorAction SilentlyContinue -ErrorVariable updateErrors

		$updateErrors.Count | Should -Be 1
		$updateErrors[0].Exception | Should -BeOfType ([MG.Sonarr.Next.Exceptions.SonarrHttpException])
		$updateErrors[0].Exception.StatusCode | Should -Be ([System.Net.HttpStatusCode]::BadRequest)
		$updateErrors[0].CategoryInfo.Category | Should -Be ([System.Management.Automation.ErrorCategory]::InvalidData)
		$updateErrors[0].TargetObject | Should -BeExactly '/config/host'
		foreach ($name in $hiddenProperties) {
			$config.psobject.Properties[$name] | Should -BeNullOrEmpty -Because "the reset takes $name out of the object again"
		}
		(Invoke-SandboxApi config/host -ExpectStatus 200).Body.backupRetention | Should -Be $before.backupRetention
	}
}
