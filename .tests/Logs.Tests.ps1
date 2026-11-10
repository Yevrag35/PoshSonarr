#Requires -Version 7.6
#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '6.2.0'; MaximumVersion = '6.99.99' }

# Sonarr's log: the entries that it keeps in its database, and its log files. Sonarr logs as it runs, its scheduled
# tasks included, and writes its entries to the database in the background, in a batch every half second, so an entry
# can land between two reads of the newest ones. A test of the entries reads the page through the API before and after
# the cmdlet's read, and compares only reads between which nothing landed. Sonarr only adds to a log file, so a file
# that a test downloads holds the start of the file as Sonarr serves it afterward. Each test saves its files to Pester's
# TestDrive:, under names of its own.

BeforeAll {
	. "$PSScriptRoot/setup.ps1"

	# Gets a page of Sonarr's log with Get-SonarrLog and the arguments that -Arguments holds, between two reads of the
	# same page through GET log with the query that -Query gives, and returns the cmdlet's entries with the page's
	# records, from the first of five tries in which both reads returned the same records. When they did, no entry
	# landed between them, so the cmdlet read that page too.
	function Get-LogPage {
		param (
			[Parameter(Mandatory = $true)]
			[hashtable] $Arguments,

			[Parameter(Mandatory = $false)]
			[string] $Query = ''
		)

		for ($try = 1; $try -le 5; $try++) {
			$before = @((Invoke-SandboxApi "log$Query" -ExpectStatus 200).Body.records)
			$entries = @(Get-SonarrLog @Arguments -ErrorAction Stop)
			$after = @((Invoke-SandboxApi "log$Query" -ExpectStatus 200).Body.records)
			if (($before.id -join ',') -eq ($after.id -join ',')) {
				return [pscustomobject]@{ Entries = $entries; Records = $before }
			}
		}
		throw "Sonarr logged an entry between the two reads of GET log$Query in each of five tries."
	}

	# The log file that Sonarr wrote to last, as GET log/file returns it, which lists the files newest first.
	$newest = @((Invoke-SandboxApi log/file -ExpectStatus 200).Body)[0]
}

Describe 'Get-SonarrLog' {
	It 'returns the entries on <Case>, sorted by time, as LogObjects with the #log_item tag' -ForEach @(
		@{ Case = 'the first page of GET log, which holds the newest 10'; Arguments = @{}; Query = '' }
		@{ Case = 'the page that -PageNumber and -PageSize pick'; Arguments = @{ PageNumber = 2; PageSize = 3 }; Query = '?page=2&pageSize=3' }
		@{ Case = 'the first page in the order that -SortDirection gives'; Arguments = @{ SortDirection = 'Ascending' }; Query = '?sortDirection=ascending' }
	) {
		$page = Get-LogPage -Arguments $Arguments -Query $Query
		$page.Records | Should -Not -BeNullOrEmpty -Because 'Sonarr logs as it starts and runs'

		$page.Entries.Id | Sort-Object | Should -Be ($page.Records.id | Sort-Object)
		foreach ($item in $page.Entries) {
			$item | Should -BeOfType ([MG.Sonarr.Next.Models.System.LogObject])
			$item.psobject.TypeNames[0] | Should -BeExactly 'MG.Sonarr.Next.Models.System.LogObject'
			$item.MetadataTag.Value | Should -BeExactly '#log_item'
			$expected = $page.Records | Where-Object id -EQ $item.Id
			$item.Level | Should -BeExactly $expected.level
			$item.Logger | Should -BeExactly $expected.logger
			$item.Message | Should -BeExactly $expected.message
		}
		# Sonarr pages the entries newest first unless -SortDirection says otherwise, and the cmdlet writes each page's
		# entries sorted by time.
		$times = @($page.Entries.Time)
		$times | Should -Be @($times | Sort-Object)
	}
}

Describe 'Get-SonarrLogFile' {
	It 'returns every log file that GET log/file and GET log/file/update return, oldest first, as LogFileObjects with the #log_file tag' {
		$general = @((Invoke-SandboxApi log/file -ExpectStatus 200).Body)
		$update = @((Invoke-SandboxApi log/file/update -ExpectStatus 200).Body)
		$general | Should -Not -BeNullOrEmpty -Because 'Sonarr writes its log to files'
		$apiFiles = $general + $update

		$files = @(Get-SonarrLogFile -ErrorAction Stop)
		# A general and an update log file could share a name, so each file is matched by its download URL, which names
		# the log too.
		$files.DownloadUrl | Sort-Object | Should -BeExactly ($apiFiles.downloadUrl | Sort-Object)
		foreach ($item in $files) {
			$item | Should -BeOfType ([MG.Sonarr.Next.Models.System.LogFileObject])
			$item.psobject.TypeNames[0] | Should -BeExactly 'MG.Sonarr.Next.Models.System.LogFileObject'
			$item.MetadataTag.Value | Should -BeExactly '#log_file'
			$expected = $apiFiles | Where-Object downloadUrl -CEQ $item.DownloadUrl
			$item.Filename | Should -BeExactly $expected.filename
			$item.ContentsUrl | Should -BeExactly $expected.contentsUrl
			# The sandbox has never updated, so it has no update log files.
			$type = if ($update.downloadUrl -ccontains $item.DownloadUrl) { 'Update' } else { 'General' }
			$item.Type | Should -Be ([MG.Sonarr.Next.Models.System.LogFileType] $type)
			# Sonarr numbers the files by their place in the folder's listing, and the model takes that ID out.
			$item.psobject.Properties['Id'] | Should -BeNullOrEmpty
		}
		$times = @($files.LastWriteTime)
		$times | Should -Be @($times | Sort-Object)
	}
}

Describe 'Save-SonarrLogFile' {
	It 'writes a piped log file to a folder, under the file''s name, as the start of what Sonarr serves for it' {
		$folder = New-TestName
		$null = New-Item -ItemType Directory -Path (Join-Path $TestDrive $folder)
		$file = Get-SonarrLogFile -ErrorAction Stop | Where-Object DownloadUrl -CEQ $newest.downloadUrl
		$saved = @($file | Save-SonarrLogFile -Path "TestDrive:/$folder" -ErrorAction Stop)

		$saved | Should -HaveCount 1
		$saved[0] | Should -BeOfType ([System.IO.FileInfo])
		$saved[0].FullName | Should -Be (Join-Path $TestDrive $folder $newest.filename)
		$text = [System.IO.File]::ReadAllText($saved[0].FullName)
		$text | Should -Not -BeNullOrEmpty
		$served = (Invoke-SandboxApi "log/file/$($newest.filename)" -ExpectStatus 200).Content
		$served.StartsWith($text, [System.StringComparison]::Ordinal) | Should -BeTrue -Because 'Sonarr only adds to a log file'
	}

	It 'writes the log file that -LogUri names to a .log file' {
		$name = "$(New-TestName).log"
		$saved = @(Save-SonarrLogFile -LogUri $newest.downloadUrl -Path "TestDrive:/$name" -ErrorAction Stop)

		$saved | Should -HaveCount 1
		$saved[0].FullName | Should -Be (Join-Path $TestDrive $name)
		$text = [System.IO.File]::ReadAllText($saved[0].FullName)
		$text | Should -Not -BeNullOrEmpty
		$served = (Invoke-SandboxApi "log/file/$($newest.filename)" -ExpectStatus 200).Content
		$served.StartsWith($text, [System.StringComparison]::Ordinal) | Should -BeTrue -Because 'Sonarr only adds to a log file'
	}

	It 'stops with an error, and writes no file, for a path whose extension isn''t .txt or .log' {
		$name = "$(New-TestName).zip"
		Save-SonarrLogFile -LogUri $newest.downloadUrl -Path "TestDrive:/$name" -ErrorAction SilentlyContinue -ErrorVariable saveErrors |
			Should -BeNullOrEmpty

		$saveErrors.Count | Should -Be 1
		$saveErrors[0].Exception | Should -BeOfType ([System.ArgumentException])
		$saveErrors[0].CategoryInfo.Category | Should -Be ([System.Management.Automation.ErrorCategory]::InvalidArgument)
		$saveErrors[0].TargetObject | Should -Be (Join-Path $TestDrive $name)
		Test-Path -LiteralPath (Join-Path $TestDrive $name) | Should -BeFalse
	}

	It 'writes an error, and leaves the file as it was, for a file that exists' {
		$path = Join-Path $TestDrive "$(New-TestName).txt"
		Set-Content -LiteralPath $path -Value 'pester' -NoNewline
		Save-SonarrLogFile -LogUri $newest.downloadUrl -Path $path -ErrorAction SilentlyContinue -ErrorVariable saveErrors |
			Should -BeNullOrEmpty

		$saveErrors.Count | Should -Be 1
		$saveErrors[0].Exception | Should -BeOfType ([System.IO.IOException])
		$saveErrors[0].CategoryInfo.Category | Should -Be ([System.Management.Automation.ErrorCategory]::WriteError)
		$saveErrors[0].TargetObject | Should -Be $path
		Get-Content -LiteralPath $path -Raw | Should -BeExactly 'pester'
	}

	It 'replaces a file that exists under -Force' {
		$path = Join-Path $TestDrive "$(New-TestName).txt"
		Set-Content -LiteralPath $path -Value 'pester' -NoNewline
		$saved = @(Save-SonarrLogFile -LogUri $newest.downloadUrl -Path $path -Force -ErrorAction Stop)

		$saved | Should -HaveCount 1
		$saved[0].FullName | Should -Be $path
		$text = [System.IO.File]::ReadAllText($path)
		$text | Should -Not -BeExactly 'pester'
		$served = (Invoke-SandboxApi "log/file/$($newest.filename)" -ExpectStatus 200).Content
		$served.StartsWith($text, [System.StringComparison]::Ordinal) | Should -BeTrue -Because 'Sonarr only adds to a log file'
	}

	It 'writes the error for a log file that Sonarr doesn''t have, and writes no file' {
		$folder = New-TestName
		$null = New-Item -ItemType Directory -Path (Join-Path $TestDrive $folder)
		$uri = "/logfile/$(New-TestName).txt"
		Save-SonarrLogFile -LogUri $uri -Path "TestDrive:/$folder" -ErrorAction SilentlyContinue -ErrorVariable saveErrors |
			Should -BeNullOrEmpty

		$saveErrors.Count | Should -Be 1
		$saveErrors[0].Exception | Should -BeOfType ([MG.Sonarr.Next.Exceptions.SonarrHttpException])
		$saveErrors[0].Exception.StatusCode | Should -Be ([System.Net.HttpStatusCode]::NotFound)
		$saveErrors[0].CategoryInfo.Category | Should -Be ([System.Management.Automation.ErrorCategory]::ObjectNotFound)
		$saveErrors[0].TargetObject | Should -BeExactly $uri
		Get-ChildItem -LiteralPath (Join-Path $TestDrive $folder) | Should -BeNullOrEmpty
	}
}
