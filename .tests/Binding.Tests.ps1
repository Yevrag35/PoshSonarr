#Requires -Version 7.6
#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '6.2.0'; MaximumVersion = '6.99.99' }

# Each cmdlet here takes a model that another cmdlet returns, piped or by name, and checks the model's ID before it
# runs. The tests of cmdlets that change something run them with -WhatIf, so they check that the model binds and that
# the cmdlet acts on the model's ID, and they leave the sandbox as it was. The round trips in each area's file check
# what Sonarr stores.

BeforeAll {
	. "$PSScriptRoot/setup.ps1"

	# Runs a command, with -WhatIf, under a transcript, and returns the target of each What if line that it showed.
	# ShouldProcess writes those lines to the host, where no redirection captures them, not even *>&1, while a
	# transcript records them (measured 2026-11-01 on PowerShell 7.6.9).
	function Get-WhatIfTarget {
		param (
			[Parameter(Mandatory = $true)]
			[scriptblock] $Command
		)

		$path = Join-Path $TestDrive "$(New-TestName).txt"
		$null = Start-Transcript -LiteralPath $path -UseMinimalHeader
		try {
			$null = & $Command
		}
		finally {
			$null = Stop-Transcript
		}

		foreach ($line in Get-Content -LiteralPath $path) {
			if ($line -match '^What if: .+ on target "(?<target>[^"]+)"\.$') {
				$Matches.target
			}
		}
	}

	# The sandbox has no media files for Sonarr to offer for import, so the manual import tests edit a stand-in: a
	# record built in C# with an Id and a Name property, whose deserialization hook, run through .psobject, copies the
	# Id into its C# Id and adds the properties that an import needs, as it does for a record that Sonarr sends. Its
	# ID is the stand-in's own, not a database ID.
	function New-StandInImport {
		$record = [MG.Sonarr.Next.Models.ManualImports.ManualImportObject]::new()
		$record.psobject.Properties.Add([psnoteproperty]::new('Id', 1))
		$record.psobject.Properties.Add([psnoteproperty]::new('Name', 'pester.mkv'))
		$record.psobject.OnDeserialized()
		$record
	}

	$seriesByTvdbId = @{}
	foreach ($item in (Invoke-SandboxApi series -ExpectStatus 200).Body) {
		$seriesByTvdbId[[int] $item.tvdbId] = [int] $item.id
	}
	$endedId = $seriesByTvdbId[$Fixtures.SeededSeries[0].TvdbId]
	$continuingId = $seriesByTvdbId[$Fixtures.SeededSeries[1].TvdbId]
	$episodeId = [int] @((Invoke-SandboxApi "episode?seriesId=$endedId" -ExpectStatus 200).Body)[0].id
	$tagId = [int] ((Invoke-SandboxApi tag -ExpectStatus 200).Body | Where-Object label -eq $Fixtures.Tag).id
}

Describe 'Remove-SonarrSeries' {
	It 'takes a piped series' {
		Get-WhatIfTarget { Get-SonarrSeries -Id $endedId | Remove-SonarrSeries -WhatIf -ErrorAction Stop } |
			Should -Be "/series/$endedId"
	}

	It 'takes several piped series' {
		$targets = Get-WhatIfTarget { Get-SonarrSeries -Id $endedId, $continuingId | Remove-SonarrSeries -WhatIf -ErrorAction Stop }
		$targets | Sort-Object | Should -Be (@("/series/$endedId", "/series/$continuingId") | Sort-Object)
	}

	It 'takes a series by -InputObject' {
		$series = Get-SonarrSeries -Id $endedId -ErrorAction Stop
		Get-WhatIfTarget { Remove-SonarrSeries -InputObject $series -WhatIf -ErrorAction Stop } |
			Should -Be "/series/$endedId"
	}

	It 'turns away a series that the binder builds from a hashtable, whose C# ID is 0' {
		$record = { @{ Id = $endedId } | Remove-SonarrSeries -WhatIf -ErrorAction Stop } |
			Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException]) -ErrorId 'ParameterArgumentValidationError,MG.Sonarr.Next.Shell.Cmdlets.Series.RemoveSonarrSeriesCmdlet' -PassThru
		$record.Exception.ParameterName | Should -BeExactly 'InputObject'
	}
}

Describe 'Update-SonarrSeries' {
	It 'takes a piped series' {
		Get-WhatIfTarget { Get-SonarrSeries -Id $endedId | Update-SonarrSeries -WhatIf -ErrorAction Stop } |
			Should -Be "/series/$endedId"
	}
}

Describe 'Update-SonarrEpisode' {
	It 'takes a piped episode' {
		Get-WhatIfTarget { Get-SonarrEpisode -Id $episodeId | Update-SonarrEpisode -WhatIf -ErrorAction Stop } |
			Should -Be "/episode/$episodeId"
	}
}

Describe 'Rename-SonarrTag' {
	It 'takes a piped tag' {
		$newName = New-TestName
		Get-WhatIfTarget { Get-SonarrTag -Name $Fixtures.Tag | Rename-SonarrTag -NewName $newName -WhatIf -ErrorAction Stop } |
			Should -Be "/tag/$tagId"
	}
}

Describe 'Remove-SonarrTag' {
	It 'takes a piped tag' {
		Get-WhatIfTarget { Get-SonarrTag -Name $Fixtures.Tag | Remove-SonarrTag -WhatIf -ErrorAction Stop } |
			Should -Be "/tag/$tagId"
	}
}

Describe 'Remove-SonarrRootFolder' {
	It 'takes a piped root folder' {
		$folderId = [int] ((Invoke-SandboxApi rootfolder -ExpectStatus 200).Body | Where-Object { $_.path.TrimEnd('/') -eq $Fixtures.RootFolder }).id
		Get-WhatIfTarget { Get-SonarrRootFolder | Remove-SonarrRootFolder -WhatIf -ErrorAction Stop } |
			Should -Be "/rootfolder/$folderId"
	}
}

Describe 'Update-SonarrDelayProfile' {
	It 'takes a piped delay profile' {
		$profileIds = @((Invoke-SandboxApi delayprofile -ExpectStatus 200).Body.id)
		$targets = Get-WhatIfTarget { Get-SonarrDelayProfile | Update-SonarrDelayProfile -WhatIf -ErrorAction Stop }
		$targets | Should -Be ($profileIds | ForEach-Object { "/delayprofile/$_" })
	}
}

Describe 'Update-SonarrNamingConfig' {
	It 'takes the piped naming settings' {
		$configId = [int] (Invoke-SandboxApi config/naming -ExpectStatus 200).Body.id
		Get-WhatIfTarget { Get-SonarrNamingConfig | Update-SonarrNamingConfig -WhatIf -ErrorAction Stop } |
			Should -Be "/config/naming/$configId"
	}
}

Describe 'Update-SonarrHostConfig' {
	It 'takes the piped host config' {
		Get-WhatIfTarget { Get-SonarrHostConfig | Update-SonarrHostConfig -WhatIf -ErrorAction Stop } |
			Should -Be '/config/host'
	}
}

Describe 'Update-SonarrMediaManagement' {
	It 'takes the piped media management settings' {
		$configId = [int] (Invoke-SandboxApi config/mediamanagement -ExpectStatus 200).Body.id
		Get-WhatIfTarget { Get-SonarrMediaManagement | Update-SonarrMediaManagement -WhatIf -ErrorAction Stop } |
			Should -Be "/config/mediamanagement/$configId"
	}
}

Describe 'Get-SonarrHistory' {
	It 'takes a series by -Series' {
		$series = Get-SonarrSeries -Id $endedId -ErrorAction Stop
		{ Get-SonarrHistory -Series $series -ErrorAction Stop } | Should -Not -Throw
	}
}

Describe 'Edit-SonarrManualImport' {
	BeforeAll {
		$series = Get-SonarrSeries -Id $endedId -ErrorAction Stop
		$episode = Get-SonarrEpisode -Id $episodeId -ErrorAction Stop
		$quality = @((Invoke-SandboxApi qualitydefinition -ExpectStatus 200).Body.quality | Where-Object id -gt 0)[0]
		$cmdletName = 'MG.Sonarr.Next.Shell.Cmdlets.ManualImports.EditSonarrManualImportCmdlet'
	}

	It 'takes a series by its ID' {
		$record = Edit-SonarrManualImport -InputObject (New-StandInImport) -Series $endedId -PassThru -ErrorAction Stop
		$record.Series.Id | Should -Be $endedId
	}

	It 'takes a series as the object itself' {
		$record = Edit-SonarrManualImport -InputObject (New-StandInImport) -Series $series -PassThru -ErrorAction Stop
		[object]::ReferenceEquals($record.Series, $series) | Should -BeTrue
	}

	It 'takes an episode by its ID or as the object itself' {
		$byId = Edit-SonarrManualImport -InputObject (New-StandInImport) -Episode $episodeId -PassThru -ErrorAction Stop
		@($byId.Episodes).Id | Should -Be $episodeId

		$byObject = Edit-SonarrManualImport -InputObject (New-StandInImport) -Episode $episode -PassThru -ErrorAction Stop
		@($byObject.Episodes).Id | Should -Be $episodeId
	}

	It 'takes a quality by its ID' {
		$record = Edit-SonarrManualImport -InputObject (New-StandInImport) -Quality $quality.id -PassThru -ErrorAction Stop
		$record.Quality.Quality.Id | Should -Be $quality.id
		$record.Quality.Quality.Name | Should -BeExactly $quality.name
	}

	It 'takes a quality revision, which has no ID of its own' {
		$revision = [MG.Sonarr.Next.Models.Qualities.QualityRevisionObject]::new([MG.Sonarr.Next.Models.Qualities.QualityObject]::Default)
		$record = Edit-SonarrManualImport -InputObject (New-StandInImport) -Quality $revision -PassThru -ErrorAction Stop
		[object]::ReferenceEquals($record.Quality, $revision) | Should -BeTrue
	}

	It 'turns away <Case>' -ForEach @(
		@{ Case = 'a series ID of 0'; Parameter = 'Series'; Argument = { 0 } }
		@{ Case = 'a series without an ID'; Parameter = 'Series'; Argument = { [MG.Sonarr.Next.Models.Series.SeriesObject]::new() } }
		@{ Case = 'a quality ID of 0'; Parameter = 'Quality'; Argument = { 0 } }
	) {
		$arguments = @{ InputObject = New-StandInImport; $Parameter = & $Argument }
		$record = { Edit-SonarrManualImport @arguments -ErrorAction Stop } |
			Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException]) -ErrorId "ParameterArgumentValidationError,$cmdletName" -PassThru
		$record.Exception.ParameterName | Should -BeExactly $Parameter
	}

	It 'turns away <Case>, which is neither an ID nor a series' -ForEach @(
		@{ Case = 'an episode'; Argument = { $episode } }
		@{ Case = 'a title'; Argument = { $Fixtures.SeededSeries[0].Title } }
		@{ Case = 'a list of IDs'; Argument = { $endedId, $continuingId } }
	) {
		$value = & $Argument
		$record = { Edit-SonarrManualImport -InputObject (New-StandInImport) -Series $value -ErrorAction Stop } |
			Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException]) -ErrorId "ParameterArgumentTransformationError,$cmdletName" -PassThru
		$record.Exception.ParameterName | Should -BeExactly 'Series'
	}
}
