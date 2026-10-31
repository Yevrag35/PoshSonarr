using System.Collections.Immutable;

namespace MG.Sonarr.Next.Json.Attributes;

/// <summary>
/// Specifies private field names to include in JSON metadata generation.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class JsonIncludePrivateFieldsAttribute : Attribute
{
	/// <summary>
	/// Gets the private field names to include.
	/// </summary>
	/// <value>The configured private field names.</value>
	public ImmutableArray<string> FieldNames { get; }

	/// <summary>
	/// Initializes a new <see cref="JsonIncludePrivateFieldsAttribute"/> instance with the specified field names.
	/// </summary>
	/// <param name="fieldNames">The private field names to include.</param>
	public JsonIncludePrivateFieldsAttribute(params string[] fieldNames)
	{
		this.FieldNames = [.. fieldNames];
	}
}
