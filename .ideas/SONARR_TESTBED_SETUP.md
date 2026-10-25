Set up a repeatable local Sonarr environment for testing my C# API client, using the supplied Dockerfile. Docker is already installed and running. Implement the setup and verify it works.

Use PowerShell for host-side commands and scripts so I can operate everything from Windows.

1. Project layout and isolation

Create the harness under `testbed/sonarr/`, following existing repository conventions where appropriate. Include the Dockerfile, `compose.yaml`, configuration/bootstrap files, PowerShell scripts, and a README.

Use a dedicated Compose project and project-scoped named volume for `/config`. Mount no existing Sonarr configuration or real media folders. All reset operations must affect only this test environment.

Publish Sonarr as `127.0.0.1:18989:8989`, with a configurable host port.

2. Image

Keep Sonarr pinned to `4.0.20.3014` and disable application self-updates. Preserve execution as the non-root `sonarr` user.

The supplied archive is Linux x64, so explicitly use `linux/amd64` for the build and container.

Verify the release download against its published SHA-256 checksum. Make download or extraction failures fail the build reliably.

Create an empty writable `/tv` directory for API root-folder tests. Ensure UID/GID 1000 can write to both `/config` and `/tv`.

3. Automatic configuration

Make startup unattended, including after a complete reset. No browser configuration or manual copying of an API key should be required.

Use this fixed, disposable test API key:

`0123456789abcdef0123456789abcdef`

Verify the supported configuration mechanism against the pinned Sonarr version. Use supported environment variables or initialize `/config/config.xml` before starting Sonarr. Do not assume environment variables provided by third-party Docker images exist in this custom image.

Keep API-key authentication enforced. Missing or incorrect API keys must receive HTTP 401 on protected API endpoints.

If bootstrap scripts are necessary, ensure Linux scripts have LF line endings and launch Sonarr using `exec` so Docker shutdown signals reach it.

4. Readiness

Add a Docker health check using an authenticated request to:

`GET http://127.0.0.1:8989/api/v3/system/status`

The host-side start command must wait for readiness with a bounded timeout. Verify successful JSON output and the expected Sonarr version, rather than merely checking whether the port is open.

On startup failure, show useful container logs and return a nonzero exit code. Do not use a fixed sleep as the readiness mechanism.

5. Repeatable baseline

After readiness, seed a small baseline through Sonarr's API:

- A tag named `testbed-baseline`.
- A root folder at `/tv`.

Make seeding idempotent. Obtain resource IDs from API responses rather than hard-coding them.

Keep baseline setup and smoke tests independent of external metadata services, indexers, download clients, and real media. Document which additional tests would require those dependencies.

6. Lifecycle commands

Provide straightforward PowerShell commands for:

- Start: build if needed, start Sonarr, wait for readiness, and ensure baseline fixtures exist.
- Stop: stop the environment while preserving its state.
- Reset: stop/remove the test containers and test volumes, recreate everything, wait for readiness, and seed the baseline.
- Test: execute the smoke tests.
- Logs: display or follow container logs.

Resolve file paths relative to the scripts so commands work from any current directory. Check Docker command exit codes and propagate failures. Avoid automatic container restarts that could conceal crashes during testing.

7. Verification

Run and verify this acceptance sequence:

- Start from an empty environment.
- Confirm authenticated system status returns the pinned version.
- Confirm missing and incorrect keys return HTTP 401.
- Confirm baseline fixtures exist.
- Create, retrieve, update, and delete a temporary tag through the API.
- Create another temporary tag and confirm it survives Stop followed by Start.
- Reset the environment.
- Confirm the temporary tag is gone, baseline fixtures have been restored, and the fixed API key still works.
- Run the smoke tests again successfully.

Use the actual HTTP responses to diagnose failures. Do not report checks as passing unless they were executed successfully.

8. Documentation and handoff

Document exact build/start/stop/reset/test commands and provide a PowerShell API-call example.

Document these connection values for my client:

- Server URL: `http://127.0.0.1:18989`
- API prefix: `/api/v3`
- Authentication header: `X-Api-Key`
- API key: `0123456789abcdef0123456789abcdef`

Explain how to change the host port and Sonarr version. Keep generated databases and logs out of source control.

Finish by reporting the files created, commands executed, verification results, and any unresolved issues. Leave the working test instance running.