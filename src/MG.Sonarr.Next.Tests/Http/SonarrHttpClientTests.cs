using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Services.Http.Clients;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace MG.Sonarr.Next.Tests.Http;

/// <summary>
/// Tests the HTTP client that every request goes through, with the module's handler chain above a fake Sonarr.
/// </summary>
public sealed class SonarrHttpClientTests
{
	/// <summary>
	/// The ways that a request fails before any response arrives.
	/// </summary>
	public enum Failure
	{
		/// <summary>No response arrives, as for a refused or dropped connection.</summary>
		NoResponse,
		/// <summary>The request outlasts the connection's timeout.</summary>
		Timeout,
		/// <summary>The caller's token is canceled, as Ctrl+C cancels a cmdlet's.</summary>
		Canceled,
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
		FakeSonarr sonarr = failure switch
		{
			Failure.NoResponse => new("refusing", new ProbeLog(), (_, _) => Task.FromException<HttpResponseMessage>(
				new HttpRequestException(HttpRequestError.ConnectionError, "No connection could be made because the target machine actively refused it."))),
			Failure.Timeout => new("stalling", new ProbeLog(), Stall, TimeSpan.FromMilliseconds(250)),
			_ => new("stalling", new ProbeLog(), Stall),
		};

		(ErrorCategory category, string url) = failure switch
		{
			Failure.NoResponse => (ErrorCategory.ConnectionError, ClientCalls.AbsoluteUrl),
			Failure.Timeout => (ErrorCategory.OperationTimeout, ClientCalls.PATH),
			_ => (ErrorCategory.OperationStopped, ClientCalls.PATH),
		};

		using CancellationTokenSource canceled = new();
		canceled.Cancel();

		using ServiceProvider provider = ModuleServices.Build(sonarr);
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
				foreach (Failure failure in Enum.GetValues<Failure>())
				{
					data.Add(method, failure);
				}
			}

			return data;
		}
	}

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
	/// Answers no request, and ends when the request's token is canceled, as a network handler does.
	/// </summary>
	private static async Task<HttpResponseMessage> Stall(HttpRequestMessage request, CancellationToken token)
	{
		await Task.Delay(Timeout.Infinite, token).ConfigureAwait(false);
		return new HttpResponseMessage(HttpStatusCode.OK) { RequestMessage = request };
	}
}
