using MG.Sonarr.Next.Json.Naming;
using MG.Sonarr.Next.Models.Commands;

namespace MG.Sonarr.Next.Json.Converters;

public sealed class PostCommandWriter : ByPolicyJsonConverter<PostCommand>//JsonConverter<PostCommand>
{
	public PostCommandWriter(WorkingNamingPolicy namingPolicy)
		: base(namingPolicy)
	{
	}

	public override PostCommand? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		throw new NotSupportedException();
	}

	//const string SEND_UPDATES = "SendUpdatesToClient";
	const string TRIGGER = "Trigger";
	public override void Write(Utf8JsonWriter writer, PostCommand value, JsonSerializerOptions options)
	{
		const string MANUAL = "manual";

		ArgumentNullException.ThrowIfNull(writer);
		writer.WriteStartObject();
		var policy = this.NamingPolicy;

		if (!writer.TryWriteEndObject(value))
		{
			writer.WriteString(policy, value.Name);
			writer.WriteString(policy, value.CommandName);

			writer.WriteBoolean(policy, "SendUpdatesToClient"u8, true);
			writer.WriteBoolean(policy, value.UpdateScheduledTask);
			writer.WriteString(policy, "Trigger"u8, MANUAL);

			writer.WriteString(policy, "Priority"u8, value.Priority.ToStringFast());
			writer.WriteEndObject();
		}
	}
}
