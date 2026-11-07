using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Services.Http.Clients;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace MG.Sonarr.Next.Tests.Http;

/// <summary>
/// Tests the HTTP clients that every request goes through, with the module's handler chains above a fake Sonarr.
/// </summary>
public sealed class SonarrHttpClientTests
{
	/// <summary>
	/// The ways that a request fails.
	/// </summary>
	public enum Failure
	{
		/// <summary>No response arrives, as for a refused or dropped connection.</summary>
		NoResponse,
		/// <summary>The request outlasts the connection's timeout.</summary>
		Timeout,
		/// <summary>The caller's token is canceled, as Ctrl+C cancels a cmdlet's.</summary>
		Canceled,
		/// <summary>The response arrives, and the file that it holds can't be written, as in a folder that doesn't exist.</summary>
		Unwritable,
	}

	/// <summary>
	/// B75, B74, O26, B78, B76: no send method throws for a request that fails before a response arrives. One that gets
	/// no response gives the category <see cref="ErrorCategory.ConnectionError"/> and the request's absolute URL, a
	/// timeout gives <see cref="ErrorCategory.OperationTimeout"/>, and any other cancellation gives
	/// <see cref="ErrorCategory.OperationStopped"/>, with the relative path, since neither exception carries a URL,
	/// whichever method sends the request and whether or not it returns a value.
	/// </summary>
	/// <param name="method">The send method, by its name in <see cref="ClientCalls.ApiMethods"/>.</param>
	/// <param name="failure">How the request fails.</param>
	[Theory]
	[MemberData(nameof(Failures))]
	public void B75_ARequestThatGetsNoResponseGivesTheCategoryOfItsFailure(string method, Failure failure)
	{
		(ErrorCategory category, string url) = failure switch
		{
			Failure.NoResponse => (ErrorCategory.ConnectionError, ClientCalls.AbsoluteUrl),
			Failure.Timeout => (ErrorCategory.OperationTimeout, ClientCalls.PATH),
			_ => (ErrorCategory.OperationStopped, ClientCalls.PATH),
		};

		using CancellationTokenSource canceled = new();
		canceled.Cancel();

		using ServiceProvider provider = ModuleServices.Build(Fail(failure, new ProbeLog()));
		using IServiceScope scope = provider.CreateScope();
		ISonarrClient client = scope.ServiceProvider.GetRequiredService<ISonarrClient>();

		SonarrClientResult result = ClientCalls.ApiMethods[method](
			client,
			failure == Failure.Canceled ? canceled.Token : TestContext.Current.CancellationToken);

		Assert.True(result.IsError, $"{method} succeeded.");
		Assert.True(
			category == result.Error.CategoryInfo.Category,
			$"{method} gave {result.Error.CategoryInfo.Category}, where {category} was expected, for {result.Error.Exception}");
		Assert.Equal(url, result.RequestUrl);
	}

	public static TheoryData<string, Failure> Failures
	{
		get
		{
			TheoryData<string, Failure> data = [];
			foreach (string method in ClientCalls.ApiMethods.Keys)
			{
				foreach (Failure failure in (Failure[])[Failure.NoResponse, Failure.Timeout, Failure.Canceled])
				{
					data.Add(method, failure);
				}
			}

			return data;
		}
	}

	/// <summary>
	/// O27: a ping and a download give every failure as an error result, as the clients' docs say, rather than throwing.
	/// One that gets no response gives <see cref="ErrorCategory.ConnectionError"/>, a timeout
	/// <see cref="ErrorCategory.OperationTimeout"/>, and any other cancellation
	/// <see cref="ErrorCategory.OperationStopped"/>, and a download whose file can't be written gives
	/// <see cref="ErrorCategory.WriteError"/>, with the file's path as the error's target.
	/// </summary>
	/// <param name="call">The method that sends the request, <c>SendPingAsync</c> or <c>DownloadToPathAsync</c>.</param>
	/// <param name="failure">How the request fails.</param>
	[Theory]
	[MemberData(nameof(PingAndDownloadFailures))]
	public async Task O27_APingOrADownloadGivesEveryFailureAsAnErrorResult(string call, Failure failure)
	{
		ErrorCategory category = failure switch
		{
			Failure.NoResponse => ErrorCategory.ConnectionError,
			Failure.Timeout => ErrorCategory.OperationTimeout,
			Failure.Canceled => ErrorCategory.OperationStopped,
			_ => ErrorCategory.WriteError,
		};

		using CancellationTokenSource canceled = new();
		await canceled.CancelAsync();
		CancellationToken token = failure == Failure.Canceled ? canceled.Token : TestContext.Current.CancellationToken;

		ProbeLog log = new();
		DirectoryInfo folder = Directory.CreateTempSubdirectory("PoshSonarr.Tests.");
		string path = failure == Failure.Unwritable
			? Path.Combine(folder.FullName, "missing", "backup.zip")
			: Path.Combine(folder.FullName, "backup.zip");

		try
		{
			using ServiceProvider provider = ModuleServices.Build(Fail(failure, log));
			using IServiceScope scope = provider.CreateScope();

			SonarrClientResult result = call == nameof(ISonarrClient.SendPingAsync)
				? await scope.ServiceProvider.GetRequiredService<ISonarrClient>().SendPingAsync(token)
				: await scope.ServiceProvider.GetRequiredService<ISonarrDownloadClient>().DownloadToPathAsync(ClientCalls.DOWNLOAD_PATH, path, token);

			Assert.True(result.IsError, $"{call} succeeded.");
			Assert.True(
				category == result.Error.CategoryInfo.Category,
				$"{call} gave {result.Error.CategoryInfo.Category}, where {category} was expected, for {result.Error.Exception}");

			// The request reached the fake, which answered it as the row says.
			Assert.Equal(
				[call == nameof(ISonarrClient.SendPingAsync) ? "HEAD /ping" : "GET " + ClientCalls.DOWNLOAD_PATH],
				log.Snapshot());

			if (failure == Failure.Unwritable)
			{
				Assert.Equal(path, result.Error.TargetObject);
			}
		}
		finally
		{
			folder.Delete(recursive: true);
		}
	}

	public static TheoryData<string, Failure> PingAndDownloadFailures => new()
	{
		{ nameof(ISonarrClient.SendPingAsync), Failure.NoResponse },
		{ nameof(ISonarrClient.SendPingAsync), Failure.Timeout },
		{ nameof(ISonarrClient.SendPingAsync), Failure.Canceled },
		{ nameof(ISonarrDownloadClient.DownloadToPathAsync), Failure.NoResponse },
		{ nameof(ISonarrDownloadClient.DownloadToPathAsync), Failure.Timeout },
		{ nameof(ISonarrDownloadClient.DownloadToPathAsync), Failure.Canceled },
		{ nameof(ISonarrDownloadClient.DownloadToPathAsync), Failure.Unwritable },
	};

	/// <summary>
	/// B1: no await in the client or its handlers resumes on the caller's <see cref="SynchronizationContext"/>, since a
	/// cmdlet blocks on the client's tasks with <c>GetAwaiter().GetResult()</c>. Under a context that runs posted work
	/// only when its thread is free, which it never is while it blocks, every send method finishes, and nothing is posted
	/// to the context.
	/// </summary>
	/// <param name="method">The send method, by its name in <see cref="ClientCalls.AllMethods"/>.</param>
	[Theory]
	[MemberData(nameof(AllMethods))]
	public void B1_ARequestFinishesUnderAContextThatNeverRunsPostedWork(string method)
	{
		// The response comes later, on a thread-pool thread, so every await above the fake handler is still pending when
		// it's reached, and a continuation that captured the context would wait for it forever.
		FakeSonarr sonarr = new("delaying", new ProbeLog(), async (request, token) =>
		{
			await Task.Delay(20, token).ConfigureAwait(false);
			return Respond.Ok(request);
		});

		using ServiceProvider provider = ModuleServices.Build(sonarr);
		using IServiceScope scope = provider.CreateScope();
		ISonarrClient client = scope.ServiceProvider.GetRequiredService<ISonarrClient>();
		CancellationToken token = TestContext.Current.CancellationToken;

		SonarrClientResult? result = null;
		BlackHoleRun run = BlackHoleSynchronizationContext.Run(
			() => result = ClientCalls.AllMethods[method](client, token),
			TimeSpan.FromSeconds(15));

		Assert.True(run.Completed, $"{method} didn't finish within 15 seconds, so it's waiting on the blocked context.");
		Assert.Null(run.Error);
		Assert.Equal(0, run.Posts);
		Assert.NotNull(result);
		Assert.False(result.IsError, result.Error?.Exception.ToString());
	}

	public static TheoryData<string> AllMethods => [.. ClientCalls.AllMethods.Keys];

	/// <summary>
	/// Creates a fake instance that fails each request as the specified failure says, and logs each request that reaches
	/// it as its method and path.
	/// </summary>
	/// <param name="failure">How each request fails.</param>
	/// <param name="log">The log that the instance writes each request to.</param>
	/// <returns>
	/// An instance that refuses the connection, that stalls past a timeout of 250 milliseconds or until the request is
	/// canceled, or that answers <c>200 OK</c>, for a file that the test can't write.
	/// </returns>
	private static FakeSonarr Fail(Failure failure, ProbeLog log)
	{
		Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond = failure switch
		{
			Failure.NoResponse => (_, _) => Task.FromException<HttpResponseMessage>(new HttpRequestException(
				HttpRequestError.ConnectionError, "No connection could be made because the target machine actively refused it.")),
			Failure.Unwritable => (request, _) => Task.FromResult(Respond.Ok(request)),
			_ => Stall,
		};

		return new(
			failure.ToString(),
			log,
			(request, token) =>
			{
				log.Add(string.Concat(request.Method.Method, " ", request.RequestUri?.AbsolutePath));
				return respond(request, token);
			},
			failure == Failure.Timeout ? TimeSpan.FromMilliseconds(250) : null);
	}

	/// <summary>
	/// Answers no request, and ends when the request's token is canceled, as a network handler does.
	/// </summary>
	private static async Task<HttpResponseMessage> Stall(HttpRequestMessage request, CancellationToken token)
	{
		await Task.Delay(Timeout.Infinite, token).ConfigureAwait(false);
		return new HttpResponseMessage(HttpStatusCode.OK) { RequestMessage = request };
	}
}
