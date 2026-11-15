#Requires -Version 7.6
#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '6.2.0'; MaximumVersion = '6.99.99' }

# Sonarr's commands, which it queues and runs in the background, and the module's history of the commands that its
# cmdlets started on the connection, which setup.ps1 makes for each file. The tests start RSS syncs, which the sandbox
# finishes at once without grabbing anything, since it has no enabled indexers. Sonarr answers a command's POST once it
# has queued the command, so a test waits for the command to finish through GET command/{id}. The test of
# Get-SonarrCommand starts its sync through the API, so a broken Invoke-SonarrRssSync fails only its own test, while the
# history holds only the commands that the module's cmdlets started, so the history's tests start theirs with
# Invoke-SonarrRssSync.

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
}

Describe 'Invoke-SonarrRssSync' {
	It 'starts an RSS sync, which completes without grabbing anything, and returns the command as a CommandObject with the #command tag' {
		$commands = @(Invoke-SonarrRssSync -ErrorAction Stop)

		$commands | Should -HaveCount 1
		$commands[0] | Should -BeOfType ([MG.Sonarr.Next.Models.Commands.CommandObject])
		$commands[0].psobject.TypeNames[0] | Should -BeExactly 'MG.Sonarr.Next.Models.Commands.CommandObject'
		$commands[0].MetadataTag.Value | Should -BeExactly '#command'
		$commands[0].Name | Should -BeExactly 'RssSync'

		$finished = Wait-TestCommand $commands[0].Id
		$finished.name | Should -BeExactly 'RssSync'
		$finished.status | Should -BeExactly 'completed'
		# A grab would add a Grabbed record, event type 1, to the history.
		(Invoke-SandboxApi 'history?eventType=1' -ExpectStatus 200).Body.totalRecords | Should -Be 0
	}
}

Describe 'Get-SonarrCommand' {
	BeforeAll {
		$started = (Invoke-SandboxApi command -Method Post -Body @{ name = 'RssSync' } -ExpectStatus 201).Body
		$command = Wait-TestCommand $started.id
	}

	It 'returns the command that -Id names, in the state that GET command/{id} returns, as a CommandObject with the #command tag' {
		$commands = @(Get-SonarrCommand -Id $command.id -ErrorAction Stop)

		$commands | Should -HaveCount 1
		$commands[0] | Should -BeOfType ([MG.Sonarr.Next.Models.Commands.CommandObject])
		$commands[0].psobject.TypeNames[0] | Should -BeExactly 'MG.Sonarr.Next.Models.Commands.CommandObject'
		$commands[0].MetadataTag.Value | Should -BeExactly '#command'
		$commands[0].Id | Should -Be $command.id
		$commands[0].Name | Should -BeExactly $command.name
		# The model gives the status an uppercase first letter, such as Completed for completed.
		$commands[0].Status | Should -BeExactly ($command.status.Substring(0, 1).ToUpperInvariant() + $command.status.Substring(1))
	}

	It 'finds the command by its ID given by position' {
		@(Get-SonarrCommand $command.id -ErrorAction Stop).Id | Should -Be $command.id
	}

	It 'gets a piped command again' {
		$item = Get-SonarrCommand -Id $command.id -ErrorAction Stop
		$commands = @($item | Get-SonarrCommand -ErrorAction Stop)

		$commands | Should -HaveCount 1
		$commands[0].Id | Should -Be $command.id
	}

	It 'returns nothing, and writes no error, for an ID that Sonarr doesn''t have' {
		(Invoke-SandboxApi "command/$([int]::MaxValue)").StatusCode | Should -Be 404 -Because 'Sonarr has no command with that ID'

		Get-SonarrCommand -Id ([int]::MaxValue) -ErrorAction Stop | Should -BeNullOrEmpty
	}
}

Describe 'Get-SonarrCommandHistory' {
	BeforeAll {
		# The history keeps the command that Invoke-SonarrRssSync returned, as Sonarr answered the POST.
		$started = Invoke-SonarrRssSync -ErrorAction Stop
		$finished = Wait-TestCommand $started.Id
	}

	It 'lists the command that Invoke-SonarrRssSync started, under -All' {
		$commands = @(Get-SonarrCommandHistory -All -ErrorAction Stop)

		$commands.Id | Should -Contain $started.Id
		$item = $commands | Where-Object Id -EQ $started.Id
		$item | Should -BeOfType ([MG.Sonarr.Next.Models.Commands.CommandObject])
		$item.Name | Should -BeExactly 'RssSync'
	}

	It 'lists the unfinished command without a switch, and once Sonarr has finished it, writes it under -Refresh, after which only -All lists it' {
		# Sonarr answers the POST once it has queued the sync, so the history holds the sync as queued, unless it had
		# finished by then. -Refresh stores the state that Sonarr has for each unfinished command.
		if ($started.Status -notin 'Queued', 'Started') {
			Set-ItResult -Inconclusive -Because "the sync had finished by the time Sonarr answered its POST, so the history never held it unfinished"
		}
		$finished.status | Should -BeExactly 'completed' -Because 'the sync has finished'
		@(Get-SonarrCommandHistory -ErrorAction Stop).Id | Should -Contain $started.Id

		$refreshed = @(Get-SonarrCommandHistory -Refresh -ErrorAction Stop)
		$refreshed.Id | Should -Contain $started.Id
		($refreshed | Where-Object Id -EQ $started.Id).Status | Should -BeExactly 'Completed'
		@(Get-SonarrCommandHistory -ErrorAction Stop).Id | Should -Not -Contain $started.Id
		(Get-SonarrCommandHistory -All -ErrorAction Stop | Where-Object Id -EQ $started.Id).Status | Should -BeExactly 'Completed'
	}
}
