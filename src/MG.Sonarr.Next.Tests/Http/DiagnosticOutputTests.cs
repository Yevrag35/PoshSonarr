using System.Collections.Concurrent;
using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Services.Http.Clients;

namespace MG.Sonarr.Next.Tests.Http;

/// <summary>
/// Tests the verbose and debug output that a cmdlet asks for, which the HTTP handlers write by reading and buffering
/// the bodies of its requests and their responses.
/// </summary>
/// <remarks>
/// The output runs only while someone is diagnosing a problem, so a change that it made to a request would change the
/// very failure that it's meant to show.
/// </remarks>
public sealed class DiagnosticOutputTests
{
	/// <summary>
	/// A tag whose label is outside ASCII.
	/// </summary>
	private const string TAG = """{"id":1,"label":"Café ☕ 日本"}""";

	/// <summary>
	/// A request body whose label is outside ASCII.
	/// </summary>
	private static readonly object s_tag = new { id = 1, label = "Café ☕ 日本" };

	/// <summary>
	/// A request body longer than the 8,192 bytes that a pooled stream starts with.
	/// </summary>
	private static readonly object s_series = new { id = 1, title = "Sandbox Ended Show", overview = new string('é', 6_000) };

	/// <summary>
	/// A response body longer than the 8,192 bytes that a pooled stream starts with.
	/// </summary>
	private static readonly string s_seriesJson = $$"""{"id":1,"title":"Sandbox Ended Show","overview":"{{new string('ü', 6_000)}}"}""";

	/// <summary>
	/// The exchanges that the tests send, each a call through the client and the response that the fake answers with.
	/// </summary>
	private static readonly Dictionary<string, Exchange> s_exchanges = new(StringComparer.Ordinal)
	{
		["GET of a list outside ASCII"] = new(
			(client, token) => client.SendGet<object>("/tag", token),
			HttpStatusCode.OK, "application/json", $"[{TAG},{{\"id\":2,\"label\":\"2024\"}}]"),
		["asynchronous GET"] = new(
			(client, token) => client.SendGetAsync<object>("/tag/1", token).GetAwaiter().GetResult(),
			HttpStatusCode.OK, "application/json", TAG),
		["POST of a body outside ASCII"] = new(
			(client, token) => client.SendPost<object>("/tag", s_tag, token),
			HttpStatusCode.Created, "application/json", TAG),
		["asynchronous POST without a result"] = new(
			(client, token) => client.SendPostAsync("/tag", s_tag, token).GetAwaiter().GetResult(),
			HttpStatusCode.Created, "application/json", TAG),
		["PUT of a body past a pooled stream's first buffer"] = new(
			(client, token) => client.SendPut("/series/1", s_series, token),
			HttpStatusCode.Accepted, "application/json", s_seriesJson),
		["asynchronous PUT"] = new(
			(client, token) => client.SendPutAsync("/series/1", s_series, token).GetAwaiter().GetResult(),
			HttpStatusCode.Accepted, "application/json", s_seriesJson),
		["DELETE with no body either way"] = new(
			(client, token) => client.SendDelete("/tag/1", token),
			HttpStatusCode.OK, null, null),
		["PUT that Sonarr turns away"] = new(
			(client, token) => client.SendPut("/series/1", s_tag, token),
			HttpStatusCode.BadRequest, "application/problem+json", """
				{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"$.label":["The JSON value could not be converted to System.String."]}}
				"""),
	};

	/// <summary>
	/// A request goes to Sonarr with the same method, URL, headers, and body bytes, and its result reads the same, whether
	/// its cmdlet asks for verbose and debug output or not. The cmdlet gets, in order, the method and URL before the send,
	/// the request's body after its size in bytes when it has one, the response's body after its size in bytes, and the
	/// status code and URL after the response.
	/// </summary>
	/// <param name="exchange">The exchange, by its key in <see cref="s_exchanges"/>.</param>
	[Theory]
	[MemberData(nameof(Exchanges))]
	public void LeavesEachRequestAndItsResultAsTheyWere(string exchange)
	{
		Exchange sent = s_exchanges[exchange];
		ConcurrentQueue<Received> received = new();
		FakeSonarr sonarr = new("diagnostics", new ProbeLog(), (request, token) =>
		{
			received.Enqueue(Received.From(request, token));
			return Task.FromResult(sent.Answer(request));
		});

		using ServiceProvider provider = ModuleServices.Build(sonarr);
		CancellationToken token = TestContext.Current.CancellationToken;
		DiagnosedCmdlet cmdlet = new();

		string plain = Describe(Send(provider, sent.Send, cmdlet: null, token));
		string diagnosed = Describe(Send(provider, sent.Send, cmdlet, token));

		Received[] requests = received.ToArray();
		Assert.Equal(2, requests.Length);
		Assert.Equal(requests[0], requests[1]);
		Assert.Equal(plain, diagnosed);

		Received request = requests[1];
		string response = sent.Body ?? string.Empty;
		List<string> expected = [$"Verbose: {request.Method} {request.Url}"];
		if (request.Text is not null)
		{
			expected.Add($"Debug: HTTP Request (size in bytes: {request.ContentLength}) -> {request.Text}");
		}

		expected.Add($"Debug: HTTP Response (size in bytes: {Encoding.UTF8.GetByteCount(response)}) -> {response}");
		expected.Add($"Verbose: {(int)sent.Status} {request.Url}");

		Assert.Equal(expected, cmdlet.Lines);
	}

	public static TheoryData<string> Exchanges => [.. s_exchanges.Keys];

	/// <summary>
	/// A download writes the same bytes, from a body longer than the 8,192 bytes that a pooled stream starts with,
	/// whether its cmdlet asks for verbose and debug output or not, and the cmdlet gets the download's URL before the
	/// send, the body's size after it, and the status code and URL after the response.
	/// </summary>
	[Fact]
	public async Task DownloadsTheSameFile()
	{
		byte[] file = [.. Enumerable.Range(0, 20_000).Select(i => (byte)(i * 7))];
		FakeSonarr sonarr = new("diagnostics", new ProbeLog(), (request, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
		{
			RequestMessage = request,
			Content = new ByteArrayContent(file),
		}));

		using ServiceProvider provider = ModuleServices.Build(sonarr);
		CancellationToken token = TestContext.Current.CancellationToken;
		DiagnosedCmdlet cmdlet = new();
		DirectoryInfo folder = Directory.CreateTempSubdirectory("PoshSonarr.Tests.");
		try
		{
			string plainPath = Path.Combine(folder.FullName, "plain.zip");
			string diagnosedPath = Path.Combine(folder.FullName, "diagnosed.zip");

			using (IServiceScope scope = provider.CreateScope())
			{
				SonarrClientResult<string> result = await scope.ServiceProvider.GetRequiredService<ISonarrDownloadClient>()
					.DownloadToPathAsync(ClientCalls.DOWNLOAD_PATH, plainPath, token);
				Assert.False(result.IsError, result.Error?.Exception.ToString());
			}

			using (IServiceScope scope = provider.CreateScope())
			{
				scope.ServiceProvider.GetRequiredService<ApiCmdletAccessor>().Cmdlet = cmdlet;
				SonarrClientResult<string> result = await scope.ServiceProvider.GetRequiredService<ISonarrDownloadClient>()
					.DownloadToPathAsync(ClientCalls.DOWNLOAD_PATH, diagnosedPath, token);
				Assert.False(result.IsError, result.Error?.Exception.ToString());
			}

			Assert.Equal(file, await File.ReadAllBytesAsync(plainPath, token));
			Assert.Equal(file, await File.ReadAllBytesAsync(diagnosedPath, token));

			string url = new Uri(FakeSonarr.BaseUri, ClientCalls.DOWNLOAD_PATH).ToString();
			string[] lines = cmdlet.Lines;
			Assert.Equal(3, lines.Length);
			Assert.Equal($"Verbose: GET {url}", lines[0]);
			Assert.StartsWith("Debug: HTTP Response (size in bytes: 20000) -> ", lines[1], StringComparison.Ordinal);
			Assert.Equal($"Verbose: 200 {url}", lines[2]);
		}
		finally
		{
			folder.Delete(recursive: true);
		}
	}

	/// <summary>
	/// Sends an exchange's request from a new scope, whose requests carry the specified cmdlet.
	/// </summary>
	private static SonarrClientResult Send(
		ServiceProvider provider,
		Func<ISonarrClient, CancellationToken, SonarrClientResult> send,
		IApiCmdlet? cmdlet,
		CancellationToken token)
	{
		using IServiceScope scope = provider.CreateScope();
		scope.ServiceProvider.GetRequiredService<ApiCmdletAccessor>().Cmdlet = cmdlet;
		return send(scope.ServiceProvider.GetRequiredService<ISonarrClient>(), token);
	}

	/// <summary>
	/// Describes what a caller reads from a result: whether it failed, its status code and URL, its error's message, and
	/// its value, written back as JSON.
	/// </summary>
	private static string Describe(SonarrClientResult result)
	{
		string value = result is SonarrClientResult<object> { IsError: false, Value: { } read }
			? JsonSerializer.Serialize(read, read.GetType(), ModuleServices.Json.ForSerializing)
			: string.Empty;

		return string.Join(
			Environment.NewLine,
			result.IsError ? "failed" : "succeeded",
			((int)result.StatusCode).ToString(CultureInfo.InvariantCulture),
			result.RequestUrl,
			result.Error?.Message,
			value);
	}

	/// <summary>
	/// A request that a client sends and the response that the fake answers it with.
	/// </summary>
	/// <param name="Send">The call that sends the request through a client.</param>
	/// <param name="Status">The status code of the response.</param>
	/// <param name="MediaType">The content type of the response's body, or <see langword="null"/> for no body.</param>
	/// <param name="Body">The response's body, or <see langword="null"/> for none.</param>
	private sealed record Exchange(
		Func<ISonarrClient, CancellationToken, SonarrClientResult> Send,
		HttpStatusCode Status,
		string? MediaType,
		string? Body)
	{
		/// <summary>
		/// Creates the response to the specified request.
		/// </summary>
		internal HttpResponseMessage Answer(HttpRequestMessage request)
		{
			HttpResponseMessage response = new(this.Status) { RequestMessage = request };
			if (this.Body is not null && this.MediaType is not null)
			{
				response.Content = new StringContent(this.Body, Encoding.UTF8, this.MediaType);
			}

			return response;
		}
	}

	/// <summary>
	/// What Sonarr receives of a request: its method, URL, content headers, and body.
	/// </summary>
	/// <param name="Method">The request's method.</param>
	/// <param name="Url">The request's URL.</param>
	/// <param name="ContentType">The body's content type, or <see langword="null"/> for no body.</param>
	/// <param name="ContentLength">The body's <c>Content-Length</c>, or <see langword="null"/> when it has none.</param>
	/// <param name="Chunked">Whether the body goes out in chunks.</param>
	/// <param name="Bytes">The body's bytes in hexadecimal, or <see langword="null"/> for no body.</param>
	/// <param name="Text">The body decoded as UTF-8, or <see langword="null"/> for no body.</param>
	private sealed record Received(
		string Method,
		string Url,
		string? ContentType,
		long? ContentLength,
		bool? Chunked,
		string? Bytes,
		string? Text)
	{
		/// <summary>
		/// Reads what Sonarr receives of the specified request.
		/// </summary>
		internal static Received From(HttpRequestMessage request, CancellationToken token)
		{
			string? bytes = null;
			string? text = null;
			if (request.Content is HttpContent content)
			{
				using MemoryStream copy = new();
				using (Stream stream = content.ReadAsStream(token))
				{
					stream.CopyTo(copy);
				}

				bytes = Convert.ToHexString(copy.ToArray());
				text = Encoding.UTF8.GetString(copy.ToArray());
			}

			return new(
				request.Method.Method,
				request.RequestUri?.ToString() ?? string.Empty,
				request.Content?.Headers.ContentType?.ToString(),
				request.Content?.Headers.ContentLength,
				request.Headers.TransferEncodingChunked,
				bytes,
				text);
		}
	}

	/// <summary>
	/// A cmdlet that asks for every verbose and debug line about its requests, and keeps them in the order that the
	/// handlers write them, from any thread.
	/// </summary>
	private sealed class DiagnosedCmdlet : IApiCmdlet
	{
		private readonly ConcurrentQueue<string> _lines = new();

		public bool CanDebugSerializeBefore => true;

		public bool CanDebugSerializeAfter => true;

		public bool CanWriteVerbose => true;

		/// <summary>
		/// Gets the lines that the cmdlet has written so far.
		/// </summary>
		/// <value>Each line, after <c>Verbose: </c> or <c>Debug: </c>.</value>
		internal string[] Lines => [.. _lines];

		public void WriteDebugPayload(string jsonPayload)
		{
			_lines.Enqueue("Debug: " + jsonPayload);
		}

		public void WriteVerboseBefore(IHttpRequestDetails request)
		{
			_lines.Enqueue($"Verbose: {request.RequestMethod} {request.RequestUrl}");
		}

		public void WriteVerboseAfter(ISonarrResponse response, IServiceProvider provider, JsonSerializerOptions? options)
		{
			_lines.Enqueue(string.Create(CultureInfo.InvariantCulture, $"Verbose: {(int)response.StatusCode} {response.RequestUrl}"));
		}
	}
}
