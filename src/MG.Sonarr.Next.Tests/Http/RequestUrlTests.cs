using System.Collections.Concurrent;
using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Services.Http.Clients;
using MG.Sonarr.Next.Services.Http.Queries;

namespace MG.Sonarr.Next.Tests.Http;

/// <summary>
/// Tests the URLs that the module's requests go to: an API request's, which <c>PathHandler</c> builds, a download's,
/// which the download client builds, and the query that a <see cref="QueryCol"/> writes.
/// </summary>
public sealed class RequestUrlTests
{
	/// <summary>
	/// The text of a query or a field that runs past the 256 characters that a URL is built in on the stack.
	/// </summary>
	private static readonly string s_longText = new('x', 300);

	/// <summary>
	/// The keys that fields are added under: names that differ only in case, inside and outside ASCII.
	/// </summary>
	private static readonly Gen<string> s_keys = Gen.OneOf(
		"a", "A", "b", "page", "Page", "pageSize", "sortKey", "seriesId", "SeriesID", "é", "É");

	/// <summary>
	/// The strings that fields write: none and blank ones, which write the key alone, text with a space or with letters
	/// outside ASCII, text that's escaped already, and text longer than the 256 characters that a query is built in on
	/// the stack.
	/// </summary>
	private static readonly Gen<string?> s_strings = Gen.OneOf<string?>(null, "", " ", "x", "a b", "日本", "%2F", s_longText);

	/// <summary>
	/// The changes that a cmdlet makes to its query: each way of adding, replacing, and removing a field.
	/// </summary>
	private static readonly Gen<QueryChange> s_queryChanges = Gen.OneOf<QueryChange>(
		Gen.Select(s_keys, Gen.Int, (key, value) => (QueryChange)new AddInt(key, value)),
		Gen.Select(s_keys, Gen.Long, (key, value) => (QueryChange)new AddLong(key, value)),
		Gen.Select(s_keys, s_strings, (key, value) => (QueryChange)new AddString(key, value)),
		Gen.Select(s_keys, Gen.DateTime, (key, value) => (QueryChange)new AddDate(key, value)),
		Gen.Select(s_keys, Gen.Bool, (key, value) => (QueryChange)new AddBoolean(key, value)),
		Gen.Select(s_keys, Gen.Bool, (key, value) => (QueryChange)new AddIfTrue(key, value)),
		Gen.Select(s_keys, Gen.Int, (key, value) => (QueryChange)new AddOrUpdateInt(key, value)),
		Gen.Select(s_keys, Gen.Long, (key, value) => (QueryChange)new AddOrUpdateLong(key, value)),
		Gen.Select(s_keys, s_strings, (key, value) => (QueryChange)new AddOrUpdateString(key, value)),
		s_keys.Select(key => (QueryChange)new RemoveKey(key)),
		Gen.Int[0, 16].Select(index => (QueryChange)new RemoveAtIndex(index)),
		Gen.Const<QueryChange>(new ClearAll()));

	/// <summary>
	/// An API request goes to the scheme and server of the connection's URL, then its URL base without a trailing slash,
	/// then <c>/api/v3/</c>, or <c>/v3/</c> under <c>-NoApiInPath</c>, then the endpoint's path and query with the
	/// escaping that the caller gave them, and the result reports that URL, from the synchronous send and the
	/// asynchronous one alike. A path that starts with <c>api/v3</c> already gets the URL base without the prefix, one
	/// that starts with the URL base and <c>api/v3</c> already, in any case, goes out as it is, and a URL longer than the
	/// 256 characters that it's built in on the stack goes out as a shorter one does.
	/// </summary>
	/// <param name="serviceUri">The URL that the connection names.</param>
	/// <param name="noApiInPath"><see langword="true"/> to connect as <c>-NoApiInPath</c> does.</param>
	/// <param name="path">The path and query that the caller passes.</param>
	/// <param name="expected">The URL that the request goes to.</param>
	[Theory]
	[MemberData(nameof(ApiUrls))]
	public void SendsAnApiRequestToItsUrlBasePrefixAndPath(string serviceUri, bool noApiInPath, string path, string expected)
	{
		ConcurrentQueue<string> received = new();
		using ServiceProvider provider = ModuleServices.Build(RecordUrls(received, serviceUri, noApiInPath));
		using IServiceScope scope = provider.CreateScope();
		ISonarrClient client = scope.ServiceProvider.GetRequiredService<ISonarrClient>();

		SonarrClientResult[] results = SendGetBothWays(client, path, TestContext.Current.CancellationToken);

		Assert.Equal([expected, expected], received.ToArray());
		foreach (SonarrClientResult result in results)
		{
			Assert.False(result.IsError, result.Error?.Exception.ToString());
			Assert.Equal(new Uri(expected).ToString(), result.RequestUrl);
		}
	}

	public static TheoryData<string, bool, string, string> ApiUrls => new()
	{
		{ "http://sonarr.test:8989/", false, "/series", "http://sonarr.test:8989/api/v3/series" },
		{ "http://sonarr.test:8989", false, "/series", "http://sonarr.test:8989/api/v3/series" },
		{ "http://sonarr.test:8989/", true, "/series", "http://sonarr.test:8989/v3/series" },
		{ "http://sonarr.test:8989/sonarr", false, "/series/1", "http://sonarr.test:8989/sonarr/api/v3/series/1" },
		{ "http://sonarr.test:8989/sonarr/", false, "/series/1", "http://sonarr.test:8989/sonarr/api/v3/series/1" },
		{ "http://sonarr.test:8989/sonarr/", true, "/series/1", "http://sonarr.test:8989/sonarr/v3/series/1" },
		{ "https://media.test/apps/sonarr/", false, "/tag", "https://media.test/apps/sonarr/api/v3/tag" },
		{ "http://sonarr.test:8989/", false, "/api/v3/series", "http://sonarr.test:8989/api/v3/series" },
		{ "http://sonarr.test:8989/sonarr/", false, "/api/v3/series", "http://sonarr.test:8989/sonarr/api/v3/series" },
		{ "http://sonarr.test:8989/sonarr/", false, "/sonarr/api/v3/series", "http://sonarr.test:8989/sonarr/api/v3/series" },
		{ "http://sonarr.test:8989/Sonarr/", false, "/sonarr/api/v3/series", "http://sonarr.test:8989/sonarr/api/v3/series" },
		{
			"http://sonarr.test:8989/",
			false,
			"/series/lookup?term=a%20b%26c%2Bd%3D",
			"http://sonarr.test:8989/api/v3/series/lookup?term=a%20b%26c%2Bd%3D"
		},
		{
			"http://sonarr.test:8989/sonarr/",
			false,
			"/filesystem?path=%2Ftv%2FShow%20Name&includeFiles=true",
			"http://sonarr.test:8989/sonarr/api/v3/filesystem?path=%2Ftv%2FShow%20Name&includeFiles=true"
		},
		{
			"http://sonarr.test:8989/sonarr/",
			false,
			"/series/lookup?term=" + s_longText,
			"http://sonarr.test:8989/sonarr/api/v3/series/lookup?term=" + s_longText
		},
	};

	/// <summary>
	/// A ping goes to the connection's URL base and <c>/ping</c>, without the API prefix, under <c>-NoApiInPath</c> or
	/// not.
	/// </summary>
	/// <param name="serviceUri">The URL that the connection names.</param>
	/// <param name="noApiInPath"><see langword="true"/> to connect as <c>-NoApiInPath</c> does.</param>
	/// <param name="expected">The URL that the ping goes to.</param>
	[Theory]
	[InlineData("http://sonarr.test:8989/", false, "http://sonarr.test:8989/ping")]
	[InlineData("http://sonarr.test:8989/", true, "http://sonarr.test:8989/ping")]
	[InlineData("http://sonarr.test:8989/sonarr/", false, "http://sonarr.test:8989/sonarr/ping")]
	[InlineData("http://sonarr.test:8989/sonarr/", true, "http://sonarr.test:8989/sonarr/ping")]
	public async Task SendsAPingToItsUrlBaseWithoutThePrefix(string serviceUri, bool noApiInPath, string expected)
	{
		ConcurrentQueue<string> received = new();
		using ServiceProvider provider = ModuleServices.Build(RecordUrls(received, serviceUri, noApiInPath));
		using IServiceScope scope = provider.CreateScope();
		ISonarrClient client = scope.ServiceProvider.GetRequiredService<ISonarrClient>();

		SonarrClientResult result = await client.SendPingAsync(TestContext.Current.CancellationToken);

		Assert.False(result.IsError, result.Error?.Exception.ToString());
		Assert.Equal([expected], received.ToArray());
	}

	/// <summary>
	/// A download goes to the connection's scheme and server, then its URL base, then the download URL, and a URL that
	/// starts with the URL base already, in any case and as whole segments, as a log file's URL from Sonarr does, gets
	/// the URL base in place of its own spelling. The URL base is spelled as the server's status reported it when the two
	/// match without regard to case, and as the connection's URL has it otherwise, as for a server behind a reverse proxy
	/// that maps one path to another. An absolute URL goes out as it is.
	/// </summary>
	/// <param name="serviceUri">The URL that the connection names.</param>
	/// <param name="serverUrlBase">The URL base that the server's status reported, or <see langword="null"/> for
	/// none.</param>
	/// <param name="url">The download URL that Sonarr gave.</param>
	/// <param name="expected">The URL that the download goes to.</param>
	[Theory]
	[MemberData(nameof(DownloadUrls))]
	public async Task DownloadsAFileFromItsUrlBaseAndDownloadUrl(string serviceUri, string? serverUrlBase, string url, string expected)
	{
		ConcurrentQueue<string> received = new();
		FakeSonarr sonarr = new("downloads", new ProbeLog(), (request, _) =>
		{
			received.Enqueue(request.RequestUri!.AbsoluteUri);
			return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
			{
				RequestMessage = request,
				Content = new ByteArrayContent([1, 2, 3]),
			});
		}, serviceUri: new Uri(serviceUri));
		sonarr.Settings.ServerUrlBase = serverUrlBase;

		CancellationToken token = TestContext.Current.CancellationToken;
		DirectoryInfo folder = Directory.CreateTempSubdirectory("PoshSonarr.Tests.");
		try
		{
			string path = Path.Combine(folder.FullName, "download.bin");
			using ServiceProvider provider = ModuleServices.Build(sonarr);
			using IServiceScope scope = provider.CreateScope();
			ISonarrDownloadClient client = scope.ServiceProvider.GetRequiredService<ISonarrDownloadClient>();

			SonarrClientResult<string> result = await client.DownloadToPathAsync(url, path, token);

			Assert.False(result.IsError, result.Error?.Exception.ToString());
			Assert.Equal([expected], received.ToArray());
			Assert.Equal((byte[])[1, 2, 3], await File.ReadAllBytesAsync(path, token));
		}
		finally
		{
			folder.Delete(recursive: true);
		}
	}

	public static TheoryData<string, string?, string, string> DownloadUrls => new()
	{
		{ "http://sonarr.test:8989/", null, "/backup/manual/sonarr_backup.zip", "http://sonarr.test:8989/backup/manual/sonarr_backup.zip" },
		{ "http://sonarr.test:8989/sonarr/", null, "/backup/manual/sonarr_backup.zip", "http://sonarr.test:8989/sonarr/backup/manual/sonarr_backup.zip" },
		{ "http://sonarr.test:8989/sonarr", null, "backup/manual/sonarr_backup.zip", "http://sonarr.test:8989/sonarr/backup/manual/sonarr_backup.zip" },
		{ "http://sonarr.test:8989/sonarr/", null, "/sonarr/logfile/sonarr.txt", "http://sonarr.test:8989/sonarr/logfile/sonarr.txt" },
		{ "http://sonarr.test:8989/Sonarr/", null, "/sonarr/logfile/sonarr.txt", "http://sonarr.test:8989/Sonarr/logfile/sonarr.txt" },
		{ "http://sonarr.test:8989/son/", null, "/sonarr/logfile/sonarr.txt", "http://sonarr.test:8989/son/sonarr/logfile/sonarr.txt" },
		{ "http://sonarr.test:8989/sonarr/", null, "http://files.test:9000/backup.zip", "http://files.test:9000/backup.zip" },
		{ "http://sonarr.test:8989/Sonarr/", "/sonarr", "/backup/manual/sonarr_backup.zip", "http://sonarr.test:8989/sonarr/backup/manual/sonarr_backup.zip" },
		{ "http://sonarr.test:8989/Sonarr/", "/sonarr", "/sonarr/logfile/sonarr.txt", "http://sonarr.test:8989/sonarr/logfile/sonarr.txt" },
		{ "http://sonarr.test:8989/sonarr/", "/Sonarr", "/SONARR/logfile/sonarr.txt", "http://sonarr.test:8989/Sonarr/logfile/sonarr.txt" },
		{ "http://sonarr.test:8989/tv/", "/sonarr", "/backup/manual/sonarr_backup.zip", "http://sonarr.test:8989/tv/backup/manual/sonarr_backup.zip" },
		{ "http://sonarr.test:8989/", "", "/backup/manual/sonarr_backup.zip", "http://sonarr.test:8989/backup/manual/sonarr_backup.zip" },
	};

	/// <summary>
	/// A <see cref="QueryCol"/> writes <c>?</c> and its fields joined by <c>&amp;</c>, in the order of their keys under
	/// <see cref="StringComparer.OrdinalIgnoreCase"/>, with one field for each key, which a second add of turns away and
	/// an <c>AddOrUpdate</c> replaces. A field writes its numbers and dates in the invariant culture, under every culture,
	/// its string as it is, and its key alone for a string that's blank or missing. Its
	/// <see cref="QueryCol.MaxLength"/> never falls short of what it writes, whatever adds, replaces, and removes its
	/// fields, so <see cref="QueryColExtensions.GetUrl(QueryCol?, string)"/> never throws, and its <c>TryFormat</c>
	/// fills a span of exactly that length and turns away a shorter one without throwing.
	/// </summary>
	[Fact]
	public void WritesItsFieldsInKeyOrderWithinItsMaxLength()
	{
		foreach (string culture in CultureScope.Names)
		{
			using CultureScope scope = CultureScope.Set(culture);
			s_queryChanges.Array[0, 24].Sample(changes =>
			{
				QueryCol query = new();
				SortedDictionary<string, string> model = new(StringComparer.OrdinalIgnoreCase);

				foreach (QueryChange change in changes)
				{
					Apply(query, model, change);
					AssertWrites(query, model);
				}
			}, iter: 300, print: changes => $"under '{culture}': {string.Join(", ", changes)}");
		}
	}

	/// <summary>
	/// Makes a change to a query and to the model of what it should write: the field text that each key writes, in
	/// key order.
	/// </summary>
	private static void Apply(QueryCol query, SortedDictionary<string, string> model, QueryChange change)
	{
		switch (change)
		{
			case AddInt(string key, int value):
				Add(model, key, Field(key, value.ToString(CultureInfo.InvariantCulture)), () => query.Add(key, value));
				break;

			case AddLong(string key, long value):
				Add(model, key, Field(key, value.ToString(CultureInfo.InvariantCulture)), () => query.Add(key, value));
				break;

			case AddString(string key, var value):
				Add(model, key, Field(key, value), () => query.Add(key, value));
				break;

			case AddDate(string key, DateTime value):
				string format = Constants.CalendarDateTimeFormat;
				Add(model, key, Field(key, value.ToString(format, CultureInfo.InvariantCulture)), () => query.Add(key, value, format.Length, format));
				break;

			case AddBoolean(string key, bool value):
				Add(model, key, Field(key, value ? "true" : "false"), () => query.AddBoolean(value, key));
				break;

			case AddIfTrue(string key, bool value) when value:
				Add(model, key, Field(key, "true"), () => query.AddIfTrue(value, key));
				break;

			case AddIfTrue(string key, bool value):
				query.AddIfTrue(value, key);
				break;

			case AddOrUpdateInt(string key, int value):
				query.AddOrUpdate(key, value);
				model[key] = Field(key, value.ToString(CultureInfo.InvariantCulture));
				break;

			case AddOrUpdateLong(string key, long value):
				query.AddOrUpdate(key, value);
				model[key] = Field(key, value.ToString(CultureInfo.InvariantCulture));
				break;

			case AddOrUpdateString(string key, var value):
				query.AddOrUpdate(key, value);
				model[key] = Field(key, value);
				break;

			case RemoveKey(string key):
				Assert.Equal(model.Remove(key), query.Remove(key));
				break;

			case RemoveAtIndex(int index) when model.Count > 0:
				int at = index % model.Count;
				string removed = model.Keys.ElementAt(at);
				Assert.Equal(removed, query[at].Key, ignoreCase: true);
				query.RemoveAt(at);
				_ = model.Remove(removed);
				break;

			case RemoveAtIndex:
				break;

			case ClearAll:
				query.Clear();
				model.Clear();
				break;

			default:
				throw new UnreachableException($"No case for {change}.");
		}
	}

	/// <summary>
	/// Adds a field to a query, which turns it away when the query holds its key already, and to the model when it
	/// doesn't.
	/// </summary>
	private static void Add(SortedDictionary<string, string> model, string key, string field, Action add)
	{
		if (model.ContainsKey(key))
		{
			_ = Assert.Throws<ArgumentException>(add);
			return;
		}

		add();
		model.Add(key, field);
	}

	/// <summary>
	/// Gives the text of a field: the key alone for a value that's blank or missing, and the key, <c>=</c>, and the value
	/// otherwise.
	/// </summary>
	private static string Field(string key, string? value)
	{
		return string.IsNullOrWhiteSpace(value) ? key : string.Concat(key, "=", value);
	}

	/// <summary>
	/// Asserts that a query writes the fields of the model in their order, and that it fits what it writes in its
	/// maximum length.
	/// </summary>
	private static void AssertWrites(QueryCol query, SortedDictionary<string, string> model)
	{
		string expected = model.Count == 0 ? string.Empty : "?" + string.Join('&', model.Values);

		Assert.Equal(model.Count, query.Count);
		Assert.Equal(expected, ((IFormattable)query).ToString(format: null, formatProvider: null));
		Assert.True(query.MaxLength >= expected.Length, $"MaxLength is {query.MaxLength}, and the query writes {expected.Length} characters.");
		Assert.Equal("/series" + expected, query.GetUrl("/series"));

		char[] exact = new char[expected.Length];
		Assert.True(query.TryFormat(exact, out int written), "TryFormat turned away a span of exactly the query's length.");
		Assert.Equal(expected, new string(exact, 0, written));

		if (expected.Length > 0)
		{
			Assert.False(query.TryFormat(new char[expected.Length - 1], out _), "TryFormat wrote the query into a span that's too short.");
		}
	}

	/// <summary>
	/// Sends a GET request for the specified path through the synchronous send and then through the asynchronous one,
	/// blocking on it as a cmdlet does.
	/// </summary>
	private static SonarrClientResult[] SendGetBothWays(ISonarrClient client, string path, CancellationToken token)
	{
		return
		[
			client.SendGet<object>(path, token),
			client.SendGetAsync<object>(path, token).GetAwaiter().GetResult(),
		];
	}

	/// <summary>
	/// Creates a fake instance that answers every request with <c>200 OK</c> and records the absolute URL of each.
	/// </summary>
	private static FakeSonarr RecordUrls(ConcurrentQueue<string> received, string serviceUri, bool noApiInPath)
	{
		return new FakeSonarr("urls", new ProbeLog(), (request, _) =>
		{
			received.Enqueue(request.RequestUri!.AbsoluteUri);
			return Task.FromResult(Respond.Ok(request));
		}, serviceUri: new Uri(serviceUri), noApiInPath: noApiInPath);
	}

	/// <summary>
	/// A change that a cmdlet makes to its query.
	/// </summary>
	private abstract record QueryChange;

	private sealed record AddInt(string Key, int Value) : QueryChange;

	private sealed record AddLong(string Key, long Value) : QueryChange;

	private sealed record AddString(string Key, string? Value) : QueryChange;

	private sealed record AddDate(string Key, DateTime Value) : QueryChange;

	private sealed record AddBoolean(string Key, bool Value) : QueryChange;

	private sealed record AddIfTrue(string Key, bool Value) : QueryChange;

	private sealed record AddOrUpdateInt(string Key, int Value) : QueryChange;

	private sealed record AddOrUpdateLong(string Key, long Value) : QueryChange;

	private sealed record AddOrUpdateString(string Key, string? Value) : QueryChange;

	private sealed record RemoveKey(string Key) : QueryChange;

	private sealed record RemoveAtIndex(int Index) : QueryChange;

	private sealed record ClearAll : QueryChange;
}
