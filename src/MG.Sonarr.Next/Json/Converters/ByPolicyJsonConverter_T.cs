using MG.Sonarr.Next.Json.Naming;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Json.Converters;

/// <summary>
/// Provides a base class for JSON converters that apply a specific naming policy when serializing or deserializing
/// objects of type <typeparamref name="T"/>.
/// </summary>
/// <remarks>This abstract class is intended to be extended by custom JSON converters that require a specific
/// naming policy, such as camel case or snake case, to be applied during serialization or deserialization. The naming
/// policy is provided via the <see cref="WorkingNamingPolicy"/> property.</remarks>
/// <typeparam name="T">The type of object to be converted.</typeparam>
public abstract class ByPolicyJsonConverter<T> : JsonConverter<T>
{
	/// <summary>
	/// Gets the naming policy used to transform property names during serialization or deserialization.
	/// </summary>
	protected internal WorkingNamingPolicy NamingPolicy { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="ByPolicyJsonConverter{T}"/> class with the specified naming policy.
	/// </summary>
	/// <param name="namingPolicy">The naming policy to be used by the converter.</param>
	protected ByPolicyJsonConverter(WorkingNamingPolicy namingPolicy)
	{
		ArgumentNullException.ThrowIfNull(namingPolicy);
		this.NamingPolicy = namingPolicy;
	}
	/// <summary>
	/// Reads and converts the JSON data to the specified type.
	/// </summary>
	/// <param name="reader">The <see cref="Utf8JsonReader"/> to read the JSON data from.</param>
	/// <param name="typeToConvert">The type of the object to convert the JSON data to.</param>
	/// <param name="options">The <see cref="JsonSerializerOptions"/> to use for deserialization.</param>
	/// <returns>The converted object of type <typeparamref name="T"/> if the operation is supported; otherwise, this method throws
	/// an exception.</returns>
	/// <exception cref="NotSupportedException">Always thrown, as this method is not supported.</exception>
	public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		throw new NotSupportedException();
	}
}
