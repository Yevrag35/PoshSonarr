# Threading

## Sending requests synchronously

The HTTP clients and their handlers send each request synchronously, through `HttpClient.Send`, and nothing on that path awaits or blocks on a task. A cmdlet sends on its pipeline thread, so the handlers write through the cmdlet's `WriteVerbose` and `WriteDebug`, which PowerShell lets only that thread call. Nothing on the path may post work to the caller's `SynchronizationContext` and wait for it, since a GUI host's pipeline thread runs that work only when it's free, which it isn't while it sends. `B1_ARequestFinishesUnderAContextThatNeverRunsPostedWork` in `src/MG.Sonarr.Next.Tests/Http/SonarrHttpClientTests.cs` checks every send method for it.

- A delegating handler overrides `Send` only. `HttpClient.Send` calls each handler's `Send`, and `DelegatingHandler.Send` passes the request to the inner handler without calling the handler's own `SendAsync`, so code in a `SendAsync` override never runs for the module's requests.
- A handler or the response reader reads and buffers a body with the synchronous helpers in `src/MG.Sonarr.Next/Services/Http/Extensions/HttpBodyExtensions.cs`, never with `ReadAsStringAsync`, `ReadFromJsonAsync`, or `LoadIntoBufferAsync` and a block on the task. A synchronous read takes no token, so each helper disposes the content when the request's token is canceled, which ends a read that waits on the server.
- A client method sends with `HttpClient.Send` and passes its token to the send and to every body read, and a cmdlet that calls a client or a service itself passes its `PipelineStopToken`, so Ctrl+C ends the request.

## Shared state

Thread safety matters even in a cmdlet: PowerShell can run the module's cmdlets on several threads at once, as `ForEach-Object -Parallel` and thread jobs do, and every runspace in the process shares the module's static state, the connection's `ServiceProvider` and every static cache included.

Before you choose between one `System.Threading.Lock` and a `System.Collections.Concurrent` collection, consider contention, how often the state is touched, and the cost of taking the lock. Finding one pattern or the other in the codebase doesn't make it the right choice for your case.
