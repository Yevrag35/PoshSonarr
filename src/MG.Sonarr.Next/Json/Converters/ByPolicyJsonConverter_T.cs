using MG.Sonarr.Next.Json.Naming;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Json.Converters;

/// <summary>
/// Provides a base class for the module's JSON converters that write property names through a
/// <see cref="WorkingNamingPolicy"/>.
/// </summary>
/// <remarks>
/// A connection builds one naming policy for its serializer options and passes it to each converter that derives
/// from this class, so every request body names its properties the same way.
/// </remarks>
/// <typeparam name="T">The type that the converter writes.</typeparam>
public abstract class ByPolicyJsonConverter<T> : JsonConverter<T>
{
	/// <summary>
	/// Gets the naming policy that the converter writes property names with.
	/// </summary>
	/// <value>The naming policy that the constructor received.</value>
	protected internal WorkingNamingPolicy NamingPolicy { get; }

	/// <summary>
	/// Initializes a new <see cref="ByPolicyJsonConverter{T}"/> instance that writes property names with the specified
	/// naming policy.
	/// </summary>
	/// <param name="namingPolicy">The naming policy that the converter writes property names with.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="namingPolicy"/> is null.</exception>
	protected ByPolicyJsonConverter(WorkingNamingPolicy namingPolicy)
	{
		ArgumentNullException.ThrowIfNull(namingPolicy);
		this.NamingPolicy = namingPolicy;
	}
	/// <summary>
	/// Throws a <see cref="NotSupportedException"/>, because the converter only writes, unless a derived class
	/// overrides the method.
	/// </summary>
	/// <param name="reader">The reader positioned at the value.</param>
	/// <param name="typeToConvert">The type to convert the value to.</param>
	/// <param name="options">The serializer options in use.</param>
	/// <returns>Nothing, since the method always throws.</returns>
	/// <exception cref="NotSupportedException">Thrown when the method is called.</exception>
	public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		throw new NotSupportedException();
	}
}
