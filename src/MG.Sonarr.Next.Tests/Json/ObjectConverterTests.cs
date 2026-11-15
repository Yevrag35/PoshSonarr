using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models.Config;
using MG.Sonarr.Next.Models.DownloadClients;
using MG.Sonarr.Next.Models.Episodes;
using MG.Sonarr.Next.Models.Fields;
using MG.Sonarr.Next.Models.Media;
using MG.Sonarr.Next.Models.RemotePaths;
using MG.Sonarr.Next.Models.Series;
using MG.Sonarr.Next.Models.System;

namespace MG.Sonarr.Next.Tests.Json;

/// <summary>
/// Tests how the converter that every model shares reads Sonarr's JSON and writes it back.
/// </summary>
public sealed class ObjectConverterTests
{
	/// <summary>
	/// O23: <c>ReadNumber</c> reads a whole number exactly, as an <see cref="int"/> when it fits and as a
	/// <see cref="long"/> when it doesn't, and any other number as a <see cref="double"/>, as its doc says, in an object
	/// and in an array alike.
	/// </summary>
	/// <remarks>
	/// The models read their integers as <see cref="int"/> values, so a whole number that reads as a <see cref="long"/>
	/// leaves their C# copies, such as an episode's IDs, at 0.
	/// </remarks>
	/// <param name="literal">The number as JSON writes it.</param>
	/// <param name="expected">The value that it reads as, of the type that it reads as.</param>
	[Theory]
	[MemberData(nameof(Numbers))]
	public void O23_ReadsAJsonNumberAsTheNarrowestTypeThatHoldsIt(string literal, object expected)
	{
		PSObject pso = Deserialize<PSObject>($$"""{"number":{{literal}},"array":[{{literal}}]}""");

		object? inObject = pso.Properties["Number"].Value;
		object? inArray = Assert.IsType<object[]>(pso.Properties["Array"].Value)[0];

		Assert.Multiple(
			() => Assert.IsType(expected.GetType(), inObject),
			() => Assert.Equal(expected, inObject),
			() => Assert.IsType(expected.GetType(), inArray),
			() => Assert.Equal(expected, inArray));
	}

	public static TheoryData<string, object> Numbers => new()
	{
		{ "5", 5 },
		{ "-5", -5 },
		{ "2147483647", int.MaxValue },
		{ "-2147483648", int.MinValue },
		{ "2147483648", 2147483648L },
		{ "-2147483649", -2147483649L },
		{ "9007199254740993", 9007199254740993L },
		{ "9223372036854775807", long.MaxValue },
		{ "1.0", 1 },
		{ "1.5", 1.5 },
		{ "1e20", 1e20 },
		{ "18446744073709551615", 18446744073709551615.0 },
	};

	/// <summary>
	/// O1, B65, B66, B3: Sonarr writes its values with the invariant culture, so the converter reads a string the same
	/// under every culture. It's a number only when it's the text of an <see cref="int"/>, a <see cref="long"/>, or a
	/// finite <see cref="double"/>, with no group separators or trailing sign, a date only in the ISO 8601 forms that
	/// Sonarr writes, a <see cref="Version"/> when it has three or four parts, and a string otherwise, and a property to
	/// capitalize gets its first letter uppercased with the invariant culture.
	/// </summary>
	/// <param name="culture">The current culture's name, or empty text for the invariant culture.</param>
	[Theory]
	[MemberData(nameof(Cultures))]
	public void O1_ReadsSonarrsTextTheSameInEveryCulture(string culture)
	{
		DateTimeOffset instant = new(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);
		(string Field, string Text, object Expected)[] cases =
		[
			("decimal", "1.5", 1.5),
			("negative", "-5", -5),
			("large", "3000000000", 3000000000L),
			("exponent", "1.5e3", 1500.0),
			("dottedQuad", "192.168.1.10", new Version(192, 168, 1, 10)),
			("threePartVersion", "3.31.1", new Version(3, 31, 1)),
			("uniqueId", "0f8fad5b-d9cb-469f-a165-70867728950e", new Guid("0f8fad5b-d9cb-469f-a165-70867728950e")),
			("isoDate", "2024-01-15", new DateOnly(2024, 1, 15)),
			("changedUtc", "2024-01-15T10:30:00Z", instant),
			("changed", "2024-01-15T10:30:00.5+02:00", instant.AddHours(-2).AddMilliseconds(500)),
			("unzoned", "2024-01-15T10:30", instant),
			("clockTime", "12:00:00", "12:00:00"),
			("lastDuration", "00:00:00.0210631", "00:00:00.0210631"),
			("slashDate", "1/2", "1/2"),
			("commaDecimal", "1,5", "1,5"),
			("grouped", "1,000", "1,000"),
			("notANumber", "NaN", "NaN"),
			("pastDouble", "1e400", "1e400"),
			("trailingSign", "5-", "5-"),
			("minusSign", "−5", "−5"),
		];

		string body = JsonSerializer.Serialize(cases.ToDictionary(c => c.Field, c => c.Text));

		PSObject pso;
		HostObject host;
		using (CultureScope.Set(culture))
		{
			pso = Deserialize<PSObject>(body);
			host = Deserialize<HostObject>("""{"id":1,"logLevel":"info","proxyType":"http"}""");
		}

		Assert.Multiple(
		[
			.. cases.Select(c => (Action)(() =>
			{
				object? actual = pso.Properties[char.ToUpperInvariant(c.Field[0]) + c.Field[1..]].Value;
				Assert.True(
					c.Expected.GetType() == actual?.GetType() && c.Expected.Equals(actual),
					$"'{c.Text}' read as {Show(actual)}, where {Show(c.Expected)} was expected.");
			})),
			() => Assert.Equal(TimeSpan.Zero, Assert.IsType<DateTimeOffset>(pso.Properties["ChangedUtc"].Value).Offset),
			() => Assert.Equal("Info", host.Properties["LogLevel"].Value),
			() => Assert.Equal("Http", host.Properties["ProxyType"].Value),
		]);
	}

	public static TheoryData<string> Cultures => [.. CultureScope.Names];

	/// <summary>
	/// B2, O66, B69, B93, O22: a value whose property is on the always-string list reads as the string that Sonarr sent
	/// and goes back to Sonarr as that string, exactly as it came for credentials, names, a show's slug, folder, network,
	/// and overview, an alternate title's comment, a release's group and name, the languages that a file's tags name,
	/// text that a person types into Sonarr's settings, paths and URLs included, a provider field's value, a hint, a
	/// placeholder, and a release's guid, since Sonarr answers a number there with a 400 and the inference would read
	/// each of these as a number, a <see cref="Guid"/>, a date, or a <see cref="Version"/>.
	/// </summary>
	/// <param name="field">The JSON field's name.</param>
	/// <param name="value">The string that Sonarr sent.</param>
	/// <param name="model">The type to read the object as.</param>
	[Theory]
	[MemberData(nameof(AlwaysStrings))]
	public void B2_SendsAnAlwaysStringValueBackAsItCame(string field, string value, Type model)
	{
		string body = $$"""{"id":1,"{{field}}":{{JsonSerializer.Serialize(value)}}}""";

		object read = JsonSerializer.Deserialize(body, model, ModuleServices.Json.ForDeserializing)!;
		string written = JsonSerializer.Serialize(read, read.GetType(), ModuleServices.Json.ForSerializing);

		using JsonDocument document = JsonDocument.Parse(written);
		Assert.True(document.RootElement.TryGetProperty(field, out JsonElement element), $"The body lacks '{field}': {written}");
		Assert.Equal(JsonValueKind.String, element.ValueKind);
		Assert.Equal(value, element.GetString());
	}

	public static TheoryData<string, string, Type> AlwaysStrings => new()
	{
		{ "label", "1080", typeof(PSObject) },
		{ "chmodFolder", "755", typeof(PSObject) },
		{ "chownGroup", "100", typeof(PSObject) },
		{ "username", "007", typeof(PSObject) },
		{ "password", " p@ss 2024 ", typeof(PSObject) },
		{ "apiKey", "0123456789abcdef0123456789abcdef", typeof(PSObject) },
		{ "name", "2024", typeof(PSObject) },
		{ "name", "007", typeof(PSObject) },
		{ "name", " Spaced Name ", typeof(PSObject) },
		{ "value", "0123", typeof(PSObject) },
		{ "hint", "2020", typeof(PSObject) },
		{ "placeholder", "8080", typeof(PSObject) },
		{ "guid", "12345", typeof(PSObject) },
		{ "guid", "2024-01-15", typeof(PSObject) },
		{ "guid", "0f8fad5b-d9cb-469f-a165-70867728950e", typeof(PSObject) },
		{ "host", "192.168.1.10", typeof(RemotePathObject) },
		{ "titleSlug", "24", typeof(SeriesObject) },
		{ "folder", "1883", typeof(AddSeriesObject) },
		{ "network", "10", typeof(SeriesObject) },
		{ "overview", "2024", typeof(SeriesObject) },
		{ "comment", "2", typeof(PSObject) },
		{ "releaseGroup", "126811", typeof(PSObject) },
		{ "sceneName", "1883", typeof(PSObject) },
		{ "audioLanguages", "1", typeof(PSObject) },
		{ "subtitles", "007", typeof(PSObject) },
		{ "allowedHosts", "10", typeof(HostObject) },
		{ "applicationUrl", "8989", typeof(HostObject) },
		{ "backupFolder", "2024", typeof(HostObject) },
		{ "bindAddress", "1", typeof(HostObject) },
		{ "branch", "4", typeof(HostObject) },
		{ "instanceName", "2", typeof(HostObject) },
		{ "proxyBypassFilter", "10", typeof(HostObject) },
		{ "proxyHostname", "10", typeof(HostObject) },
		{ "sslCertPath", "001", typeof(HostObject) },
		{ "trustedNetworks", "10", typeof(HostObject) },
		{ "updateScriptPath", "007", typeof(HostObject) },
		{ "animeEpisodeFormat", "001", typeof(NamingConfigObject) },
		{ "customColonReplacementFormat", "0", typeof(NamingConfigObject) },
		{ "dailyEpisodeFormat", "2024", typeof(NamingConfigObject) },
		{ "seasonFolderFormat", "01", typeof(NamingConfigObject) },
		{ "seriesFolderFormat", "24", typeof(NamingConfigObject) },
		{ "specialsFolderFormat", "00", typeof(NamingConfigObject) },
		{ "standardEpisodeFormat", "001", typeof(NamingConfigObject) },
		{ "extraFileExtensions", "001", typeof(MediaManagementObject) },
		{ "scriptImportPath", "01", typeof(MediaManagementObject) },
		{ "remotePath", "2024", typeof(RemotePathObject) },
	};

	/// <summary>
	/// B2: in an array whose property is on the always-string list, each string reads as the string that Sonarr sent and
	/// goes back to Sonarr as that string, and each number stays a number, so a provider field's list, such as Flood's
	/// tags, goes back as it came, while an indexer's categories stay numbers.
	/// </summary>
	[Fact]
	public void B2_SendsEachStringOfAnAlwaysStringArrayBackAsItCame()
	{
		const string body = """
			{
				"id": 1,
				"name": "Sandbox Flood",
				"fields": [
					{ "order": 0, "name": "tags", "type": "tag", "value": ["2024", "007", "sonarr"] },
					{ "order": 1, "name": "categories", "type": "select", "value": [5030, 5040] }
				]
			}
			""";

		object read = JsonSerializer.Deserialize(body, typeof(DownloadClientObject), ModuleServices.Json.ForDeserializing)!;
		string written = JsonSerializer.Serialize(read, read.GetType(), ModuleServices.Json.ForSerializing);

		using JsonDocument document = JsonDocument.Parse(written);
		JsonElement[] fields = [.. document.RootElement.GetProperty("fields").EnumerateArray()];
		Assert.Equal(["\"2024\"", "\"007\"", "\"sonarr\""], ValueOf(fields, "tags"));
		Assert.Equal(["5030", "5040"], ValueOf(fields, "categories"));

		static string[] ValueOf(JsonElement[] fields, string name)
		{
			JsonElement field = Assert.Single(fields, f => f.GetProperty("name").GetString() == name);
			return [.. field.GetProperty("value").EnumerateArray().Select(item => item.GetRawText())];
		}
	}

	/// <summary>
	/// A string that its property's span converter can't read, such as an air date that isn't a date, reads as the
	/// string that Sonarr sent, which a model keeps in view rather than holding it back, and goes back to Sonarr as that
	/// string.
	/// </summary>
	/// <param name="field">The JSON field's name.</param>
	/// <param name="value">The string that Sonarr sent.</param>
	/// <param name="model">The type to read the object as.</param>
	[Theory]
	[MemberData(nameof(UnreadableSpanValues))]
	public void KeepsAStringThatItsSpanConverterCantReadAsItCame(string field, string value, Type model)
	{
		string body = $$"""{"id":1,"{{field}}":{{JsonSerializer.Serialize(value)}}}""";

		PSObject read = Assert.IsAssignableFrom<PSObject>(JsonSerializer.Deserialize(body, model, ModuleServices.Json.ForDeserializing));
		object? inView = read.Properties[char.ToUpperInvariant(field[0]) + field[1..]]?.Value;
		string written = JsonSerializer.Serialize(read, read.GetType(), ModuleServices.Json.ForSerializing);

		using JsonDocument document = JsonDocument.Parse(written);
		Assert.Multiple(
			() => Assert.Equal(value, Assert.IsType<string>(inView)),
			() =>
			{
				Assert.True(document.RootElement.TryGetProperty(field, out JsonElement element), $"The body lacks '{field}': {written}");
				Assert.Equal(JsonValueKind.String, element.ValueKind);
				Assert.Equal(value, element.GetString());
			});
	}

	public static TheoryData<string, string, Type> UnreadableSpanValues => new()
	{
		{ "airDate", "TBA", typeof(EpisodeObject) },
		{ "firstAired", "TBA", typeof(SeriesObject) },
		{ "airTime", "evenings", typeof(SeriesObject) },
		{ "duration", "about an hour", typeof(PSObject) },
	};

	/// <summary>
	/// An array that doesn't read as the type that its property's name gives, such as tags that hold strings or a
	/// field's choices that hold numbers, reads as an array of objects, as an array under any other name does, so the
	/// rest of the body still reads, the property keeps what Sonarr sent, the model knows that its tags aren't tag IDs,
	/// and the array goes back to Sonarr as it came.
	/// </summary>
	[Fact]
	public void KeepsAnArrayThatDoesntFitItsTypeAsItCame()
	{
		const string body = """
			{
				"id": 1,
				"name": "Sandbox Flood",
				"priority": 1,
				"tags": ["anime", "4k"],
				"fields": [
					{ "order": 0, "name": "category", "type": "select", "value": 1, "selectOptions": [1, 2] }
				]
			}
			""";

		DownloadClientObject client = Deserialize<DownloadClientObject>(body);
		FieldObject field = Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<FieldObject>>(client.Properties["Fields"].Value));
		string written = JsonSerializer.Serialize(client, ModuleServices.Json.ForSerializing);

		using JsonDocument document = JsonDocument.Parse(written);
		JsonElement root = document.RootElement;
		Assert.Multiple(
			() => Assert.Equal(["anime", "4k"], Assert.IsType<object[]>(client.Properties["Tags"].Value)),
			() => Assert.False(((ITagPipeable)client).HasReadableTags),
			() => Assert.Equal(1, client.Properties["Priority"].Value),
			() => Assert.Equal([1, 2], Assert.IsType<object[]>(field.Properties["SelectOptions"].Value)),
			() => Assert.Empty(field.SelectOptions),
			() => Assert.Equal("""["anime","4k"]""", root.GetProperty("tags").GetRawText()),
			() => Assert.Equal("[1,2]", root.GetProperty("fields")[0].GetProperty("selectOptions").GetRawText()));
	}

	private static T Deserialize<T>(string json)
	{
		return JsonSerializer.Deserialize<T>(json, ModuleServices.Json.ForDeserializing)
			?? throw new InvalidOperationException("The body read as null.");
	}

	private static string Show(object? value)
	{
		return value is null
			? "null"
			: string.Create(CultureInfo.InvariantCulture, $"{value} ({value.GetType().Name})");
	}
}
