using MG.Sonarr.Next.Buffers;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Json.Naming;
using MG.Sonarr.Next.Services.Http;
using System.Management.Automation;

namespace MG.Sonarr.Next.Json.Converters;

public sealed class SonarrResponseConverter : ByPolicyJsonConverter<ISonarrResponse>
{
	public SonarrResponseConverter(WorkingNamingPolicy namingPolicy)
		: base(namingPolicy)
	{
	}

	public override bool CanConvert(Type typeToConvert)
	{
		return typeof(ISonarrResponse).IsAssignableFrom(typeToConvert);
	}

	public override ISonarrResponse? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		throw new NotSupportedException();
	}

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

	private static void WriteAsError(Utf8JsonWriter writer, ErrorRecord record, ISonarrResponse value, WorkingNamingPolicy policy)
	{
		const int max_stackalloc = 128;

		Type errType = record.Exception.GetType();
		writer.WriteString(policy, value.RequestUrl);
		writer.WriteNumber(policy, "StatusCode"u8, (int)value.StatusCode);
		writer.WriteFormattable(policy, "Status"u8, value.StatusCode, max_stackalloc);
		writer.WriteString(policy, "ErrorType"u8, errType.GetName());
		writer.WriteString(policy, record.Exception.GetBaseException().Message);
	}

	[SkipLocalsInit]
	private static void WriteAsSuccess(Utf8JsonWriter writer, ISonarrResponse value)
	{
		const string RECEIVED = "Received response -> ";
		const int buffer_size = 256;

		using SpanStringBuilder builder = new(stackalloc char[buffer_size]);
		builder.Append(RECEIVED);
		builder.Append((int)value.StatusCode, LengthConstants.INT_MAX);
		builder.AppendChars(' ', '(');
		builder.Append(value.StatusCode);
		builder.Append(')');

		writer.WriteRawValue(builder.AsSpan(), skipInputValidation: true);
	}
}
