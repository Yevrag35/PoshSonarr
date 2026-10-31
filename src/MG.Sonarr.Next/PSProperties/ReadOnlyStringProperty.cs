using System.Management.Automation;

namespace MG.Sonarr.Next.PSProperties;

/// <summary>
/// Represents a read-only note property that holds a string.
/// </summary>
public sealed class ReadOnlyStringProperty : ReadOnlyProperty<string>
{
	/// <summary>
	/// Gets the string that the property holds.
	/// </summary>
	/// <value>The property's value, which is never null.</value>
	public string StringValue { get; }
	/// <summary>
	/// Gets the full name of the property's value type.
	/// </summary>
	/// <value><c>System.String</c>.</value>
	public override string TypeNameOfValue => StringNoteProperty.StringType;
	/// <summary>
	/// Gets the property's value as a <see cref="string"/>.
	/// </summary>
	/// <value>The value of <see cref="StringValue"/>.</value>
	protected override string ValueAsT => this.StringValue;

	/// <summary>
	/// Initializes a new <see cref="ReadOnlyStringProperty"/> instance with the specified name and string.
	/// </summary>
	/// <param name="propertyName">The name of the property.</param>
	/// <param name="value">The property's value, or <see langword="null"/> for an empty string.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="propertyName"/> is empty.</exception>
	public ReadOnlyStringProperty(string propertyName, string? value)
		: base(propertyName)
	{
		this.StringValue = value ?? string.Empty;
	}

	/// <summary>
	/// Creates a copy of the property.
	/// </summary>
	/// <returns>A new <see cref="ReadOnlyStringProperty"/> with the same name and string.</returns>
	public override PSMemberInfo Copy()
	{
		return new ReadOnlyStringProperty(this.Name, this.StringValue);
	}
}

