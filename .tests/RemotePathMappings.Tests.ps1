#Requires -Version 7.6
#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '6.2.0'; MaximumVersion = '6.99.99' }

# Remote path mappings. The baseline has none, so each test works on a mapping of its own, which it creates through the
# API unless it tests New-SonarrRemotePathMapping, so a broken cmdlet fails only its own tests. Each mapping is for a
# host named by New-TestName, which no other mapping has, since Sonarr turns away a second mapping with the same host and
# remote path. It maps a remote path named after the host to /tv/, since Sonarr requires a mapping's local path to exist,
# and /tv is the baseline's root folder. Every path ends with a /, which Sonarr adds to the paths of a new mapping, so
# what Sonarr stores is what the test sent. Afterward, the mappings for each test's host are deleted through the API, if
# any are left.

BeforeAll {
	. "$PSScriptRoot/setup.ps1"

	# The local path of every mapping that the tests create.
	$tvPath = "$($Fixtures.RootFolder)/"

	# Creates a remote path mapping through the API, for a host named by New-TestName, from a remote path named after the
	# host to /tv/, and returns it as the API returned it.
	function New-TestMapping {
		$hostName = New-TestName
		$body = @{
			host       = $hostName
			remotePath = "/downloads/$hostName/"
			localPath  = $tvPath
		}
		(Invoke-SandboxApi remotepathmapping -Method Post -Body $body -ExpectStatus 201).Body
	}

	# Deletes through the API each remote path mapping for a test's host, if any are left.
	function Remove-TestMapping {
		param (
			[Parameter(Mandatory = $true)]
			[string] $HostName
		)

		foreach ($item in (Invoke-SandboxApi remotepathmapping -ExpectStatus 200).Body) {
			if ($item.host -ceq $HostName) {
				$null = Invoke-SandboxApi "remotepathmapping/$($item.id)" -Method Delete -ExpectStatus 200
			}
		}
	}

	# Returns the names of the fields whose values differ between two copies of a mapping, as the API returned them, so
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

Describe 'New-SonarrRemotePathMapping' {
	BeforeAll {
		$hostName = New-TestName
	}
	AfterAll {
		Remove-TestMapping $hostName
	}

	It 'creates a mapping from a remote path on a host to a local path, and returns it as a RemotePathObject with the #remote_path_mapping tag' {
		$remotePath = "/downloads/$hostName/"
		$mappings = @(New-SonarrRemotePathMapping -HostName $hostName -RemotePath $remotePath -LocalPath $tvPath -ErrorAction Stop)

		$mappings | Should -HaveCount 1
		$mappings[0] | Should -BeOfType ([MG.Sonarr.Next.Models.RemotePaths.RemotePathObject])
		$mappings[0].psobject.TypeNames[0] | Should -BeExactly 'MG.Sonarr.Next.Models.RemotePaths.RemotePathObject'
		$mappings[0].MetadataTag.Value | Should -BeExactly '#remote_path_mapping'
		# The model shows Sonarr's host field as HostName.
		$mappings[0].HostName | Should -BeExactly $hostName
		$mappings[0].RemotePath | Should -BeExactly $remotePath
		$mappings[0].LocalPath | Should -BeExactly $tvPath

		$stored = (Invoke-SandboxApi "remotepathmapping/$($mappings[0].Id)" -ExpectStatus 200).Body
		$stored.host | Should -BeExactly $hostName
		$stored.remotePath | Should -BeExactly $remotePath
		$stored.localPath | Should -BeExactly $tvPath
	}

	It 'writes an error of its own for a local path that isn''t absolute on Sonarr''s computer, and creates no mapping' {
		$remotePath = "/downloads/$hostName-relative/"
		New-SonarrRemotePathMapping -HostName $hostName -RemotePath $remotePath -LocalPath 'tv' -ErrorAction SilentlyContinue -ErrorVariable newErrors |
			Should -BeNullOrEmpty

		# The cmdlet checks the path itself before it sends anything, so the error isn't one of Sonarr's.
		$newErrors.Count | Should -Be 1
		$newErrors[0].Exception | Should -BeOfType ([MG.Sonarr.Next.Exceptions.Shell.SonarrParameterException])
		$newErrors[0].CategoryInfo.Category | Should -Be ([System.Management.Automation.ErrorCategory]::InvalidArgument)
		$newErrors[0].FullyQualifiedErrorId | Should -BeExactly 'New-SonarrRemotePathMapping.InvalidLocalPath,MG.Sonarr.Next.Shell.Cmdlets.RemotePath.NewSonarrRemotePathMappingCmdlet'
		$newErrors[0].TargetObject | Should -BeExactly 'tv'
		@((Invoke-SandboxApi remotepathmapping -ExpectStatus 200).Body | Where-Object remotePath -CEQ $remotePath) | Should -BeNullOrEmpty
	}

	It 'writes Sonarr''s validation error for a local path that doesn''t exist, and creates no mapping' {
		$remotePath = "/downloads/$hostName-missing/"
		$missingPath = "/$(New-TestName)/"
		New-SonarrRemotePathMapping -HostName $hostName -RemotePath $remotePath -LocalPath $missingPath -ErrorAction SilentlyContinue -ErrorVariable newErrors |
			Should -BeNullOrEmpty

		$newErrors.Count | Should -Be 1
		$newErrors[0].Exception | Should -BeOfType ([MG.Sonarr.Next.Exceptions.SonarrHttpException])
		$newErrors[0].Exception.StatusCode | Should -Be ([System.Net.HttpStatusCode]::BadRequest)
		$newErrors[0].CategoryInfo.Category | Should -Be ([System.Management.Automation.ErrorCategory]::InvalidData)
		$newErrors[0].TargetObject | Should -BeExactly '/remotepathmapping'
		@((Invoke-SandboxApi remotepathmapping -ExpectStatus 200).Body | Where-Object remotePath -CEQ $remotePath) | Should -BeNullOrEmpty
	}
}

Describe 'Get-SonarrRemotePathMapping' {
	BeforeAll {
		$mapping = New-TestMapping
	}
	AfterAll {
		Remove-TestMapping $mapping.host
	}

	It 'returns every mapping that GET remotepathmapping returns, as RemotePathObjects with the #remote_path_mapping tag' {
		$apiMappings = @((Invoke-SandboxApi remotepathmapping -ExpectStatus 200).Body)
		$apiMappings.id | Should -Contain $mapping.id -Because 'the test created a mapping'

		$mappings = @(Get-SonarrRemotePathMapping -ErrorAction Stop)
		$mappings.Id | Sort-Object | Should -Be ($apiMappings.id | Sort-Object)
		foreach ($item in $mappings) {
			$item | Should -BeOfType ([MG.Sonarr.Next.Models.RemotePaths.RemotePathObject])
			$item.psobject.TypeNames[0] | Should -BeExactly 'MG.Sonarr.Next.Models.RemotePaths.RemotePathObject'
			$item.MetadataTag.Value | Should -BeExactly '#remote_path_mapping'
			$expected = $apiMappings | Where-Object id -EQ $item.Id
			$item.HostName | Should -BeExactly $expected.host
			$item.RemotePath | Should -BeExactly $expected.remotePath
			$item.LocalPath | Should -BeExactly $expected.localPath
		}
	}

	It 'finds the mapping by its ID, in -Id or given by position' {
		@(Get-SonarrRemotePathMapping -Id $mapping.id -ErrorAction Stop).RemotePath | Should -BeExactly $mapping.remotePath
		@(Get-SonarrRemotePathMapping $mapping.id -ErrorAction Stop).RemotePath | Should -BeExactly $mapping.remotePath
	}
}

Describe 'Update-SonarrRemotePathMapping' {
	BeforeEach {
		$mapping = New-TestMapping
		$newRemotePath = "/downloads/$($mapping.host)-changed/"
	}
	AfterEach {
		Remove-TestMapping $mapping.host
	}

	It 'changes the remote path of the mapping that -Id names, and keeps its host and local path' {
		Update-SonarrRemotePathMapping -Id $mapping.id -RemotePath $newRemotePath -ErrorAction Stop | Should -BeNullOrEmpty

		# Sonarr replaces the whole mapping, so the cmdlet reads the host and the local path from the mapping and sends
		# them with the new remote path.
		$stored = (Invoke-SandboxApi "remotepathmapping/$($mapping.id)" -ExpectStatus 200).Body
		$stored.remotePath | Should -BeExactly $newRemotePath
		Get-ChangedField $mapping $stored | Should -Be 'remotePath'
	}

	It 'saves the changed remote path of a piped mapping, and changes nothing else' {
		$item = Get-SonarrRemotePathMapping -Id $mapping.id -ErrorAction Stop
		$item.RemotePath = $newRemotePath
		$item | Update-SonarrRemotePathMapping -ErrorAction Stop | Should -BeNullOrEmpty

		# The cmdlet takes the ID, the host, and both paths from the piped mapping's properties.
		$stored = (Invoke-SandboxApi "remotepathmapping/$($mapping.id)" -ExpectStatus 200).Body
		$stored.remotePath | Should -BeExactly $newRemotePath
		Get-ChangedField $mapping $stored | Should -Be 'remotePath'
	}
}

Describe 'Remove-SonarrRemotePathMapping' {
	BeforeEach {
		$mapping = New-TestMapping
	}
	AfterEach {
		Remove-TestMapping $mapping.host
	}

	# Sonarr answers the delete of a mapping that it doesn't have with a 200 too, so the tests check through GET that the
	# mapping is gone.
	It 'deletes the mapping that -Id names, <Case>' -ForEach @(
		@{ Case = 'under -Confirm:$false'; Force = $false }
		@{ Case = 'with -Force, without asking'; Force = $true }
	) {
		$removeArgs = if ($Force) { @{ Force = $true } } else { @{ Confirm = $false } }
		Remove-SonarrRemotePathMapping -Id $mapping.id @removeArgs -ErrorAction Stop | Should -BeNullOrEmpty

		(Invoke-SandboxApi "remotepathmapping/$($mapping.id)").StatusCode | Should -Be 404
	}

	It 'deletes a piped mapping' {
		Get-SonarrRemotePathMapping -Id $mapping.id -ErrorAction Stop | Remove-SonarrRemotePathMapping -Force -ErrorAction Stop |
			Should -BeNullOrEmpty

		(Invoke-SandboxApi "remotepathmapping/$($mapping.id)").StatusCode | Should -Be 404
	}
}
