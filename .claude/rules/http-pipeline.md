---
paths:
- "src/MG.Sonarr.Next/Services/Http/**/*.cs"
- "src/MG.Sonarr.Next/Services/Jobs/ApiCmdletQueue.cs"
- "src/MG.Sonarr.Next/Exceptions/SonarrHttpException.cs"
- "src/MG.Sonarr.Next/Exceptions/SonarrErrorRecord.cs"
- "src/MG.Sonarr.Next.Shell.Bases/Cmdlets/SonarrApiCmdletBase.cs"
- "src/MG.Sonarr.Next.Shell.Bases/Cmdlets/TimedCmdlet.cs"
- "src/MG.Sonarr.Next.Shell.Bases/Context/CmdletContext.cs"
- "src/MG.Sonarr.Next.Shell/Cmdlets/Connection/ConnectSonarrInstanceCmdlet.cs"
---

# HTTP pipeline

Every request the module makes to Sonarr's API goes through `ISonarrClient`, which `SonarrHttpClient` implements in `src/MG.Sonarr.Next/Services/Http/Clients/`. The client builds a request message, sends it through a chain of delegating handlers, and has `IResponseReader` turn the response into a `SonarrClientResult` or `SonarrClientResult<T>`. No exception leaves the client: a failure comes back as a result whose `IsError` is `true` and whose `Error` is a `SonarrErrorRecord`.

## Registration

`SonarrClientDependencyInjection`, at the bottom of `SonarrHttpClient.cs`, registers the client. `SonarrContext.Initialize` in `src/MG.Sonarr.Next.Shell.Bases/Context/CmdletContext.cs` calls `AddSonarrClient` once per connection, in the container that also holds the singleton `ApiCmdletQueue` and the memory cache where `AuthHandler` keeps its login cookie.

- `AddHttpClient<ISonarrClient, SonarrHttpClient>` makes the client a typed client of `IHttpClientFactory`. Its `HttpClient` takes `IConnectionSettings.ServiceUri` as the base address and `IConnectionSettings.Timeout` as the timeout, and sends the API key in the `X-Api-Key` header and `PoshSonarr-Next/2.0.0` as the user agent on every request.
- `ISonarrClient` resolves as a transient, with a new `HttpClient` each time, but the factory reuses one handler chain until its handler lifetime runs out, two minutes by default. A handler that needs `IConnectionSettings` reads it in its constructor, so it sees the settings as they were when its chain was built. Every request and cmdlet in that window shares the chain, so a handler keeps no per-request or per-cmdlet state in its fields.
- The same registration adds the metadata tags, the singleton `IResponseReader`, the `IConnectionSettings` instance, `ISonarrJsonOptions`, and the download client.
- `AddSonarrNonPSClient` registers the client for a host outside PowerShell, with only `PathHandler` and `TestingHandler` in its chain.
- A new delegating handler is registered as a transient beside the others and added with `AddHttpMessageHandler<T>()` at its place in the chain.

## Handler chain

`AddSonarrClient` adds the handlers in this order. The first one added is the outermost: it sees the request first and the response last.

1. `PathHandler` prefixes the endpoint path with `/api/v3/`, or with `/v3/` when `-NoApiInPath` sets `IConnectionSettings.NoApiInPath`. Callers pass the path relative to the API, as the paths in `Constants` are, such as `/series` or `/system/status`. The handler leaves alone a path that already starts with `api/v3`, an `AuthedRequestMessage`, and a request whose options hold the `PathHandler.Ping` key. The path and query keep the escaping the caller gave them.
2. `VerboseHandler` takes the running cmdlet off the `ApiCmdletQueue`. When the cmdlet's `CanWriteVerbose` is `true`, the handler calls its `WriteVerboseBefore` with the request before sending it, and its `WriteVerboseAfter` with a result that holds the response's status code and URL afterward. The handler puts the cmdlet back on the queue for the inner handlers and removes it when the request finishes, so each entry on the queue covers one request.
3. `DebugSerializeHandler` peeks at the queued cmdlet and writes payloads to its debug output through `WriteDebugPayload`: the request body before sending when `CanDebugSerializeBefore` is `true`, and the response body after receiving, as `HTTP Response (size in bytes: <length>) -> <body>`, when `CanDebugSerializeAfter` is `true`.
4. `TestingHandler` acts only on a request whose `SonarrRequestMessage.IsTest` is `true`, the `TestRequestMessage` that `SendTest` sends. When that request succeeds with a body that starts like an HTML or XML page (`<html`, `<?xml`, or `<!doctype`) or is shorter than five bytes, the URL doesn't point at Sonarr's API, so the handler swaps the response for a 503 whose JSON body's `message` says so. Otherwise it puts the buffered body back.
5. `ErrorHandler` acts on a non-success response. A 404 passes through unchanged, with the `ErrorHandler.Is404` key added to its request's options for the response reader. Any other error response with a JSON content type has its body parsed into a server error, which goes into the request's options under `ErrorHandler.ErrorKey`. Whatever the body holds, one error object or the array of validation failures that Sonarr returns for a 400, the response reaches the response reader with its body intact.

`SonarrClientHandler`, the primary handler, sends through the proxy from `IConnectionSettings.TryGetProxy`, or answers a server's authentication challenge with the user's default credentials when there's no proxy. It accepts any server certificate when `-SkipCertificateCheck` sets `IConnectionSettings.SkipCertValidation`.

A delegating handler follows these rules:

- It overrides both `Send` and `SendAsync`. The synchronous client methods send with `HttpClient.Send`, which calls each handler's `Send`, and `DelegatingHandler.Send` passes the request to the inner handler without calling the handler's own `SendAsync`, so a handler that overrides only `SendAsync` is skipped on that path (measured 2026-10-25 on .NET 10.0.14). A handler with no synchronous logic of its own implements `Send` as `this.SendAsync(request, cancellationToken).GetAwaiter().GetResult()`.
- When it reads a response body, it puts a buffered copy back as the response's content, with the original content headers copied over by `HttpContentHeaderExtensions.CopyTo`, so the handlers outside it and the response reader can read the body again.
- Flags for one request travel in the request's `Options`. `HttpResponseMetadataExtensions` reads and writes them from a response, through its `RequestMessage`.

## Request messages

Every request is a `SonarrRequestMessage`, from `src/MG.Sonarr.Next/Services/Http/Requests/`.

- It keeps the path the caller passed as `OriginalRequestUri`, and it opens a DI scope that it disposes with itself. As an `IHttpRequestDetails`, it gives `WriteVerboseBefore` the method, the URL, and that scope's services.
- `ApiKeyRequestMessage` carries API calls, `TestRequestMessage` carries `SendTest`, and `AuthedRequestMessage` carries a download that may need credentials.
- A request message, with its content and its scope, stays alive until the response has been read, so a method that disposes its request with `using` awaits the send first. In a method that returns the send's `Task` without awaiting it, `using` disposes the request when the method returns, while the request is still in flight, and a handler that reads the body after that throws `ObjectDisposedException` (measured 2026-10-25 on .NET 10.0.14).

## SonarrHttpClient

`SonarrHttpClient.cs` holds the type's fields, its constructor, its synchronous methods, and the registration class, and `SonarrHttpClient-Async.cs` holds its asynchronous methods. `ISonarrClient` is split the same way, with each member declared in the file that implements it.

- The constructor takes the typed `HttpClient`, `IConnectionSettings`, `ISonarrJsonOptions`, `IMetadataResolver`, `IResponseReader`, and the `IServiceScopeFactory` that each request message opens its scope from.
- `SendDelete`, `SendGet<T>`, `SendPost`, and `SendPut`, each with an `Async` twin, create an `ApiKeyRequestMessage` for the relative path, serialize a body as JSON with `ISonarrJsonOptions.ForSerializing`, send the request, and hand an `HttpCall` (the path, the request, and the response) to `IResponseReader`: `ReadNoResultAsync` for the methods that return no data, and `ReadResultAsync<T>` for the rest.
- The response is disposed before the method returns, so the reader takes what the result needs from it first. A `SonarrHttpException` in an error result still holds that response in `Response`, so code that reads the error later uses the exception's own `StatusCode`, `ReasonPhrase`, and `Headers`, which it copied, and never the response's content.
- `SendGet<T>` and `SendPost<TBody, TOutput>` set the metadata tag on a value that is `IJsonMetadataTaggable`, with `SetTag(IMetadataResolver)`. `SendGet<T>` also sorts a value that is `ISortable` and holds more than one item.
- An exception becomes an error result. A timeout, which `HttpClient` reports as a `TaskCanceledException` whose inner exception is a `TimeoutException`, gets the category `OperationTimeout`, and any other cancellation gets `OperationStopped`. An `HttpRequestException` is wrapped in a `SonarrHttpException`, along with any server error parsed from the response. When the exception gives no request URL, the result's `RequestUrl` falls back to the relative path.
- `SendTest` is how `Connect-SonarrInstance` checks the URL and API key. It sends `GET /system/status` as a `TestRequestMessage`, and on success it reads the server's authentication type from the status and stores it in `IConnectionSettings.AuthType`. An error status becomes an `HttpResponseException` with the `message` from the JSON body and the category `ResourceUnavailable`, and an exception, such as one from a body that isn't JSON, becomes a `ConnectionError`. With `-PassThru`, `Connect-SonarrInstance` gets the status through `SendGetAsync<SystemStatusObject>` instead, writes it out, and stores the authentication type itself.
- `SendPingAsync` sends `HEAD /ping` with the `PathHandler.Ping` key in its options, so the path stays at the server's root. It turns a 404, 403, 401, or 5xx response into `ObjectNotFound`, `PermissionDenied`, `AuthenticationError`, or `ConnectionError`, and any other failure into `InvalidResult`.
- The `Send*Request` helpers on `SonarrApiCmdletBase` call the asynchronous methods and block on them with `GetAwaiter().GetResult()`. `SendTest`, `CommandTracker`, and `TestingService` use the synchronous ones. Both kinds run through the same handlers.

## Response reader

`SonarrResponseReader`, the singleton `IResponseReader`, turns an `HttpCall` into a result.

- A call with no response becomes an `EmptyHttpResponseException` error with the category `InvalidResult`.
- In `ReadResultAsync<T>`, a request marked with `ErrorHandler.Is404` becomes `SonarrClientResult.NotFound<T>()`, an ignorable `ObjectNotFound` error with the status 404.
- A status below 400 is a success. `ReadResultAsync<T>` deserializes the body as `T` with `ISonarrJsonOptions.ForDeserializing`, and a body that is `null` or can't be deserialized becomes a `ParserError` error.
- Any other status becomes a `SonarrHttpException` that holds the server's errors, read from a body that is one error object or an array of them, such as Sonarr's validation failures. A `SonarrErrorRecord` wraps it, with the category that `SonarrErrorRecord.GetCategoryFromStatusCode` picks for the status code, such as `InvalidData` for 400, `AuthenticationError` for 401 and 403, and `ObjectNotFound` for 404, which also makes the record ignorable.

A result carries `StatusCode`, `RequestUrl`, `IsError`, and `Error`, and `SonarrClientResult<T>` adds `Value`, which isn't `null` when `IsError` is `false`. `IsIgnoreable` is `true` for a status below 400 and for 404. A result that `SonarrClientResult.FromException` builds takes the status code its caller passes, which is `ErrorHandler.NoResponseCode`, 600, when there's no response.

## The cmdlet queue

The handlers reach the cmdlet that sent a request through the singleton `ApiCmdletQueue` in `src/MG.Sonarr.Next/Services/Jobs/`, which holds `IApiCmdlet` instances.

- The sender enqueues the cmdlet once, immediately before each request. The `Send*Request` helpers on `SonarrApiCmdletBase` do that and start the timer that `TimedCmdlet.WriteVerboseAfter` reports as the request's elapsed time. Code that calls `ISonarrClient` or `ISonarrDownloadClient` itself enqueues the cmdlet first, and a request sent with nothing on the queue writes no verbose or debug output.
- Cmdlets resolve the queue as `ApiCmdletQueue`, the only queue type the container registers. `SonarrCmdletBase` clears it when a cmdlet ends or is stopped, so a leftover entry can't take a later request's output.
- A handler calls `WriteVerboseAfter` and `WriteDebugPayload` after an `await`, often on a thread-pool thread, so an `IApiCmdlet` writes through `Host.UI.WriteVerboseLine` and `Host.UI.WriteDebugLine`. Off the pipeline thread, `Cmdlet.WriteVerbose` throws a `PSInvalidOperationException`, and a line written through `Host.UI` goes straight to the host, so `4>$null` doesn't hide it and `4>&1` doesn't capture it (measured 2026-10-25 on PowerShell 7.6.9).

## Download client

`ISonarrDownloadClient`, in `SonarrDownloadClient.cs`, saves a file that Sonarr serves, such as a backup or a log file, to a path. `AddSonarrClient` registers it as well.

- Its handler chain is `VerboseHandler`, `DebugSerializeHandler`, and `AuthHandler`, in that order, in front of `SonarrClientHandler`. It has no `PathHandler`, so a download URL goes out as Sonarr gave it, relative to the base address.
- `AuthHandler` adds credentials when `IConnectionSettings.AuthType` says the server needs them: an `Authorization: Basic` header, or for forms authentication, the cookie from a login that it caches for a day. It takes them from an `AuthedRequestMessage`, which the download overload with a `NetworkCredential` sends, and without them the download fails with a `SonarrHttpException`.
