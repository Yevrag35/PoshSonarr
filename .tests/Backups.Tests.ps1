#Requires -Version 7.6
#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '6.2.0'; MaximumVersion = '6.99.99' }

# Sonarr's backups, each a zip of its database and config.xml. Sonarr names each backup itself, after the second that it
# made the backup in, and makes scheduled backups of its own, so each test makes its backup through the API: it posts a
# Backup command, waits for Sonarr to finish it, and takes the manual backup whose path GET system/backup didn't list
# before. The tests of each cmdlet make a backup of their own, so a broken cmdlet fails only its own tests, and
# afterward each test's backup is deleted through the API if it's still there. Each test saves its files to Pester's
# TestDrive:, under names of its own.

BeforeAll {
	. "$PSScriptRoot/setup.ps1"

	# Waits until Sonarr has finished the command with an ID, for at most 30 seconds, and returns the command as GET
	# command/{id} returns it. Sonarr answers a command's POST once it has queued the command, and runs the command in
	# the background.
	function Wait-TestCommand {
		param (
			[Parameter(Mandatory = $true)]
			[int] $Id
		)

		$deadline = [datetime]::UtcNow.AddSeconds(30)
		while ($true) {
			$command = (Invoke-SandboxApi "command/$Id" -ExpectStatus 200).Body
			if ($command.status -notin 'queued', 'started') {
				return $command
			}
			if ([datetime]::UtcNow -ge $deadline) {
				throw "Sonarr didn't finish the $($command.name) command $Id within 30 seconds: it's still $($command.status)."
			}
			Start-Sleep -Milliseconds 100
		}
	}

	# Makes a backup through the API, and returns it as GET system/backup lists it.
	function New-TestBackup {
		$before = @((Invoke-SandboxApi system/backup -ExpectStatus 200).Body | ForEach-Object path)
		$started = (Invoke-SandboxApi command -Method Post -Body @{ name = 'Backup' } -ExpectStatus 201).Body
		$finished = Wait-TestCommand $started.id
		if ($finished.status -cne 'completed') {
			throw "Sonarr's Backup command ended $($finished.status): $($finished.message)"
		}

		$new = @((Invoke-SandboxApi system/backup -ExpectStatus 200).Body | Where-Object { $_.type -ceq 'manual' -and $_.path -cnotin $before })
		if ($new.Count -ne 1) {
			throw "GET system/backup lists $($new.Count) new manual backups after the Backup command, instead of one."
		}
		$new[0]
	}

	# Deletes a backup through the API, if GET system/backup still lists it.
	function Remove-TestBackup {
		param (
			[Parameter(Mandatory = $true)]
			[object] $Backup
		)

		if (@((Invoke-SandboxApi system/backup -ExpectStatus 200).Body | ForEach-Object id) -contains $Backup.id) {
			$null = Invoke-SandboxApi "system/backup/$($Backup.id)" -Method Delete -ExpectStatus 200
		}
	}

	# Returns the IDs of the backups that GET system/backup lists.
	function Get-BackupId {
		@((Invoke-SandboxApi system/backup -ExpectStatus 200).Body | ForEach-Object id)
	}

	# Returns a file's first two bytes as text. Every zip file starts with PK.
	function Get-FileSignature {
		param (
			[Parameter(Mandatory = $true)]
			[string] $Path
		)

		-join [char[]] (Get-Content -LiteralPath $Path -AsByteStream -TotalCount 2)
	}
}

Describe 'Get-SonarrBackup' {
	BeforeAll {
		$backup = New-TestBackup
	}
	AfterAll {
		Remove-TestBackup $backup
	}

	It 'returns every backup that GET system/backup returns, as BackupObjects with the #backup tag' {
		$apiBackups = @((Invoke-SandboxApi system/backup -ExpectStatus 200).Body)
		$apiBackups.id | Should -Contain $backup.id -Because 'the test made a backup'

		$backups = @(Get-SonarrBackup -ErrorAction Stop)
		$backups.Id | Sort-Object | Should -Be ($apiBackups.id | Sort-Object)
		foreach ($item in $backups) {
			$item | Should -BeOfType ([MG.Sonarr.Next.Models.System.BackupObject])
			$item.psobject.TypeNames[0] | Should -BeExactly 'MG.Sonarr.Next.Models.System.BackupObject'
			$item.MetadataTag.Value | Should -BeExactly '#backup'
			$expected = $apiBackups | Where-Object id -EQ $item.Id
			$item.Name | Should -BeExactly $expected.name
			# The model shows Sonarr's path as BackupUri.
			$item.BackupUri | Should -BeExactly $expected.path
			$item.Type | Should -BeExactly $expected.type
			$item.Size | Should -Be $expected.size
		}
	}
}

Describe 'Save-SonarrBackup' {
	BeforeAll {
		$backup = New-TestBackup
	}
	AfterAll {
		Remove-TestBackup $backup
	}

	It 'writes a piped backup to a folder, under the backup''s name, as the zip that Sonarr made' {
		$folder = New-TestName
		$null = New-Item -ItemType Directory -Path (Join-Path $TestDrive $folder)
		$item = Get-SonarrBackup -ErrorAction Stop | Where-Object Id -EQ $backup.id
		$saved = @($item | Save-SonarrBackup -Path "TestDrive:/$folder" -ErrorAction Stop)

		$saved | Should -HaveCount 1
		$saved[0] | Should -BeOfType ([System.IO.FileInfo])
		$saved[0].FullName | Should -Be (Join-Path $TestDrive $folder $backup.name)
		$saved[0].Length | Should -Be $backup.size
		Get-FileSignature $saved[0].FullName | Should -BeExactly 'PK'
	}

	It 'writes the backup that -BackupUri names to a .zip file' {
		$name = "$(New-TestName).zip"
		$saved = @(Save-SonarrBackup -BackupUri $backup.path -Path "TestDrive:/$name" -ErrorAction Stop)

		$saved | Should -HaveCount 1
		$saved[0].FullName | Should -Be (Join-Path $TestDrive $name)
		$saved[0].Length | Should -Be $backup.size
		Get-FileSignature $saved[0].FullName | Should -BeExactly 'PK'
	}

	It 'stops with an error, and writes no file, for a path whose extension isn''t .zip' {
		$name = "$(New-TestName).txt"
		Save-SonarrBackup -BackupUri $backup.path -Path "TestDrive:/$name" -ErrorAction SilentlyContinue -ErrorVariable saveErrors |
			Should -BeNullOrEmpty

		$saveErrors.Count | Should -Be 1
		$saveErrors[0].Exception | Should -BeOfType ([MG.Sonarr.Next.Exceptions.Shell.SonarrParameterException])
		$saveErrors[0].CategoryInfo.Category | Should -Be ([System.Management.Automation.ErrorCategory]::InvalidArgument)
		$saveErrors[0].TargetObject | Should -Be (Join-Path $TestDrive $name)
		Test-Path -LiteralPath (Join-Path $TestDrive $name) | Should -BeFalse
	}

	It 'writes an error, and leaves the file as it was, for a file that exists' {
		$path = Join-Path $TestDrive "$(New-TestName).zip"
		Set-Content -LiteralPath $path -Value 'pester' -NoNewline
		Save-SonarrBackup -BackupUri $backup.path -Path $path -ErrorAction SilentlyContinue -ErrorVariable saveErrors |
			Should -BeNullOrEmpty

		$saveErrors.Count | Should -Be 1
		$saveErrors[0].Exception | Should -BeOfType ([System.IO.IOException])
		$saveErrors[0].CategoryInfo.Category | Should -Be ([System.Management.Automation.ErrorCategory]::WriteError)
		$saveErrors[0].TargetObject | Should -Be $path
		Get-Content -LiteralPath $path -Raw | Should -BeExactly 'pester'
	}

	It 'replaces a file that exists under -Force' {
		$path = Join-Path $TestDrive "$(New-TestName).zip"
		Set-Content -LiteralPath $path -Value 'pester' -NoNewline
		$saved = @(Save-SonarrBackup -BackupUri $backup.path -Path $path -Force -ErrorAction Stop)

		$saved | Should -HaveCount 1
		$saved[0].FullName | Should -Be $path
		$saved[0].Length | Should -Be $backup.size
		Get-FileSignature $path | Should -BeExactly 'PK'
	}
}

Describe 'Remove-SonarrBackup' {
	BeforeEach {
		$backup = New-TestBackup
	}
	AfterEach {
		Remove-TestBackup $backup
	}

	It 'deletes the backup that -Id names with -Force, without asking' {
		Remove-SonarrBackup -Id $backup.id -Force -ErrorAction Stop | Should -BeNullOrEmpty

		Get-BackupId | Should -Not -Contain $backup.id
	}

	It 'deletes a piped backup' {
		Get-SonarrBackup -ErrorAction Stop | Where-Object Id -EQ $backup.id | Remove-SonarrBackup -Force -ErrorAction Stop |
			Should -BeNullOrEmpty

		Get-BackupId | Should -Not -Contain $backup.id
	}

	It 'asks a question of its own under -Confirm:$false, and deletes nothing when the question fails' {
		# -Confirm:$false skips the prompt that ShouldProcess shows, but not the cmdlet's own question, which only -Force
		# skips. The runner runs Pester under -NonInteractive, where a prompt throws instead of waiting for an answer.
		Remove-SonarrBackup -Id $backup.id -Confirm:$false -ErrorAction SilentlyContinue -ErrorVariable removeErrors

		$removeErrors.Count | Should -Be 1
		$removeErrors[0].Exception | Should -BeOfType ([System.Management.Automation.PSInvalidOperationException])
		Get-BackupId | Should -Contain $backup.id
	}

	It 'writes a warning, and no error, for a backup that Sonarr doesn''t have' {
		# Sonarr answers the delete of a backup that it doesn't have with a 404, which the cmdlet writes as a warning.
		$null = Invoke-SandboxApi "system/backup/$($backup.id)" -Method Delete -ExpectStatus 200
		$removeArgs = @{
			Force           = $true
			ErrorAction     = 'SilentlyContinue'
			ErrorVariable   = 'removeErrors'
			WarningAction   = 'SilentlyContinue'
			WarningVariable = 'removeWarnings'
		}
		Remove-SonarrBackup -Id $backup.id @removeArgs

		$removeErrors.Count | Should -Be 0
		$removeWarnings.Count | Should -Be 1
		$removeWarnings[0].Message | Should -BeLike "*system/backup/$($backup.id)*"
	}
}
