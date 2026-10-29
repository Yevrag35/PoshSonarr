# Sonarr testbed

A disposable Sonarr instance in Docker for testing PoshSonarr against. It runs Sonarr 4.0.20.3014, pinned and unable to update itself, listens only on this machine's loopback address, and starts unattended with a fixed API key. Its data lives in two Docker volumes that belong to it alone, so nothing it does reaches another Sonarr instance or real media, and a reset returns it to a known baseline.

## Requirements

- Docker Desktop, running Linux containers.
- PowerShell 7.6 or later.

## Commands

Run these from the repository root. Each works from any other directory too.

| Command | What it does |
|---|---|
| `pwsh -NoProfile -File testbed/sonarr/start.ps1` | Builds the image if the Dockerfile or `compose.yaml` changed, starts Sonarr, and waits for its health check. Then it checks from the host that the port answers with the pinned version and the instance name `Sonarr Testbed`, creates whatever the baseline lacks, and prints the connection values. `-TimeoutSeconds`, 120 by default, bounds the wait for the health check, and building the image doesn't count against it. |
| `pwsh -NoProfile -File testbed/sonarr/stop.ps1` | Stops Sonarr and keeps the container and its volumes, so the next start brings back the same data. |
| `pwsh -NoProfile -File testbed/sonarr/reset.ps1` | Removes the container and both volumes, and with them everything Sonarr stored, then does everything `start.ps1` does. It takes the same `-TimeoutSeconds`. |
| `pwsh -NoProfile -File testbed/sonarr/logs.ps1` | Shows the last 200 lines of Sonarr's log. `-Tail <count>` shows another number, and `-Follow` keeps streaming new lines until Ctrl+C. |

Each script exits 1 when something fails, and a failure while starting first shows the container's state and the last 200 lines of its log.

The baseline is what a new Sonarr install holds, plus a tag labeled `testbed-baseline` and a root folder at `/tv`. `start.ps1` creates whichever of the two is missing and changes nothing else, so it's safe to run again, and whatever you create stays until `reset.ps1`.

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

With PoshSonarr imported, this connects the module to the testbed. Leave off `-NoApiInPath`, because nothing in front of the container removes `/api`:

```powershell
Connect-SonarrInstance -Url http://127.0.0.1:18989 -ApiKey 0123456789abcdef0123456789abcdef
```

To point an interactive debugging session at the testbed instead of the development instance, pass the testbed's values to `Debug.ps1` after a Debug build:

```powershell
pwsh -NoExit -File src/MG.Sonarr.Next.Shell/bin/Debug/net10.0/Debug.ps1 -SonarrUrl http://127.0.0.1:18989 -ApiKey 0123456789abcdef0123456789abcdef -NoApiInPath:$false
```

Each of the script's parameters overrides only its own setting in the repository's `.env` file, so `-NoApiInPath:$false` keeps a `SONARR_NO_API_IN_PATH=true` line, written for a development instance behind a reverse proxy, from removing `/api` from the testbed's paths.

## Changing the port

The testbed publishes port 18989 unless `SONARR_TESTBED_PORT` names another. Set it in the shell, or put a `SONARR_TESTBED_PORT=<port>` line in `testbed/sonarr/.env`, which git ignores and which Compose reads whatever directory a command runs from. A value set in the shell wins over the file's. The scripts get the port from Compose, so their checks and the values that `start.ps1` prints follow it, and the next `start.ps1` recreates the container on the new port with its volumes intact.

```powershell
$env:SONARR_TESTBED_PORT = '18990'
pwsh -NoProfile -File testbed/sonarr/start.ps1
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
docker compose --project-name poshsonarr-testbed --file testbed/sonarr/compose.yaml ps --all
docker compose --project-name poshsonarr-testbed --file testbed/sonarr/compose.yaml exec sonarr sqlite3 /config/sonarr.db .tables
```

From Git Bash, set `MSYS_NO_PATHCONV=1` first, or Git Bash rewrites container paths such as `/config/sonarr.db` into Windows paths.

## Troubleshooting

- **The port doesn't bind.** Windows reserves ranges of TCP ports, for Hyper-V and WSL among others, and the ranges differ from machine to machine and can change after a restart. When 18989 falls in one, starting fails with "Ports are not available" or "An attempt was made to access a socket in a way forbidden by its access permissions". This lists the ranges, and a port outside all of them goes in `SONARR_TESTBED_PORT`:

	```powershell
	netsh interface ipv4 show excludedportrange protocol=tcp
	```

- **Docker isn't running.** A command that fails with "failed to connect to the docker API" needs Docker Desktop started.
- **Sonarr doesn't become healthy.** `logs.ps1` shows what Sonarr reported. A slow machine may need a longer `-TimeoutSeconds`.

## What the testbed can't test

These need services that the testbed leaves out on purpose.

| Tests of | Need |
|---|---|
| Series, episodes, the calendar, and wanted/missing | Sonarr's metadata service, SkyHook, on the internet. Adding a series fetches it from there, so the results follow live data. |
| Indexers, release searches, RSS sync | Real or mock indexers, such as a mock Newznab or Torznab server |
| Download clients, the queue, grabbing releases | A download client, such as a SABnzbd or qBittorrent container |
| Episodes with files, manual import, renaming, media info | Media files under `/tv` |
| Notifications and connections | External services or mock receivers |
| PoshSonarr's cmdlets, through Pester | A working Pester suite in `.tests/` |
| Running the testbed in CI | A GitHub Actions workflow with Docker |
