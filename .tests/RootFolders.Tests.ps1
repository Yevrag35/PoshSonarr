#Requires -Version 7.6
#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '6.2.0'; MaximumVersion = '6.99.99' }

# Root folders. The baseline has one, /tv, which setup.ps1 checks is there. The tests that add or remove a root folder
# use /tv-temp, an empty folder beside /tv in the sandbox's image, owned by the user that Sonarr runs as, since Sonarr
# adds a root folder only when the folder exists and Sonarr can write to it, and turns away a folder that's already a
# root folder. Each of those tests adds /tv-temp through the API, unless it tests New-SonarrRootFolder, so a broken
# cmdlet fails only its own tests. Afterward, /tv-temp is removed through the API if it's still a root folder, which
# leaves the folder itself in place for the next test.

BeforeAll {
	. "$PSScriptRoot/setup.ps1"

	# The folder that the tests add as a root folder.
	$tempFolder = '/tv-temp'

	# Returns the root folders whose path, without a trailing /, is the one given, as GET rootfolder returns them.
	function Get-ApiRootFolder {
		param (
			[Parameter(Mandatory = $true)]
			[string] $Path
		)

		(Invoke-SandboxApi rootfolder -ExpectStatus 200).Body | Where-Object { $_.path.TrimEnd('/') -ceq $Path }
	}

	# Adds /tv-temp as a root folder through the API, and returns it as the API returned it.
	function Add-TempRootFolder {
		(Invoke-SandboxApi rootfolder -Method Post -Body @{ path = $tempFolder } -ExpectStatus 201).Body
	}

	# Removes /tv-temp as a root folder through the API, if it's still one.
	function Remove-TempRootFolder {
		foreach ($item in Get-ApiRootFolder $tempFolder) {
			$null = Invoke-SandboxApi "rootfolder/$($item.id)" -Method Delete -ExpectStatus 200
		}
	}
}

Describe 'Get-SonarrRootFolder' {
	BeforeAll {
		$apiFolders = @((Invoke-SandboxApi rootfolder -ExpectStatus 200).Body)
		$tvFolder = $apiFolders | Where-Object { $_.path.TrimEnd('/') -ceq $Fixtures.RootFolder }
	}

	It 'returns every root folder that GET rootfolder returns, /tv among them, as RootFolderObjects with the #root_folder tag' {
		$folders = @(Get-SonarrRootFolder -ErrorAction Stop)

		$folders.Id | Sort-Object | Should -Be ($apiFolders.id | Sort-Object)
		$folders.Id | Should -Contain $tvFolder.id
		foreach ($item in $folders) {
			$item | Should -BeOfType ([MG.Sonarr.Next.Models.RootFolders.RootFolderObject])
			$item.psobject.TypeNames[0] | Should -BeExactly 'MG.Sonarr.Next.Models.RootFolders.RootFolderObject'
			$item.MetadataTag.Value | Should -BeExactly '#root_folder'
			# Free space changes between two requests, as Sonarr and the container write files, so it isn't compared.
			$expected = $apiFolders | Where-Object id -EQ $item.Id
			$item.Path | Should -BeExactly $expected.path
			$item.Accessible | Should -Be $expected.accessible
		}
	}

	It 'finds /tv by its ID, in -Id or given by position' {
		@(Get-SonarrRootFolder -Id $tvFolder.id -ErrorAction Stop).Path | Should -BeExactly $tvFolder.path
		@(Get-SonarrRootFolder $tvFolder.id -ErrorAction Stop).Path | Should -BeExactly $tvFolder.path
	}

	Context 'a second root folder' {
		BeforeAll {
			$tempRootFolder = Add-TempRootFolder
		}
		AfterAll {
			Remove-TempRootFolder
		}

		It 'returns /tv-temp beside /tv, as GET rootfolder does' {
			$apiIds = @((Invoke-SandboxApi rootfolder -ExpectStatus 200).Body.id)
			$apiIds | Should -Contain $tempRootFolder.id -Because 'the test added /tv-temp'

			$folders = @(Get-SonarrRootFolder -ErrorAction Stop)
			$folders.Id | Sort-Object | Should -Be ($apiIds | Sort-Object)
			$folders.Id | Should -Contain $tvFolder.id
			($folders | Where-Object Id -EQ $tempRootFolder.id).Path | Should -BeExactly $tempRootFolder.path
		}

		# With two root folders, a cmdlet that ignored -Id would return both, so these find only the one that they name.
		It 'finds /tv-temp by its ID, in -Id or given by position' {
			@(Get-SonarrRootFolder -Id $tempRootFolder.id -ErrorAction Stop).Path | Should -BeExactly $tempRootFolder.path
			@(Get-SonarrRootFolder $tempRootFolder.id -ErrorAction Stop).Path | Should -BeExactly $tempRootFolder.path
		}
	}
}

Describe 'New-SonarrRootFolder' {
	AfterEach {
		Remove-TempRootFolder
	}

	It 'adds the folder at a path that''s absolute on Sonarr''s computer, as it''s given, and returns it as a RootFolderObject with the #root_folder tag' {
		$folders = @(New-SonarrRootFolder $tempFolder -ErrorAction Stop)

		$folders | Should -HaveCount 1
		$folders[0] | Should -BeOfType ([MG.Sonarr.Next.Models.RootFolders.RootFolderObject])
		$folders[0].psobject.TypeNames[0] | Should -BeExactly 'MG.Sonarr.Next.Models.RootFolders.RootFolderObject'
		$folders[0].MetadataTag.Value | Should -BeExactly '#root_folder'
		$folders[0].Path | Should -BeExactly $tempFolder

		# The cmdlet sends a path that's fully qualified on Sonarr's computer as it's given, so from a session on Windows
		# too, /tv-temp reaches Sonarr without the current drive in front of it.
		$stored = (Invoke-SandboxApi "rootfolder/$($folders[0].Id)" -ExpectStatus 200).Body
		$stored.path | Should -BeExactly $tempFolder
		$stored.accessible | Should -BeTrue
	}

	It 'adds the folder that a piped object''s FullName names' {
		$folders = @([pscustomobject]@{ FullName = $tempFolder } | New-SonarrRootFolder -ErrorAction Stop)

		$folders | Should -HaveCount 1
		$folders[0].Path | Should -BeExactly $tempFolder
		@(Get-ApiRootFolder $tempFolder).id | Should -Be $folders[0].Id
	}

	It 'writes Sonarr''s validation error for a folder that doesn''t exist, and adds no root folder' {
		$missingPath = "/$(New-TestName)"
		New-SonarrRootFolder $missingPath -ErrorAction SilentlyContinue -ErrorVariable newErrors | Should -BeNullOrEmpty

		$newErrors.Count | Should -Be 1
		$newErrors[0].Exception | Should -BeOfType ([MG.Sonarr.Next.Exceptions.SonarrHttpException])
		$newErrors[0].Exception.StatusCode | Should -Be ([System.Net.HttpStatusCode]::BadRequest)
		$newErrors[0].CategoryInfo.Category | Should -Be ([System.Management.Automation.ErrorCategory]::InvalidData)
		$newErrors[0].TargetObject | Should -BeExactly '/rootfolder'
		# The message names the path that Sonarr checked, so it shows that the cmdlet sent the path as the test gave it,
		# and that Sonarr turned the folder away because it doesn't exist.
		$newErrors[0].Exception.Message | Should -BeLike "*Path '$missingPath' does not exist*"
		Get-ApiRootFolder $missingPath | Should -BeNullOrEmpty
	}
}

Describe 'Remove-SonarrRootFolder' {
	BeforeEach {
		$folder = Add-TempRootFolder
	}
	AfterEach {
		Remove-TempRootFolder
	}

	# Sonarr answers the delete of a root folder that it doesn't have with a 200 too, so the tests check through GET that
	# the root folder is gone.
	It 'removes the root folder that -Id names, <Case>' -ForEach @(
		@{ Case = 'under -Confirm:$false'; Force = $false }
		@{ Case = 'with -Force, without asking'; Force = $true }
	) {
		$removeArgs = if ($Force) { @{ Force = $true } } else { @{ Confirm = $false } }
		Remove-SonarrRootFolder -Id $folder.id @removeArgs -ErrorAction Stop | Should -BeNullOrEmpty

		(Invoke-SandboxApi "rootfolder/$($folder.id)").StatusCode | Should -Be 404
		@(Get-ApiRootFolder $Fixtures.RootFolder) | Should -HaveCount 1 -Because 'the cmdlet removes only the root folder that -Id names'
	}

	It 'removes a piped root folder' {
		Get-SonarrRootFolder -Id $folder.id -ErrorAction Stop | Remove-SonarrRootFolder -Force -ErrorAction Stop |
			Should -BeNullOrEmpty

		(Invoke-SandboxApi "rootfolder/$($folder.id)").StatusCode | Should -Be 404
	}
}
