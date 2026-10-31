using MG.Sonarr.Next.Extensions.Reflection;
using System.Management.Automation;
using System.Numerics;

namespace MG.Sonarr.Next.PSProperties;

/// <summary>
/// Provides a factory method that creates a <see cref="NumberNoteProperty{T}"/> with its type argument inferred.
/// </summary>
public static class NumberProperty
{
	/// <summary>
	/// Creates a writable number property with the specified name and value.
	/// </summary>
	/// <typeparam name="T">The number type of the property's value.</typeparam>
	/// <param name="propertyName">The name of the property.</param>
	/// <param name="value">The property's initial value.</param>
	/// <returns>A new <see cref="NumberNoteProperty{T}"/> that holds <paramref name="value"/>.</returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="propertyName"/> is null or empty.</exception>
	public static NumberNoteProperty<T> Create<T>(string propertyName, T value) where T : unmanaged, INumber<T>
	{
		return new NumberNoteProperty<T>(propertyName, value);
	}
}
/// <summary>
/// Represents a writable note property that holds a number.
/// </summary>
/// <remarks>
/// <para>Setting <see cref="WritableProperty{T}.Value"/> accepts a <typeparamref name="T"/>, a string that parses as
/// one, or another number type whose text parses as one, such as an <see cref="int"/> for a <see cref="long"/>
/// property. Strings and other values are parsed with <see cref="Statics.DefaultProvider"/>.</para>
/// <para>The property's <see cref="WritableProperty{T}.ToString"/> formats the value into a buffer of up to 40
/// characters, which holds every value of the built-in number types.</para>
/// </remarks>
/// <typeparam name="T">The number type of the property's value.</typeparam>
public sealed class NumberNoteProperty<T> : WritableProperty<T> where T : unmanaged, INumber<T>
{
	/// <summary>
	/// Gets the most characters that the property's value formats to.
	/// </summary>
	/// <value>The length of the longest <see cref="Int128"/> value as text.</value>
	protected override int MaxValueCharacterLength => LengthConstants.INT128_MAX;
	/// <summary>
	/// Gets or sets the number that the property holds.
	/// </summary>
	/// <value>The property's value.</value>
	public T NumValue { get; set; }

	/// <summary>
	/// Gets the full name of the property's value type.
	/// </summary>
	/// <value>The full name of <typeparamref name="T"/>.</value>
	public override string TypeNameOfValue => typeof(T).GetName();
	/// <summary>
	/// Gets or sets the property's value as a <typeparamref name="T"/>.
	/// </summary>
	/// <value>The value of <see cref="NumValue"/>.</value>
	protected override T ValueAsT
	{
		get => this.NumValue;
		set => this.NumValue = value;
	}

	/// <summary>
	/// Initializes a new <see cref="NumberNoteProperty{T}"/> instance with the specified name and a value of
	/// <see langword="default"/>.
	/// </summary>
	/// <param name="propertyName">The name of the property.</param>
	/// <exception cref="ArgumentException">Thrown when <paramref name="propertyName"/> is null or empty.</exception>
	public NumberNoteProperty(string propertyName)
	{
		this.SetMemberName(propertyName);
	}
	/// <summary>
	/// Initializes a new <see cref="NumberNoteProperty{T}"/> instance with the specified name and value.
	/// </summary>
	/// <param name="propertyName">The name of the property.</param>
	/// <param name="value">The property's initial value.</param>
	/// <exception cref="ArgumentException">Thrown when <paramref name="propertyName"/> is null or empty.</exception>
	public NumberNoteProperty(string propertyName, T value)
		: this(propertyName)
	{
		this.NumValue = value;
	}
	/// <summary>
	/// Converts a value that is being set on the property to a <typeparamref name="T"/>.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	/// <returns>
	/// <paramref name="value"/> itself when it's a <typeparamref name="T"/>; otherwise, the number that its text
	/// parses as.
	/// </returns>
	/// <exception cref="SetValueException">
	/// Thrown when <paramref name="value"/> is null, or is neither a <typeparamref name="T"/> nor a string or
	/// <see cref="ISpanFormattable"/> whose text parses as one.
	/// </exception>
	protected override T ConvertFromObject(object? value)
	{
		if (value is T intVal)
		{
			return intVal;
		}
		else if (value is string strVal && T.TryParse(strVal, Statics.DefaultProvider, out T fromStr))
		{
			return fromStr;
		}
		else if (value is ISpanFormattable spanFormattable)
		{
			Span<char> chars = stackalloc char[LengthConstants.INT128_MAX];
			if (spanFormattable.TryFormat(chars, out int written, default, Statics.DefaultProvider)
				&&
				T.TryParse(chars.Slice(0, written), Statics.DefaultProvider, out T intRes))
			{
				return intRes;
			}

			Debug.Fail("Shit");
		}

		return this.ThrowNotType<T>();
	}
	/// <summary>
	/// Creates a writable number property that holds the same value under the specified name.
	/// </summary>
	/// <param name="name">The name of the new property.</param>
	/// <returns>A new <see cref="NumberNoteProperty{T}"/> that holds <see cref="NumValue"/>.</returns>
	protected override PSPropertyInfo CopyIntoNew(string name)
	{
		return new NumberNoteProperty<T>(this.Name, this.NumValue);
	}
	/// <summary>
	/// Creates a read-only number property with the property's name and value.
	/// </summary>
	/// <returns>A new <see cref="ReadOnlyNumberProperty{T}"/> that holds <see cref="NumValue"/>.</returns>
	protected override ReadOnlyProperty<T> CopyToReadOnly()
	{
		return new ReadOnlyNumberProperty<T>(this.Name, this.NumValue);
	}
	/// <summary>
	/// Determines whether a value can be set on the property.
	/// </summary>
	/// <param name="value">The value to check.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="value"/> is a <typeparamref name="T"/> or an
	/// <see cref="IConvertible"/>; otherwise, <see langword="false"/>.
	/// </returns>
	public override bool ValueIsProper([NotNullWhen(true)] object? value)
	{
		return value is T || value is IConvertible;
	}
}
