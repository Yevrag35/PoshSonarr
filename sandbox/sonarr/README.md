# Sonarr sandbox

A disposable Sonarr instance in Docker for testing PoshSonarr against. It runs Sonarr 4.0.20.3014, pinned and unable to update itself, listens only on this machine's loopback address, and starts unattended with a fixed API key. Beside it runs a mock of Sonarr's metadata service that serves three made-up shows, so series and episodes work offline and come out the same every time. Its data lives in two Docker volumes that belong to it alone, so nothing it does reaches another Sonarr instance or real media, and a reset returns it to a known baseline.

## Requirements

- Docker Desktop, running Linux containers.
- PowerShell 7.6 or later.

## Commands

Run these from the repository root. Each works from any other directory too.

| Command | What it does |
|---|---|
| `pwsh -NoProfile -File sandbox/sonarr/start.ps1` | Builds the images whose files changed, Sonarr's and the mock metadata service's, starts both, and waits for Sonarr's health check. Then it checks from the host that the port answers with the pinned version and the instance name `Sonarr Sandbox`, creates whatever the baseline lacks, adds whichever seeded series is missing, and prints the connection values. `-TimeoutSeconds`, 120 by default, bounds the wait for the health check, and building the images doesn't count against it. |
| `pwsh -NoProfile -File sandbox/sonarr/stop.ps1` | Stops both services and keeps their containers and Sonarr's volumes, so the next start brings back the same data. |
| `pwsh -NoProfile -File sandbox/sonarr/reset.ps1` | Removes the containers and both volumes, and with them everything Sonarr stored, then does everything `start.ps1` does. It takes the same `-TimeoutSeconds`. |
| `pwsh -NoProfile -File sandbox/sonarr/test.ps1` | Runs the smoke tests against the running sandbox and prints a PASS or FAIL line for each check. The Smoke tests section says what they check. |
| `pwsh -NoProfile -File sandbox/sonarr/logs.ps1` | Shows the last 200 lines of Sonarr's log. `-Tail <count>` shows another number, `-Service skyhook` shows the mock metadata service's log, with a line for each request Sonarr made to it, and `-Follow` keeps streaming new lines until Ctrl+C. |

Each script exits 1 when something fails, and a failure while starting or while adding the seeded series first shows the containers' state and the last 200 lines of each one's log.

The baseline is what a new Sonarr install holds, plus a tag labeled `sandbox-baseline` and a root folder at `/tv`. On top of it, `start.ps1` makes Sonarr accept the mock's certificate and adds the two seeded series that the next section describes. Each run creates only what's missing and changes nothing else, so it's safe to run again, and whatever you create stays until `reset.ps1`.

## Seeded series

Sonarr gets every series and episode from its metadata service, SkyHook, at `https://skyhook.sonarr.tv`. The Compose project's second service, `skyhook`, stands in for it: nginx serving fixed JSON for three made-up shows from the files under `skyhook/v1/tvdb/`. Docker's DNS answers `skyhook.sonarr.tv` with the mock's address for Sonarr's container, so adding, looking up, and refreshing a series all run against the fixtures, without internet access and with the same results every time. The mock publishes no port, so nothing outside the project's network reaches it.

The mock's image generates a throwaway self-signed certificate when it's built, so no key lives in the repository. Sonarr accepts it because its `certificateValidation` setting, in `config/host`, is `disabledForLocalAddresses`, which skips the check for a host whose addresses are all private, as the mock's is, and still checks hosts on the internet. The setting lives in Sonarr's database, which a reset wipes, so `start.ps1` sets it whenever it isn't.

| TVDB ID | Title | Status | Episodes, airing at 20:00 UTC | Seeded |
|---|---|---|---|---|
| 9000001 | Sandbox Ended Show | ended | S1E1–3 and S2E1–3, weekly from 2020-01-06 to 2020-02-10 | Yes, with the `sandbox-baseline` tag |
| 9000002 | Sandbox Continuing Show | continuing | S1E1–3, weekly from 2021-03-01 to 2021-03-15, and S2E1–3, weekly from 2099-01-05 to 2099-01-19 | Yes |
| 9000003 | Sandbox Lookup Show | ended | S1E1–2, on 2019-05-06 and 2019-05-13 | No, it's left to look up, add, and delete |

The seeded series are monitored, in `/tv`, with all their episodes monitored and no searches. Sonarr counts an episode as aired once its air time plus the show's 30-minute runtime has passed, so `wanted/missing` reports the 9 episodes that aired, and the calendar from 2099-01-01 to 2099-01-31 holds the other 3. A lookup for `tvdb:<id>` finds that one show and any other term finds all three, and adding a TVDB ID that the mock lacks gets a 400 that says "A series with this ID was not found". `logs.ps1 -Service skyhook` shows each request Sonarr made to the mock. After every refresh, Sonarr warns that the `/tv` root folder is empty, because the sandbox holds no media files.

To change a show or add one, edit or add its file in `skyhook/v1/tvdb/shows/en/`, named for its TVDB ID, and make the same change in `skyhook/v1/tvdb/search/en/index.json`, which holds every show's object in one array. Then run `reset.ps1`: `start.ps1` alone rebuilds the mock, but Sonarr keeps what it fetched until it refreshes the show. A show's JSON needs its `malIds` and `aniListIds` lists, even when they're empty, because Sonarr stores them in database columns that can't be null. A new show joins the seeded ones through the `$seededShows` list in `sandbox.psm1`. `test.ps1` and PoshSonarr's Pester suite, through the `$Fixtures` table in `.tests/setup.ps1`, each expect the shows, the episode counts, and the totals that this section gives, so update both along with them.

## Smoke tests

`test.ps1` checks that the running sandbox is the one this README describes. It sends raw HTTP requests to the API rather than going through PoshSonarr, so it tests the sandbox, not the module. It expects the baseline, as `start.ps1` and `reset.ps1` leave it, and first makes the same status call that they make, so that it sends nothing to another server that answers on the port. Its checks come in these groups, in this order:

| Group | Checks |
|---|---|
| Identity | `system/status` answers 200 with the pinned version, the instance name `Sonarr Sandbox`, and `isDocker` true. |
| Keys | `system/status` and `tag` each answer 401 to a request without a key and to one with a wrong key. |
| Update lock | `system/status` reports `packageUpdateMechanism` `docker`, and `config/host` reports `updateAutomatically` false and `updateMechanism` `docker`. |
| Baseline | The `sandbox-baseline` tag exists, and so does the `/tv` root folder, with `accessible` true. |
| Seeded series | `series` lists TVDB IDs 9000001 and 9000002, each has 6 episodes, and the `sandbox-baseline` tag lists Sandbox Ended Show. |
| Mock lookup | `series/lookup` finds Sandbox Lookup Show alone for `tvdb:9000003` and all three shows for `sandbox`, and `config/host` reports `certificateValidation` `disabledForLocalAddresses`. |
| Wanted and calendar | `wanted/missing` reports 9 episodes, and the calendar from 2099-01-01 to 2099-01-31 holds 3. These run before the round trips, which add a show with two aired episodes for a while. |
| Unknown show | Adding TVDB ID 9009999, which the mock lacks, gets a 400 that says "A series with this ID was not found". |
| Tag round trip | For a tag labeled `sandbox-temp-` and 8 random hex digits, POST returns 201 with an ID, GET returns the label, PUT with a new label returns 202, GET returns the new label, DELETE returns 200, and GET then returns 404. |
| Series round trip | For Sandbox Lookup Show, POST returns 201 with an ID, Sonarr finishes adding the show, with its 2 episodes, within 60 seconds, GET returns 200, PUT with `monitored` false returns 202, DELETE returns 200, and GET then returns 404. |
| Container | Sonarr runs as UID 1000, and port 8989 is published on `127.0.0.1`. |

Each check prints a PASS or FAIL line that starts with its group, and a failed check shows what the sandbox answered. The script exits 1 when any check fails or the sandbox isn't answering. A round trip deletes what it added even when one of its checks fails, so a run leaves the sandbox as it found it. An interrupted run can leave its tag or Sandbox Lookup Show behind, and with the show still there, the next run fails until `reset.ps1` brings back the baseline. Each run leaves an error and a warning in Sonarr's log for the show that the mock lacks, besides the warning about the empty `/tv` root folder that adding Sandbox Lookup Show brings.

## Connecting

| Setting | Value |
|---|---|
| URL | `http://127.0.0.1:18989` |
| API prefix | `/api/v3` |
| API key | `0123456789abcdef0123456789abcdef`, in the `X-Api-Key` header |

The key is fixed and published on purpose, since only this machine can reach the container. A request with no key or a wrong one gets a 401.

```powershell
$headers = @{ 'X-Api-Key' = '0123456789abcdef0123456789abcdef' }
Invoke-RestMethod -Uri 'http://127.0.0.1:18989/api/v3/system/status' -Headers $headers |
	Select-Object appName, instanceName, version
```

With PoshSonarr imported, this connects the module to the sandbox. Leave off `-NoApiInPath`, because nothing in front of the container removes `/api`:

```powershell
Connect-SonarrInstance -Url http://127.0.0.1:18989 -ApiKey 0123456789abcdef0123456789abcdef
```

To point an interactive debugging session at the sandbox instead of the development instance, pass the sandbox's values to `Debug.ps1` after a Debug build:

```powershell
pwsh -NoExit -File src/MG.Sonarr.Next.Shell/bin/Debug/net10.0/Debug.ps1 -SonarrUrl http://127.0.0.1:18989 -ApiKey 0123456789abcdef0123456789abcdef -NoApiInPath:$false
```

Each of the script's parameters overrides only its own setting in the repository's `.env` file, so `-NoApiInPath:$false` keeps a `SONARR_NO_API_IN_PATH=true` line, written for a development instance behind a reverse proxy, from removing `/api` from the sandbox's paths.

## Changing the port

The sandbox publishes port 18989 unless `SONARR_SANDBOX_PORT` names another. Set it in the shell, or put a `SONARR_SANDBOX_PORT=<port>` line in `sandbox/sonarr/.env`, which git ignores and which Compose reads whatever directory a command runs from. A value set in the shell wins over the file's. The scripts get the port from Compose, so their checks and the values that `start.ps1` prints follow it, and the next `start.ps1` recreates the container on the new port with its volumes intact.

```powershell
$env:SONARR_SANDBOX_PORT = '18990'
pwsh -NoProfile -File sandbox/sonarr/start.ps1
```

## Changing the Sonarr version

`compose.yaml` pins the version and the SHA-256 digest of its download in two build arguments, `SONARR_VERSION` and `SONARR_SHA256`, and the build stops at its `sha256sum -c` step when the download doesn't match. Sonarr publishes no checksum file, so the digest is the one that GitHub records for the release asset. This prints it for a version, without the `sha256:` prefix that GitHub puts in front:

```powershell
gh api repos/Sonarr/Sonarr/releases/tags/v<version> --jq '.assets[] | select(.name | endswith("linux-x64.tar.gz")) | .digest | ltrimstr("sha256:")'
```

Update both arguments, then run `reset.ps1`, so that the database starts at the new version's schema instead of an older one that Sonarr would have to migrate.

## Running Compose directly

Pass the project name and the file to every `docker compose` command, as the scripts do. `COMPOSE_PROJECT_NAME`, from the environment or a `.env` file, outranks the file's own `name:` and would point the command at another project.

```powershell
docker compose --project-name poshsonarr-sandbox --file sandbox/sonarr/compose.yaml ps --all
docker compose --project-name poshsonarr-sandbox --file sandbox/sonarr/compose.yaml exec sonarr sqlite3 /config/sonarr.db .tables
```

From Git Bash, set `MSYS_NO_PATHCONV=1` first, or Git Bash rewrites container paths such as `/config/sonarr.db` into Windows paths.

## Troubleshooting

- **The port doesn't bind.** Windows reserves ranges of TCP ports, for Hyper-V and WSL among others, and the ranges differ from machine to machine and can change after a restart. When 18989 falls in one, starting fails with "Ports are not available" or "An attempt was made to access a socket in a way forbidden by its access permissions". This lists the ranges, and a port outside all of them goes in `SONARR_SANDBOX_PORT`:

	```powershell
	netsh interface ipv4 show excludedportrange protocol=tcp
	```

- **Docker isn't running.** A command that fails with "failed to connect to the docker API" needs Docker Desktop started.
- **Sonarr doesn't become healthy.** `logs.ps1` shows what Sonarr reported. A slow machine may need a longer `-TimeoutSeconds`.
- **Adding a seeded series fails.** `start.ps1` shows both services' logs. When the mock's log has no request from Sonarr, Sonarr didn't reach it, and a 409 that names a column of the `Series` table means a show's JSON lacks a field that Sonarr's database requires.
- **A smoke test fails.** Its line shows what the sandbox answered. Tests of your own, or a `test.ps1` run that was interrupted, can leave the sandbox off its baseline, so run `reset.ps1` and then `test.ps1` again first. A failure that comes back from a fresh baseline is real, and `logs.ps1`, or `logs.ps1 -Service skyhook` for the mock, shows Sonarr's side of the request.

## What the sandbox can't test

These need services that the sandbox leaves out on purpose.

| Tests of | Need |
|---|---|
| Real shows and their live metadata | Sonarr's real metadata service, SkyHook, and internet access |
| Indexers, release searches, RSS sync | Real or mock indexers, such as a mock Newznab or Torznab server |
| Download clients, the queue, grabbing releases | A download client, such as a SABnzbd or qBittorrent container |
| Episodes with files, manual import, renaming, media info | Media files under `/tv` |
| Notifications and connections | External services or mock receivers |
| Running the sandbox in CI | A GitHub Actions workflow with Docker |
