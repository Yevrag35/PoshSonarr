# Debugging PoshSonarr

`Debug.ps1` in this folder opens a PowerShell session on a Debug build of the module, for debugging it by hand. It imports the module, loads the format and type files, and connects to a Sonarr instance: the development instance that the repository's `.env` file names, unless the script's parameters name another.

## Requirements

- A .NET 11 SDK for the build.
- PowerShell 7.7 or later, since the module targets .NET 11.

## Running it

A Debug build copies `Debug.ps1` into the project's output folder, beside the module's assemblies, and a Release build leaves it out, so the packaged module doesn't carry it. Run the copy in the output folder, from the repository root:

```powershell
dotnet build PoshSonarr.slnx -c Debug
pwsh -NoExit -File src/MG.Sonarr.Next.Shell/bin/Debug/net11.0/Debug.ps1
```

A Debug build writes no module manifest, so the script imports `MG.Sonarr.Next.Shell.dll` from its own folder and loads the format and type files in `PSFormats/` and `PSTypes/` beside it itself. It also changes to your desktop folder, and last, it connects with `Connect-SonarrInstance -PassThru`, which shows the server's status. `-NoExit` keeps the session open afterward. The copy in this folder has no DLL beside it, so it stops with an error.

The project's Visual Studio launch profiles, in `Properties/launchSettings.json`, run the script the same way: `Pwsh7` with the PowerShell installed in `$env:PROGRAMFILES\PowerShell\7`, and `Pwsh-Preview` with the one in `$env:PROGRAMFILES\PowerShell\7-preview`. Use the one whose PowerShell is 7.7 or later.

## Connection settings

`.env` at the repository root holds the development instance's settings as `KEY=value` lines. Git ignores the file, so the instance's real API key goes in it. On a machine without one, copy `.env.example` to `.env` and fill in the key.

| Setting | What it holds |
|---|---|
| `SONARR_URL` | The instance's URL, such as `http://localhost:8989`. |
| `SONARR_API_KEY` | The instance's API key. |
| `SONARR_NO_API_IN_PATH` | `true` when a reverse proxy in front of the instance removes `/api` from the path, which makes the script connect with `-NoApiInPath`. Any other value, or none, keeps `/api` in the path. |

A line that starts with `#` is a comment, and the script drops the quotes around a value in single or double quotes.

## Parameters

A parameter replaces only its own setting from `.env`, so the settings that no parameter replaces still come from the file.

| Parameter | What it does |
|---|---|
| `-SonarrUrl <url>` | Connects to this URL instead of the one in `SONARR_URL`. |
| `-ApiKey <key>` | Connects with this API key instead of the one in `SONARR_API_KEY`. |
| `-NoApiInPath` | Leaves `/api` out of the path, whatever `SONARR_NO_API_IN_PATH` holds. `-NoApiInPath:$false` keeps it in, even when the file sets `true`. |
| `-LibraryName <name>` | Imports `<name>.dll` from the script's folder instead of `MG.Sonarr.Next.Shell.dll`. |

When the script ends up without a URL or an API key, it warns and skips connecting, and the session opens with the module imported but not connected.

To debug against the Docker sandbox in `sandbox/sonarr/` instead of the development instance, pass the URL and API key that `sandbox/sonarr/start.ps1` prints, and `-NoApiInPath:$false`, since nothing in front of the sandbox removes `/api`.
