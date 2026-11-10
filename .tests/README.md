# PoshSonarr's Pester suite

The Pester suite in this folder tests PoshSonarr's cmdlets against the Sonarr sandbox, the disposable Sonarr container in `sandbox/sonarr/`. Each run builds the module in Debug, imports the build by a manifest written for it in a new PowerShell process, connects to the sandbox and nothing else, and leaves the sandbox at its baseline.

## Requirements

- Docker Desktop, running Linux containers, with the sandbox started by `sandbox/sonarr/start.ps1`, or reset by the runner's `-Reset`.
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
| `pwsh -NoProfile -File .tests/run.ps1 -Reset` | Resets the sandbox to its baseline first, with `sandbox/sonarr/reset.ps1`. |
| `pwsh -NoProfile -File .tests/run.ps1 -NoBuild -Path .tests/Connection.Tests.ps1` | Runs one file, on the last build. |
| `pwsh -NoProfile -File .tests/run.ps1 -Tag Restart` | Runs only the tests tagged `Restart`, which every other run leaves out. |

A run goes through these steps, and stops with exit code 1 at the first that fails:

1. It checks for Pester 6.2.0 or a later 6.x, and names the install command when there's none.
2. It builds the solution in Debug, unless `-NoBuild` reuses the last build. A Debug build turns on the dependency injection container's checks of its registrations and scopes, so a mistake in either fails a test instead of staying hidden.
3. It replaces `.temp/pester/PoshSonarr/`, which git ignores, with a copy of the Shell project's Debug output, without `Debug.ps1`. When a process holds a file in the folder, such as a PowerShell session that imported the module from it, the runner names the files and stops without deleting anything. Copying the build keeps `bin/` unlocked, so the next build works while a session has the copy loaded.
4. It writes the copy's manifest, `PoshSonarr.psd1`, with `.tasks/publish_module.ps1`, the script that a Release build runs, so the tests import the cmdlets, aliases, and format and type files that a user's import gets.
5. With `-Reset`, it resets the sandbox. Otherwise it checks that the sandbox answers with its pinned version and instance name, and shows the containers and their logs when it doesn't.
6. It runs Pester in a new `pwsh -NoProfile -NonInteractive` process, in which `POSHSONARR_TEST_MANIFEST` names the staged manifest. Pester runs the files one after another, with detailed output, and writes NUnit XML results to `.temp/pester/results.xml`.

The runner exits 0 when every selected test passes and 1 otherwise. Under `-File`, separate several paths or tags with commas, or pass the paths first, without `-Path`.

Run the suite only through the runner. `setup.ps1` stops a run that `Invoke-Pester` starts directly, which could otherwise test a stale build or an installed copy of PoshSonarr. Don't run the suite while `sandbox/sonarr/test.ps1` runs, since both add Sandbox Lookup Show.

## What setup.ps1 gives each file

Every test file's top-level `BeforeAll` dot-sources `setup.ps1`. It imports the staged module and `sandbox/sonarr/sandbox.psm1`, checks that the sandbox answers as the sandbox, and checks the parts of its baseline that tests rely on: the tags are exactly `sandbox-baseline`, the series are exactly TVDB IDs 9000001 and 9000002, and the root folders are exactly `/tv`. When something differs, it stops the file with a message that names the difference and `-Reset`. Last, it connects the module to the sandbox, and leaves the file these names:

| Name | What it is |
|---|---|
| `$SandboxSettings` | The sandbox's `BaseUrl`, `ApiKey`, `Version`, and `InstanceName`, as `Get-SandboxSettings` reads them from `sandbox/sonarr/compose.yaml`. |
| `$Fixtures` | The fixtures' facts: the four shows' TVDB IDs, titles, and episode counts, the seeded shows' tags, the baseline's tag and root folder, the search term that finds all four shows, the 9 episodes that wanted/missing lists, the 3 episodes that the calendar lists from 2099-01-01 to 2099-01-31, and a TVDB ID that the mock doesn't have. |
| `New-TestName` | Returns `pester-` and 8 random hex digits, a name for whatever a test creates. |
| `Invoke-TestProcess` | Runs a script in a new PowerShell process that has imported the module, with no connection, and returns the lines that it writes, its errors included. |
| `Invoke-SandboxApi` | Sends one raw request to the sandbox's API, for setup, cleanup, and checks that don't go through the module. |
| `Wait-SandboxSeries` | Waits until Sonarr has finished adding a series: its episodes exist and its `addOptions` are gone. |
| `Invoke-SandboxCompose` | Runs `docker compose` on the sandbox's project. |

## Writing a test

Each file covers one area and is named after its cmdlets' nouns, such as `Tags.Tests.ps1`. `Binding.Tests.ps1` is named instead for what it checks across cmdlets: that each one takes the models that other cmdlets return, piped or by name. A file stands alone: it starts with these `#Requires` lines and this `BeforeAll`, so it imports, checks, and connects for itself.

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
		$tags = (Invoke-SandboxApi tag -ExpectStatus 200).Body | Where-Object label -eq $label
		foreach ($tag in $tags) {
			Invoke-SandboxApi "tag/$($tag.id)" -Method Delete -ExpectStatus 200 | Out-Null
		}
	}

	It 'creates a tag with the label it is given' {
		$tag = New-SonarrTag -Label $label -ErrorAction Stop
		$tag.Label | Should -BeExactly $label
		(Invoke-SandboxApi "tag/$($tag.Id)" -ExpectStatus 200).Body.label | Should -BeExactly $label
	}
}
```

A `Describe` block names the cmdlet that it tests, and each `It` says what the cmdlet does, in the present tense. Assertions use the `Should -Be` style throughout, not Pester 6's `Should-*` commands, so every file reads the same way.

### The sandbox and its fixtures

- Connect only to the sandbox. Every `Connect-SonarrInstance` and `Test-SonarrHost` call passes `$SandboxSettings.BaseUrl`, and no test reads `.env` or references `Debug.ps1`.
- Find the fixtures by TVDB ID, title, or label. Sonarr gives each add of a series, tag, or profile a new database ID, so no test holds an ID as a literal; a test that needs one reads it from the API.
- Take counts that the fixtures set from `$Fixtures`, and counts that Sonarr sets, such as its number of quality definitions, from the API, so that a Sonarr upgrade doesn't break them.
- `$Fixtures` is the suite's own copy of the fixtures' facts, and `sandbox/sonarr/test.ps1` keeps another. A change to the mock's shows updates both.
- The files run one after another in one process, so the tests that add Sandbox Lookup Show or Sandbox Wild*Card Show, the two shows a test can add, never overlap. Sandbox Wild*Card Show is for tests of wildcard matching: its title holds a `*`, so a name such as `'Sandbox Wild*Show'` finds it only when the `*` in the name is a wildcard wherever it stands. Sonarr turns that `*` into a `-` in the show's folder, `/tv/Sandbox Wild-Card Show`.
- Sonarr does some of its work in the background. It answers a command's POST once it has queued the command, so a test that starts a command waits for it to finish, polling `GET command/{id}` until its status is neither `queued` nor `started`. It writes its log entries to its database in a batch every half second, and its scheduled tasks log as they run, so an entry can land between two reads of the newest entries. A test that compares those with the API reads the page through the API before and after the cmdlet, and compares only reads that returned the same entries. Sonarr only adds to a log file, so a log file that a test downloads holds the start of what `GET log/file/{name}` returns afterward (read in Sonarr 4.0.20.3014's source, and measured 2026-11-09).
- The module's command history belongs to the connection that `setup.ps1` makes for each file, so `Get-SonarrCommandHistory` lists only the commands that the file's own cmdlets started, such as `Invoke-SonarrRssSync`.

### What a test creates or changes

- Name whatever a test creates with `New-TestName`, and remove it in `AfterAll` or a `finally` block with `Invoke-SandboxApi`, so a broken `Remove-*` cmdlet can't leave anything behind. Each `Remove-*` cmdlet gets tests of its own.
- Run a cmdlet that asks a question of its own after `ShouldProcess`, as `Remove-SonarrSeries`, `Remove-SonarrEpisodeFile`, `Remove-SonarrBackup`, and `Update-SonarrNotification` do, with `-Force`. `-Confirm:$false` skips only the prompt that `ShouldProcess` shows, and the runner's Pester process is non-interactive, so the question fails the call with a `PSInvalidOperationException`, and the cmdlet changes nothing.
- Confirm what Sonarr stored through `Invoke-SandboxApi` too, so a bug in a `Get-*` cmdlet can't hide one in the cmdlet under test.
- The naming settings, the media management settings, and the host config are single objects that every later test sees. A test reads the current object, changes one field, checks the change, and puts the original back in `AfterAll` through `Invoke-SandboxApi`. The host config tests change only `backupRetention`: by one, within the range of 1 to 90 that Sonarr enforces, or, to test a failed update, to 0, which Sonarr turns away with a 400. The sandbox authenticates with `External`, so a host config update needs no username or password. The naming settings tests change only `replaceIllegalCharacters`, and the media management tests only `recycleBinCleanupDays`, by one. To test a failed update, they send a `multiEpisodeStyle` of 6, outside the 0 to 5 that Sonarr allows, or a `recycleBinCleanupDays` of -1, and Sonarr turns either away with a 400 and saves nothing.
- The seeded series and their episodes are part of the baseline that every later test sees, while `setup.ps1` checks only which series there are. A test that changes one, such as an episode's monitoring, puts back what it changed in `AfterAll` through `Invoke-SandboxApi`. An episode left unmonitored would drop out of wanted/missing and the calendar, whose counts later tests expect.
- Sonarr doesn't delete a tag that anything still carries, such as a series or a delay profile, and answers the delete with a 409 (measured 2026-11-09 on Sonarr 4.0.20.3014). So the cleanup of a test that put its tag on something takes the tag off, or deletes what carries it, before it deletes the tag.
- Every delay profile but Sonarr's built-in one needs a tag that no other delay profile has, and every release profile needs a required or ignored term. Sonarr answers a profile without one with a 400 (measured 2026-11-09 on Sonarr 4.0.20.3014), so a test that creates a delay profile creates a tag of its own for it, and one that creates a release profile gives it a `pester-` term.
- A remote path mapping's local path has to exist, and Sonarr turns away a second mapping with the same host and remote path with a 500 (measured 2026-11-09 on Sonarr 4.0.20.3014). So a test maps to `/tv/`, the baseline's root folder, for a host named by `New-TestName`. Sonarr ends both paths of a new mapping with a `/`, adding one where it's missing, but saves an update's paths as they come, so a test ends every path with a `/`, and Sonarr stores what the test sent.
- Sonarr adds a root folder only when the folder exists and Sonarr can write to it, and answers a folder that's already a root folder with a 400 (read in Sonarr 4.0.20.3014's source, and measured 2026-11-10). So a test that adds a root folder adds `/tv-temp`, an empty folder that the sandbox's image holds beside `/tv` for these tests, and removes it again through `Invoke-SandboxApi` in `AfterAll` or `AfterEach`. Removing a root folder only takes it off Sonarr's list, so the folder stays for the next test, and `setup.ps1` stops the next file while `/tv-temp` is still a root folder.
- Never change the host config's API key, port, bind address, URL base, or authentication, which could lock the suite out; its instance name or update settings, which the readiness check and the sandbox's update lock rely on; or `certificateValidation`, which lets Sonarr accept the mock metadata service's certificate.
- A test that runs a cmdlet with `-WhatIf`, to check what it would act on without changing anything, reads the target from the What if line, which `ShouldProcess` writes to the host. No redirection captures that line, not even `*>&1`, while a transcript records it, so the test runs the cmdlet under `Start-Transcript`, as `Get-WhatIfTarget` in `Binding.Tests.ps1` does.
- Create indexers, download clients, and notifications disabled. Sonarr runs a provider's connection test when it saves an enabled one, and skips the test and the settings validation for a disabled one, so a disabled provider needs no mock service. `New-SonarrDownloadClient` creates a disabled client unless `-Enabled` is passed. Create indexers and notifications with `Invoke-SandboxApi`, from the schema that `GET indexer/schema` or `GET notification/schema` returns. Sonarr counts an indexer as enabled when any of `enableRss`, `enableAutomaticSearch`, and `enableInteractiveSearch` is on, since an indexer has no enable flag of its own, and a notification when any of its triggers, such as `onGrab`, is on. Some indexer templates come with those three flags on, while every notification template comes with its triggers off (measured 2026-11-08 on Sonarr 4.0.20.3014), so turn all three off before posting an indexer.
- Sonarr checks a provider's name whether or not the provider is enabled: it turns away a create or an update without a name, or with one that another provider of the same kind has, ignoring case, with a 400 (measured 2026-11-09 on Sonarr 4.0.20.3014). So name each provider with `New-TestName`, and to test a failed create or update, send an empty name or a taken one. The module's indexer model sends the name that an indexer was read with in place of an empty one, so a test of a failed indexer update sends a taken name.
- Sonarr names each backup itself, after the second that it made the backup in, and makes scheduled backups of its own. So a test that needs a backup posts a `Backup` command through `Invoke-SandboxApi`, waits for the command to finish, and takes the manual backup whose path `GET system/backup` didn't list before. It deletes the backup in `AfterAll` or `AfterEach` through `DELETE system/backup/{id}`, if the list still has it.
- Save each file that a cmdlet writes, such as a backup or a log file that it downloads, to Pester's `TestDrive:`, under a name from `New-TestName`. The tests in a `Describe` block share its `TestDrive:`, so a name of each test's own keeps one test's file from turning up in another's.

### Failures

- The module's HTTP client never throws. A failed call comes back as a result, and the cmdlet writes its error record, usually without ending the statement. Some cmdlets end the statement instead, such as `Connect-SonarrInstance` when its check of the URL and key fails.
- Test a failure by running the cmdlet with `-ErrorAction Stop` in a script block that `Should -Throw` checks, or by collecting its records with `-ErrorVariable`. Assert on the record's category, its target, and the HTTP status where the record carries one, and on message text only when the message is what's under test.
- `-ErrorAction` doesn't hide an error that ends the statement, so a script block that runs the cmdlet with `-ErrorAction SilentlyContinue` throws only for such an error.
- Pester 6.2's `Should -Throw -ErrorId` compares the whole `FullyQualifiedErrorId`, so pass all of it, such as `ParameterArgumentValidationError,MG.Sonarr.Next.Shell.Cmdlets.Systems.TestSonarrHostCmdlet` for an argument that fails a cmdlet's validation. That error's exception, a `ParameterBindingValidationException`, isn't public, so check for its base type, `ParameterBindingException`.
- Pester runs every test inside a `try` block, where an error that ends its statement and one that ends the script both jump to the `catch`. To test how far an error reaches, run the command with `Invoke-TestProcess`, followed by a statement that writes a line, and check for that line.

### Bugs that a test finds

When a test fails because the module is wrong, a small, contained fix ships with the test. A larger fix is reported instead, and the test starts with `Set-ItResult -Skipped -Because`, with a reason that states the intended behavior, so the suite passes and each skipped test shows what's still owed. Pester 6.2's `It -Skip` prints no reason.
