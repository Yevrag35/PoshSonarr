# Threading

## Awaiting in the HTTP client

The HTTP client and its handlers await with `ConfigureAwait(false)`. The `Send*Request` helpers on `SonarrApiCmdletBase` block the pipeline thread on those calls with `GetAwaiter().GetResult()`, so a continuation that waited to resume on a captured context could deadlock under a host that installs one.

## Shared state

Thread safety matters even in a cmdlet: PowerShell can run the module's cmdlets on several threads at once, as `ForEach-Object -Parallel` and thread jobs do, and every runspace in the process shares the module's static state, the connection's `ServiceProvider` and every static cache included.

Before you choose between one `System.Threading.Lock` and a `System.Collections.Concurrent` collection, consider contention, how often the state is touched, and the cost of taking the lock. Finding one pattern or the other in the codebase doesn't make it the right choice for your case.
