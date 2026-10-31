using MG.Sonarr.Next.Extensions.Reflection;
using System.Management.Automation;
using System.Numerics;

namespace MG.Sonarr.Next.PSProperties;

/// <summary>
/// Provides a factory method that creates a <see cref="ReadOnlyNumberProperty{T}"/> with its type argument inferred.
/// </summary>
public static class ReadOnlyNumberProperty
{
	/// <summary>
	/// Creates a read-only number property with the specified name and value.
	/// </summary>
	/// <typeparam name="T">The number type of the property's value.</typeparam>
	/// <param name="propertyName">The name of the property.</param>
	/// <param name="value">The property's value.</param>
	/// <returns>A new <see cref="ReadOnlyNumberProperty{T}"/> that holds <paramref name="value"/>.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="propertyName"/> is empty.</exception>
	public static ReadOnlyNumberProperty<T> Create<T>(string propertyName, T value) where T : unmanaged, INumber<T>
	{
		return new ReadOnlyNumberProperty<T>(propertyName, value);
	}
}
/// <summary>
/// Represents a read-only note property that holds a number.
/// </summary>
/// <typeparam name="T">The number type of the property's value.</typeparam>
public sealed class ReadOnlyNumberProperty<T> : ReadOnlyProperty<T> where T : unmanaged, INumber<T>
{
	/// <summary>
	/// Gets the most characters that the property's value formats to.
	/// </summary>
	/// <value>The length of the longest <see cref="Int128"/> value as text.</value>
	protected override int MaxValueCharacterLength => LengthConstants.INT128_MAX;
	/// <summary>
	/// Gets the number that the property holds.
	/// </summary>
	/// <value>The property's value.</value>
	public T NumberValue { get; }
	/// <summary>
	/// Gets the full name of the property's value type.
	/// </summary>
	/// <value>The full name of <typeparamref name="T"/>.</value>
	public override string TypeNameOfValue => typeof(T).GetName();
	/// <summary>
	/// Gets the property's value as a <typeparamref name="T"/>.
	/// </summary>
	/// <value>The value of <see cref="NumberValue"/>.</value>
	protected override T ValueAsT => this.NumberValue;

	/// <summary>
	/// Initializes a new <see cref="ReadOnlyNumberProperty{T}"/> instance with the specified name and value.
	/// </summary>
	/// <param name="propertyName">The name of the property.</param>
	/// <param name="value">The property's value.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="propertyName"/> is empty.</exception>
	public ReadOnlyNumberProperty(string propertyName, T value)
		: base(propertyName)
	{
		this.NumberValue = value;
	}
	/// <summary>
	/// Creates a copy of the property.
	/// </summary>
	/// <returns>A new <see cref="ReadOnlyNumberProperty{T}"/> with the same name and value.</returns>
	public override PSMemberInfo Copy()
	{
		return new ReadOnlyNumberProperty<T>(this.Name, this.NumberValue);
	}
}
