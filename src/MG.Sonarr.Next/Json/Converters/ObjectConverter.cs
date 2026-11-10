using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Json.Converters.Spans;
using MG.Sonarr.Next.Json.Naming;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models;
using MG.Sonarr.Next.Models.Episodes;
using MG.Sonarr.Next.Models.History;
using MG.Sonarr.Next.Models.ManualImports;
using MG.Sonarr.Next.Models.Qualities;
using MG.Sonarr.Next.Models.Series;
using MG.Sonarr.Next.PSProperties;

namespace MG.Sonarr.Next.Json.Converters;

/// <summary>
/// Provides the converter that reads Sonarr's JSON into PowerShell objects and writes their note properties back,
/// which every model's converter shares.
/// </summary>
/// <remarks>
/// <para>
/// A JSON object becomes a <see cref="PSObject"/> with a note property for each field, named after the field with
/// its first letter uppercased and then renamed by the model's map or else the global one. A numeric <c>id</c>
/// becomes a read-only property. A string is read by the span converter registered for its property's name, or else
/// as a number, a <see cref="Guid"/>, a <see cref="DateOnly"/>, a <see cref="DateTimeOffset"/>, or a
/// <see cref="Version"/>, tried in that order, and otherwise stays a string. A nested object becomes a model when its
/// property's name and its parent's type call for one, and a plain <see cref="PSObject"/> otherwise. An array becomes
/// the type that the settings give its property's name, or else an array of objects, in which a string is read by its
/// property's span converter too, when the property has one.
/// </para>
/// <para>
/// Sonarr writes its values with the invariant culture, so they're read with it, whatever the current culture. A
/// number is the text of an <see cref="int"/>, a <see cref="long"/>, or a finite <see cref="double"/>, with no group
/// separators. A <see cref="DateOnly"/> is an ISO 8601 date, <c>yyyy-MM-dd</c>, and a <see cref="DateTimeOffset"/>
/// is an ISO 8601 date and time, <c>yyyy-MM-ddTHH:mm</c> with optional seconds and fractions of a second, followed
/// by <c>Z</c>, an offset, or nothing, which is read as UTC. A value reads the same whether or not Sonarr escaped
/// any of its characters.
/// </para>
/// <para>
/// A <see cref="DateTimeOffset"/> is converted to local time unless its property's name ends in <c>Utc</c>, or the
/// local time would fall outside the range of <see cref="DateTime"/>, as it does for <c>0001-01-01T00:00:00Z</c>
/// west of UTC, where the value keeps its own offset.
/// </para>
/// <para>
/// A history record's <c>Data</c> is read as Sonarr sends it: a plain <see cref="PSObject"/> whose property names
/// are capitalized and renamed as every object's are, and whose strings stay strings, with no span converter or
/// parsing.
/// </para>
/// <para>
/// The converter keeps no state between calls beyond its settings, so a connection's serializer options share one
/// instance across threads.
/// </para>
/// </remarks>
public sealed class ObjectConverter : ByPolicyJsonConverter<object>
{
	/// <summary>
	/// The longest string value, in UTF-8 bytes, whose characters are copied to the stack instead of a rented buffer.
	/// </summary>
	private const int MAX_STACKALLOC = 256;
	/// <summary>
	/// The settings that the constructor's callback filled.
	/// </summary>
	private readonly ObjectConverterConfiguration _config;

	/// <summary>
	/// Initializes a new <see cref="ObjectConverter"/> instance with the resolver that nested models are tagged with,
	/// the naming policy that property names are written with, and a callback that supplies its settings.
	/// </summary>
	/// <param name="resolver">The resolver that holds the tags that the connection registered.</param>
	/// <param name="policy">The naming policy that the converter writes property names with.</param>
	/// <param name="configure">
	/// The callback that adds the converter's span converters, array types, global renames, and ignored properties.
	/// </param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="resolver"/>, <paramref name="policy"/>, or <paramref name="configure"/> is null.</exception>
	public ObjectConverter(IMetadataResolver resolver, WorkingNamingPolicy policy, Action<IObjectConverterConfig> configure)
		: base(policy)
	{
		ArgumentNullException.ThrowIfNull(resolver);
		ArgumentNullException.ThrowIfNull(configure);
		ObjectConverterConfiguration config = new(resolver);
		configure(config);

		_config = config;
	}

	/// <summary>
	/// Determines whether the converter can read and write the specified type.
	/// </summary>
	/// <param name="typeToConvert">The type to check.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="typeToConvert"/> is <see cref="PSObject"/>, <see cref="object"/>, or
	/// <see cref="PSCustomObject"/>; otherwise, <see langword="false"/>.
	/// </returns>
	public override bool CanConvert(Type typeToConvert)
	{
		return typeToConvert.Equals(typeof(PSObject))
			   ||
			   typeToConvert.Equals(typeof(object))
			   ||
			   typeToConvert.Equals(typeof(PSCustomObject));
	}

	/// <summary>
	/// Reads any JSON value as the object that PowerShell sees.
	/// </summary>
	/// <param name="reader">The reader positioned at the value.</param>
	/// <param name="typeToConvert">The type to convert the value to, which isn't used.</param>
	/// <param name="options">The serializer options that nested values are read with.</param>
	/// <returns>
	/// An array of objects for a JSON array, a <see cref="PSObject"/> for a JSON object, a parsed value or a string for
	/// a JSON string, an <see cref="int"/>, <see cref="long"/>, or <see cref="double"/> for a number, a
	/// <see cref="bool"/> for <c>true</c> or <c>false</c>, or <see langword="null"/> for a JSON null.
	/// </returns>
	/// <exception cref="JsonException">
	/// Thrown when the reader holds another kind of token, or when a value can't be read.
	/// </exception>
	public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		return reader.TokenType switch
		{
			JsonTokenType.StartArray => ConvertToListOfObjects(ref reader, options),
			JsonTokenType.StartObject => this.ConvertToObject<PSObject>(ref reader, options, null, null),
			JsonTokenType.String => this.ReadString(ref reader, string.Empty, null),
			JsonTokenType.Number => ReadNumber(ref reader),
			JsonTokenType.True => true,
			JsonTokenType.False => false,
			JsonTokenType.None or JsonTokenType.Null => null,
			_ => throw new JsonException($"Unable to process object with token type '{reader.TokenType}'."),
		};
	}

	/// <summary>
	/// Reads a JSON array as an array of objects, each read by this converter.
	/// </summary>
	/// <param name="reader">The reader positioned at the start of the array.</param>
	/// <param name="options">The serializer options that the items are read with.</param>
	/// <returns>The array's values.</returns>
	/// <exception cref="JsonException">Thrown when the array reads as a JSON null, or when an item can't be read.</exception>
	private static object[] ConvertToListOfObjects(ref Utf8JsonReader reader, JsonSerializerOptions options)
	{
		return JsonSerializer.Deserialize<object[]>(ref reader, options) ??
			throw new JsonException("Unable to deserialize into an array of object instances.");
	}

	/// <summary>
	/// Reads a JSON object into a new <typeparamref name="T"/>, adding a note property for each field.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A numeric <c>id</c> becomes a <see cref="ReadOnlyNumberProperty{T}"/> of <see cref="int"/>, and every other
	/// value the property that <see cref="WritableProperty.ToProperty{T}(string, object?)"/> picks for it. The caller
	/// runs the object's <see cref="SonarrObject.OnDeserialized()"/>, if it has one.
	/// </para>
	/// <para>
	/// The reader holds its value in one span, as the serializer's stream and span paths give it, so the method reads
	/// <see cref="Utf8JsonReader.ValueSpan"/>, which a Debug build asserts.
	/// </para>
	/// </remarks>
	/// <typeparam name="T">The type of object to create, a model or <see cref="PSObject"/>.</typeparam>
	/// <param name="reader">The reader positioned at the start of the object, and left at its end.</param>
	/// <param name="options">The serializer options that nested values are read with.</param>
	/// <param name="replaceNames">
	/// The model's renames of fields to property names, which win over the global renames, or <see langword="null"/>
	/// for none.
	/// </param>
	/// <param name="propertiesToCapitalize">
	/// The names of the string properties whose values get an uppercase first letter, or <see langword="null"/> for
	/// none.
	/// </param>
	/// <param name="asSent">
	/// <see langword="true"/> to read every value as Sonarr sent it, a string as a string and a nested object or array
	/// the same way, with no span converter, model, or parsing; <see langword="false"/> to read values as the
	/// converter reads them everywhere else.
	/// </param>
	/// <returns>The object, with a property for each of the JSON object's fields.</returns>
	/// <exception cref="JsonException">Thrown when a value can't be read.</exception>
	internal T ConvertToObject<T>(
		ref Utf8JsonReader reader,
		JsonSerializerOptions options,
		IReadOnlyDictionary<string, string>? replaceNames,
		IReadOnlySet<string>? propertiesToCapitalize,
		bool asSent = false)
			where T : PSObject, new()
	{
		Debug.Assert(!reader.HasValueSequence, "The converter reads values from ValueSpan, which a value sequence leaves empty.");

		var pso = new T();
		replaceNames ??= EmptyNameDictionary.Empty<string>();
		propertiesToCapitalize ??= EmptyNameDictionary.Empty<string>();

		while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
		{
			if (reader.TokenType == JsonTokenType.PropertyName)
			{
				string pn = ReadPropertyName(
					ref reader, replaceNames, _config.GlobalReplaceNames.DeserializationNames);

				reader.Read();

				if (reader.TokenType == JsonTokenType.Number
					&&
					Constants.Id.Equals(pn, StringComparison.OrdinalIgnoreCase))
				{
					//TODO: Move functionality into a read-only property key lookup operation.
					pso.Properties.Add(new ReadOnlyNumberProperty<int>(pn, reader.GetInt32()));
					continue;
				}

				if (asSent)
				{
					pso.Properties.Add(WritableProperty.ToProperty<T>(pn, this.ReadAsSent(ref reader, options)));
					continue;
				}

				object? o;
				switch (reader.TokenType)
				{
					case JsonTokenType.StartObject:
						o = this.ReadObject<T>(ref reader, options, pn);
						break;

					case JsonTokenType.StartArray:
						o = this.ConvertToEnumerable(ref reader, pn, options);
						break;

					case JsonTokenType.String when reader.ValueSpan.IsEmpty:
						o = string.Empty;
						break;

					case JsonTokenType.String:
						o = this.ReadString(ref reader, pn, propertiesToCapitalize);
						break;

					case JsonTokenType.Number:
						o = ReadNumber(ref reader);
						break;

					case JsonTokenType.True:
						o = true;
						break;

					case JsonTokenType.False:
						o = false;
						break;

					case JsonTokenType.None:
					case JsonTokenType.Comment:
					case JsonTokenType.Null:
						o = null;
						break;

					case JsonTokenType.EndObject:
					case JsonTokenType.EndArray:
					case JsonTokenType.PropertyName:
					default:
						throw new JsonException("Unable to deserialize the value(s).");
				}

				pso.Properties.Add(WritableProperty.ToProperty<T>(pn, o));  //TODO: Add lookup for read-only properties.
			}
		}

		return pso;
	}

	/// <summary>
	/// Reads a JSON array as the type that the settings give its property, or else as an array of objects.
	/// </summary>
	/// <remarks>
	/// In an array of objects whose property has a span converter, such as a provider field's <c>Value</c>, each string
	/// is read by that converter, as a string value of the property is, so a list of a field's settings keeps
	/// <c>2024</c> a string, while its numbers stay numbers.
	/// </remarks>
	/// <param name="reader">The reader positioned at the start of the array.</param>
	/// <param name="propertyName">The name of the property that the array belongs to.</param>
	/// <param name="options">The serializer options that the array is read with.</param>
	/// <returns>The array, as its property's type or as an array of objects.</returns>
	private object? ConvertToEnumerable(ref Utf8JsonReader reader, string propertyName, JsonSerializerOptions options)
	{
		if (_config.ConvertProperties.TryGetValue(propertyName, out Type? convertTo))
		{
			return JsonSerializer.Deserialize(ref reader, convertTo, options);
		}

		return _config.SpanConverters.ContainsKey(propertyName)
			? this.ReadArrayOfProperty(ref reader, propertyName, options)
			: JsonSerializer.Deserialize<object[]>(ref reader, options);
	}
	/// <summary>
	/// Reads a JSON array whose property has a span converter, reading each string in it through that converter and
	/// every other item as a value outside an object is read.
	/// </summary>
	/// <remarks>
	/// Like the serializer's own path, which every other array of objects takes, the method fills a list and copies it
	/// to an array.
	/// </remarks>
	/// <param name="reader">The reader positioned at the start of the array, and left at its end.</param>
	/// <param name="propertyName">The name of the property that the array belongs to.</param>
	/// <param name="options">The serializer options that the items are read with.</param>
	/// <returns>The array's values.</returns>
	/// <exception cref="JsonException">Thrown when an item can't be read.</exception>
	private object?[] ReadArrayOfProperty(ref Utf8JsonReader reader, string propertyName, JsonSerializerOptions options)
	{
		List<object?> items = [];
		while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
		{
			items.Add(reader.TokenType == JsonTokenType.String
				? this.ReadString(ref reader, propertyName, capitalize: null)
				: this.Read(ref reader, typeof(object), options));
		}

		return [.. items];
	}

	/// <summary>
	/// Reads a JSON number as the narrowest of <see cref="int"/>, <see cref="long"/>, and <see cref="double"/> that
	/// holds it.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A whole number written without a fraction or an exponent is read exactly. One written with them, such as
	/// <c>1.0</c> or <c>1.5e2</c>, is read as a <see cref="double"/> and becomes an <see cref="int"/> or a
	/// <see cref="long"/> when the <see cref="double"/> is whole and fits.
	/// </para>
	/// <para>
	/// A whole number past the range of <see cref="long"/>, such as <c>18446744073709551615</c>, stays a
	/// <see cref="double"/>.
	/// </para>
	/// </remarks>
	/// <param name="reader">The reader positioned at the number.</param>
	/// <returns>
	/// The number as an <see cref="int"/> when it's whole and fits, as a <see cref="long"/> when it's whole, fits a
	/// <see cref="long"/>, and doesn't fit an <see cref="int"/>, and as a <see cref="double"/> otherwise.
	/// </returns>
	/// <exception cref="FormatException">Thrown when the number is outside the range of <see cref="double"/>.</exception>
	private static ValueType ReadNumber(ref Utf8JsonReader reader)
	{
		// Each return boxes its own type. A conditional of an int and a long has the type long, which boxed every whole
		// number as a long.
		if (reader.TryGetInt32(out int intNum))
		{
			return intNum;
		}

		if (reader.TryGetInt64(out long longNum))
		{
			return longNum;
		}

		double number = reader.GetDouble();
		if (double.IsInteger(number))
		{
			if (number >= int.MinValue && number <= int.MaxValue)
			{
				return (int)number;
			}

			// 2^63 is the first double past long.MaxValue, and -2^63 is long.MinValue itself.
			if (number >= long.MinValue && number < 9223372036854775808.0)
			{
				return (long)number;
			}
		}

		return number;
	}

	/// <summary>
	/// Reads a nested JSON object as the model that its property's name and its parent's type call for, or else as a
	/// plain <see cref="PSObject"/>.
	/// </summary>
	/// <remarks>
	/// <c>Series</c>, <c>Episode</c>, and <c>EpisodeFile</c> become a <see cref="SeriesObject"/>, an
	/// <see cref="EpisodeObject"/>, and an <see cref="EpisodeFileObject"/> in any parent. <c>Quality</c> becomes a
	/// <see cref="QualityObject"/> in a <see cref="QualityRevisionObject"/> or a <see cref="QualityDefinitionObject"/>,
	/// and a <see cref="QualityRevisionObject"/> in a <see cref="ManualImportObject"/>. <c>Revision</c> in a
	/// <see cref="QualityRevisionObject"/> becomes a <see cref="RevisionObject"/>. <c>Data</c> in a
	/// <see cref="HistoryObject"/> becomes a plain <see cref="PSObject"/> whose values are read as Sonarr sent them,
	/// since Sonarr sends every one of them as a string whatever the event, and the inference would make numbers,
	/// dates, and versions of some.
	/// </remarks>
	/// <typeparam name="TParent">The type of the object that holds the property.</typeparam>
	/// <param name="reader">The reader positioned at the start of the nested object.</param>
	/// <param name="options">The serializer options that nested values are read with.</param>
	/// <param name="pn">The property's name, after its first letter is uppercased and it's renamed.</param>
	/// <returns>The nested object, as a deserialized and tagged model or a plain <see cref="PSObject"/>.</returns>
	private PSObject ReadObject<TParent>(ref Utf8JsonReader reader, JsonSerializerOptions options, string pn) where TParent : PSObject
	{
		Type parentType = typeof(TParent);
		return pn switch
		{
			Constants.Property_Data when parentType.Equals(typeof(HistoryObject)) => this.ConvertToObject<PSObject>(
								ref reader,
								options,
								replaceNames: null,
								propertiesToCapitalize: null,
								asSent: true),
			Constants.Property_Episode => this.ReadPSObject<EpisodeObject>(ref reader, options),
			Constants.Property_EpisodeFile => this.ReadPSObject<EpisodeFileObject>(ref reader, options),
			Constants.Property_Quality when parentType.Equals(typeof(QualityRevisionObject))
											||
											parentType.Equals(typeof(QualityDefinitionObject)) => this.ReadPSObject<QualityObject>(ref reader, options),
			Constants.Property_Quality when parentType.Equals(typeof(ManualImportObject)) => this.ReadPSObject<QualityRevisionObject>(ref reader, options),
			Constants.Property_Revision when parentType.Equals(typeof(QualityRevisionObject)) => this.ReadPSObject<RevisionObject>(ref reader, options),
			Constants.Property_Series => this.ReadPSObject<SeriesObject>(ref reader, options),
			_ => this.ConvertToObject<PSObject>(
								ref reader,
								options,
								replaceNames: null,
								propertiesToCapitalize: null),
		};
	}
	/// <summary>
	/// Reads a JSON value as Sonarr sent it: a string as a string, a number as <see cref="ReadNumber"/> reads it, and
	/// a nested object or array the same way.
	/// </summary>
	/// <param name="reader">The reader positioned at the value.</param>
	/// <param name="options">The serializer options in use, which nested objects pass on.</param>
	/// <returns>
	/// The value: a string, a number, a <see cref="bool"/>, a plain <see cref="PSObject"/>, an array of objects, or
	/// <see langword="null"/> for a JSON null.
	/// </returns>
	/// <exception cref="JsonException">Thrown when the reader holds a token that doesn't start a value.</exception>
	private object? ReadAsSent(ref Utf8JsonReader reader, JsonSerializerOptions options)
	{
		return reader.TokenType switch
		{
			JsonTokenType.String => reader.GetString(),
			JsonTokenType.Number => ReadNumber(ref reader),
			JsonTokenType.True => true,
			JsonTokenType.False => false,
			JsonTokenType.Null => null,
			JsonTokenType.StartObject => this.ConvertToObject<PSObject>(
				ref reader, options, replaceNames: null, propertiesToCapitalize: null, asSent: true),
			JsonTokenType.StartArray => this.ReadArrayAsSent(ref reader, options),
			_ => throw new JsonException($"Unable to read a value from token type '{reader.TokenType}'."),
		};
	}
	/// <summary>
	/// Reads a JSON array as an array of the values that <see cref="ReadAsSent"/> reads.
	/// </summary>
	/// <remarks>
	/// Sonarr's <c>Data</c> holds no arrays today, so the method is off the hot path.
	/// </remarks>
	/// <param name="reader">The reader positioned at the start of the array, and left at its end.</param>
	/// <param name="options">The serializer options in use, which nested objects pass on.</param>
	/// <returns>The array's values.</returns>
	/// <exception cref="JsonException">Thrown when an item can't be read.</exception>
	private object?[] ReadArrayAsSent(ref Utf8JsonReader reader, JsonSerializerOptions options)
	{
		List<object?> items = [];
		while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
		{
			items.Add(this.ReadAsSent(ref reader, options));
		}

		return [.. items];
	}

	/// <summary>
	/// Reads a nested JSON object as a <typeparamref name="T"/> with the model's own name maps, then deserializes and
	/// tags it.
	/// </summary>
	/// <typeparam name="T">The model type to read.</typeparam>
	/// <param name="reader">The reader positioned at the start of the nested object.</param>
	/// <param name="options">The serializer options that nested values are read with.</param>
	/// <returns>The model, after its <see cref="SonarrObject.OnDeserialized()"/> and its tagging.</returns>
	private T ReadPSObject<T>(ref Utf8JsonReader reader, JsonSerializerOptions options) where T : SonarrObject, ISerializableNames<T>, new()
	{
		var sonarrObj = this.ConvertToObject<T>(
			ref reader, options, T.GetDeserializedNames(), T.GetPropertiesToCapitalize());

		sonarrObj.OnDeserialized();
		sonarrObj.SetTag(_config.Resolver);
		return sonarrObj;
	}

	/// <summary>
	/// Reads a property's name, uppercases its first letter with the invariant culture, and renames it.
	/// </summary>
	/// <remarks>
	/// The name is unescaped into a buffer of as many characters as its raw UTF-8 bytes, which is never fewer than it
	/// unescapes to, on the stack when that's up to <see cref="MAX_STACKALLOC"/> and rented otherwise, which goes back
	/// to its pool before the method returns.
	/// </remarks>
	/// <param name="reader">The reader positioned at the property's name.</param>
	/// <param name="replaceNames">The model's renames, which are tried first.</param>
	/// <param name="globalReplace">The renames that apply to every object, which are tried second.</param>
	/// <returns>The renamed property name, or the capitalized name when no rename matches.</returns>
	private static string ReadPropertyName(ref Utf8JsonReader reader, IReadOnlyDictionary<string, string> replaceNames, IReadOnlyDictionary<string, string> globalReplace)
	{
		int length = reader.ValueSpan.Length;
		using RentedBuffer<char> buffer = RentedBuffer.Rent<char>(
			length <= MAX_STACKALLOC
				? stackalloc char[length]
				: length);

		int written = reader.CopyString(buffer.Span);
		Span<char> chars = buffer[..written];

		if (!chars.IsEmpty)
		{
			chars[0] = char.ToUpperInvariant(chars[0]);
		}

		string propertyName = new(chars);
		if (replaceNames.TryGetValue(propertyName, out string? replacement))
		{
			return replacement;
		}
		else if (globalReplace.TryGetValue(propertyName, out string? gbReplacement))
		{
			return gbReplacement;
		}

		return propertyName;
	}
	/// <summary>
	/// The format of an ISO 8601 date, which a string has to match exactly to become a <see cref="DateOnly"/>.
	/// </summary>
	private const string ISO_DATE = "yyyy-MM-dd";
	/// <summary>
	/// The formats of an ISO 8601 date and time, one of which a string has to match exactly to become a
	/// <see cref="DateTimeOffset"/>.
	/// </summary>
	/// <remarks>
	/// <c>FFFFFFF</c> takes no fraction of a second, or up to seven digits of one, and the period before it goes with
	/// it. <c>K</c> takes <c>Z</c>, an offset, or nothing.
	/// </remarks>
	private static readonly string[] _isoDateTimes = ["yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK", "yyyy-MM-dd'T'HH:mmK"];

	/// <summary>
	/// Parses a string value as a <see cref="Guid"/>, a <see cref="DateOnly"/>, a <see cref="DateTimeOffset"/>, or a
	/// <see cref="Version"/>, tried in that order, or else keeps it a string.
	/// </summary>
	/// <remarks>
	/// Dates are read only in the ISO 8601 forms that Sonarr writes, with the invariant culture, so text such as
	/// <c>12:00:00</c>, <c>1/2</c>, or <c>3.31.1</c> isn't taken for a date.
	/// </remarks>
	/// <param name="chars">The value's unescaped characters.</param>
	/// <param name="propertyName">
	/// The name of the property that the value belongs to. A <see cref="DateTimeOffset"/> is converted to local time
	/// unless the name ends in <c>Utc</c>, without regard to case.
	/// </param>
	/// <returns>The parsed value, or the value as a string.</returns>
	private static object ReadString(ReadOnlySpan<char> chars, string propertyName)
	{
		if (Guid.TryParse(chars, CultureInfo.InvariantCulture, out Guid guidStr))
		{
			return guidStr;
		}
		else if (TryReadIsoDate(chars, propertyName, out object? date))
		{
			return date;
		}
		else if (Version.TryParse(chars, out Version? version))
		{
			return version;
		}
		else
		{
			return new string(chars);
		}
	}
	/// <summary>
	/// Tries to parse a string value as an ISO 8601 date, <c>yyyy-MM-dd</c>, or an ISO 8601 date and time.
	/// </summary>
	/// <remarks>
	/// A value that doesn't start with a date shaped like <c>yyyy-MM-dd</c> fails before either parse runs, so most
	/// strings cost two comparisons.
	/// </remarks>
	/// <param name="chars">The value's unescaped characters.</param>
	/// <param name="propertyName">
	/// The name of the property that the value belongs to, which decides whether a <see cref="DateTimeOffset"/> is
	/// converted to local time.
	/// </param>
	/// <param name="result">
	/// When this method returns, a boxed <see cref="DateOnly"/> or <see cref="DateTimeOffset"/> if parsing succeeded;
	/// otherwise, <see langword="null"/>.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if the value is an ISO 8601 date or date and time; otherwise, <see langword="false"/>.
	/// </returns>
	private static bool TryReadIsoDate(ReadOnlySpan<char> chars, string propertyName, [NotNullWhen(true)] out object? result)
	{
		if (chars.Length >= ISO_DATE.Length && chars[4] == '-' && chars[7] == '-')
		{
			if (chars.Length == ISO_DATE.Length)
			{
				if (DateOnly.TryParseExact(chars, ISO_DATE, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date))
				{
					result = date;
					return true;
				}
			}
			else if (chars[ISO_DATE.Length] == 'T'
				&&
				DateTimeOffset.TryParseExact(chars, _isoDateTimes, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset offset))
			{
				result = propertyName.EndsWith(['U', 'T', 'C'], StringComparison.OrdinalIgnoreCase)
					? offset
					: ToLocalTime(offset);

				return true;
			}
		}

		result = null;
		return false;
	}
	/// <summary>
	/// Converts a <see cref="DateTimeOffset"/> to local time when the local time stands for the same instant.
	/// </summary>
	/// <remarks>
	/// <see cref="DateTimeOffset.ToLocalTime"/> clamps a local time outside the range of <see cref="DateTime"/>, so
	/// west of UTC, Sonarr's <c>0001-01-01T00:00:00Z</c> would become <c>0001-01-01T00:00:00</c> at the local offset,
	/// another instant, which a request would then send back.
	/// </remarks>
	/// <param name="offset">The value to convert.</param>
	/// <returns>
	/// The value in local time, or <paramref name="offset"/> itself when its local time falls outside the range of
	/// <see cref="DateTime"/>.
	/// </returns>
	private static DateTimeOffset ToLocalTime(DateTimeOffset offset)
	{
		DateTimeOffset local = offset.ToLocalTime();
		return local.UtcTicks == offset.UtcTicks
			? local
			: offset;
	}
	/// <summary>
	/// Reads a JSON string value as its property's span converter, the number parsing, or the string parsing makes it.
	/// </summary>
	/// <remarks>
	/// <para>
	/// When the property is one to capitalize, the value's first letter is uppercased first, with the invariant
	/// culture. Then the property's span converter reads the value, or else it's parsed as an <see cref="int"/>,
	/// <see cref="long"/>, or <see cref="double"/>, or else as <see cref="ReadString(ReadOnlySpan{char}, string)"/>
	/// parses it. An escaped value goes through the same steps once the reader unescapes it.
	/// </para>
	/// <para>
	/// The characters are copied to the stack when the value is up to <see cref="MAX_STACKALLOC"/> bytes long, and to a
	/// rented buffer otherwise, which goes back to its pool before the method returns. The raw bytes, escapes
	/// included, are never fewer than the characters that they unescape to.
	/// </para>
	/// </remarks>
	/// <param name="reader">The reader positioned at the string value.</param>
	/// <param name="propertyName">
	/// The name of the property that the value, or the array that holds it, belongs to, or empty for a value that isn't
	/// in an object.
	/// </param>
	/// <param name="capitalize">
	/// The names of the properties whose values get an uppercase first letter, or <see langword="null"/> for none.
	/// </param>
	/// <returns>The converted value.</returns>
	private object? ReadString(ref Utf8JsonReader reader, string propertyName, IReadOnlySet<string>? capitalize)
	{
		Debug.Assert(!reader.HasValueSequence, "The converter reads values from ValueSpan, which a value sequence leaves empty.");
		capitalize ??= EmptyNameDictionary.Empty<string>();

		int length = reader.ValueSpan.Length;

		using (RentedBuffer<char> buffer = RentedBuffer.Rent<char>(
			length <= MAX_STACKALLOC
				? stackalloc char[length]
				: length))
		{
			int written = reader.CopyString(buffer.Span);

			if (written > 0 && capitalize.Contains(propertyName) && char.IsLower(buffer[0]))
			{
				buffer[0] = char.ToUpperInvariant(buffer[0]);
			}

			object? result;
			if (_config.SpanConverters.TryGetValue(propertyName, out SpanConverter? converter))
			{
				result = converter.ConvertSpan(buffer[..written], propertyName);
			}
			else if (TryReadAsNumber(buffer[..written], out ValueType? asValueType))
			{
				result = asValueType;
			}
			else
			{
				result = ReadString(buffer[..written], propertyName);
			}

			return result;
		}
	}

	/// <summary>
	/// Throws a <see cref="JsonException"/> that says a token couldn't be read as an array of objects.
	/// </summary>
	/// <typeparam name="T">The type that the caller would have returned.</typeparam>
	/// <returns>Nothing, since the method always throws.</returns>
	/// <exception cref="JsonException">Thrown when the method is called.</exception>
	[DoesNotReturn]
	private static T ThrowCantRead<T>()
	{
		throw new JsonException("Unable to read the JSON token into an array of objects.");
	}

	/// <summary>
	/// Tries to parse a string value as an <see cref="int"/>, a <see cref="long"/>, or a finite <see cref="double"/>,
	/// in that order, with the invariant culture.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The value may have white space at either end and a leading sign, and a <see cref="double"/> a decimal point and
	/// an exponent, but no group separators, which <see cref="double.TryParse(ReadOnlySpan{char}, IFormatProvider, out double)"/>
	/// would otherwise take, so <c>1,5</c> stays a string where it would read as 15.
	/// </para>
	/// <para>
	/// <c>NaN</c>, <c>Infinity</c>, and a value past the range of <see cref="double"/>, such as <c>1e400</c>, stay
	/// strings, since a JSON number can't hold them and writing one back would throw.
	/// </para>
	/// </remarks>
	/// <param name="chars">The value's unescaped characters.</param>
	/// <param name="result">
	/// When this method returns, the parsed number, boxed, if parsing succeeded; otherwise, <see langword="null"/>.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if the value parsed as one of the three types; otherwise, <see langword="false"/>.
	/// </returns>
	private static bool TryReadAsNumber(ReadOnlySpan<char> chars, [NotNullWhen(true)] out ValueType? result)
	{
		if (int.TryParse(chars, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intNum))
		{
			result = intNum;
			return true;
		}
		else if (long.TryParse(chars, NumberStyles.Integer, CultureInfo.InvariantCulture, out long longNum))
		{
			result = longNum;
			return true;
		}
		else if (double.TryParse(chars, NumberStyles.Float, CultureInfo.InvariantCulture, out double dubNum)
			&&
			double.IsFinite(dubNum))
		{
			result = dubNum;
			return true;
		}

		result = null;
		return false;
	}

	/// <summary>
	/// Writes an object as JSON.
	/// </summary>
	/// <remarks>
	/// A <see cref="PSObject"/> is written as an object of its gettable note properties, a bare
	/// <see cref="PSCustomObject"/> as an empty object, <see langword="null"/> as a JSON null, and anything else by its
	/// runtime type.
	/// </remarks>
	/// <param name="writer">The writer to write the value to.</param>
	/// <param name="value">The value to write.</param>
	/// <param name="options">The serializer options that nested values are written with.</param>
	public override void Write(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
	{
		if (value is PSObject pso)
		{
			this.WritePSObject(writer, options, pso);
		}
		else if (value is PSCustomObject)
		{
			writer.WriteEmptyObject();
		}
		else if (value is null)
		{
			writer.WriteNullValue();
		}
		else
		{
			writer.WriteRawValue(JsonSerializer.Serialize(value, value.GetType(), options));
		}
	}

	/// <summary>
	/// Writes an object's gettable note properties as a JSON object.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A property that the settings ignore is left out. Each name is renamed by <paramref name="replaceNames"/>, or
	/// else by the global renames, and then written through the naming policy.
	/// </para>
	/// <para>
	/// A string, <see cref="bool"/>, <see cref="int"/>, <see cref="long"/>, <see cref="decimal"/>, or
	/// <see cref="double"/> value is written directly, a <see langword="null"/> as a JSON null, and any other value
	/// through the serializer by its runtime type, so a nested model goes through its own converter.
	/// </para>
	/// </remarks>
	/// <param name="writer">The writer to write the object to.</param>
	/// <param name="options">The serializer options that nested values are written with.</param>
	/// <param name="pso">The object whose note properties are written.</param>
	/// <param name="replaceNames">
	/// The model's renames of property names back to JSON fields, or <see langword="null"/> for none.
	/// </param>
	internal void WritePSObject(Utf8JsonWriter writer, JsonSerializerOptions options, PSObject pso, IReadOnlyDictionary<string, string>? replaceNames = null)
	{
		replaceNames ??= EmptyNameDictionary.Empty<string>();
		var globalReplace = _config.GlobalReplaceNames.SerializationNames;

		writer.WriteStartObject();

		PSPropertyInfo[] props = [.. pso.Properties.Where(x => x.MemberType == PSMemberTypes.NoteProperty && x.IsGettable)];
		var policy = this.NamingPolicy;

		foreach (PSPropertyInfo prop in props)
		{
			if (_config.IgnoreProperties.Contains(prop.Name))
			{
				continue;
			}

			string pn = replaceNames.TryGetValue(prop.Name, out string? newPn)
				? newPn
				: globalReplace.TryGetValue(prop.Name, out string? globalPn)
					? globalPn
					: prop.Name;

			policy.WritePropertyName(writer, pn);

			switch (prop.Value)
			{
				case string strVal:
					writer.WriteStringValue(strVal);
					break;

				case bool boolVal:
					writer.WriteBooleanValue(boolVal);
					break;

				case int intVal:
					writer.WriteNumberValue(intVal);
					break;

				case long longVal:
					writer.WriteNumberValue(longVal);
					break;

				case decimal decVal:
					writer.WriteNumberValue(decVal);
					break;

				case double dubVal:
					writer.WriteNumberValue(dubVal);
					break;

				case null:
					writer.WriteNullValue();
					break;

				default:
					JsonSerializer.Serialize(
						writer,
						prop.Value,
						prop.Value.GetType(),
						options);
					break;
			}
		}

		writer.WriteEndObject();

		return;
	}
}
