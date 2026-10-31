using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.Errors;

/// <summary>
/// Represents one error in the body of a Sonarr error response, such as a field that failed validation.
/// </summary>
/// <remarks>
/// <para>
/// An error stands for no endpoint, so it keeps whatever metadata tag it has.
/// </para>
/// <para>
/// Errors sort by <see cref="PropertyName"/> and then by <see cref="Message"/>, ignoring case.
/// </para>
/// </remarks>
[SonarrObject]
public sealed class SonarrServerError : SonarrObject,
	IComparable<SonarrServerError>,
	ISerializableNames<SonarrServerError>
{
	/// <summary>
	/// The number of properties that a new error reserves room for.
	/// </summary>
	const int CAPACITY = 5;
	/// <summary>
	/// The type name of a property whose value is a string.
	/// </summary>
	const string STRING_TYPE = "System.String";
	/// <summary>
	/// The full name of the type, which <see cref="SetPSTypeName"/> inserts into the object's type names.
	/// </summary>
	static readonly string _typeName = typeof(SonarrServerError).GetName();

	/// <summary>
	/// Gets the error's message.
	/// </summary>
	/// <remarks>
	/// It's built while the error is deserialized, from the values of the string properties whose names end in
	/// <c>message</c>, ignoring case, such as <c>ErrorMessage</c>.
	/// </remarks>
	/// <value>
	/// Those values, each on its own line, or an empty string when the error has no such property.
	/// </value>
	public string Message { get; private set; } = string.Empty;
	/// <summary>
	/// Gets the name of the field that the error is about.
	/// </summary>
	/// <remarks>
	/// It's a copy of the <c>PropertyName</c> property, taken while the error is deserialized.
	/// </remarks>
	/// <value>The field's name, or <see langword="null"/> when Sonarr sends none.</value>
	public string? PropertyName { get; private set; }

	/// <summary>
	/// Initializes a new <see cref="SonarrServerError"/> instance with no properties.
	/// </summary>
	public SonarrServerError()
		: base(CAPACITY)
	{
	}

	/// <summary>
	/// Compares this error with another by property name and then by message.
	/// </summary>
	/// <param name="other">The error to compare with, or <see langword="null"/>.</param>
	/// <returns>
	/// A negative number if this error sorts before <paramref name="other"/>, 0 if they sort together, or a positive
	/// number if it sorts after. Both strings compare by the invariant culture, ignoring case.
	/// </returns>
	public int CompareTo(SonarrServerError? other)
	{
		int compare = StringComparer.InvariantCultureIgnoreCase.Compare(this.PropertyName, other?.PropertyName);
		if (compare == 0)
		{
			compare = StringComparer.InvariantCultureIgnoreCase.Compare(this.Message, other?.Message);
		}

		return compare;
	}
	/// <summary>
	/// Gets the metadata tag for an error, which is the tag that it already has.
	/// </summary>
	/// <param name="resolver">The resolver that holds the connection's tags, which an error doesn't use.</param>
	/// <param name="existing">The tag that the object has now.</param>
	/// <returns><paramref name="existing"/>.</returns>
	protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
	{
		return existing;
	}

	/// <summary>
	/// Finishes deserializing the error and builds <see cref="Message"/> and <see cref="PropertyName"/> from its
	/// properties.
	/// </summary>
	public override void OnDeserialized()
	{
		base.OnDeserialized();
		var col = this.Properties.Match("*message");
		if (col.Count > 0)
		{
			var values = col
				.Where(x => x.IsGettable && x.TypeNameOfValue == STRING_TYPE)
				.Select(x => x.Value.ToString());

			this.Message = string.Join(Environment.NewLine, values);
		}

		if (this.TryGetNonNullProperty(nameof(this.PropertyName), out string? propName))
		{
			this.PropertyName = propName;
		}
	}

	/// <summary>
	/// Inserts the error's own type name ahead of the base type names.
	/// </summary>
	protected override void SetPSTypeName()
	{
		base.SetPSTypeName();
		this.TypeNames.Insert(0, _typeName);
	}
}
