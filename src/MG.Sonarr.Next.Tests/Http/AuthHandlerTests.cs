using System.Collections.Concurrent;
using System.Net.Http.Headers;
using MG.Sonarr.Next.Services.Auth;
using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Services.Http.Clients;

namespace MG.Sonarr.Next.Tests.Http;

/// <summary>
/// Tests what a download sends to a server whose authentication is Basic or forms, through the download client's handler
/// chain above a fake Sonarr that logs users in as Sonarr does.
/// </summary>
public sealed class AuthHandlerTests
{
	/// <summary>
	/// The ways that a forms login fails to let a download through.
	/// </summary>
	public enum LoginFault
	{
		/// <summary>Sonarr takes the login but sets no cookie, so the URL base that it redirects to sends the login on to the login page.</summary>
		CookieNotSet,
		/// <summary>Sonarr takes the login and sets its cookie, but sends every download to its login page all the same.</summary>
		CookieNotHonored,
	}

	/// <summary>
	/// The bytes of the file that the fake serves, which a download saves.
	/// </summary>
	private static readonly byte[] s_file = [0x50, 0x4B, 0x03, 0x04];

	/// <summary>
	/// A download without credentials goes out as it is to a server whose authentication is Basic or forms, with no
	/// <c>Authorization</c> header and no login, and the file that the server serves, as Sonarr does for a client whose
	/// address needs no login, is saved.
	/// </summary>
	/// <param name="authType">The server's authentication type.</param>
	[Theory]
	[InlineData(SonarrAuthType.Basic)]
	[InlineData(SonarrAuthType.Forms)]
	public async Task SendsADownloadWithoutCredentialsAsItIs(SonarrAuthType authType)
	{
		LoginSonarr server = new(authType) { RequiresLogin = false };
		using ServiceProvider provider = server.Build();

		DownloadOutcome outcome = await DownloadAsync(provider, ClientCalls.DOWNLOAD_PATH, credentials: null);

		Assert.False(outcome.Result.IsError, outcome.Result.Error?.Exception.ToString());
		Assert.Equal(s_file, outcome.Saved);
		Assert.Equal(["GET /backup/manual/sonarr_backup.zip"], server.Received.ToArray());
	}

	/// <summary>
	/// A download without credentials that the server turns away for want of a login, a Basic server with a 401 and a
	/// forms server by sending it to the login page, whose own status is 200 OK, fails with the category
	/// <see cref="ErrorCategory.AuthenticationError"/> and the status 401, and saves nothing.
	/// </summary>
	/// <param name="authType">The server's authentication type.</param>
	[Theory]
	[InlineData(SonarrAuthType.Basic)]
	[InlineData(SonarrAuthType.Forms)]
	public async Task FailsADownloadWithoutCredentialsThatTheServerTurnsAway(SonarrAuthType authType)
	{
		LoginSonarr server = new(authType);
		using ServiceProvider provider = server.Build();

		DownloadOutcome outcome = await DownloadAsync(provider, ClientCalls.DOWNLOAD_PATH, credentials: null);

		AssertUnauthorized(outcome);
		Assert.Equal(["GET /backup/manual/sonarr_backup.zip"], server.Received.ToArray());
	}

	/// <summary>
	/// A Basic download sends its user name and password, encoded as UTF-8 and then Base64, in its <c>Authorization</c>
	/// header, as Sonarr decodes them, whether they fit the stack buffer that they're encoded in or need a pooled one.
	/// </summary>
	/// <param name="password">The password, which the fake takes.</param>
	[Theory]
	[InlineData("pässwörd")]
	[InlineData("p")]
	[MemberData(nameof(LongPasswords))]
	public async Task SendsBasicCredentialsInTheAuthorizationHeader(string password)
	{
		LoginSonarr server = new(SonarrAuthType.Basic) { Password = password };
		using ServiceProvider provider = server.Build();

		DownloadOutcome outcome = await DownloadAsync(provider, ClientCalls.DOWNLOAD_PATH, new NetworkCredential(LoginSonarr.USER_NAME, password));

		Assert.False(outcome.Result.IsError, outcome.Result.Error?.Exception.ToString());
		Assert.Equal(s_file, outcome.Saved);
		Assert.Equal(["GET /backup/manual/sonarr_backup.zip with Authorization"], server.Received.ToArray());
	}

	public static TheoryData<string> LongPasswords => [new string('ü', 200)];

	/// <summary>
	/// A download whose password the server doesn't take, a Basic server with a 401 and a forms server by sending the
	/// login back to its login page with <c>loginFailed=true</c>, fails with the category
	/// <see cref="ErrorCategory.AuthenticationError"/> and the status 401, says in the same words under both that the user
	/// name or password is incorrect, and saves nothing.
	/// </summary>
	/// <param name="authType">The server's authentication type.</param>
	[Theory]
	[InlineData(SonarrAuthType.Basic)]
	[InlineData(SonarrAuthType.Forms)]
	public async Task FailsADownloadWhosePasswordIsWrong(SonarrAuthType authType)
	{
		LoginSonarr server = new(authType);
		using ServiceProvider provider = server.Build();

		DownloadOutcome outcome = await DownloadAsync(provider, ClientCalls.DOWNLOAD_PATH, new NetworkCredential(LoginSonarr.USER_NAME, "wrong"));

		AssertUnauthorized(outcome);
		Assert.Equal("The username or password is incorrect.", outcome.Result.Error?.Exception.InnerException?.Message);
	}

	/// <summary>
	/// A download with credentials, under a connection whose test stored Basic, None, or External, to a server whose
	/// authentication has changed to forms since, which sends the download to its login page, fails with the category
	/// <see cref="ErrorCategory.AuthenticationError"/> and the status 401, though it ends on the login page's 200 OK, says
	/// that the server's authentication has changed, and saves nothing. The handler sends the download's credentials in an
	/// <c>Authorization</c> header under Basic, and none under None or External.
	/// </summary>
	/// <param name="storedType">The authentication type that the connection's test stored.</param>
	[Theory]
	[InlineData(SonarrAuthType.Basic)]
	[InlineData(SonarrAuthType.None)]
	[InlineData(SonarrAuthType.External)]
	public async Task FailsADownloadOnceTheServersAuthenticationChangesToForms(SonarrAuthType storedType)
	{
		LoginSonarr server = new(SonarrAuthType.Forms);
		using ServiceProvider provider = server.Build();
		server.Settings.AuthType = storedType;

		DownloadOutcome outcome = await DownloadAsync(provider, ClientCalls.DOWNLOAD_PATH, new NetworkCredential(LoginSonarr.USER_NAME, server.Password));

		AssertUnauthorized(outcome);
		Assert.StartsWith(
			"The Sonarr server sent the request to its login page, so its authentication has changed since the connection was made.",
			outcome.Result.Error?.Exception.InnerException?.Message);

		string authorization = storedType == SonarrAuthType.Basic ? " with Authorization" : string.Empty;
		Assert.Equal([$"GET /backup/manual/sonarr_backup.zip{authorization}"], server.Received.ToArray());
	}

	/// <summary>
	/// A forms download whose login gets no cookie, so that Sonarr sends the login on to its login page, or whose request
	/// Sonarr sends to its login page again after a new login, fails with the category
	/// <see cref="ErrorCategory.AuthenticationError"/> and the status 401, though each of those requests ends on the login
	/// page's 200 OK, and saves nothing.
	/// </summary>
	/// <param name="fault">How the login fails.</param>
	[Theory]
	[InlineData(LoginFault.CookieNotSet)]
	[InlineData(LoginFault.CookieNotHonored)]
	public async Task FailsAFormsDownloadWhoseLoginDoesNotHold(LoginFault fault)
	{
		LoginSonarr server = new(SonarrAuthType.Forms)
		{
			SetsCookie = fault != LoginFault.CookieNotSet,
			HonorsCookie = fault != LoginFault.CookieNotHonored,
		};

		using ServiceProvider provider = server.Build();

		DownloadOutcome outcome = await DownloadAsync(provider, ClientCalls.DOWNLOAD_PATH, new NetworkCredential(LoginSonarr.USER_NAME, server.Password));

		AssertUnauthorized(outcome);
	}

	/// <summary>
	/// The connection's test stores the URL base that the server reports, and a forms download logs in and downloads under
	/// the server's spelling of it, though the connection's URL spells it otherwise, so the cookie that Sonarr scopes to
	/// its own spelling goes back with every download, and one login serves a backup's path and a log file's URL alike.
	/// </summary>
	[Fact]
	public async Task LogsInAndDownloadsUnderTheServersSpellingOfTheUrlBase()
	{
		LoginSonarr server = new(SonarrAuthType.Forms, "/sonarr");
		using ServiceProvider provider = server.Build(new Uri("http://sonarr.test:8989/Sonarr/"), tested: false);

		using (IServiceScope scope = provider.CreateScope())
		{
			SonarrClientResult test = scope.ServiceProvider.GetRequiredService<ISonarrClient>().SendTest(TestContext.Current.CancellationToken);
			Assert.False(test.IsError, test.Error?.Exception.ToString());
		}

		Assert.Equal(SonarrAuthType.Forms, server.Settings.AuthType);
		Assert.Equal("/sonarr", server.Settings.ServerUrlBase);

		NetworkCredential credentials = new(LoginSonarr.USER_NAME, server.Password);
		DownloadOutcome backup = await DownloadAsync(provider, ClientCalls.DOWNLOAD_PATH, credentials);
		DownloadOutcome log = await DownloadAsync(provider, "/sonarr/logfile/sonarr.txt", credentials);

		Assert.False(backup.Result.IsError, backup.Result.Error?.Exception.ToString());
		Assert.False(log.Result.IsError, log.Result.Error?.Exception.ToString());
		Assert.Equal(s_file, backup.Saved);
		Assert.Equal(s_file, log.Saved);
		Assert.Equal(
			[
				"GET /Sonarr/api/v3/system/status",
				"POST /sonarr/login?returnUrl=/sonarr/",
				"GET /sonarr/backup/manual/sonarr_backup.zip",
				"GET /sonarr/logfile/sonarr.txt",
			],
			server.Received.ToArray());
	}

	/// <summary>
	/// Asserts that a download failed for want of a login, with the category
	/// <see cref="ErrorCategory.AuthenticationError"/> and the status 401, and saved nothing.
	/// </summary>
	private static void AssertUnauthorized(DownloadOutcome outcome)
	{
		Assert.True(outcome.Result.IsError, "The download succeeded.");
		Assert.True(
			ErrorCategory.AuthenticationError == outcome.Result.Error.CategoryInfo.Category,
			$"The download gave {outcome.Result.Error.CategoryInfo.Category} for {outcome.Result.Error.Exception}");
		Assert.Equal(HttpStatusCode.Unauthorized, outcome.Result.StatusCode);
		Assert.Null(outcome.Saved);
	}

	/// <summary>
	/// Downloads the file at the specified URL into a folder of its own, through the download client of a new scope on the
	/// specified provider, and reads back what it saved.
	/// </summary>
	private static async Task<DownloadOutcome> DownloadAsync(ServiceProvider provider, string url, NetworkCredential? credentials)
	{
		CancellationToken token = TestContext.Current.CancellationToken;
		DirectoryInfo folder = Directory.CreateTempSubdirectory("PoshSonarr.Tests.");
		try
		{
			string path = Path.Combine(folder.FullName, "download.bin");
			using IServiceScope scope = provider.CreateScope();
			ISonarrDownloadClient client = scope.ServiceProvider.GetRequiredService<ISonarrDownloadClient>();

			SonarrClientResult<string> result = await client.DownloadToPathAsync(url, path, credentials, token);
			byte[]? saved = File.Exists(path) ? await File.ReadAllBytesAsync(path, token) : null;

			return new DownloadOutcome(result, saved);
		}
		finally
		{
			folder.Delete(recursive: true);
		}
	}

	/// <summary>
	/// The result of a download and the bytes that it saved, or <see langword="null"/> when it saved no file.
	/// </summary>
	private sealed record DownloadOutcome(SonarrClientResult<string> Result, byte[]? Saved);

	/// <summary>
	/// Stands in for a Sonarr server whose authentication is Basic or forms, and serves a file only to a request that
	/// logged in, unless it requires no login.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A Basic server serves a request whose <c>Authorization</c> header holds its user name and password, and answers
	/// any other with a 401. A forms server takes a login's form at <c>&lt;base&gt;/login</c>, sets the login's cookie in
	/// the connection's own <see cref="CookieContainer"/> with the URL base, in its own spelling, as its path, as Sonarr v3
	/// does, and redirects to the URL base, or back to the login page with <c>loginFailed=true</c> when it turns the
	/// password away. It serves the URL base and a file to a request whose URL the container holds a cookie for, as the
	/// primary handler would send it, and sends any other to the login page, as it does every file request when it
	/// doesn't honor the cookie.
	/// </para>
	/// <para>
	/// It answers a request that it redirects as .NET's primary handler leaves one after following the redirect: the
	/// request's URL is where the redirect led, a POST has become a GET without its body, and the response is the last one.
	/// It matches its URL base without regard to case, as Sonarr does.
	/// </para>
	/// </remarks>
	private sealed class LoginSonarr
	{
		internal const string USER_NAME = "mike";

		private readonly SonarrAuthType _authType;
		private readonly string _urlBase;
		private FakeSonarr? _sonarr;
		private CookieContainer? _jar;

		/// <summary>
		/// Initializes a new <see cref="LoginSonarr"/> instance with the specified authentication type and URL base.
		/// </summary>
		/// <param name="authType">The server's authentication type, Basic or forms.</param>
		/// <param name="urlBase">The server's spelling of its URL base, or an empty string for none.</param>
		internal LoginSonarr(SonarrAuthType authType, string urlBase = "")
		{
			_authType = authType;
			_urlBase = urlBase;
		}

		internal string Password { get; init; } = "pw";

		internal bool RequiresLogin { get; init; } = true;

		internal bool SetsCookie { get; init; } = true;

		internal bool HonorsCookie { get; init; } = true;

		/// <summary>
		/// Gets each request that reached the server, as its method and path and query, and whether it had an
		/// <c>Authorization</c> header.
		/// </summary>
		internal ConcurrentQueue<string> Received { get; } = new();

		internal IConnectionSettings Settings => _sonarr!.Settings;

		/// <summary>
		/// Builds a connection's services with the server as the primary handler of both typed clients.
		/// </summary>
		/// <param name="serviceUri">The URL that the connection names, or <see langword="null"/> for
		/// <see cref="FakeSonarr.BaseUri"/>.</param>
		/// <param name="tested">
		/// <see langword="true"/> to store the server's authentication type in the connection's settings, as its test
		/// would; <see langword="false"/> to leave them as they are before the test.
		/// </param>
		/// <returns>A new provider, which the caller disposes.</returns>
		internal ServiceProvider Build(Uri? serviceUri = null, bool tested = true)
		{
			_sonarr = new FakeSonarr("login", new ProbeLog(), this.RespondAsync, serviceUri: serviceUri);
			if (tested)
			{
				_sonarr.Settings.AuthType = _authType;
			}

			ServiceProvider provider = ModuleServices.Build(_sonarr);
			_jar = provider.GetRequiredService<CookieContainer>();
			return provider;
		}

		private async Task<HttpResponseMessage> RespondAsync(HttpRequestMessage request, CancellationToken token)
		{
			token.ThrowIfCancellationRequested();
			this.Received.Enqueue(string.Concat(
				request.Method.Method,
				" ",
				request.RequestUri!.PathAndQuery,
				request.Headers.Authorization is null ? string.Empty : " with Authorization"));

			string path = this.LocalPath(request.RequestUri!);
			if (path.EndsWith("/system/status", StringComparison.OrdinalIgnoreCase))
			{
				string authentication = _authType.ToString().ToLowerInvariant();
				return Respond.Json(request, HttpStatusCode.OK, $$"""{"authentication":"{{authentication}}","urlBase":"{{_urlBase}}"}""");
			}

			if (_authType == SonarrAuthType.Basic)
			{
				return !this.RequiresLogin || this.IsBasicLogin(request.Headers.Authorization)
					? Serve(request)
					: new HttpResponseMessage(HttpStatusCode.Unauthorized) { RequestMessage = request };
			}

			if (request.Method == HttpMethod.Post && path.Equals("/login", StringComparison.OrdinalIgnoreCase))
			{
				string form = await request.Content!.ReadAsStringAsync(token).ConfigureAwait(false);
				return this.LogIn(request, form);
			}

			return this.Get(request);
		}

		private bool IsBasicLogin(AuthenticationHeaderValue? authorization)
		{
			return authorization is { Scheme: "Basic", Parameter: string parameter }
				&& Encoding.UTF8.GetString(Convert.FromBase64String(parameter)) == $"{USER_NAME}:{this.Password}";
		}

		private HttpResponseMessage LogIn(HttpRequestMessage request, string form)
		{
			string expected = $"username={USER_NAME}&password={Uri.EscapeDataString(this.Password)}";
			if (form != expected)
			{
				return this.Redirect(request, $"{_urlBase}/login?returnUrl={_urlBase}/&loginFailed=true");
			}

			if (this.SetsCookie)
			{
				string cookiePath = _urlBase.Length == 0 ? "/" : _urlBase;
				_jar!.Add(new Uri(request.RequestUri!, cookiePath), new Cookie("SonarrAuth", "session", cookiePath));
			}

			return this.Redirect(request, _urlBase + "/");
		}

		private HttpResponseMessage Get(HttpRequestMessage request)
		{
			string path = this.LocalPath(request.RequestUri!);
			if (path.Equals("/login", StringComparison.OrdinalIgnoreCase))
			{
				return Page(request, "login");
			}

			// The cookie lets a request through to the URL base, and to a file too unless the server no longer honors it.
			bool loggedIn = _jar!.GetCookies(request.RequestUri!).Count > 0 && (path == "/" || this.HonorsCookie);
			if (this.RequiresLogin && !loggedIn)
			{
				return this.Redirect(request, $"{_urlBase}/login?returnUrl={Uri.EscapeDataString(request.RequestUri!.AbsolutePath)}");
			}

			return path == "/" ? Page(request, "index") : Serve(request);
		}

		private HttpResponseMessage Redirect(HttpRequestMessage request, string pathAndQuery)
		{
			request.RequestUri = new Uri(request.RequestUri!, pathAndQuery);
			if (request.Method == HttpMethod.Post)
			{
				request.Method = HttpMethod.Get;
				request.Content = null;
			}

			return this.Get(request);
		}

		private string LocalPath(Uri uri)
		{
			string path = uri.AbsolutePath;
			if (!path.StartsWith(_urlBase, StringComparison.OrdinalIgnoreCase))
			{
				return path;
			}

			string local = path[_urlBase.Length..];
			return local.Length == 0 ? "/" : local;
		}

		private static HttpResponseMessage Page(HttpRequestMessage request, string name)
		{
			return new HttpResponseMessage(HttpStatusCode.OK)
			{
				RequestMessage = request,
				Content = new StringContent($"<html>{name}</html>", Encoding.UTF8, "text/html"),
			};
		}

		private static HttpResponseMessage Serve(HttpRequestMessage request)
		{
			return new HttpResponseMessage(HttpStatusCode.OK)
			{
				RequestMessage = request,
				Content = new ByteArrayContent(s_file),
			};
		}
	}
}
