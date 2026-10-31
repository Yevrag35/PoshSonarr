using System.Management.Automation;

namespace MG.Sonarr.Next.PSProperties;

/// <summary>
/// Represents a writable note property that holds a string.
/// </summary>
/// <remarks>
/// The property's value is never null: setting it to <see langword="null"/> stores an empty string, and setting it
/// to another value stores that value's text, formatted with <see cref="Statics.DefaultProvider"/>.
/// </remarks>
public sealed class StringNoteProperty : WritableProperty<string>
{
	/// <summary>
	/// The full name of <see cref="string"/>, which <see cref="TypeNameOfValue"/> returns.
	/// </summary>
	internal const string StringType = "System.String";
	/// <summary>
	/// The string that the property holds.
	/// </summary>
	private string _value;

	/// <summary>
	/// Gets or sets the string that the property holds.
	/// </summary>
	/// <value>The property's value, or an empty string when it's set to <see langword="null"/>.</value>
	public string StringValue
	{
		get => _value;
		set => _value = value ?? string.Empty;
	}
	/// <summary>
	/// Gets the full name of the property's value type.
	/// </summary>
	/// <value><c>System.String</c>.</value>
	public override string TypeNameOfValue => StringType;
	/// <summary>
	/// Gets or sets the property's value as a <see cref="string"/>.
	/// </summary>
	/// <value>The value of <see cref="StringValue"/>.</value>
	protected override string? ValueAsT
	{
		get => this.StringValue;
		set => this.StringValue = value!;
	}

	/// <summary>
	/// Initializes a new <see cref="StringNoteProperty"/> instance with the specified name and an empty string.
	/// </summary>
	/// <param name="propertyName">The name of the property.</param>
	/// <exception cref="ArgumentException">Thrown when <paramref name="propertyName"/> is null or empty.</exception>
	public StringNoteProperty(string propertyName)
	{
		this.SetMemberName(propertyName);
		_value = string.Empty;
	}
	/// <summary>
	/// Initializes a new <see cref="StringNoteProperty"/> instance with the specified name and string.
	/// </summary>
	/// <param name="name">The name of the property.</param>
	/// <param name="value">The property's initial value, or <see langword="null"/> for an empty string.</param>
	/// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or empty.</exception>
	public StringNoteProperty(string name, string? value)
	{
		this.SetMemberName(name);
		_value = value ?? string.Empty;
	}

	/// <summary>
	/// Creates a writable string property that holds the same string under the specified name.
	/// </summary>
	/// <param name="name">The name of the new property.</param>
	/// <returns>A new <see cref="StringNoteProperty"/> that holds <see cref="StringValue"/>.</returns>
	protected override PSPropertyInfo CopyIntoNew(string name)
	{
		return new StringNoteProperty(name, _value);
	}

	/// <summary>
	/// Converts a value that is being set on the property to a string.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	/// <returns>
	/// An empty string for <see langword="null"/>, <paramref name="value"/> itself when it's a string, or else its
	/// text from <see cref="IFormattable"/> or <see cref="IConvertible"/>.
	/// </returns>
	/// <exception cref="SetValueException">
	/// Thrown when <paramref name="value"/> is neither a string, an <see cref="IFormattable"/>, nor an
	/// <see cref="IConvertible"/>.
	/// </exception>
	protected override string ConvertFromObject(object? value)
	{
		return value switch
		{
			null => string.Empty,
			string strVal => strVal,
			IFormattable formattable => formattable.ToString(null, Statics.DefaultProvider),
			IConvertible icon => Convert.ToString(icon, Statics.DefaultProvider) ?? string.Empty,

			_ => this.ThrowNotType<string>(),
		};
	}
	/// <summary>
	/// Creates a read-only string property with the property's name and string.
	/// </summary>
	/// <returns>A new <see cref="ReadOnlyStringProperty"/> that holds <see cref="StringValue"/>.</returns>
	protected override ReadOnlyProperty<string> CopyToReadOnly()
	{
		return new ReadOnlyStringProperty(this.Name, _value);
	}

	/// <summary>
	/// Determines whether a value can be set on the property.
	/// </summary>
	/// <param name="value">The value to check.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="value"/> is <see langword="null"/>, a string, an
	/// <see cref="IFormattable"/>, or an <see cref="IConvertible"/>; otherwise, <see langword="false"/>.
	/// </returns>
	public override bool ValueIsProper(object? value)
	{
		return value is null || value is string || value is IFormattable || value is IConvertible;
	}
}
