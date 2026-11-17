using MG.Sonarr.Next.Json.Naming;
using MG.Sonarr.Next.Models.Commands;

namespace MG.Sonarr.Next.Json.Converters;

/// <summary>
/// Provides a write-only converter that writes a <see cref="PostCommand"/> as the body that starts a command.
/// </summary>
public sealed class PostCommandWriter : ByPolicyJsonConverter<PostCommand>
{
	/// <summary>
	/// Initializes a new <see cref="PostCommandWriter"/> instance that writes property names with the specified
	/// naming policy.
	/// </summary>
	/// <param name="namingPolicy">The naming policy that the converter writes property names with.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="namingPolicy"/> is null.</exception>
	public PostCommandWriter(WorkingNamingPolicy namingPolicy)
		: base(namingPolicy)
	{
	}

	/// <summary>
	/// Throws a <see cref="NotSupportedException"/>, because the converter only writes.
	/// </summary>
	/// <param name="reader">The reader positioned at the value.</param>
	/// <param name="typeToConvert">The type to convert the value to.</param>
	/// <param name="options">The serializer options in use.</param>
	/// <returns>Nothing, since the method always throws.</returns>
	/// <exception cref="NotSupportedException">Thrown when the method is called.</exception>
	public override PostCommand? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		throw new NotSupportedException();
	}

	//const string SEND_UPDATES = "SendUpdatesToClient";
	/// <summary>
	/// Writes a command's body.
	/// </summary>
	/// <remarks>
	/// The body holds the command's <c>name</c>, <c>commandName</c>, and <c>updateScheduledTask</c>, a
	/// <c>sendUpdatesToClient</c> of <see langword="true"/>, a <c>trigger</c> of <c>manual</c>, and the
	/// <c>priority</c>, with each name written through the naming policy. A <see langword="null"/> command is
	/// written as an empty object.
	/// </remarks>
	/// <param name="writer">The writer to write the body to.</param>
	/// <param name="value">The command to write.</param>
	/// <param name="options">The serializer options in use.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="writer"/> is null.</exception>
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
