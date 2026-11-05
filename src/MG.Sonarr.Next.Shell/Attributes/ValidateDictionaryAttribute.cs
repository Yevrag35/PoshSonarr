using MG.Sonarr.Next.Exceptions;
using System.Collections;

namespace MG.Sonarr.Next.Shell.Attributes;

/// <summary>
/// Provides the validation of a dictionary argument's keys and values against the types that the parameter expects.
/// </summary>
/// <remarks>
/// <para>
/// Each key must be an instance of <see cref="KeyType"/>, and each value an instance of <see cref="ValueType"/>, or
/// <see langword="null"/> when that type allows it. A value type of <see cref="object"/> takes any value. An argument
/// that isn't an <see cref="IDictionary"/>, or is an empty one, passes.
/// </para>
/// <para>
/// The attribute receives the dictionary itself on an <see cref="IDictionary"/> or
/// <see cref="System.Collections.Hashtable"/> parameter. A value that a command outputs is stored in a hashtable literal as the <see cref="PSObject"/> around it,
/// so such a value is meant to be checked by the object that the <see cref="PSObject"/> wraps.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class ValidateDictionaryAttribute : ValidateArgumentsAttribute
{
	readonly bool _valueTypeIsObject;
	readonly bool _valueTypeAllowsNull;

	/// <summary>
	/// Gets the type that each key of the dictionary must be an instance of.
	/// </summary>
	/// <value>The key type, without the <see cref="Nullable{T}"/> that the constructor may have been given.</value>
	public Type KeyType { get; }
	/// <summary>
	/// Gets the type that each value of the dictionary must be an instance of.
	/// </summary>
	/// <value>The value type, or <see cref="object"/> to take any value.</value>
	public Type ValueType { get; }

	/// <summary>
	/// Initializes a new <see cref="ValidateDictionaryAttribute"/> instance with the specified key type, which takes any
	/// value.
	/// </summary>
	/// <param name="keyType">The type that each key must be an instance of. A <see cref="Nullable{T}"/> stands for its
	/// underlying type.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="keyType"/> is null.</exception>
	public ValidateDictionaryAttribute(Type keyType)
		: this(keyType, typeof(object), true)
	{
	}
	/// <summary>
	/// Initializes a new <see cref="ValidateDictionaryAttribute"/> instance with the specified key and value types.
	/// </summary>
	/// <param name="keyType">The type that each key must be an instance of. A <see cref="Nullable{T}"/> stands for its
	/// underlying type.</param>
	/// <param name="valueType">The type that each value must be an instance of. A reference type or a
	/// <see cref="Nullable{T}"/> also takes a <see langword="null"/> value, and <see cref="object"/> takes any
	/// value.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="keyType"/> or <paramref name="valueType"/> is
	/// null.</exception>
	public ValidateDictionaryAttribute(Type keyType, Type valueType)
		: this(keyType, valueType, typeof(object).Equals(valueType))
	{

	}
	/// <summary>
	/// Initializes a new <see cref="ValidateDictionaryAttribute"/> instance with the specified key and value types and
	/// whether the value type takes any value.
	/// </summary>
	/// <param name="keyType">The type that each key must be an instance of.</param>
	/// <param name="valueType">The type that each value must be an instance of.</param>
	/// <param name="valueTypeIsObject"><see langword="true"/> to skip the check of the values; otherwise,
	/// <see langword="false"/>.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="keyType"/> or <paramref name="valueType"/> is
	/// null.</exception>
	private ValidateDictionaryAttribute(Type keyType, Type valueType, bool valueTypeIsObject)
	{
		ArgumentNullException.ThrowIfNull(keyType);
		ArgumentNullException.ThrowIfNull(valueType);

		_valueTypeIsObject = valueTypeIsObject;
		_valueTypeAllowsNull = !valueType.IsValueType || Nullable.GetUnderlyingType(valueType) is not null;
		this.KeyType = GetNonNullableKeyType(keyType);
		this.ValueType = valueType;
	}

	/// <summary>
	/// Gets the underlying type of a <see cref="Nullable{T}"/>, or the specified type itself.
	/// </summary>
	/// <param name="possibleNullable">The key type that the constructor was given.</param>
	/// <returns>The underlying type when <paramref name="possibleNullable"/> is a <see cref="Nullable{T}"/>; otherwise,
	/// <paramref name="possibleNullable"/>.</returns>
	private static Type GetNonNullableKeyType(Type possibleNullable)
	{
		Type? possible = Nullable.GetUnderlyingType(possibleNullable);
		return possible ?? possibleNullable;
	}

	/// <summary>
	/// Checks the type of each key and value of a dictionary argument.
	/// </summary>
	/// <param name="arguments">The argument, which passes when it isn't an <see cref="IDictionary"/> or is empty.</param>
	/// <param name="engineIntrinsics">The engine APIs of the session that binds the argument, which the method doesn't
	/// read.</param>
	/// <exception cref="ValidationMetadataException">Thrown when a key isn't a <see cref="KeyType"/>, or a value isn't a
	/// <see cref="ValueType"/> or is null when that type doesn't allow it.</exception>
	protected override void Validate(object arguments, EngineIntrinsics engineIntrinsics)
	{
		if (arguments is not IDictionary dict || dict.Count <= 0)
		{
			return;
		}

		foreach (DictionaryEntry entry in dict)
		{
			if (!this.KeyType.IsInstanceOfType(entry.Key))
			{
				throw ToValidationException(new InvalidKeyTypeException(this.KeyType, entry.Key));
			}

			if (!_valueTypeIsObject && !this.IsValidValue(entry.Value))
			{
				throw ToValidationException(new InvalidValueTypeException(this.ValueType, entry.Value));
			}
		}
	}

	/// <summary>
	/// Determines whether a dictionary's value is one that <see cref="ValueType"/> takes.
	/// </summary>
	/// <param name="value">The value, which can be <see langword="null"/>.</param>
	/// <returns><see langword="true"/> if <paramref name="value"/> is an instance of <see cref="ValueType"/>, or is
	/// <see langword="null"/> and that type allows it; otherwise, <see langword="false"/>.</returns>
	private bool IsValidValue(object? value)
	{
		return value is null
			? _valueTypeAllowsNull
			: this.ValueType.IsInstanceOfType(value);
	}

	/// <summary>
	/// Wraps the exception that describes a key or value of the wrong type in the exception that fails the binding.
	/// </summary>
	/// <param name="invalidType">The exception that names the expected type and the key or value.</param>
	/// <returns>A <see cref="ValidationMetadataException"/> with the message of <paramref name="invalidType"/>, which it
	/// holds as its inner exception.</returns>
	private static ValidationMetadataException ToValidationException(InvalidTypeException invalidType)
	{
		return new ValidationMetadataException(invalidType.Message, invalidType);
	}
}

