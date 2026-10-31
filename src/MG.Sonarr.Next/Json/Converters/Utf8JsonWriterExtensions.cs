using MG.Sonarr.Next.Buffers;
using MG.Sonarr.Next.Extensions.Strings;
using MG.Sonarr.Next.Json.Naming;

namespace MG.Sonarr.Next.Json.Converters;

/// <summary>
/// Provides extension methods for <see cref="Utf8JsonWriter"/> that write a property's name through a
/// <see cref="WorkingNamingPolicy"/> and then its value.
/// </summary>
/// <remarks>
/// <para>
/// The overloads without a property name take it from the expression passed as the value, through
/// <see cref="CallerArgumentExpressionAttribute"/>, and keep only the part after its last period, so
/// <c>writer.WriteString(policy, value.Name)</c> writes a property named <c>Name</c> before the policy converts it.
/// </para>
/// <para>
/// Each method writes into the object that the writer has open.
/// </para>
/// </remarks>
public static class Utf8JsonWriterExtensions
{
	/// <summary>
	/// The longest buffer, in elements, that a formatting method allocates on the stack instead of renting.
	/// </summary>
	private const int MAX_STACKALLOC = 256;

	/// <summary>
	/// Ends the object that the writer has open when a value is <see langword="null"/>.
	/// </summary>
	/// <param name="writer">The writer whose open object is ended.</param>
	/// <param name="value">The value to check.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="value"/> is <see langword="null"/> and the object was ended;
	/// otherwise, <see langword="false"/>.
	/// </returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryWriteEndObject(this Utf8JsonWriter writer, [NotNullWhen(false)] object? value)
	{
		bool result = false;
		if (value is null)
		{
			writer.WriteEndObject();
			result = true;
		}

		return result;
	}
	/// <summary>
	/// Writes a JSON null when a value is <see langword="null"/>.
	/// </summary>
	/// <param name="writer">The writer to write the null to.</param>
	/// <param name="value">The value to check.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="value"/> is <see langword="null"/> and a JSON null was written;
	/// otherwise, <see langword="false"/>.
	/// </returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryWriteNullValue(this Utf8JsonWriter writer, [NotNullWhen(false)] object? value)
	{
		bool result = false;
		if (value is null)
		{
			writer.WriteNullValue();
			result = true;
		}

		return result;
	}

	/// <summary>
	/// Writes a property with a Boolean value, named after the expression passed as the value.
	/// </summary>
	/// <param name="writer">The writer to write the property to.</param>
	/// <param name="policy">The naming policy that the property's name is written with.</param>
	/// <param name="value">The value to write.</param>
	/// <param name="propertyName">
	/// The property's name, which the compiler fills with the expression passed as <paramref name="value"/>. Only the
	/// part after its last period is used.
	/// </param>
	public static void WriteBoolean(this Utf8JsonWriter writer,
		WorkingNamingPolicy policy,
		bool value,
		[CallerArgumentExpression(nameof(value))] string propertyName = "")
	{
		ReadOnlySpan<char> propName = FormatPropertyName(propertyName);
		policy.WritePropertyName(writer, propName);
		writer.WriteBooleanValue(value);
	}

	/// <summary>
	/// Writes a property with a Boolean value, named by a UTF-8 name.
	/// </summary>
	/// <param name="writer">The writer to write the property to.</param>
	/// <param name="policy">The naming policy that the property's name is written with.</param>
	/// <param name="utf8PropertyName">The property's name, in UTF-8.</param>
	/// <param name="value">The value to write.</param>
	public static void WriteBoolean(this Utf8JsonWriter writer,
		WorkingNamingPolicy policy,
		ReadOnlySpan<byte> utf8PropertyName,
		bool value)
	{
		policy.WritePropertyName(writer, utf8PropertyName);
		writer.WriteBooleanValue(value);
	}

	/// <summary>
	/// Writes a property with a nullable Boolean value, named by a UTF-8 name.
	/// </summary>
	/// <param name="writer">The writer to write the property to.</param>
	/// <param name="policy">The naming policy that the property's name is written with.</param>
	/// <param name="utf8PropertyName">The property's name, in UTF-8.</param>
	/// <param name="value">The value to write, or <see langword="null"/> to write a JSON null.</param>
	public static void WriteBoolean(this Utf8JsonWriter writer,
		WorkingNamingPolicy policy,
		ReadOnlySpan<byte> utf8PropertyName,
		bool? value)
	{
		policy.WritePropertyName(writer, utf8PropertyName);

		if (value.HasValue)
			writer.WriteBooleanValue(value.Value);
		else
			writer.WriteNullValue();
	}

	/// <summary>
	/// Writes an empty JSON array.
	/// </summary>
	/// <remarks>
	/// The method is the same as calling <see cref="Utf8JsonWriter.WriteStartArray()"/> and then
	/// <see cref="Utf8JsonWriter.WriteEndArray"/>.
	/// </remarks>
	/// <param name="writer">The writer to write the array to.</param>
	public static void WriteEmptyArray(this Utf8JsonWriter writer)
	{
		writer.WriteStartArray();
		writer.WriteEndArray();
	}

	/// <summary>
	/// Writes an empty JSON object.
	/// </summary>
	/// <remarks>
	/// The method is the same as calling <see cref="Utf8JsonWriter.WriteStartObject()"/> and then
	/// <see cref="Utf8JsonWriter.WriteEndObject"/>.
	/// </remarks>
	/// <param name="writer">The writer to write the object to.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WriteEmptyObject(this Utf8JsonWriter writer)
	{
		writer.WriteStartObject();
		writer.WriteEndObject();
	}

	/// <summary>
	/// Writes a property with a formattable value as a string, named by a UTF-8 name.
	/// </summary>
	/// <remarks>
	/// The value is formatted into a buffer of <paramref name="maxLength"/> characters, on the stack when that's up to
	/// 256 and rented otherwise, which goes back to its pool before the method returns.
	/// </remarks>
	/// <typeparam name="T">The type of the value.</typeparam>
	/// <param name="writer">The writer to write the property to.</param>
	/// <param name="policy">The naming policy that the property's name is written with.</param>
	/// <param name="utf8PropertyName">The property's name, in UTF-8.</param>
	/// <param name="value">The value to format and write.</param>
	/// <param name="maxLength">The most characters that the formatted value can take.</param>
	/// <param name="format">The format to apply, or empty for the type's default format.</param>
	/// <param name="provider">
	/// The provider of culture-specific formatting, or <see langword="null"/> for the current culture.
	/// </param>
	/// <exception cref="FormatException">
	/// Thrown when the formatted value doesn't fit in <paramref name="maxLength"/> characters, or when the value can't
	/// be formatted.
	/// </exception>
	public static void WriteFormattable<T>(this Utf8JsonWriter writer,
		WorkingNamingPolicy policy,
		ReadOnlySpan<byte> utf8PropertyName,
		T value,
		int maxLength,
		ReadOnlySpan<char> format = default,
		IFormatProvider? provider = null)
			where T : ISpanFormattable
	{
		policy.WritePropertyName(writer, utf8PropertyName);

		using (var buffer = RentedBuffer.Rent<char>(
			maxLength <= MAX_STACKALLOC
				? stackalloc char[maxLength]
				: maxLength))
		{
			if (!value.TryFormat(buffer.Span, out int charsWritten, format, provider))
			{
				throw new FormatException("Failed to format value.");
			}

			writer.WriteStringValue(buffer[..charsWritten]);
		}
	}

	/// <summary>
	/// Writes a property with a formattable value as a string, named after the expression passed as the value.
	/// </summary>
	/// <remarks>
	/// The value is formatted into a buffer of <paramref name="maxLength"/> characters, on the stack when that's up to
	/// 256 and rented otherwise, which goes back to its pool before the method returns.
	/// </remarks>
	/// <typeparam name="T">The type of the value.</typeparam>
	/// <param name="writer">The writer to write the property to.</param>
	/// <param name="policy">The naming policy that the property's name is written with.</param>
	/// <param name="value">The value to format and write.</param>
	/// <param name="maxLength">The most characters that the formatted value can take.</param>
	/// <param name="propertyName">
	/// The property's name, which the compiler fills with the expression passed as <paramref name="value"/>. Only the
	/// part after its last period is used.
	/// </param>
	/// <param name="format">The format to apply, or empty for the type's default format.</param>
	/// <param name="provider">
	/// The provider of culture-specific formatting, or <see langword="null"/> for the current culture.
	/// </param>
	/// <exception cref="FormatException">
	/// Thrown when the formatted value doesn't fit in <paramref name="maxLength"/> characters, or when the value can't
	/// be formatted.
	/// </exception>
	public static void WriteFormattable<T>(this Utf8JsonWriter writer,
		WorkingNamingPolicy policy,
		T value,
		int maxLength,
		[CallerArgumentExpression(nameof(value))] string propertyName = "",
		ReadOnlySpan<char> format = default,
		IFormatProvider? provider = null)
			where T : ISpanFormattable
	{
		ReadOnlySpan<char> propName = FormatPropertyName(propertyName);
		policy.WritePropertyName(writer, propName);

		using (var buffer = RentedBuffer.Rent<char>(
			maxLength <= MAX_STACKALLOC
				? stackalloc char[maxLength]
				: maxLength))
		{
			if (!value.TryFormat(buffer.Span, out int charsWritten, format, provider))
			{
				throw new FormatException("Failed to format value.");
			}

			writer.WriteStringValue(buffer[..charsWritten]);
		}
	}

	/// <summary>
	/// Writes a property with an <see cref="int"/> value, named after the expression passed as the value.
	/// </summary>
	/// <param name="writer">The writer to write the property to.</param>
	/// <param name="policy">The naming policy that the property's name is written with.</param>
	/// <param name="value">The value to write.</param>
	/// <param name="propertyName">
	/// The property's name, which the compiler fills with the expression passed as <paramref name="value"/>. Only the
	/// part after its last period is used.
	/// </param>
	public static void WriteNumber(this Utf8JsonWriter writer,
		WorkingNamingPolicy policy,
		int value,
		[CallerArgumentExpression(nameof(value))] string propertyName = "")
	{
		ReadOnlySpan<char> propName = FormatPropertyName(propertyName);
		policy.WritePropertyName(writer, propName);
		writer.WriteNumberValue(value);
	}

	/// <summary>
	/// Writes a property with a nullable <see cref="int"/> value, named after the expression passed as the value.
	/// </summary>
	/// <param name="writer">The writer to write the property to.</param>
	/// <param name="policy">The naming policy that the property's name is written with.</param>
	/// <param name="value">The value to write, or <see langword="null"/> to write a JSON null.</param>
	/// <param name="propertyName">
	/// The property's name, which the compiler fills with the expression passed as <paramref name="value"/>. Only the
	/// part after its last period is used.
	/// </param>
	public static void WriteNumber(this Utf8JsonWriter writer,
		WorkingNamingPolicy policy,
		int? value,
		[CallerArgumentExpression(nameof(value))] string propertyName = "")
	{
		if (value.HasValue)
		{
			writer.WriteNumber(policy, value.Value, propertyName);
		}
		else
		{
			WriteNullValue(writer, policy, propertyName);
		}
	}

	/// <summary>
	/// Writes a property with an <see cref="int"/> value, named by a UTF-8 name.
	/// </summary>
	/// <param name="writer">The writer to write the property to.</param>
	/// <param name="policy">The naming policy that the property's name is written with.</param>
	/// <param name="utf8PropertyName">The property's name, in UTF-8.</param>
	/// <param name="value">The value to write.</param>
	public static void WriteNumber(this Utf8JsonWriter writer,
		WorkingNamingPolicy policy,
		ReadOnlySpan<byte> utf8PropertyName,
		int value)
	{
		policy.WritePropertyName(writer, utf8PropertyName);
		writer.WriteNumberValue(value);
	}

	/// <summary>
	/// Writes a property with a <see cref="long"/> value, named after the expression passed as the value.
	/// </summary>
	/// <param name="writer">The writer to write the property to.</param>
	/// <param name="policy">The naming policy that the property's name is written with.</param>
	/// <param name="value">The value to write.</param>
	/// <param name="propertyName">
	/// The property's name, which the compiler fills with the expression passed as <paramref name="value"/>. Only the
	/// part after its last period is used.
	/// </param>
	public static void WriteNumber(this Utf8JsonWriter writer,
		WorkingNamingPolicy policy,
		long value,
		[CallerArgumentExpression(nameof(value))] string propertyName = "")
	{
		ReadOnlySpan<char> propName = FormatPropertyName(propertyName);
		policy.WritePropertyName(writer, propName);
		writer.WriteNumberValue(value);
	}

	/// <summary>
	/// Writes a property with a <see cref="double"/> value, named after the expression passed as the value.
	/// </summary>
	/// <param name="writer">The writer to write the property to.</param>
	/// <param name="policy">The naming policy that the property's name is written with.</param>
	/// <param name="value">The value to write.</param>
	/// <param name="propertyName">
	/// The property's name, which the compiler fills with the expression passed as <paramref name="value"/>. Only the
	/// part after its last period is used.
	/// </param>
	public static void WriteNumber(this Utf8JsonWriter writer,
		WorkingNamingPolicy policy,
		double value,
		[CallerArgumentExpression(nameof(value))] string propertyName = "")
	{
		ReadOnlySpan<char> propName = FormatPropertyName(propertyName);
		policy.WritePropertyName(writer, propName);
		writer.WriteNumberValue(value);
	}

	/// <summary>
	/// Writes a property with a value serialized as a specified type, named after the expression passed as the
	/// value.
	/// </summary>
	/// <param name="writer">The writer to write the property to.</param>
	/// <param name="policy">
	/// The naming policy that the property's name is written with, whose serializer options the value is serialized
	/// with.
	/// </param>
	/// <param name="value">The value to write.</param>
	/// <param name="valueType">The type to serialize the value as.</param>
	/// <param name="propertyName">
	/// The property's name, which the compiler fills with the expression passed as <paramref name="value"/>. Only the
	/// part after its last period is used.
	/// </param>
	public static void WriteObject(this Utf8JsonWriter writer,
		WorkingNamingPolicy policy,
		object? value,
		Type valueType,
		[CallerArgumentExpression(nameof(value))] string propertyName = "")
	{
		ReadOnlySpan<char> propName = FormatPropertyName(propertyName);
		policy.WritePropertyName(writer, propName);
		JsonSerializer.Serialize(writer, value, valueType, policy.Options);
	}

	/// <summary>
	/// Writes a property with a value serialized as its runtime type, named after the expression passed as the
	/// value.
	/// </summary>
	/// <param name="writer">The writer to write the property to.</param>
	/// <param name="policy">
	/// The naming policy that the property's name is written with, whose serializer options the value is serialized
	/// with.
	/// </param>
	/// <param name="value">The value to write, or <see langword="null"/> to write a JSON null.</param>
	/// <param name="propertyName">
	/// The property's name, which the compiler fills with the expression passed as <paramref name="value"/>. Only the
	/// part after its last period is used.
	/// </param>
	public static void WriteObjectOfBaseType(this Utf8JsonWriter writer,
		WorkingNamingPolicy policy,
		object? value,
		[CallerArgumentExpression(nameof(value))] string propertyName = "")
	{
		ReadOnlySpan<char> propName = FormatPropertyName(propertyName);
		policy.WritePropertyName(writer, propName);

		if (value is null)
		{
			writer.WriteNullValue();
			return;
		}

		JsonSerializer.Serialize(writer, value, value.GetType(), policy.Options);
	}

	/// <summary>
	/// Writes a property with a value serialized as its runtime type, named by a UTF-8 name.
	/// </summary>
	/// <param name="writer">The writer to write the property to.</param>
	/// <param name="policy">
	/// The naming policy that the property's name is written with, whose serializer options the value is serialized
	/// with.
	/// </param>
	/// <param name="utf8PropertyName">The property's name, in UTF-8.</param>
	/// <param name="value">The value to write, or <see langword="null"/> to write a JSON null.</param>
	public static void WriteObjectOfBaseType(this Utf8JsonWriter writer,
		WorkingNamingPolicy policy,
		ReadOnlySpan<byte> utf8PropertyName,
		object? value)
	{
		policy.WritePropertyName(writer, utf8PropertyName);

		if (value is null)
		{
			writer.WriteNullValue();
			return;
		}

		JsonSerializer.Serialize(writer, value, value.GetType(), policy.Options);
	}

	/// <summary>
	/// Writes a property with a value serialized as its runtime type, named by a span of characters.
	/// </summary>
	/// <param name="writer">The writer to write the property to.</param>
	/// <param name="policy">
	/// The naming policy that the property's name is written with, whose serializer options the value is serialized
	/// with.
	/// </param>
	/// <param name="value">The value to write, or <see langword="null"/> to write a JSON null.</param>
	/// <param name="propertyName">The property's name, used whole.</param>
	public static void WriteObjectOfBaseType(this Utf8JsonWriter writer,
		WorkingNamingPolicy policy,
		object? value,
		ReadOnlySpan<char> propertyName)
	{
		policy.WritePropertyName(writer, propertyName);

		if (value is null)
		{
			writer.WriteNullValue();
			return;
		}

		JsonSerializer.Serialize(writer, value, value.GetType(), policy.Options);
	}

	/// <summary>
	/// Writes a property with a value serialized as <typeparamref name="T"/>, named by a UTF-8 name.
	/// </summary>
	/// <typeparam name="T">The type to serialize the value as.</typeparam>
	/// <param name="writer">The writer to write the property to.</param>
	/// <param name="policy">
	/// The naming policy that the property's name is written with, whose serializer options the value is serialized
	/// with.
	/// </param>
	/// <param name="utf8PropertyName">The property's name, in UTF-8.</param>
	/// <param name="value">The value to write, or <see langword="null"/> to write a JSON null.</param>
	public static void WriteObject<T>(this Utf8JsonWriter writer,
		WorkingNamingPolicy policy,
		ReadOnlySpan<byte> utf8PropertyName,
		T? value)
	{
		policy.WritePropertyName(writer, utf8PropertyName);

		if (value is null)
		{
			writer.WriteNullValue();
			return;
		}

		JsonSerializer.Serialize(writer, value, policy.Options);
	}

	/// <summary>
	/// Writes a property's name, given as a span of characters, and starts a JSON array as its value.
	/// </summary>
	/// <param name="writer">The writer to write the property to.</param>
	/// <param name="policy">The naming policy that the property's name is written with.</param>
	/// <param name="propertyName">The property's name, used whole.</param>
	public static void WriteStartArray(this Utf8JsonWriter writer,
		WorkingNamingPolicy policy,
		ReadOnlySpan<char> propertyName)
	{
		policy.WritePropertyName(writer, propertyName);
		writer.WriteStartArray();
	}

	/// <summary>
	/// Writes a property's UTF-8 name and starts a JSON array as its value.
	/// </summary>
	/// <param name="writer">The writer to write the property to.</param>
	/// <param name="policy">The naming policy that the property's name is written with.</param>
	/// <param name="utf8PropertyName">The property's name, in UTF-8.</param>
	public static void WriteStartArray(this Utf8JsonWriter writer,
		WorkingNamingPolicy policy,
		ReadOnlySpan<byte> utf8PropertyName)
	{
		policy.WritePropertyName(writer, utf8PropertyName);
		writer.WriteStartArray();
	}

	/// <summary>
	/// Writes a property's name, given as a span of characters, and starts a JSON object as its value.
	/// </summary>
	/// <param name="writer">The writer to write the property to.</param>
	/// <param name="policy">The naming policy that the property's name is written with.</param>
	/// <param name="propertyName">The property's name, used whole.</param>
	public static void WriteStartObject(this Utf8JsonWriter writer,
		WorkingNamingPolicy policy,
		ReadOnlySpan<char> propertyName)
	{
		policy.WritePropertyName(writer, propertyName);
		writer.WriteStartObject();
	}

	/// <summary>
	/// Writes a property's UTF-8 name and starts a JSON object as its value.
	/// </summary>
	/// <param name="writer">The writer to write the property to.</param>
	/// <param name="policy">The naming policy that the property's name is written with.</param>
	/// <param name="utf8PropertyName">The property's name, in UTF-8.</param>
	public static void WriteStartObject(this Utf8JsonWriter writer,
		WorkingNamingPolicy policy,
		ReadOnlySpan<byte> utf8PropertyName)
	{
		policy.WritePropertyName(writer, utf8PropertyName);
		writer.WriteStartObject();
	}

	/// <summary>
	/// Writes a property with a <see cref="DateTime"/> value as an ISO 8601 string, named by a UTF-8 name.
	/// </summary>
	/// <param name="writer">The writer to write the property to.</param>
	/// <param name="policy">The naming policy that the property's name is written with.</param>
	/// <param name="utf8PropertyName">The property's name, in UTF-8.</param>
	/// <param name="dateTime">The value to write.</param>
	public static void WriteString(this Utf8JsonWriter writer,
		WorkingNamingPolicy policy,
		ReadOnlySpan<byte> utf8PropertyName,
		DateTime dateTime)
	{
		policy.WritePropertyName(writer, utf8PropertyName);
		writer.WriteStringValue(dateTime);
	}

	/// <summary>
	/// Writes a property with a string value, named by a UTF-8 name.
	/// </summary>
	/// <param name="writer">The writer to write the property to.</param>
	/// <param name="policy">The naming policy that the property's name is written with.</param>
	/// <param name="utf8PropertyName">The property's name, in UTF-8.</param>
	/// <param name="value">The value to write, or <see langword="null"/> to write a JSON null.</param>
	public static void WriteString(this Utf8JsonWriter writer,
		WorkingNamingPolicy policy,
		ReadOnlySpan<byte> utf8PropertyName,
		string? value)
	{
		policy.WritePropertyName(writer, utf8PropertyName);
		writer.WriteStringValue(value);
	}

	/// <summary>
	/// Writes a property with a string value, given as a span of characters, named by a UTF-8 name.
	/// </summary>
	/// <param name="writer">The writer to write the property to.</param>
	/// <param name="policy">The naming policy that the property's name is written with.</param>
	/// <param name="utf8PropertyName">The property's name, in UTF-8.</param>
	/// <param name="value">The value to write.</param>
	public static void WriteString(this Utf8JsonWriter writer,
		WorkingNamingPolicy policy,
		ReadOnlySpan<byte> utf8PropertyName,
		ReadOnlySpan<char> value)
	{
		policy.WritePropertyName(writer, utf8PropertyName);
		writer.WriteStringValue(value);
	}
	/// <summary>
	/// Writes a property with a string value, given as UTF-8 bytes, named by a UTF-8 name.
	/// </summary>
	/// <param name="writer">The writer to write the property to.</param>
	/// <param name="policy">The naming policy that the property's name is written with.</param>
	/// <param name="utf8PropertyName">The property's name, in UTF-8.</param>
	/// <param name="utf8Value">The value to write, in UTF-8.</param>
	public static void WriteString(this Utf8JsonWriter writer,
		WorkingNamingPolicy policy,
		ReadOnlySpan<byte> utf8PropertyName,
		ReadOnlySpan<byte> utf8Value)
	{
		policy.WritePropertyName(writer, utf8PropertyName);
		writer.WriteStringValue(utf8Value);
	}

	/// <summary>
	/// Writes a property with a string value, given as a span of characters, named after the expression passed as
	/// the value.
	/// </summary>
	/// <param name="writer">The writer to write the property to.</param>
	/// <param name="policy">The naming policy that the property's name is written with.</param>
	/// <param name="value">The value to write.</param>
	/// <param name="propertyName">
	/// The property's name, which the compiler fills with the expression passed as <paramref name="value"/>. Only the
	/// part after its last period is used.
	/// </param>
	public static void WriteString(this Utf8JsonWriter writer,
		WorkingNamingPolicy policy,
		ReadOnlySpan<char> value,
		[CallerArgumentExpression(nameof(value))] string propertyName = "")
	{
		ReadOnlySpan<char> propName = FormatPropertyName(propertyName);
		policy.WritePropertyName(writer, propName);
		writer.WriteStringValue(value);
	}

	/// <summary>
	/// Writes a property with a string value, named after the expression passed as the value.
	/// </summary>
	/// <param name="writer">The writer to write the property to.</param>
	/// <param name="policy">The naming policy that the property's name is written with.</param>
	/// <param name="value">The value to write, or <see langword="null"/> to write a JSON null.</param>
	/// <param name="propertyName">
	/// The property's name, which the compiler fills with the expression passed as <paramref name="value"/>. Only the
	/// part after its last period is used.
	/// </param>
	public static void WriteString(this Utf8JsonWriter writer,
		WorkingNamingPolicy policy,
		string? value,
		[CallerArgumentExpression(nameof(value))] string propertyName = "")
	{
		ReadOnlySpan<char> propName = FormatPropertyName(propertyName);
		policy.WritePropertyName(writer, propName);
		writer.WriteStringValue(value);
	}

	/// <summary>
	/// Writes a property with a UTF-8 formattable value as a string, named after the expression passed as the value.
	/// </summary>
	/// <remarks>
	/// The value is formatted into a byte buffer sized for the UTF-8 encoding of <paramref name="maxLength"/>
	/// characters, on the stack when that's up to 256 bytes and rented otherwise, which goes back to its pool before
	/// the method returns.
	/// </remarks>
	/// <typeparam name="T">The type of the value.</typeparam>
	/// <param name="writer">The writer to write the property to.</param>
	/// <param name="policy">The naming policy that the property's name is written with.</param>
	/// <param name="value">The value to format and write.</param>
	/// <param name="maxLength">The most characters that the formatted value can take.</param>
	/// <param name="propertyName">
	/// The property's name, which the compiler fills with the expression passed as <paramref name="value"/>. Only the
	/// part after its last period is used.
	/// </param>
	/// <param name="format">The format to apply, or empty for the type's default format.</param>
	/// <param name="provider">
	/// The provider of culture-specific formatting, or <see langword="null"/> for the current culture.
	/// </param>
	/// <exception cref="FormatException">
	/// Thrown when the formatted value doesn't fit in the buffer, or when the value can't be formatted.
	/// </exception>
	public static void WriteUtf8Formattable<T>(this Utf8JsonWriter writer,
		WorkingNamingPolicy policy,
		T value,
		int maxLength,
		[CallerArgumentExpression(nameof(value))] string propertyName = "",
		ReadOnlySpan<char> format = default,
		IFormatProvider? provider = null)
		where T : IUtf8SpanFormattable
	{
		maxLength = Encoding.UTF8.GetMaxByteCount(maxLength);

		ReadOnlySpan<char> propName = FormatPropertyName(propertyName);
		policy.WritePropertyName(writer, propName);

		using (var buffer = RentedBuffer.Rent<byte>(
			maxLength <= MAX_STACKALLOC
				? stackalloc byte[maxLength]
				: maxLength))
		{
			if (!value.TryFormat(buffer.Span, out int bytesWritten, format, provider))
			{
				throw new FormatException("Failed to format value.");
			}

			writer.WriteStringValue(buffer.Slice(0, bytesWritten));
		}
	}

	/// <summary>
	/// Gets the part of a property name after its last period, or the whole name when it has none.
	/// </summary>
	/// <remarks>
	/// A name that ends in a period is returned whole.
	/// </remarks>
	/// <param name="propertyName">The property name, usually an expression such as <c>value.Name</c>.</param>
	/// <returns>A slice of <paramref name="propertyName"/>.</returns>
	private static ReadOnlySpan<char> FormatPropertyName(ReadOnlySpan<char> propertyName)
	{
		if (TryGetLastIndexOfPeriod(propertyName, out int index))
		{
			propertyName = propertyName.UnsafeSlice(index + 1);
		}

		return propertyName;
	}

	/// <summary>
	/// Tries to find the last period in a property name that has at least one character after it.
	/// </summary>
	/// <param name="propertyName">The property name to search.</param>
	/// <param name="index">
	/// When this method returns, the index of the last period if the name has one; otherwise, an unspecified value.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if the name has a period that isn't its last character; otherwise,
	/// <see langword="false"/>.
	/// </returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryGetLastIndexOfPeriod(ReadOnlySpan<char> propertyName, out int index)
	{
		return propertyName.TryLastIndexOf('.', out index)
			&& index < propertyName.Length - 1;
	}

	/// <summary>
	/// Writes a property with a JSON null value, named by the part of a name after its last period.
	/// </summary>
	/// <param name="writer">The writer to write the property to.</param>
	/// <param name="policy">The naming policy that the property's name is written with.</param>
	/// <param name="propertyName">The property's name, of which only the part after its last period is used.</param>
	private static void WriteNullValue(Utf8JsonWriter writer, WorkingNamingPolicy policy, ReadOnlySpan<char> propertyName)
	{
		ReadOnlySpan<char> propName = FormatPropertyName(propertyName);
		policy.WritePropertyName(writer, propName);
		writer.WriteNullValue();
	}
}
