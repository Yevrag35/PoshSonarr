using MG.Sonarr.Next.Json.Converters;
using MG.Sonarr.Next.Json.Naming;

namespace MG.Sonarr.Next.Tests.Json;

/// <summary>
/// Tests the naming policy that the module's converters write property names with.
/// </summary>
public sealed class NamingPolicyTests
{
	/// <summary>
	/// The names to write: ASCII letters in both cases, a digit, a space, an underscore, and letters outside ASCII in
	/// both cases, the Turkish dotted capital I and dotless small i among them.
	/// </summary>
	private static readonly Gen<string> s_names = Gen.OneOfConst(
			'A', 'B', 'I', 'a', 'b', 'i', '1', ' ', '_', 'É', 'é', 'Ä', 'ß', 'İ', 'ı', 'Ω', 'ω')
		.Array[1, 10]
		.Select(chars => new string(chars));

	/// <summary>
	/// The options that a request body's names are written with, which keep letters outside ASCII as they are.
	/// </summary>
	private static readonly JsonWriterOptions s_writerOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

	/// <summary>
	/// B72, B73: the module's converters write every property name through the connection's span policy, and each
	/// overload of <c>WritePropertyName</c> writes it as <see cref="JsonNamingPolicy.CamelCase"/> names it, letters
	/// outside ASCII included: a name whose first character isn't uppercase stays as it is, and otherwise the leading
	/// run of uppercase letters is lowercased, except for its last when a lowercase letter follows it.
	/// </summary>
	/// <remarks>
	/// The span policy converts a name in a buffer on the stack, to spare the string that the camel-case policy makes of
	/// each name, so the test holds it to that policy, first on the names that found the bugs and then on random ones.
	/// </remarks>
	[Fact]
	public void B72_WritesEachPropertyNameAsCamelCaseNamesIt()
	{
		JsonSerializerOptions options = ModuleServices.Json.ForSerializing;
		WorkingNamingPolicy[] policies =
		[
			options.Converters.OfType<ObjectConverter>().Single().NamingPolicy,
			options.Converters.OfType<PostCommandWriter>().Single().NamingPolicy,
			options.Converters.OfType<SonarrResponseConverter>().Single().NamingPolicy,
		];

		Assert.All(policies, policy => Assert.Same(JsonSpanCamelCaseNamingPolicy.SpanPolicy, policy.Policy));
		WorkingNamingPolicy policy = policies[0];

		string[] known = ["aB", "_ID", "1AB", "ID", "IMDbId", "URLValue", "A B", "AB CD", "É", "Été", "ÉTÉ", "İD", "IDÉ", "AΩb"];
		Assert.Multiple([.. known.Select(name => (Action)(() => AssertWritesAsCamelCase(policy, name)))]);

		s_names.Sample(name => AssertWritesAsCamelCase(policy, name), iter: 20_000, print: name => $"'{name}'");
	}

	/// <summary>
	/// Asserts that each overload of <c>WritePropertyName</c> writes the specified name as
	/// <see cref="JsonNamingPolicy.CamelCase"/> names it.
	/// </summary>
	private static void AssertWritesAsCamelCase(WorkingNamingPolicy policy, string name)
	{
		string expected = Write(writer => writer.WritePropertyName(JsonNamingPolicy.CamelCase.ConvertName(name)));

		(string Overload, string Written)[] writes =
		[
			("string", Write(writer => policy.WritePropertyName(writer, name))),
			("ReadOnlySpan<char>", Write(writer => policy.WritePropertyName(writer, name.AsSpan()))),
			("ReadOnlySpan<byte>", Write(writer => policy.WritePropertyName(writer, (ReadOnlySpan<byte>)Encoding.UTF8.GetBytes(name)))),
		];

		foreach ((string overload, string written) in writes)
		{
			Assert.True(
				expected == written,
				$"WritePropertyName({overload}) wrote '{name}' as {written}, where CamelCase gives {expected}.");
		}
	}

	/// <summary>
	/// Writes an object with one property, whose name the specified action writes, and a value of null.
	/// </summary>
	private static string Write(Action<Utf8JsonWriter> writeName)
	{
		ArrayBufferWriter<byte> buffer = new();
		using (Utf8JsonWriter writer = new(buffer, s_writerOptions))
		{
			writer.WriteStartObject();
			writeName(writer);
			writer.WriteNullValue();
			writer.WriteEndObject();
		}

		return Encoding.UTF8.GetString(buffer.WrittenSpan);
	}
}
