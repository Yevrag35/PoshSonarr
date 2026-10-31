using MG.Sonarr.Next.Buffers;
using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Extensions.Strings;
using MG.Sonarr.Next.Json.Converters.Spans;
using MG.Sonarr.Next.Json.Naming;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models;
using MG.Sonarr.Next.Models.Episodes;
using MG.Sonarr.Next.Models.History;
using MG.Sonarr.Next.Models.ManualImports;
using MG.Sonarr.Next.Models.Qualities;
using MG.Sonarr.Next.Models.Releases;
using MG.Sonarr.Next.Models.Series;
using MG.Sonarr.Next.PSProperties;
using System.Management.Automation;

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
/// property's name and its parent's type call for one, and a plain <see cref="PSObject"/> otherwise.
/// </para>
/// <para>
/// A <see cref="DateTimeOffset"/> is converted to local time unless its property's name ends in <c>Utc</c>.
/// </para>
/// <para>
/// The converter keeps no state between calls beyond its settings, so a connection's serializer options share one
/// instance across threads.
/// </para>
/// </remarks>
public sealed class ObjectConverter : ByPolicyJsonConverter<object>//JsonConverter<object>
{
	/// <summary>
	/// The longest string value, in UTF-8 bytes, whose characters are copied to the stack instead of a rented buffer.
	/// </summary>
	const int MAX_STACKALLOC = 256;
	/// <summary>
	/// The settings that the constructor's callback filled.
	/// </summary>
	readonly ObjectConverterConfiguration _config;

	/// <summary>
	/// Initializes a new <see cref="ObjectConverter"/> instance with the resolver that nested models are tagged with,
	/// the naming policy that property names are written with, and a callback that supplies its settings.
	/// </summary>
	/// <param name="resolver">The resolver that holds the tags that the connection registered.</param>
	/// <param name="policy">The naming policy that the converter writes property names with.</param>
	/// <param name="configure">
	/// The callback that adds the converter's span converters, array types, global renames, and ignored properties.
	/// </param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="policy"/> is null.</exception>
	/// <exception cref="NullReferenceException">Thrown when <paramref name="configure"/> is null.</exception>
	public ObjectConverter(IMetadataResolver resolver, WorkingNamingPolicy policy, Action<IObjectConverterConfig> configure)
		: base(policy)
	{
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
			JsonTokenType.String => this.ReadString(ref reader, options, string.Empty, null),
			JsonTokenType.Number => ReadNumber(ref reader, options),
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
	/// A numeric <c>id</c> becomes a <see cref="ReadOnlyNumberProperty{T}"/> of <see cref="int"/>, and every other
	/// value the property that <see cref="WritableProperty.ToProperty{T}(string, object?)"/> picks for it. The caller
	/// runs the object's <see cref="SonarrObject.OnDeserialized()"/>, if it has one.
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
	/// <returns>The object, with a property for each of the JSON object's fields.</returns>
	/// <exception cref="JsonException">Thrown when a value can't be read.</exception>
	internal T ConvertToObject<T>(
		ref Utf8JsonReader reader,
		JsonSerializerOptions options,
		IReadOnlyDictionary<string, string>? replaceNames,
		IReadOnlySet<string>? propertiesToCapitalize)
			where T : PSObject, new()
	{
		var pso = new T();
		replaceNames ??= EmptyNameDictionary.Empty<string>();
		propertiesToCapitalize ??= EmptyNameDictionary.Empty<string>();

		while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
		{
			if (reader.TokenType == JsonTokenType.PropertyName)
			{
				string pn = ReadPropertyName(
					ref reader, options, replaceNames, _config.GlobalReplaceNames.DeserializationNames);

				reader.Read();

				if (reader.TokenType == JsonTokenType.Number
					&&
					Constants.ID.Equals(pn, StringComparison.OrdinalIgnoreCase))
				{
					//TODO: Move functionality into a read-only property key lookup operation.
					pso.Properties.Add(new ReadOnlyNumberProperty<int>(pn, reader.GetInt32()));
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
						o = this.ReadString(ref reader, options, pn, propertiesToCapitalize);
						break;

					case JsonTokenType.Number:
						o = ReadNumber(ref reader, options);
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
	/// <param name="reader">The reader positioned at the start of the array.</param>
	/// <param name="propertyName">The name of the property that the array belongs to.</param>
	/// <param name="options">The serializer options that the array is read with.</param>
	/// <returns>The array, as its property's type or as an array of objects.</returns>
	private object? ConvertToEnumerable(ref Utf8JsonReader reader, string propertyName, JsonSerializerOptions options)
	{
		return _config.ConvertProperties.TryGetValue(propertyName, out Type? convertTo)
			? JsonSerializer.Deserialize(ref reader, convertTo, options)
			: JsonSerializer.Deserialize<object[]>(ref reader, options);
	}

	/// <summary>
	/// Copies a string value into a new string, reducing each backslash-quote pair to a quote and each pair of
	/// backslashes to one.
	/// </summary>
	/// <remarks>
	/// The copy is built in a stack buffer as long as the value.
	/// </remarks>
	/// <param name="chars">The value's characters.</param>
	/// <param name="propertyName">The name of the property that the value belongs to, which isn't used.</param>
	/// <returns>The value, with its escaped quotes and backslashes reduced.</returns>
	private static string ProcessQuotes(ReadOnlySpan<char> chars, string propertyName)
	{
		int position = 0;
		ReadOnlySpan<char> quotes = ['\\', '"'];
		ReadOnlySpan<char> backs = ['\\', '\\'];
		Span<char> scratch = stackalloc char[chars.Length];
		foreach (SplitEntry section in chars.SpanSplit(quotes, backs))
		{
			section.Chars.CopyTo(scratch[position..]);
			position += section.Chars.Length;

			if (!section.Separator.IsEmpty)
			{
				scratch[position++] = section.Separator[1];
			}
		}

		return new string(scratch[..position]);
	}
	//BUG: Every number is read through GetDouble, so a whole number past 2^53 loses precision: 9007199254740993 reads as the long 9007199254740992.
	/// <summary>
	/// Reads a JSON number as the narrowest of <see cref="int"/>, <see cref="long"/>, and <see cref="double"/> that
	/// holds it exactly.
	/// </summary>
	/// <param name="reader">The reader positioned at the number.</param>
	/// <param name="options">The serializer options in use, which aren't used.</param>
	/// <returns>
	/// The number as an <see cref="int"/> when it's whole and fits, as a <see cref="long"/> when it's whole and
	/// doesn't, and as a <see cref="double"/> otherwise.
	/// </returns>
	private static ValueType ReadNumber(ref Utf8JsonReader reader, JsonSerializerOptions options)
	{
		double number = reader.GetDouble();
		if (double.IsInteger(number))
		{
			return number <= int.MaxValue && number >= int.MinValue
				? (int)number
				: (long)number;
		}

		return number;
	}

	/// <summary>
	/// Reads a nested JSON object as the model that its property's name and its parent's type call for, or else as a
	/// plain <see cref="PSObject"/>.
	/// </summary>
	/// <remarks>
	/// <c>Series</c>, <c>Episode</c>, and <c>EpisodeFile</c> become a <see cref="SeriesObject"/>, an
	/// <see cref="EpisodeObject"/>, and an <see cref="EpisodeFileObject"/> in any parent. <c>Data</c> in a
	/// <see cref="HistoryObject"/> becomes a <see cref="ReleaseObject"/>. <c>Quality</c> becomes a
	/// <see cref="QualityObject"/> in a <see cref="QualityRevisionObject"/> or a <see cref="QualityDefinitionObject"/>,
	/// and a <see cref="QualityRevisionObject"/> in a <see cref="ManualImportObject"/>. <c>Revision</c> in a
	/// <see cref="QualityRevisionObject"/> becomes a <see cref="RevisionObject"/>.
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
			Constants.PROPERTY_DATA when parentType.Equals(typeof(HistoryObject)) => this.ReadPSObject<ReleaseObject>(ref reader, options),
			Constants.PROPERTY_EPISODE => this.ReadPSObject<EpisodeObject>(ref reader, options),
			Constants.PROPERTY_EPISODE_FILE => this.ReadPSObject<EpisodeFileObject>(ref reader, options),
			Constants.PROPERTY_QUALITY when parentType.Equals(typeof(QualityRevisionObject))
											||
											parentType.Equals(typeof(QualityDefinitionObject)) => this.ReadPSObject<QualityObject>(ref reader, options),
			Constants.PROPERTY_QUALITY when parentType.Equals(typeof(ManualImportObject)) => this.ReadPSObject<QualityRevisionObject>(ref reader, options),
			Constants.PROPERTY_REVISION when parentType.Equals(typeof(QualityRevisionObject)) => this.ReadPSObject<RevisionObject>(ref reader, options),
			Constants.PROPERTY_SERIES => this.ReadPSObject<SeriesObject>(ref reader, options),
			_ => this.ConvertToObject<PSObject>(
								ref reader,
								options,
								replaceNames: null,
								propertiesToCapitalize: null),
		};
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
	/// The name is unescaped into a stack buffer sized for the most characters that its UTF-8 bytes can hold.
	/// </remarks>
	/// <param name="reader">The reader positioned at the property's name.</param>
	/// <param name="options">The serializer options in use, which aren't used.</param>
	/// <param name="replaceNames">The model's renames, which are tried first.</param>
	/// <param name="globalReplace">The renames that apply to every object, which are tried second.</param>
	/// <returns>The renamed property name, or the capitalized name when no rename matches.</returns>
	private static string ReadPropertyName(ref Utf8JsonReader reader, JsonSerializerOptions options, IReadOnlyDictionary<string, string> replaceNames, IReadOnlyDictionary<string, string> globalReplace)
	{
		int length = Encoding.UTF8.GetMaxCharCount(reader.ValueSpan.Length);
		Span<char> chars = stackalloc char[length];

		int written = reader.CopyString(chars);

		chars[0] = char.ToUpperInvariant(chars[0]);

		string propertyName = new(chars[..written]);
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
	/// Parses a string value as a <see cref="Guid"/>, a <see cref="DateOnly"/>, a <see cref="DateTimeOffset"/>, or a
	/// <see cref="Version"/>, tried in that order, or else keeps it a string.
	/// </summary>
	/// <param name="chars">The value's unescaped characters.</param>
	/// <param name="propertyName">
	/// The name of the property that the value belongs to. A <see cref="DateTimeOffset"/> is converted to local time
	/// unless the name ends in <c>Utc</c>, without regard to case.
	/// </param>
	/// <returns>The parsed value, or the value as a string.</returns>
	private static object ReadString(ReadOnlySpan<char> chars, string propertyName)
	{
		if (Guid.TryParse(chars, Statics.DefaultProvider, out Guid guidStr))
		{
			return guidStr;
		}
		else if (DateOnly.TryParse(chars, Statics.DefaultProvider, DateTimeStyles.None, out DateOnly @do))
		{
			return @do;
		}
		else if (DateTimeOffset.TryParse(chars, Statics.DefaultProvider, DateTimeStyles.AssumeUniversal, out DateTimeOffset offset))
		{
			return propertyName.EndsWith(['U', 'T', 'C'], StringComparison.OrdinalIgnoreCase)
				? offset
				: offset.ToLocalTime();
		}
		else if (Version.TryParse(chars, out Version? version))
		{
			return version;
		}
		else
		{
			return ProcessQuotes(chars, propertyName);
		}
	}
	/// <summary>
	/// Reads a JSON string value as its property's span converter, the number parsing, or the string parsing makes it.
	/// </summary>
	/// <remarks>
	/// <para>
	/// When the property is one to capitalize, the value's first letter is uppercased first, with the invariant
	/// culture. Then the property's span converter reads the value, or else it's parsed as an <see cref="int"/>,
	/// <see cref="long"/>, <see cref="double"/>, or <see cref="decimal"/>, or else as
	/// <see cref="ReadString(ReadOnlySpan{char}, string)"/> parses it.
	/// </para>
	/// <para>
	/// An escaped value is returned as the reader unescapes it, without any of those steps.
	/// </para>
	/// <para>
	/// The characters are copied to the stack when the value is up to <see cref="MAX_STACKALLOC"/> bytes long, and to a
	/// rented buffer otherwise, which goes back to its pool before the method returns.
	/// </para>
	/// </remarks>
	/// <param name="reader">The reader positioned at the string value.</param>
	/// <param name="options">The serializer options in use, which aren't used.</param>
	/// <param name="propertyName">
	/// The name of the property that the value belongs to, or empty for a value that isn't in an object.
	/// </param>
	/// <param name="capitalize">
	/// The names of the properties whose values get an uppercase first letter, or <see langword="null"/> for none.
	/// </param>
	/// <returns>The converted value.</returns>
	private object? ReadString(ref Utf8JsonReader reader, JsonSerializerOptions options, string propertyName, IReadOnlySet<string>? capitalize)
	{
		capitalize ??= EmptyNameDictionary.Empty<string>();

		if (reader.ValueIsEscaped)
		{
			return reader.GetString() ?? string.Empty;
		}

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
	/// Tries to parse a string value as an <see cref="int"/>, a <see cref="long"/>, a <see cref="double"/>, or a
	/// <see cref="decimal"/>, in that order.
	/// </summary>
	/// <param name="chars">The value's unescaped characters.</param>
	/// <param name="result">
	/// When this method returns, the parsed number, boxed, if parsing succeeded; otherwise, <see langword="null"/>.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if the value parsed as one of the four types; otherwise, <see langword="false"/>.
	/// </returns>
	private static bool TryReadAsNumber(Span<char> chars, [NotNullWhen(true)] out ValueType? result)
	{
		bool returnVal = false;
		result = default;

		if (int.TryParse(chars, Statics.DefaultProvider, out int intNum))
		{
			result = intNum;
			returnVal = true;
		}
		else if (long.TryParse(chars, Statics.DefaultProvider, out long longNum))
		{
			result = longNum;
			returnVal = true;
		}
		else if (double.TryParse(chars, Statics.DefaultProvider, out double dubNum))
		{
			result = dubNum;
			returnVal = true;
		}
		else if (decimal.TryParse(chars, Statics.DefaultProvider, out decimal decNum))
		{
			result = decNum;
			returnVal = true;
		}

		return returnVal;
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
