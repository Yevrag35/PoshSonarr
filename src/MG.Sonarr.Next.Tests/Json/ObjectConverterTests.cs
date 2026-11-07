using MG.Sonarr.Next.Models.RemotePaths;
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
	/// and goes back to Sonarr as that string, exactly as it came for credentials, names, a provider field's value, a
	/// hint, a placeholder, and a release's guid, since Sonarr answers a number there with a 400 and the inference would
	/// read each of these as a number, a <see cref="Guid"/>, a date, or a <see cref="Version"/>.
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
	};

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
