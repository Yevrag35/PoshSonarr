#Requires -Modules @{ ModuleName = "Pester"; ModuleVersion = "5.5.0" }

BeforeAll {
    . "$PSScriptRoot\setup.ps1"
}

AfterAll {
    Disconnect-SonarrInstance
}

Describe "Get-SonarrStatus" {
    It "Returns PSCustomObject" {
        $status = Get-SonarrStatus
        $status | Should -BeOfType [pscustomobject]
    }
}

Describe "Get-SonarrSeries" {
    BeforeEach {
        Start-Sleep -Milliseconds 500
    }

    Context "Returns All Series" {
        It "Returns all Sonarr series" {
            $all = Get-SonarrSeries
            ($all.Count -gt 1) | Should -BeTrue -ErrorAction Stop
            $all | Should -BeOfType [MG.Sonarr.Next.Services.Models.Series.SeriesObject]

            $one = $all[0]
            $one.MetadataTag | Should -Not -BeNullOrEmpty -ErrorAction Stop
            $one.MetadataTag.Value | Should -BeExactly '#series'
        }
    }

    Context "Get Series By Id" {
        It "Returns a single series by its ID positionally" {
            $one = Get-SonarrSeries 607
            $one | Should -Not -BeNullOrEmpty -ErrorAction Stop
            $one.Id | Should -Be 607
            $one.Name | Should -BeExactly $one.Title
        }
    }

    Context "Get Series By Name" {
        It "Returns a single series by its Name positionally" {
            $one = Get-SonarrSeries "TenPuru: No One Can Live on Loneliness"
            $one | Should -Not -BeNullOrEmpty -ErrorAction Stop
            (@($one).Count -eq 1) | Should -BeTrue -ErrorAction Stop
            $one.Id | Should -Be 607
        }
    }
}

Describe "Add-SonarrTag" {
    
}