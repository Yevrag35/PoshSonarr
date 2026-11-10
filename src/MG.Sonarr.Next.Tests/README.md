# PoshSonarr's C# suite

The xUnit.net v3 suite in this folder tests PoshSonarr's libraries in-process: `MG.Sonarr.Next`, and the parts of `MG.Sonarr.Next.Shell.Bases` that every cmdlet runs on, such as the connection and the cmdlet base classes. A fake Sonarr answers each request inside the test process, so the suite needs no network and no Sonarr instance. The Pester suite in `.tests/` tests what the cmdlets do, against the Docker sandbox.

## Requirements

- A .NET 11 SDK. The project references PowerShell's assemblies as packages, so the suite needs no installed PowerShell.

## Running the suite

Run these from the repository root.

| Command | What it does |
|---|---|
| `dotnet test --solution PoshSonarr.slnx` | Builds the solution and runs every test. |
| `dotnet test --solution PoshSonarr.slnx --no-build` | Runs the tests of the last build. |
| `dotnet test --solution PoshSonarr.slnx --filter-class '*WildcardTests'` | Runs the test classes whose fully qualified names match. `--filter-method` does the same for test methods, as in `--filter-method '*B36_*'`. Each takes a name with `*` at its start, its end, or both. |
| `dotnet test --solution PoshSonarr.slnx --culture tr-TR` | Runs every test under the culture that it names. |

A run exits 0 when every test passes, 2 when a test fails, 5 when the test app turns away an option, and 8 when no test ran, as when a filter matches nothing.

The root `global.json` puts `dotnet test` into Microsoft.Testing.Platform mode, where it passes each option that it doesn't list itself to the test app. The app turns away an option that it doesn't have before any test runs, with exit code 5 and without naming the option. `--no-incremental` is one, so for a full rebuild, run `dotnet build PoshSonarr.slnx --no-incremental` and then `dotnet test --solution PoshSonarr.slnx --no-build`. `dotnet test --help`, run from the repository root, lists every option that a run takes, the test app's own included.

## What belongs in the suite

The suite covers the module's code that isn't a cmdlet: `MG.Sonarr.Next`, and the parts of `MG.Sonarr.Next.Shell.Bases` that every cmdlet runs on. What a cmdlet does belongs to the Pester suite.

- A test has to be worth its upkeep, so not every fixed bug gets one. Tests go first to rules whose bugs reached users and whose code only a test can guard: hot code tuned for speed, such as the JSON converter and the wildcard matcher; behavior that depends on the culture, the threading context, or a failure that no one can bring about by hand; and the shared bases that every cmdlet runs on. A rule that no code holds now, or code that nothing calls, rarely earns a test.
- A test of a fixed bug fails with the bug put back. Check that by putting the bug back into the current code, since the code from before the fix may no longer build with the test.
- A test of behavior that no bug calls for covers `MG.Sonarr.Next` alone.
- The source generators in `MG.Sonarr.Next.Generators` get no tests and no test project: a generator only generates source, and one that would need tests is doing too much.

## How the project is set up

- xUnit.net v3 brings its own runner on Microsoft.Testing.Platform, so the project builds as an executable, with `OutputType` set to `Exe`, and references `xunit.v3` with no `Microsoft.NET.Test.Sdk` or `xunit.runner.visualstudio`. Those two run tests through VSTest, and the root `global.json` runs `dotnet test` on Microsoft.Testing.Platform instead. They add nothing that the suite uses, though they don't break it either: on the .NET 11 SDK, a project with `xunit.v3` and either or both of them still built and passed its tests through `dotnet test`, with and without `--solution` (measured 2026-11-10 with SDK 11.0.100 and `xunit.v3` 4.0.2).
- The project references `MG.Sonarr.Next.Shell.Bases`, which brings `MG.Sonarr.Next`, and both name this assembly in their `InternalsVisibleTo`. It doesn't reference the source generators: they would inject their attributes into this assembly too, and CS0436 would warn on each, since this assembly also sees `MG.Sonarr.Next`'s internal copies of them.
- The models are PSObjects and the cmdlet tests open runspaces, so the project references `System.Management.Automation` and `Microsoft.PowerShell.Commands.Utility` itself, which brings back the runtime assets that `MG.Sonarr.Next` keeps out of the projects that reference it.
- The project's global usings cover xUnit.net, CsCheck, the `Infrastructure` namespace, and the PowerShell and .NET namespaces that many tests use, so a test file names only the namespaces that few tests need, such as the library's own.
- A module initializer in `Infrastructure/AssemblySetup.cs` replaces `Trace.Listeners` with a listener whose `Fail` throws, so a failed `Debug.Assert` in the module fails the test that reached it rather than ending the test process.
- The build copies `Fixtures/*.json` beside the test assembly, where the tests read them. They're the responses that the sandbox's Sonarr gave to a GET of each model that a request sends back, so they hold every field that Sonarr sends and none that it doesn't.

## What Infrastructure/ holds

| File | What it holds |
|---|---|
| `AssemblySetup.cs` | The module initializer that makes a failed `Debug.Assert` throw. |
| `FakeSonarr.cs` | `FakeSonarr`, which stands in for a Sonarr instance, and `Respond`, which builds its responses. |
| `ModuleServices.cs` | `ModuleServices.Build`, which builds a connection's services for a `FakeSonarr` through the module's own builder, and the JSON options and metadata resolver of a provider that every test shares. |
| `TestCmdlets.cs` | `PowerShellHost`, which opens a runspace that holds the test cmdlets and runs commands in it, the `Connection` collection, and the test cmdlets, which run on the module's base classes. |
| `Probes.cs` | `ProbeLog`, the probes that log when a connection's provider or a scope on it is disposed, and `ProbeGate`, which holds a cmdlet in its `Begin` step until the test releases it. |
| `BlackHoleSynchronizationContext.cs` | A `SynchronizationContext` that never runs what's posted to it, as a GUI host's pipeline thread doesn't while a cmdlet blocks on a task. |
| `CultureScope.cs` | `CultureScope`, which sets the current culture until it's disposed, and `CultureScope.Names`: the invariant culture, en-US, and the cultures that broke the module's reading and writing of text. |
| `ModelDescription.cs` | Describes what PowerShell shows of an object, so a test can compare a model before and after a request that sends it. |

## Writing a test

Each file covers one type or one behavior, is named for it with `Tests` at the end, such as `WildcardTests.cs` or `SentBackBodyTests.cs`, and sits in the folder named for the part of the library that it tests, such as `Json/`, or `Http/` for `Services/Http/`. `Shell/` holds the tests of `MG.Sonarr.Next.Shell.Bases`.

A test's name says what the code does, in the present tense, such as `HoldsTheTextThatAStringBuilderHoldsAfterTheSameChanges`. A test that guards a fixed bug starts its name with the bug's ID, such as `O23_` or `B117_`, and its summary starts with the ID and states the rule that the code follows rather than what the bug did.

### Cancellation tokens

- Pass `TestContext.Current.CancellationToken` to every call that takes a `CancellationToken`, never `default`, `CancellationToken.None`, or nothing, since xUnit1051 warns otherwise and the suite builds without warnings. A test that needs a canceled token cancels a `CancellationTokenSource` of its own and passes its token.
- A helper that several tests call takes no optional token parameter, which would make every call that leaves it out warn. It passes `TestContext.Current.CancellationToken` itself, or, when its code runs on another thread, such as a cmdlet's pipeline thread, takes the test's token as a required parameter.

### Awaiting and blocking

- A test method awaits without `ConfigureAwait(false)`, as xUnit1030 asks, and `.editorconfig` in this folder turns off CA2007, which would ask for it. Helper code that runs under a `SynchronizationContext` of its own, such as a fake handler under `BlackHoleSynchronizationContext`, still calls `ConfigureAwait(false)` where the context matters.
- A test that blocks on a task, as a cmdlet does, blocks in a helper, such as the ones in `Http/ClientCalls.cs`, since xUnit1031 warns about a blocking call in a test method.

### Fake Sonarr instances

- A test of the HTTP client builds a connection's services with `ModuleServices.Build`, which goes through the module's own builder and puts a `FakeSonarr` in as the primary handler of both typed clients, `ISonarrClient` and `ISonarrDownloadClient`. Every handler of the module still runs above it, and no request leaves the process.
- A fake's response function honors the request's token, as a network handler does: .NET's synchronous `HttpClient.Send` buffers a response's body under a registration that disposes the body when the token is canceled, so a fake that ignores a token that's already canceled hands the client a disposed body.
- A fake's response carries its request, as one from the network does, since the module's handlers keep their flags for a request in its options and reach them through the response. `Respond` sets it on every response that it builds.

### The connection and PowerShell pipelines

- The module's connection is static and every runspace in the process shares it, so a test that connects, with `Connect-FakeSonarr` in a runspace from `PowerShellHost.Create`, goes in the `Connection` collection, which runs apart from every other test, and disconnects in a `finally` block.
- Enumerating a stream of a pipeline that's still running, such as `PowerShell.Streams.Error` while a pipeline from `BeginInvoke` or `InvokeAsync` runs, blocks until the pipeline ends, as `string.Join` over the stream does. So a test reads a running pipeline's streams only after it ends, and never in an assertion's message before then, since C# builds the message whether or not the assertion fails.

### Property-based tests

- Property-based tests use CsCheck 5.x. `Sample` runs its checks on every logical CPU by default, so a property must be safe to run on several threads at once.
- `Sample`'s threads keep the test's culture and `TestContext.Current`, so `--culture` reaches every case that a property checks, and code inside a property can pass `TestContext.Current.CancellationToken`.
