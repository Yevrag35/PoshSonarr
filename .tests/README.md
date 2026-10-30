# PoshSonarr's Pester suite

The Pester suite in this folder tests PoshSonarr's cmdlets against the Sonarr testbed, the disposable Sonarr container in `testbed/sonarr/`. Each run builds the module in Debug, imports the build by a manifest written for it in a new PowerShell process, connects to the testbed and nothing else, and leaves the testbed at its baseline.

## Requirements

- Docker Desktop, running Linux containers, with the testbed started by `testbed/sonarr/start.ps1`, or reset by the runner's `-Reset`.
- PowerShell 7.6 or later, and a .NET 10 SDK for the build.
- Pester 6.2.0 or a later 6.x. On a machine without it, install it with:

```powershell
Install-PSResource -Name Pester -Version '[6.2.0,7.0.0)' -Scope CurrentUser -TrustRepository
```

## Running the suite

Run these from the repository root. The runner builds every path from its own location, so it works from any other directory too.

| Command | What it does |
|---|---|
| `pwsh -NoProfile -File .tests/run.ps1` | Builds, stages, and runs every `*.Tests.ps1` file in this folder. |
| `pwsh -NoProfile -File .tests/run.ps1 -Reset` | Resets the testbed to its baseline first, with `testbed/sonarr/reset.ps1`. |
| `pwsh -NoProfile -File .tests/run.ps1 -NoBuild -Path .tests/Connection.Tests.ps1` | Runs one file, on the last build. |
| `pwsh -NoProfile -File .tests/run.ps1 -Tag Restart` | Runs only the tests tagged `Restart`, which every other run leaves out. |

A run goes through these steps, and stops with exit code 1 at the first that fails:

1. It checks for Pester 6.2.0 or a later 6.x, and names the install command when there's none.
2. It builds the solution in Debug, unless `-NoBuild` reuses the last build. A Debug build turns on the dependency injection container's checks of its registrations and scopes, so a mistake in either fails a test instead of staying hidden.
3. It replaces `.temp/pester/PoshSonarr/`, which git ignores, with a copy of the Shell project's Debug output, without `Debug.ps1`. When a process holds a file in the folder, such as a PowerShell session that imported the module from it, the runner names the files and stops without deleting anything. Copying the build keeps `bin/` unlocked, so the next build works while a session has the copy loaded.
4. It writes the copy's manifest, `PoshSonarr.psd1`, with `.tasks/publish_module.ps1`, the script that a Release build runs, so the tests import the cmdlets, aliases, and format and type files that a user's import gets.
5. With `-Reset`, it resets the testbed. Otherwise it checks that the testbed answers with its pinned version and instance name, and shows the containers and their logs when it doesn't.
6. It runs Pester in a new `pwsh -NoProfile -NonInteractive` process, in which `POSHSONARR_TEST_MANIFEST` names the staged manifest. Pester runs the files one after another, with detailed output, and writes NUnit XML results to `.temp/pester/results.xml`.

The runner exits 0 when every selected test passes and 1 otherwise. Under `-File`, separate several paths or tags with commas, or pass the paths first, without `-Path`.

Run the suite only through the runner. `setup.ps1` stops a run that `Invoke-Pester` starts directly, which could otherwise test a stale build or an installed copy of PoshSonarr. Don't run the suite while `testbed/sonarr/test.ps1` runs, since both add Testbed Lookup Show.

## What setup.ps1 gives each file

Every test file's top-level `BeforeAll` dot-sources `setup.ps1`. It imports the staged module and `testbed/sonarr/testbed.psm1`, checks that the testbed answers as the testbed, and checks the parts of its baseline that tests rely on: the tags are exactly `testbed-baseline`, the series are exactly TVDB IDs 9000001 and 9000002, and the root folders are exactly `/tv`. When something differs, it stops the file with a message that names the difference and `-Reset`. Last, it connects the module to the testbed, and leaves the file these names:

| Name | What it is |
|---|---|
| `$TestbedSettings` | The testbed's `BaseUrl`, `ApiKey`, `Version`, and `InstanceName`, as `Get-TestbedSettings` reads them from `testbed/sonarr/compose.yaml`. |
| `$Fixtures` | The fixtures' facts: the three shows' TVDB IDs, titles, and episode counts, the seeded shows' tags, the baseline's tag and root folder, the search term that finds all three shows, the 9 episodes that wanted/missing lists, the 3 episodes that the calendar lists from 2099-01-01 to 2099-01-31, and a TVDB ID that the mock doesn't have. |
| `New-TestName` | Returns `pester-` and 8 random hex digits, a name for whatever a test creates. |
| `Invoke-TestProcess` | Runs a script in a new PowerShell process that has imported the module, with no connection, and returns the lines that it writes, its errors included. |
| `Invoke-TestbedApi` | Sends one raw request to the testbed's API, for setup, cleanup, and checks that don't go through the module. |
| `Wait-TestbedSeries` | Waits until Sonarr has finished adding a series: its episodes exist and its `addOptions` are gone. |
| `Invoke-TestbedCompose` | Runs `docker compose` on the testbed's project. |

## Writing a test

Each file covers one area and is named after its cmdlets' nouns, such as `Tags.Tests.ps1`. It stands alone: it starts with these `#Requires` lines and this `BeforeAll`, so it imports, checks, and connects for itself.

```powershell
#Requires -Version 7.6
#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '6.2.0'; MaximumVersion = '6.99.99' }

BeforeAll {
	. "$PSScriptRoot/setup.ps1"
}

Describe 'New-SonarrTag' {
	BeforeAll {
		$label = New-TestName
	}
	AfterAll {
		$tags = (Invoke-TestbedApi tag -ExpectStatus 200).Body | Where-Object label -eq $label
		foreach ($tag in $tags) {
			Invoke-TestbedApi "tag/$($tag.id)" -Method Delete -ExpectStatus 200 | Out-Null
		}
	}

	It 'creates a tag with the label it is given' {
		$tag = New-SonarrTag -Label $label -ErrorAction Stop
		$tag.Label | Should -BeExactly $label
		(Invoke-TestbedApi "tag/$($tag.Id)" -ExpectStatus 200).Body.label | Should -BeExactly $label
	}
}
```

A `Describe` block names the cmdlet that it tests, and each `It` says what the cmdlet does, in the present tense. Assertions use the `Should -Be` style throughout, not Pester 6's `Should-*` commands, so every file reads the same way.

### The testbed and its fixtures

- Connect only to the testbed. Every `Connect-SonarrInstance` and `Test-SonarrHost` call passes `$TestbedSettings.BaseUrl`, and no test reads `.env` or references `Debug.ps1`.
- Find the fixtures by TVDB ID, title, or label. Sonarr gives each add of a series, tag, or profile a new database ID, so no test holds an ID as a literal; a test that needs one reads it from the API.
- Take counts that the fixtures set from `$Fixtures`, and counts that Sonarr sets, such as its number of quality definitions, from the API, so that a Sonarr upgrade doesn't break them.
- `$Fixtures` is the suite's own copy of the fixtures' facts, and `testbed/sonarr/test.ps1` keeps another. A change to the mock's shows updates both.
- The files run one after another in one process, so the tests that add Testbed Lookup Show, the only show a test can add, never overlap.

### What a test creates or changes

- Name whatever a test creates with `New-TestName`, and remove it in `AfterAll` or a `finally` block with `Invoke-TestbedApi`, so a broken `Remove-*` cmdlet can't leave anything behind. Each `Remove-*` cmdlet gets tests of its own.
- Confirm what Sonarr stored through `Invoke-TestbedApi` too, so a bug in a `Get-*` cmdlet can't hide one in the cmdlet under test.
- The naming settings, the media management settings, and the host config are single objects that every later test sees. A test reads the current object, changes one field, checks the change, and puts the original back in `AfterAll` through `Invoke-TestbedApi`. The host config test changes only `backupRetention`, by one, within the range of 1 to 90 that Sonarr enforces. The testbed authenticates with `External`, so a host config update needs no username or password.
- Never change the host config's API key, port, bind address, URL base, or authentication, which could lock the suite out; its instance name or update settings, which the readiness check and the testbed's update lock rely on; or `certificateValidation`, which lets Sonarr accept the mock metadata service's certificate.
- Create indexers, download clients, and notifications disabled. Sonarr runs a provider's connection test when it saves an enabled one, and skips the test and the settings validation for a disabled one, so a disabled provider needs no mock service. `New-SonarrDownloadClient` creates a disabled client unless `-Enabled` is passed. Create indexers and notifications with `Invoke-TestbedApi`, from the schema that `GET indexer/schema` or `GET notification/schema` returns.

### Failures

- The module's HTTP client never throws. A failed call comes back as a result, and the cmdlet writes its error record, usually without ending the statement. Some cmdlets end the statement instead, such as `Connect-SonarrInstance` when its check of the URL and key fails.
- Test a failure by running the cmdlet with `-ErrorAction Stop` in a script block that `Should -Throw` checks, or by collecting its records with `-ErrorVariable`. Assert on the record's category, its target, and the HTTP status where the record carries one, and on message text only when the message is what's under test.
- `-ErrorAction` doesn't hide an error that ends the statement, so a script block that runs the cmdlet with `-ErrorAction SilentlyContinue` throws only for such an error.
- Pester runs every test inside a `try` block, where an error that ends its statement and one that ends the script both jump to the `catch`. To test how far an error reaches, run the command with `Invoke-TestProcess`, followed by a statement that writes a line, and check for that line.

### Bugs that a test finds

When a test fails because the module is wrong, a small, contained fix ships with the test. A larger fix is reported instead, and the test starts with `Set-ItResult -Skipped -Because`, with a reason that states the intended behavior, so the suite passes and each skipped test shows what's still owed. Pester 6.2's `It -Skip` prints no reason.
