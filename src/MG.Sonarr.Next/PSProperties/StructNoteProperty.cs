using MG.Sonarr.Next.Extensions.Reflection;
using System.Management.Automation;

namespace MG.Sonarr.Next.PSProperties;

/// <summary>
/// Provides a factory method that creates a <see cref="StructNoteProperty{T}"/> with its type argument inferred.
/// </summary>
public static class StructNoteProperty
{
	/// <summary>
	/// Creates a writable property with the specified name and value type value.
	/// </summary>
	/// <typeparam name="T">The value type of the property's value.</typeparam>
	/// <param name="propertyName">The name of the property.</param>
	/// <param name="value">The property's initial value.</param>
	/// <returns>A new <see cref="StructNoteProperty{T}"/> that holds <paramref name="value"/>.</returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="propertyName"/> is null or empty.</exception>
	public static StructNoteProperty<T> Create<T>(string propertyName, T value) where T : struct
	{
		return new StructNoteProperty<T>(propertyName, value);
	}
}

/// <summary>
/// Represents a writable note property that holds a value type, such as a <see cref="bool"/>, a <see cref="Guid"/>,
/// or a <see cref="DateTimeOffset"/>.
/// </summary>
/// <remarks>
/// Setting <see cref="WritableProperty{T}.Value"/> accepts only a <typeparamref name="T"/>, and no conversion is
/// tried.
/// </remarks>
/// <typeparam name="T">The value type of the property's value.</typeparam>
public sealed class StructNoteProperty<T> : WritableProperty<T> where T : struct
{
	/// <summary>
	/// Gets the full name of the property's value type.
	/// </summary>
	/// <value>The full name of <typeparamref name="T"/>.</value>
	public override string TypeNameOfValue => typeof(T).GetName();
	/// <summary>
	/// Gets or sets the value that the property holds.
	/// </summary>
	/// <value>The property's value.</value>
	public T StructValue { get; set; }

	/// <summary>
	/// Gets or sets the property's value as a <typeparamref name="T"/>.
	/// </summary>
	/// <value>The value of <see cref="StructValue"/>.</value>
	protected override T ValueAsT
	{
		get => this.StructValue;
		set => this.StructValue = value;
	}

	/// <summary>
	/// Initializes a new <see cref="StructNoteProperty{T}"/> instance with the specified name and a value of
	/// <see langword="default"/>.
	/// </summary>
	/// <param name="propertyName">The name of the property.</param>
	/// <exception cref="ArgumentException">Thrown when <paramref name="propertyName"/> is null or empty.</exception>
	public StructNoteProperty(string propertyName)
	{
		this.SetMemberName(propertyName);
	}
	/// <summary>
	/// Initializes a new <see cref="StructNoteProperty{T}"/> instance with the specified name and value.
	/// </summary>
	/// <param name="propertyName">The name of the property.</param>
	/// <param name="value">The property's initial value.</param>
	/// <exception cref="ArgumentException">Thrown when <paramref name="propertyName"/> is null or empty.</exception>
	public StructNoteProperty(string propertyName, T value)
		: this(propertyName)
	{
		this.StructValue = value;
	}

	/// <summary>
	/// Returns a value that is being set on the property as a <typeparamref name="T"/>.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	/// <returns><paramref name="value"/> as a <typeparamref name="T"/>.</returns>
	/// <exception cref="SetValueException">Thrown when <paramref name="value"/> isn't a <typeparamref name="T"/>.</exception>
	protected override T ConvertFromObject(object? value)
	{
		return value is T tVal ? tVal : this.ThrowNotType<T>();
	}
	/// <summary>
	/// Creates a writable property that holds the same value under the specified name.
	/// </summary>
	/// <param name="name">The name of the new property.</param>
	/// <returns>A new <see cref="StructNoteProperty{T}"/> that holds <see cref="StructValue"/>.</returns>
	protected override PSPropertyInfo CopyIntoNew(string name)
	{
		return new StructNoteProperty<T>(name, this.StructValue);
	}
	/// <summary>
	/// Creates a read-only property with the property's name and value.
	/// </summary>
	/// <returns>A new <see cref="ReadOnlyStructProperty{T}"/> that holds <see cref="StructValue"/>.</returns>
	protected override ReadOnlyProperty<T> CopyToReadOnly()
	{
		return new ReadOnlyStructProperty<T>(this.Name, this.StructValue);
	}

	/// <summary>
	/// Determines whether a value can be set on the property.
	/// </summary>
	/// <param name="value">The value to check.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="value"/> is a <typeparamref name="T"/>; otherwise,
	/// <see langword="false"/>.
	/// </returns>
	public override bool ValueIsProper(object? value)
	{
		return value is T;
	}
}

