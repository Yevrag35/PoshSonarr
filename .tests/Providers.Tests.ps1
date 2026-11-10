#Requires -Version 7.6
#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '6.2.0'; MaximumVersion = '6.99.99' }

# Download clients, indexers, and notifications, which Sonarr calls providers, and the templates that new download
# clients and notifications are created from. The baseline has no providers, so each test of a provider works on one of
# its own, which it creates through the API unless it tests New-SonarrDownloadClient, so a broken cmdlet fails only its
# own tests. Every provider is created disabled: Sonarr runs a provider's connection test, and validates its settings,
# only when it saves an enabled one, so a disabled provider needs no service behind it. A download client comes from the
# Transmission template, which is disabled, as every download client template is. An indexer comes from the Newznab
# template with enableRss, enableAutomaticSearch, and enableInteractiveSearch off, since Sonarr counts an indexer with
# any of them on as enabled, and a notification comes from the Webhook template with every trigger off, since Sonarr
# counts a notification with any of them on as enabled.
#
# Sonarr requires each provider's name to be one that no other provider of its kind has, ignoring case, so each one is
# named by New-TestName. Afterward, each test's providers are deleted through the API, if any are left, by that name.

BeforeAll {
	. "$PSScriptRoot/setup.ps1"

	# The implementation of the template that each kind of provider is created from, by the path of the endpoint that
	# creates it.
	$templateNames = @{
		downloadclient = 'Transmission'
		indexer        = 'Newznab'
		notification   = 'Webhook'
	}

	# Creates a disabled provider through the API, from the template that GET <path>/schema returns for its kind, with
	# the name that -Name gives, or one from New-TestName, and returns it as the API returned it.
	function New-TestProvider {
		param (
			[Parameter(Mandatory = $true)]
			[ValidateSet('downloadclient', 'indexer', 'notification')]
			[string] $Path,

			[Parameter(Mandatory = $false)]
			[string] $Name = (New-TestName)
		)

		$body = (Invoke-SandboxApi "$Path/schema" -ExpectStatus 200).Body | Where-Object implementation -CEQ $templateNames[$Path]
		$body.name = $Name
		switch ($Path) {
			'downloadclient' {
				$body.enable = $false
			}
			'indexer' {
				$body.enableRss = $false
				$body.enableAutomaticSearch = $false
				$body.enableInteractiveSearch = $false
			}
			'notification' {
				foreach ($field in $body.psobject.Properties) {
					if ($field.Name -cmatch '^on[A-Z]') {
						$field.Value = $false
					}
				}
			}
		}
		(Invoke-SandboxApi $Path -Method Post -Body $body -ExpectStatus 201).Body
	}

	# Deletes through the API each provider of a kind whose name starts with a test's name, if any are left, so a
	# provider that a test renamed goes too.
	function Remove-TestProvider {
		param (
			[Parameter(Mandatory = $true)]
			[ValidateSet('downloadclient', 'indexer', 'notification')]
			[string] $Path,

			[Parameter(Mandatory = $true)]
			[string] $Name
		)

		foreach ($item in (Invoke-SandboxApi $Path -ExpectStatus 200).Body) {
			if ($item.name.StartsWith($Name, [System.StringComparison]::Ordinal)) {
				$null = Invoke-SandboxApi "$Path/$($item.id)" -Method Delete -ExpectStatus 200
			}
		}
	}

	# Returns whether any trigger of a notification, as the API returned it, is on. A trigger is a field whose name is on
	# followed by an uppercase letter, such as onGrab.
	function Test-TriggerOn {
		param (
			[Parameter(Mandatory = $true)]
			[object] $Notification
		)

		@($Notification.psobject.Properties | Where-Object { $_.Name -cmatch '^on[A-Z]' -and $_.Value -eq $true }).Count -gt 0
	}

	# Returns the names of the fields whose values differ between two copies of a provider, as the API returned them, so
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

Describe 'Get-SonarrDownloadClientSchema' {
	BeforeAll {
		$apiTemplates = @((Invoke-SandboxApi downloadclient/schema -ExpectStatus 200).Body)
	}

	It 'returns every template that GET downloadclient/schema returns, as DownloadClientSchemaObjects with the #download_client_schema tag' {
		$templates = @(Get-SonarrDownloadClientSchema -ErrorAction Stop)

		# A template has no ID, and each one is for a different implementation.
		$templates.Implementation | Sort-Object | Should -BeExactly ($apiTemplates.implementation | Sort-Object)
		foreach ($item in $templates) {
			$item | Should -BeOfType ([MG.Sonarr.Next.Models.DownloadClients.DownloadClientSchemaObject])
			$item.psobject.TypeNames[0] | Should -BeExactly 'MG.Sonarr.Next.Models.DownloadClients.DownloadClientSchemaObject'
			$item.MetadataTag.Value | Should -BeExactly '#download_client_schema'
			$expected = $apiTemplates | Where-Object implementation -CEQ $item.Implementation
			$item.ImplementationName | Should -BeExactly $expected.implementationName
			$item.ConfigContract | Should -BeExactly $expected.configContract
			$item.Protocol | Should -BeExactly $expected.protocol
			$item.Enable | Should -Be $expected.enable
			@($item.Fields.Name) | Should -BeExactly @($expected.fields.name)
		}
	}

	It 'returns the templates whose implementation name matches ''<Name>'', given by position or in -Name' -ForEach @(
		@{ Name = 'transmission' }
		@{ Name = '*BLACKHOLE' }
		@{ Name = 'Download Station' }
	) {
		# The name matches a template's implementation name, such as Torrent Blackhole, with wildcards, ignoring case, and
		# Sonarr gives two implementations the name Download Station.
		$expected = @($apiTemplates | Where-Object implementationName -Like $Name | ForEach-Object implementation | Sort-Object)
		$expected | Should -Not -BeNullOrEmpty -Because "Sonarr has a template whose implementation name matches '$Name'"

		@(Get-SonarrDownloadClientSchema $Name -ErrorAction Stop).Implementation | Sort-Object | Should -BeExactly $expected
		@(Get-SonarrDownloadClientSchema -Name $Name -ErrorAction Stop).Implementation | Sort-Object | Should -BeExactly $expected
	}
}

Describe 'Get-SonarrNotificationSchema' {
	It 'returns every template that GET notification/schema returns, as NotificationSchemaObjects with the #notification tag' {
		$apiTemplates = @((Invoke-SandboxApi notification/schema -ExpectStatus 200).Body)
		$templates = @(Get-SonarrNotificationSchema -ErrorAction Stop)

		# A template has no ID, and each one is for a different implementation.
		$templates.Implementation | Sort-Object | Should -BeExactly ($apiTemplates.implementation | Sort-Object)
		foreach ($item in $templates) {
			$item | Should -BeOfType ([MG.Sonarr.Next.Models.Notifications.NotificationSchemaObject])
			$item.psobject.TypeNames[0] | Should -BeExactly 'MG.Sonarr.Next.Models.Notifications.NotificationSchemaObject'
			# A template takes the tag of the notifications that are created from it.
			$item.MetadataTag.Value | Should -BeExactly '#notification'
			$expected = $apiTemplates | Where-Object implementation -CEQ $item.Implementation
			$item.ImplementationName | Should -BeExactly $expected.implementationName
			$item.ConfigContract | Should -BeExactly $expected.configContract
			@($item.Fields.Name) | Should -BeExactly @($expected.fields.name)
		}
	}
}

Describe 'Get-SonarrDownloadClient' {
	It 'returns nothing, and writes no error, when Sonarr has no download clients' {
		@((Invoke-SandboxApi downloadclient -ExpectStatus 200).Body) | Should -BeNullOrEmpty -Because 'the baseline has no download clients'

		Get-SonarrDownloadClient -ErrorAction Stop | Should -BeNullOrEmpty
	}

	Context 'a client that Sonarr has' {
		BeforeAll {
			$client = New-TestProvider downloadclient
		}
		AfterAll {
			Remove-TestProvider downloadclient $client.name
		}

		It 'returns every download client that GET downloadclient returns, as DownloadClientObjects with the #download_client tag' {
			$apiClients = @((Invoke-SandboxApi downloadclient -ExpectStatus 200).Body)
			$apiClients.id | Should -Contain $client.id -Because 'the test created a download client'

			$clients = @(Get-SonarrDownloadClient -ErrorAction Stop)
			$clients.Id | Sort-Object | Should -Be ($apiClients.id | Sort-Object)
			foreach ($item in $clients) {
				$item | Should -BeOfType ([MG.Sonarr.Next.Models.DownloadClients.DownloadClientObject])
				$item.psobject.TypeNames[0] | Should -BeExactly 'MG.Sonarr.Next.Models.DownloadClients.DownloadClientObject'
				$item.MetadataTag.Value | Should -BeExactly '#download_client'
				$expected = $apiClients | Where-Object id -EQ $item.Id
				$item.Name | Should -BeExactly $expected.name
				$item.Implementation | Should -BeExactly $expected.implementation
				$item.Enable | Should -Be $expected.enable
				$item.Priority | Should -Be $expected.priority
			}
		}

		It 'finds the client by its name, given by position or in -Name, ignoring case' {
			foreach ($name in $client.name, $client.name.ToUpperInvariant()) {
				@(Get-SonarrDownloadClient $name -ErrorAction Stop).Id | Should -Be $client.id
				@(Get-SonarrDownloadClient -Name $name -ErrorAction Stop).Id | Should -Be $client.id
			}
		}

		It 'finds the client by its ID, in -Id or given by position' {
			@(Get-SonarrDownloadClient -Id $client.id -ErrorAction Stop).Name | Should -BeExactly $client.name
			@(Get-SonarrDownloadClient $client.id -ErrorAction Stop).Name | Should -BeExactly $client.name
		}
	}
}

Describe 'New-SonarrDownloadClient' {
	BeforeAll {
		$name = New-TestName
		$template = Get-SonarrDownloadClientSchema -Name $templateNames['downloadclient'] -ErrorAction Stop
	}
	AfterAll {
		Remove-TestProvider downloadclient $name
	}

	It 'creates a disabled client from a piped template, with the name and priority that it''s given, and returns it as a DownloadClientObject with the #download_client tag' {
		$template.Enable | Should -BeFalse -Because 'every download client template is disabled'
		$templatePriority = $template.Priority
		$priority = $templatePriority + 1
		$clients = @($template | New-SonarrDownloadClient -Name $name -Priority $priority -ErrorAction Stop)

		$clients | Should -HaveCount 1
		$clients[0] | Should -BeOfType ([MG.Sonarr.Next.Models.DownloadClients.DownloadClientObject])
		$clients[0].psobject.TypeNames[0] | Should -BeExactly 'MG.Sonarr.Next.Models.DownloadClients.DownloadClientObject'
		$clients[0].MetadataTag.Value | Should -BeExactly '#download_client'
		$clients[0].Name | Should -BeExactly $name

		# Without -Enabled, the client takes the template's enabled state.
		$stored = (Invoke-SandboxApi "downloadclient/$($clients[0].Id)" -ExpectStatus 200).Body
		$stored.name | Should -BeExactly $name
		$stored.implementation | Should -BeExactly $templateNames['downloadclient']
		$stored.enable | Should -BeFalse
		$stored.priority | Should -Be $priority
		# The cmdlet sends a copy of the template, so the template keeps its own values.
		$template.Name | Should -BeNullOrEmpty
		$template.Priority | Should -Be $templatePriority
	}

	It 'writes an error of its own for a template without a name, and creates no client' {
		$before = @((Invoke-SandboxApi downloadclient -ExpectStatus 200).Body).Count
		$template | New-SonarrDownloadClient -ErrorAction SilentlyContinue -ErrorVariable newErrors | Should -BeNullOrEmpty

		# The cmdlet checks the name itself before it sends anything, so the error isn't one of Sonarr's.
		$newErrors.Count | Should -Be 1
		$newErrors[0].Exception | Should -BeOfType ([MG.Sonarr.Next.Exceptions.MalformedSonarrObjectException])
		$newErrors[0].CategoryInfo.Category | Should -Be ([System.Management.Automation.ErrorCategory]::InvalidData)
		$newErrors[0].FullyQualifiedErrorId | Should -BeExactly 'New-SonarrDownloadClient.InvalidDownloadClientName,MG.Sonarr.Next.Shell.Cmdlets.DownloadClients.NewSonarrDownloadClientCmdlet'
		@((Invoke-SandboxApi downloadclient -ExpectStatus 200).Body).Count | Should -Be $before
	}

	It 'writes Sonarr''s validation error for a name that another client has, and creates no second client' {
		$existing = New-TestProvider downloadclient -Name "$name-taken"
		$template | New-SonarrDownloadClient -Name $existing.name -ErrorAction SilentlyContinue -ErrorVariable newErrors | Should -BeNullOrEmpty

		$newErrors.Count | Should -Be 1
		$newErrors[0].Exception | Should -BeOfType ([MG.Sonarr.Next.Exceptions.SonarrHttpException])
		$newErrors[0].Exception.StatusCode | Should -Be ([System.Net.HttpStatusCode]::BadRequest)
		$newErrors[0].CategoryInfo.Category | Should -Be ([System.Management.Automation.ErrorCategory]::InvalidData)
		$newErrors[0].TargetObject | Should -BeExactly '/downloadclient'
		@((Invoke-SandboxApi downloadclient -ExpectStatus 200).Body | Where-Object name -EQ $existing.name).id | Should -Be $existing.id
	}
}

Describe 'Update-SonarrDownloadClient' {
	BeforeEach {
		$client = New-TestProvider downloadclient
	}
	AfterEach {
		Remove-TestProvider downloadclient $client.name
	}

	It 'saves the changed priority of a piped client, and changes nothing else' {
		$item = Get-SonarrDownloadClient -Id $client.id -ErrorAction Stop
		$item.Priority = $client.priority + 1
		$item | Update-SonarrDownloadClient -ErrorAction Stop | Should -BeNullOrEmpty

		$stored = (Invoke-SandboxApi "downloadclient/$($client.id)" -ExpectStatus 200).Body
		$stored.priority | Should -Be ($client.priority + 1)
		Get-ChangedField $client $stored | Should -Be 'priority'
	}

	It 'writes Sonarr''s validation error for a client without a name, and changes nothing' {
		$item = Get-SonarrDownloadClient -Id $client.id -ErrorAction Stop
		$item.Name = ''
		$item | Update-SonarrDownloadClient -ErrorAction SilentlyContinue -ErrorVariable updateErrors

		$updateErrors.Count | Should -Be 1
		$updateErrors[0].Exception | Should -BeOfType ([MG.Sonarr.Next.Exceptions.SonarrHttpException])
		$updateErrors[0].Exception.StatusCode | Should -Be ([System.Net.HttpStatusCode]::BadRequest)
		$updateErrors[0].CategoryInfo.Category | Should -Be ([System.Management.Automation.ErrorCategory]::InvalidData)
		$updateErrors[0].TargetObject | Should -BeExactly "/downloadclient/$($client.id)"
		$stored = (Invoke-SandboxApi "downloadclient/$($client.id)" -ExpectStatus 200).Body
		Get-ChangedField $client $stored | Should -BeNullOrEmpty
	}
}

Describe 'Remove-SonarrDownloadClient' {
	BeforeEach {
		$client = New-TestProvider downloadclient
	}
	AfterEach {
		Remove-TestProvider downloadclient $client.name
	}

	# Sonarr answers the delete of a provider that it doesn't have with a 200 too, so the tests check through GET that
	# the provider is gone.
	It 'deletes the client that -Id names, <Case>' -ForEach @(
		@{ Case = 'under -Confirm:$false'; Force = $false }
		@{ Case = 'with -Force, without asking'; Force = $true }
	) {
		$removeArgs = if ($Force) { @{ Force = $true } } else { @{ Confirm = $false } }
		Remove-SonarrDownloadClient -Id $client.id @removeArgs -ErrorAction Stop | Should -BeNullOrEmpty

		(Invoke-SandboxApi "downloadclient/$($client.id)").StatusCode | Should -Be 404
	}

	It 'deletes a piped client' {
		Get-SonarrDownloadClient -Id $client.id -ErrorAction Stop | Remove-SonarrDownloadClient -Force -ErrorAction Stop |
			Should -BeNullOrEmpty

		(Invoke-SandboxApi "downloadclient/$($client.id)").StatusCode | Should -Be 404
	}
}

Describe 'Get-SonarrIndexer' {
	It 'returns nothing, and writes no error, when Sonarr has no indexers' {
		@((Invoke-SandboxApi indexer -ExpectStatus 200).Body) | Should -BeNullOrEmpty -Because 'the baseline has no indexers'

		Get-SonarrIndexer -ErrorAction Stop | Should -BeNullOrEmpty
	}

	Context 'an indexer that Sonarr has' {
		BeforeAll {
			$indexer = New-TestProvider indexer
		}
		AfterAll {
			Remove-TestProvider indexer $indexer.name
		}

		It 'returns every indexer that GET indexer returns, as IndexerObjects with the #indexer tag' {
			$apiIndexers = @((Invoke-SandboxApi indexer -ExpectStatus 200).Body)
			$apiIndexers.id | Should -Contain $indexer.id -Because 'the test created an indexer'

			$indexers = @(Get-SonarrIndexer -ErrorAction Stop)
			$indexers.Id | Sort-Object | Should -Be ($apiIndexers.id | Sort-Object)
			foreach ($item in $indexers) {
				$item | Should -BeOfType ([MG.Sonarr.Next.Models.Indexers.IndexerObject])
				$item.psobject.TypeNames[0] | Should -BeExactly 'MG.Sonarr.Next.Models.Indexers.IndexerObject'
				$item.MetadataTag.Value | Should -BeExactly '#indexer'
				$expected = $apiIndexers | Where-Object id -EQ $item.Id
				$item.Name | Should -BeExactly $expected.name
				$item.Protocol | Should -BeExactly $expected.protocol
				$item.Priority | Should -Be $expected.priority
				$item.EnableRss | Should -Be $expected.enableRss
				$item.EnableAutomaticSearch | Should -Be $expected.enableAutomaticSearch
				$item.EnableInteractiveSearch | Should -Be $expected.enableInteractiveSearch
			}
		}

		It 'finds the indexer by its name, given by position or in -Name, ignoring case' {
			foreach ($name in $indexer.name, $indexer.name.ToUpperInvariant()) {
				@(Get-SonarrIndexer $name -ErrorAction Stop).Id | Should -Be $indexer.id
				@(Get-SonarrIndexer -Name $name -ErrorAction Stop).Id | Should -Be $indexer.id
			}
		}

		It 'finds the indexer by its ID, in -Id or given by position' {
			@(Get-SonarrIndexer -Id $indexer.id -ErrorAction Stop).Name | Should -BeExactly $indexer.name
			@(Get-SonarrIndexer $indexer.id -ErrorAction Stop).Name | Should -BeExactly $indexer.name
		}
	}
}

Describe 'Update-SonarrIndexer' {
	BeforeEach {
		$indexer = New-TestProvider indexer
	}
	AfterEach {
		Remove-TestProvider indexer $indexer.name
	}

	It 'saves the changed name of a piped indexer, and changes nothing else' {
		$newName = "$($indexer.name)-renamed"
		$item = Get-SonarrIndexer -Id $indexer.id -ErrorAction Stop
		$item.Name = $newName
		$item | Update-SonarrIndexer -ErrorAction Stop | Should -BeNullOrEmpty

		$stored = (Invoke-SandboxApi "indexer/$($indexer.id)" -ExpectStatus 200).Body
		$stored.name | Should -BeExactly $newName
		Get-ChangedField $indexer $stored | Should -Be 'name'
	}

	It 'sends the name that the indexer was read with in place of an empty one, and 25 in place of a priority outside 0 to 100' {
		# The Newznab template's priority is 25, so the indexer first gets another one through the API, and the change to
		# 25 shows. Sonarr saves any priority that it's sent.
		$indexer.priority = 30
		$null = Invoke-SandboxApi "indexer/$($indexer.id)" -Method Put -Body $indexer -ExpectStatus 202
		$before = (Invoke-SandboxApi "indexer/$($indexer.id)" -ExpectStatus 200).Body
		$before.priority | Should -Be 30

		$item = Get-SonarrIndexer -Id $indexer.id -ErrorAction Stop
		$item.Name = ''
		$item.Priority = 101
		$item | Update-SonarrIndexer -ErrorAction Stop | Should -BeNullOrEmpty

		$stored = (Invoke-SandboxApi "indexer/$($indexer.id)" -ExpectStatus 200).Body
		$stored.name | Should -BeExactly $before.name
		$stored.priority | Should -Be 25
		Get-ChangedField $before $stored | Should -Be 'priority'
	}

	It 'writes Sonarr''s validation error for a name that another indexer has, and changes nothing' {
		$other = New-TestProvider indexer -Name "$($indexer.name)-other"
		$item = Get-SonarrIndexer -Id $indexer.id -ErrorAction Stop
		$item.Name = $other.name
		$item | Update-SonarrIndexer -ErrorAction SilentlyContinue -ErrorVariable updateErrors

		$updateErrors.Count | Should -Be 1
		$updateErrors[0].Exception | Should -BeOfType ([MG.Sonarr.Next.Exceptions.SonarrHttpException])
		$updateErrors[0].Exception.StatusCode | Should -Be ([System.Net.HttpStatusCode]::BadRequest)
		$updateErrors[0].CategoryInfo.Category | Should -Be ([System.Management.Automation.ErrorCategory]::InvalidData)
		$updateErrors[0].TargetObject | Should -BeExactly "/indexer/$($indexer.id)"
		$stored = (Invoke-SandboxApi "indexer/$($indexer.id)" -ExpectStatus 200).Body
		Get-ChangedField $indexer $stored | Should -BeNullOrEmpty
	}
}

Describe 'Remove-SonarrIndexer' {
	BeforeEach {
		$indexer = New-TestProvider indexer
	}
	AfterEach {
		Remove-TestProvider indexer $indexer.name
	}

	It 'deletes the indexer that -Id names, <Case>' -ForEach @(
		@{ Case = 'under -Confirm:$false'; Force = $false }
		@{ Case = 'with -Force, without asking'; Force = $true }
	) {
		$removeArgs = if ($Force) { @{ Force = $true } } else { @{ Confirm = $false } }
		Remove-SonarrIndexer -Id $indexer.id @removeArgs -ErrorAction Stop | Should -BeNullOrEmpty

		(Invoke-SandboxApi "indexer/$($indexer.id)").StatusCode | Should -Be 404
	}

	It 'deletes a piped indexer' {
		Get-SonarrIndexer -Id $indexer.id -ErrorAction Stop | Remove-SonarrIndexer -Force -ErrorAction Stop | Should -BeNullOrEmpty

		(Invoke-SandboxApi "indexer/$($indexer.id)").StatusCode | Should -Be 404
	}
}

Describe 'Get-SonarrNotification' {
	It 'returns nothing, and writes no error, when Sonarr has no notifications' {
		@((Invoke-SandboxApi notification -ExpectStatus 200).Body) | Should -BeNullOrEmpty -Because 'the baseline has no notifications'

		Get-SonarrNotification -ErrorAction Stop | Should -BeNullOrEmpty
	}

	Context 'a notification that Sonarr has' {
		BeforeAll {
			$notification = New-TestProvider notification
		}
		AfterAll {
			Remove-TestProvider notification $notification.name
		}

		It 'returns every notification that GET notification returns, as NotificationObjects with the #notification tag and IsEnabled' {
			$apiNotifications = @((Invoke-SandboxApi notification -ExpectStatus 200).Body)
			$apiNotifications.id | Should -Contain $notification.id -Because 'the test created a notification'

			$notifications = @(Get-SonarrNotification -ErrorAction Stop)
			$notifications.Id | Sort-Object | Should -Be ($apiNotifications.id | Sort-Object)
			foreach ($item in $notifications) {
				$item | Should -BeOfType ([MG.Sonarr.Next.Models.Notifications.NotificationObject])
				$item.psobject.TypeNames[0] | Should -BeExactly 'MG.Sonarr.Next.Models.Notifications.NotificationObject'
				$item.MetadataTag.Value | Should -BeExactly '#notification'
				$expected = $apiNotifications | Where-Object id -EQ $item.Id
				$item.Name | Should -BeExactly $expected.name
				$item.Implementation | Should -BeExactly $expected.implementation
				# The model adds IsEnabled, which is true when any of the notification's triggers is on.
				$item.IsEnabled | Should -Be (Test-TriggerOn $expected)
			}
			($notifications | Where-Object Id -EQ $notification.id).IsEnabled | Should -BeFalse
		}

		It 'finds the notification by its name, given by position or in -Name, ignoring case' {
			foreach ($name in $notification.name, $notification.name.ToUpperInvariant()) {
				@(Get-SonarrNotification $name -ErrorAction Stop).Id | Should -Be $notification.id
				@(Get-SonarrNotification -Name $name -ErrorAction Stop).Id | Should -Be $notification.id
			}
		}

		It 'finds the notification by its ID in -Id' {
			@(Get-SonarrNotification -Id $notification.id -ErrorAction Stop).Name | Should -BeExactly $notification.name
		}

		It 'writes a notification that both -Id and -Name find once' {
			# The cmdlet writes the notifications with the IDs first, and then those whose name matches, leaving out the
			# ones that it already wrote.
			$found = @(Get-SonarrNotification -Id $notification.id -Name $notification.name -ErrorAction Stop)
			$found | Should -HaveCount 1
			$found[0].Id | Should -Be $notification.id
		}
	}
}

Describe 'Update-SonarrNotification' {
	BeforeEach {
		$notification = New-TestProvider notification
		$newName = "$($notification.name)-renamed"
	}
	AfterEach {
		Remove-TestProvider notification $notification.name
	}

	It 'saves the changed name of a piped notification with -Force, and changes nothing else' {
		$item = Get-SonarrNotification -Id $notification.id -ErrorAction Stop
		$item.Name = $newName
		$item | Update-SonarrNotification -Force -ErrorAction Stop | Should -BeNullOrEmpty

		$stored = (Invoke-SandboxApi "notification/$($notification.id)" -ExpectStatus 200).Body
		$stored.name | Should -BeExactly $newName
		Get-ChangedField $notification $stored | Should -Be 'name'
	}

	It 'asks a question of its own under -Confirm:$false, and changes nothing when the question fails' {
		# -Confirm:$false skips the prompt that ShouldProcess shows, but not the cmdlet's own question, which only -Force
		# skips. The runner runs Pester under -NonInteractive, where a prompt throws instead of waiting for an answer.
		$item = Get-SonarrNotification -Id $notification.id -ErrorAction Stop
		$item.Name = $newName
		$item | Update-SonarrNotification -Confirm:$false -ErrorAction SilentlyContinue -ErrorVariable updateErrors

		$updateErrors.Count | Should -Be 1
		$updateErrors[0].Exception | Should -BeOfType ([System.Management.Automation.PSInvalidOperationException])
		$stored = (Invoke-SandboxApi "notification/$($notification.id)" -ExpectStatus 200).Body
		Get-ChangedField $notification $stored | Should -BeNullOrEmpty
	}

	It 'writes Sonarr''s validation error for a notification without a name, and changes nothing' {
		$item = Get-SonarrNotification -Id $notification.id -ErrorAction Stop
		$item.Name = ''
		$item | Update-SonarrNotification -Force -ErrorAction SilentlyContinue -ErrorVariable updateErrors

		$updateErrors.Count | Should -Be 1
		$updateErrors[0].Exception | Should -BeOfType ([MG.Sonarr.Next.Exceptions.SonarrHttpException])
		$updateErrors[0].Exception.StatusCode | Should -Be ([System.Net.HttpStatusCode]::BadRequest)
		$updateErrors[0].CategoryInfo.Category | Should -Be ([System.Management.Automation.ErrorCategory]::InvalidData)
		$updateErrors[0].TargetObject | Should -BeExactly "/notification/$($notification.id)"
		$stored = (Invoke-SandboxApi "notification/$($notification.id)" -ExpectStatus 200).Body
		Get-ChangedField $notification $stored | Should -BeNullOrEmpty
	}
}

Describe 'Remove-SonarrNotification' {
	BeforeEach {
		$notification = New-TestProvider notification
	}
	AfterEach {
		Remove-TestProvider notification $notification.name
	}

	It 'deletes the notification that -Id names, <Case>' -ForEach @(
		@{ Case = 'under -Confirm:$false'; Force = $false }
		@{ Case = 'with -Force, without asking'; Force = $true }
	) {
		$removeArgs = if ($Force) { @{ Force = $true } } else { @{ Confirm = $false } }
		Remove-SonarrNotification -Id $notification.id @removeArgs -ErrorAction Stop | Should -BeNullOrEmpty

		(Invoke-SandboxApi "notification/$($notification.id)").StatusCode | Should -Be 404
	}

	It 'deletes a piped notification' {
		Get-SonarrNotification -Id $notification.id -ErrorAction Stop | Remove-SonarrNotification -Force -ErrorAction Stop |
			Should -BeNullOrEmpty

		(Invoke-SandboxApi "notification/$($notification.id)").StatusCode | Should -Be 404
	}
}
