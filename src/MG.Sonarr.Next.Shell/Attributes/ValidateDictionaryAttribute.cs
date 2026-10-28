using MG.Sonarr.Next.Exceptions;
using System.Collections;

namespace MG.Sonarr.Next.Shell.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class ValidateDictionaryAttribute : ValidateArgumentsAttribute
{
	readonly bool _valueTypeIsObject;
	readonly bool _valueTypeAllowsNull;

	public Type KeyType { get; }
	public Type ValueType { get; }

	public ValidateDictionaryAttribute(Type keyType)
		: this(keyType, typeof(object), true)
	{
	}
	public ValidateDictionaryAttribute(Type keyType, Type valueType)
		: this(keyType, valueType, typeof(object).Equals(valueType))
	{

	}
	private ValidateDictionaryAttribute(Type keyType, Type valueType, bool valueTypeIsObject)
	{
		ArgumentNullException.ThrowIfNull(keyType);
		ArgumentNullException.ThrowIfNull(valueType);

		_valueTypeIsObject = valueTypeIsObject;
		_valueTypeAllowsNull = !valueType.IsValueType || Nullable.GetUnderlyingType(valueType) is not null;
		this.KeyType = GetNonNullableKeyType(keyType);
		this.ValueType = valueType;
	}

	private static Type GetNonNullableKeyType(Type possibleNullable)
	{
		Type? possible = Nullable.GetUnderlyingType(possibleNullable);
		return possible ?? possibleNullable;
	}

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

	private bool IsValidValue(object? value)
	{
		return value is null
			? _valueTypeAllowsNull
			: this.ValueType.IsInstanceOfType(value);
	}

	private static ValidationMetadataException ToValidationException(InvalidTypeException invalidType)
	{
		return new ValidationMetadataException(invalidType.Message, invalidType);
	}
}

