using System.Management.Automation;

namespace MG.Sonarr.Next.PSProperties;

/// <summary>
/// Provides a factory method that creates a <see cref="ReadOnlyStructProperty{T}"/> with its type argument inferred.
/// </summary>
public static class ReadOnlyStructProperty
{
	/// <summary>
	/// Creates a read-only property with the specified name and value type value.
	/// </summary>
	/// <typeparam name="T">The value type of the property's value.</typeparam>
	/// <param name="propertyName">The name of the property.</param>
	/// <param name="value">The property's value.</param>
	/// <returns>A new <see cref="ReadOnlyStructProperty{T}"/> that holds <paramref name="value"/>.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="propertyName"/> is empty.</exception>
	public static ReadOnlyStructProperty<T> Create<T>(string propertyName, T value) where T : struct
	{
		return new ReadOnlyStructProperty<T>(propertyName, value);
	}
}

/// <summary>
/// Represents a read-only note property that holds a value type, such as a <see cref="bool"/>, a <see cref="Guid"/>,
/// or a <see cref="DateTimeOffset"/>.
/// </summary>
/// <typeparam name="T">The value type of the property's value.</typeparam>
public sealed class ReadOnlyStructProperty<T> : ReadOnlyProperty<T> where T : struct
{
	/// <summary>
	/// Gets the value that the property holds.
	/// </summary>
	/// <value>The property's value.</value>
	public T StructValue { get; }
	/// <summary>
	/// Gets the property's value as a <typeparamref name="T"/>.
	/// </summary>
	/// <value>The value of <see cref="StructValue"/>.</value>
	protected override T ValueAsT => this.StructValue;

	/// <summary>
	/// Initializes a new <see cref="ReadOnlyStructProperty{T}"/> instance with the specified name and value.
	/// </summary>
	/// <param name="propertyName">The name of the property.</param>
	/// <param name="value">The property's value.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="propertyName"/> is empty.</exception>
	public ReadOnlyStructProperty(string propertyName, T value)
		: base(propertyName)
	{
		this.StructValue = value;
	}

	/// <summary>
	/// Creates a copy of the property.
	/// </summary>
	/// <returns>A new <see cref="ReadOnlyStructProperty{T}"/> with the same name and value.</returns>
	public override PSMemberInfo Copy()
	{
		return new ReadOnlyStructProperty<T>(this.Name, this.StructValue);
	}
}

