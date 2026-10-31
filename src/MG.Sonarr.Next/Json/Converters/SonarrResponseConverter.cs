using MG.Sonarr.Next.Buffers;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Json.Naming;
using MG.Sonarr.Next.Services.Http;
using System.Management.Automation;

namespace MG.Sonarr.Next.Json.Converters;

/// <summary>
/// Provides a write-only converter that writes the outcome of a request, an <see cref="ISonarrResponse"/>, for
/// display.
/// </summary>
public sealed class SonarrResponseConverter : ByPolicyJsonConverter<ISonarrResponse>
{
	/// <summary>
	/// Initializes a new <see cref="SonarrResponseConverter"/> instance that writes property names with the specified
	/// naming policy.
	/// </summary>
	/// <param name="namingPolicy">The naming policy that the converter writes property names with.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="namingPolicy"/> is null.</exception>
	public SonarrResponseConverter(WorkingNamingPolicy namingPolicy)
		: base(namingPolicy)
	{
	}

	/// <summary>
	/// Determines whether the converter can write the specified type.
	/// </summary>
	/// <param name="typeToConvert">The type to check.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="typeToConvert"/> is or implements <see cref="ISonarrResponse"/>;
	/// otherwise, <see langword="false"/>.
	/// </returns>
	public override bool CanConvert(Type typeToConvert)
	{
		return typeof(ISonarrResponse).IsAssignableFrom(typeToConvert);
	}

	/// <summary>
	/// Throws a <see cref="NotSupportedException"/>, because the converter only writes.
	/// </summary>
	/// <param name="reader">The reader positioned at the value.</param>
	/// <param name="typeToConvert">The type to convert the value to.</param>
	/// <param name="options">The serializer options in use.</param>
	/// <returns>Nothing, since the method always throws.</returns>
	/// <exception cref="NotSupportedException">Thrown when the method is called.</exception>
	public override ISonarrResponse? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		throw new NotSupportedException();
	}

	/// <summary>
	/// Writes the outcome of a request.
	/// </summary>
	/// <remarks>
	/// A failed request is written as an object with its request URL, its numeric status code, its status text, such as
	/// <c>404 (Not Found)</c>, its exception type, and the innermost exception's message. A successful one is written as
	/// the text <c>Received response -&gt; </c> followed by its status text, such as
	/// <c>Received response -&gt; 200 (OK)</c>, and a <see langword="null"/> response as an empty object.
	/// </remarks>
	/// <param name="writer">The writer to write the outcome to.</param>
	/// <param name="value">The outcome to write.</param>
	/// <param name="options">The serializer options in use.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="writer"/> is null.</exception>
	public override void Write(Utf8JsonWriter writer, ISonarrResponse value, JsonSerializerOptions options)
	{
		ArgumentNullException.ThrowIfNull(writer);
		if (value is null)
		{
			writer.WriteEmptyObject();
			return;
		}

		if (value.IsError)
		{
			writer.WriteStartObject();
			WriteAsError(writer, value.Error, value, this.NamingPolicy);
			writer.WriteEndObject();
		}
		else
		{
			WriteAsSuccess(writer, value);
		}
	}

	/// <summary>
	/// Writes the properties that describe a failed request into the object that the writer has open.
	/// </summary>
	/// <param name="writer">The writer whose open object receives the properties.</param>
	/// <param name="record">The error record of the failed request.</param>
	/// <param name="value">The outcome of the failed request.</param>
	/// <param name="policy">The naming policy that property names are written with.</param>
	private static void WriteAsError(Utf8JsonWriter writer, ErrorRecord record, ISonarrResponse value, WorkingNamingPolicy policy)
	{
		Type errType = record.Exception.GetType();
		writer.WriteString(policy, value.RequestUrl);
		writer.WriteNumber(policy, "StatusCode"u8, (int)value.StatusCode);
		writer.WriteString(policy, "Status"u8, value.StatusCode.AsResponseString);
		writer.WriteString(policy, "ErrorType"u8, errType.GetName());
		writer.WriteString(policy, record.Exception.GetBaseException().Message);
	}

	/// <summary>
	/// Writes the status line of a successful request as a JSON string.
	/// </summary>
	/// <remarks>
	/// The line is built in a 256-character stack buffer.
	/// </remarks>
	/// <param name="writer">The writer to write the line to.</param>
	/// <param name="value">The outcome of the successful request.</param>
	[SkipLocalsInit]
	private static void WriteAsSuccess(Utf8JsonWriter writer, ISonarrResponse value)
	{
		const string RECEIVED = "Received response -> ";
		const int buffer_size = 256;

		using SpanStringBuilder builder = new(stackalloc char[buffer_size]);
		builder.Append(RECEIVED);
		builder.Append(value.StatusCode.AsResponseString);

		writer.WriteRawValue(builder.AsSpan(), skipInputValidation: true);
	}
}
