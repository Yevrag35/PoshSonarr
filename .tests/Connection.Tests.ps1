#Requires -Version 7.6
#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '6.2.0'; MaximumVersion = '6.99.99' }

BeforeDiscovery {
	# -PassThru reads the status from the same test, so a failed connection fails in the same way with it.
	$passThruOptions = @(
		@{ Option = ''; PassThru = $false }
		@{ Option = ' and -PassThru'; PassThru = $true }
	)
}

BeforeAll {
	. "$PSScriptRoot/setup.ps1"

	# A key in the form of the testbed's that isn't its key.
	$wrongKey = 'fedcba9876543210fedcba9876543210'

	# Connects the module to the testbed again, after a test that left it without a connection.
	function Connect-Testbed {
		Connect-SonarrInstance -Url $TestbedSettings.BaseUrl -ApiKey $TestbedSettings.ApiKey -ErrorAction Stop
	}
}

Describe 'Connect-SonarrInstance' {
	It 'connects with the testbed''s URL and key' {
		Connect-SonarrInstance -Url $TestbedSettings.BaseUrl -ApiKey $TestbedSettings.ApiKey -ErrorAction Stop | Should -BeNullOrEmpty
		(Get-SonarrStatus -ErrorAction Stop).InstanceName | Should -BeExactly $TestbedSettings.InstanceName
	}

	It 'returns the testbed''s status with -PassThru' {
		$status = @(Connect-SonarrInstance -Url $TestbedSettings.BaseUrl -ApiKey $TestbedSettings.ApiKey -PassThru -ErrorAction Stop)
		$status | Should -HaveCount 1
		$status[0].psobject.TypeNames[0] | Should -BeExactly 'MG.Sonarr.Next.Models.System.SystemStatusObject'
		$status[0].MetadataTag.Value | Should -BeExactly '#system_status'
		[string] $status[0].Version | Should -BeExactly $TestbedSettings.Version
		$status[0].InstanceName | Should -BeExactly $TestbedSettings.InstanceName
	}

	Context 'with a wrong key<Option>' -ForEach $passThruOptions {
		AfterEach {
			Connect-Testbed
		}

		It 'ends the statement with an error that carries the 401' {
			# -ErrorAction can't hide an error that ends the statement, so this throws only for one that does.
			$record = { Connect-SonarrInstance -Url $TestbedSettings.BaseUrl -ApiKey $wrongKey -PassThru:$PassThru -ErrorAction SilentlyContinue } |
				Should -Throw -ExceptionType ([Microsoft.PowerShell.Commands.HttpResponseException]) -PassThru
			$record.Exception.StatusCode | Should -Be ([System.Net.HttpStatusCode]::Unauthorized)
			$record.CategoryInfo.Category | Should -Be ([System.Management.Automation.ErrorCategory]::ResourceUnavailable)
			$record.TargetObject | Should -Be "$($TestbedSettings.BaseUrl)/api/v3/system/status"

			# The statement after it still runs, so the error ends no more than its own statement.
			Invoke-TestProcess "Connect-SonarrInstance -Url '$($TestbedSettings.BaseUrl)' -ApiKey '$wrongKey' -PassThru:`$$PassThru`n'the next statement ran'" |
				Should -Contain 'the next statement ran'
		}

		It 'leaves no connection' {
			{ Connect-SonarrInstance -Url $TestbedSettings.BaseUrl -ApiKey $wrongKey -PassThru:$PassThru -ErrorAction Stop } | Should -Throw
			{ Get-SonarrTag -ErrorAction Stop } | Should -Throw -ExceptionType ([MG.Sonarr.Next.Shell.Exceptions.ContextNotSetException])
		}
	}

	# The testbed serves its API under /api, so without that prefix, /v3/system/status gets Sonarr's web page, which the
	# module answers with a 503 of its own, for a URL that doesn't reach Sonarr's API.
	Context 'with -NoApiInPath<Option>' -ForEach $passThruOptions {
		AfterEach {
			Connect-Testbed
		}

		It 'ends the statement with an error that carries a 503 for a URL that doesn''t reach Sonarr''s API' {
			$record = { Connect-SonarrInstance -Url $TestbedSettings.BaseUrl -ApiKey $TestbedSettings.ApiKey -NoApiInPath -PassThru:$PassThru -ErrorAction SilentlyContinue } |
				Should -Throw -ExceptionType ([Microsoft.PowerShell.Commands.HttpResponseException]) -PassThru
			$record.Exception.StatusCode | Should -Be ([System.Net.HttpStatusCode]::ServiceUnavailable)
			$record.CategoryInfo.Category | Should -Be ([System.Management.Automation.ErrorCategory]::ResourceUnavailable)
			$record.TargetObject | Should -Be "$($TestbedSettings.BaseUrl)/v3/system/status"

			Invoke-TestProcess "Connect-SonarrInstance -Url '$($TestbedSettings.BaseUrl)' -ApiKey '$($TestbedSettings.ApiKey)' -NoApiInPath -PassThru:`$$PassThru`n'the next statement ran'" |
				Should -Contain 'the next statement ran'
		}

		It 'leaves no connection' {
			{ Connect-SonarrInstance -Url $TestbedSettings.BaseUrl -ApiKey $TestbedSettings.ApiKey -NoApiInPath -PassThru:$PassThru -ErrorAction Stop } | Should -Throw
			{ Get-SonarrTag -ErrorAction Stop } | Should -Throw -ExceptionType ([MG.Sonarr.Next.Shell.Exceptions.ContextNotSetException])
		}
	}
}

Describe 'Disconnect-SonarrInstance' {
	AfterEach {
		Connect-Testbed
	}

	It 'leaves the API cmdlets failing with ContextNotSetException' {
		Disconnect-SonarrInstance -ErrorAction Stop
		{ Get-SonarrTag -ErrorAction Stop } | Should -Throw -ExceptionType ([MG.Sonarr.Next.Shell.Exceptions.ContextNotSetException])
	}

	It 'does nothing without a connection' {
		Disconnect-SonarrInstance -ErrorAction Stop
		{ Disconnect-SonarrInstance -ErrorAction Stop } | Should -Not -Throw
	}
}

Describe 'Test-SonarrHost' {
	It 'returns the testbed''s status, or $true with -Quiet' {
		$status = @(Test-SonarrHost -Url $TestbedSettings.BaseUrl -ApiKey $TestbedSettings.ApiKey -ErrorAction Stop)
		$status | Should -HaveCount 1
		$status[0].psobject.TypeNames[0] | Should -BeExactly 'MG.Sonarr.Next.Models.System.SystemStatusObject'
		[string] $status[0].Version | Should -BeExactly $TestbedSettings.Version
		$status[0].InstanceName | Should -BeExactly $TestbedSettings.InstanceName

		Test-SonarrHost -Url $TestbedSettings.BaseUrl -ApiKey $TestbedSettings.ApiKey -Quiet -ErrorAction Stop | Should -BeTrue
	}

	It 'fails with a wrong key' {
		# Without -Quiet, the 401 is an error that -ErrorAction can hide, as Connect-SonarrInstance's can't be.
		Test-SonarrHost -Url $TestbedSettings.BaseUrl -ApiKey $wrongKey -ErrorVariable testErrors -ErrorAction SilentlyContinue |
			Should -BeNullOrEmpty
		$testErrors | Should -HaveCount 1
		$testErrors[0].Exception | Should -BeOfType ([Microsoft.PowerShell.Commands.HttpResponseException])
		$testErrors[0].Exception.StatusCode | Should -Be ([System.Net.HttpStatusCode]::Unauthorized)
		$testErrors[0].CategoryInfo.Category | Should -Be ([System.Management.Automation.ErrorCategory]::ResourceUnavailable)
		$testErrors[0].TargetObject | Should -Be "$($TestbedSettings.BaseUrl)/api/v3/system/status"

		Test-SonarrHost -Url $TestbedSettings.BaseUrl -ApiKey $wrongKey -Quiet -ErrorVariable quietErrors | Should -BeFalse
		$quietErrors | Should -BeNullOrEmpty
	}

	It 'fails with -NoApiInPath, for a URL that doesn''t reach Sonarr''s API' {
		Test-SonarrHost -Url $TestbedSettings.BaseUrl -ApiKey $TestbedSettings.ApiKey -NoApiInPath -ErrorVariable testErrors -ErrorAction SilentlyContinue |
			Should -BeNullOrEmpty
		$testErrors | Should -HaveCount 1
		$testErrors[0].Exception | Should -BeOfType ([Microsoft.PowerShell.Commands.HttpResponseException])
		$testErrors[0].Exception.StatusCode | Should -Be ([System.Net.HttpStatusCode]::ServiceUnavailable)
		$testErrors[0].CategoryInfo.Category | Should -Be ([System.Management.Automation.ErrorCategory]::ResourceUnavailable)
		$testErrors[0].TargetObject | Should -Be "$($TestbedSettings.BaseUrl)/v3/system/status"

		Test-SonarrHost -Url $TestbedSettings.BaseUrl -ApiKey $TestbedSettings.ApiKey -NoApiInPath -Quiet | Should -BeFalse
	}

	It 'leaves the connection as it was' {
		Test-SonarrHost -Url $TestbedSettings.BaseUrl -ApiKey $wrongKey -Quiet | Should -BeFalse
		(Get-SonarrStatus -ErrorAction Stop).InstanceName | Should -BeExactly $TestbedSettings.InstanceName
	}

	It 'takes -SkipCertificateCheck and -Timeout' {
		# The testbed serves plain HTTP, so -SkipCertificateCheck has no certificate to skip, and the test passes with it.
		Test-SonarrHost -Url $TestbedSettings.BaseUrl -ApiKey $TestbedSettings.ApiKey -SkipCertificateCheck -Timeout '00:00:30' -Quiet -ErrorAction Stop |
			Should -BeTrue
	}

	It 'turns away a timeout of <Timeout>, which HttpClient doesn''t take' -ForEach @(
		@{ Timeout = [timespan]::Zero }
		@{ Timeout = [timespan]::FromDays(30) }
	) {
		$record = { Test-SonarrHost -Url $TestbedSettings.BaseUrl -ApiKey $TestbedSettings.ApiKey -Timeout $Timeout } |
			Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException]) -ErrorId 'ParameterArgumentValidationError,MG.Sonarr.Next.Shell.Cmdlets.Systems.TestSonarrHostCmdlet' -PassThru
		$record.Exception.ParameterName | Should -BeExactly 'Timeout'
	}

	Context 'without a connection' {
		BeforeEach {
			Disconnect-SonarrInstance -ErrorAction Stop
		}

		AfterEach {
			Connect-Testbed
		}

		It 'tests the host without connecting' {
			Test-SonarrHost -Url $TestbedSettings.BaseUrl -ApiKey $TestbedSettings.ApiKey -Quiet -ErrorAction Stop | Should -BeTrue
			{ Get-SonarrTag -ErrorAction Stop } | Should -Throw -ExceptionType ([MG.Sonarr.Next.Shell.Exceptions.ContextNotSetException])
		}
	}
}
