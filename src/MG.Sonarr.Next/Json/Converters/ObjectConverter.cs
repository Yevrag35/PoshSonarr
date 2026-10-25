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
using System.Text;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Json.Converters;

public sealed class ObjectConverter : JsonConverter<object>
{
	const int MAX_STACKALLOC = 256;
	readonly ObjectConverterConfiguration _config;

	public ObjectConverter(IMetadataResolver resolver, Action<IObjectConverterConfig> configure)
	{
		ObjectConverterConfiguration config = new(resolver);
		configure(config);

		_config = config;
	}

	public override bool CanConvert(Type typeToConvert)
	{
		return typeToConvert.Equals(typeof(PSObject))
			   ||
			   typeToConvert.Equals(typeof(object))
			   ||
			   typeToConvert.Equals(typeof(PSCustomObject));
	}

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

	private static object[] ConvertToListOfObjects(ref Utf8JsonReader reader, JsonSerializerOptions options)
	{
		return JsonSerializer.Deserialize<object[]>(ref reader, options) ??
			throw new JsonException("Unable to deserialize into an array of object instances.");
	}

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

	private object? ConvertToEnumerable(ref Utf8JsonReader reader, string propertyName, JsonSerializerOptions options)
	{
		return _config.ConvertProperties.TryGetValue(propertyName, out Type? convertTo)
			? JsonSerializer.Deserialize(ref reader, convertTo, options)
			: JsonSerializer.Deserialize<object[]>(ref reader, options);
	}

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
	private T ReadPSObject<T>(ref Utf8JsonReader reader, JsonSerializerOptions options) where T : SonarrObject, ISerializableNames<T>, new()
	{
		var sonarrObj = this.ConvertToObject<T>(
			ref reader, options, T.GetDeserializedNames(), T.GetPropertiesToCapitalize());

		sonarrObj.OnDeserialized();
		sonarrObj.SetTag(_config.Resolver);
		return sonarrObj;
	}

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

			if (capitalize.Contains(propertyName) && char.IsLower(buffer[0]))
			{
				buffer[0] = char.ToUpper(buffer[0]);
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

	[DoesNotReturn]
	private static T ThrowCantRead<T>()
	{
		throw new JsonException("Unable to read the JSON token into an array of objects.");
	}

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

	public override void Write(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
	{
		if (value is null)
		{
			writer.WriteNullValue();
			return;
		}

		if (value is PSObject pso)
		{
			WorkingNamingPolicy policy = new(options);
			this.WritePSObject(writer, options, pso, ref policy);
		}
		else if (value is PSCustomObject)
		{
			writer.WriteStartObject();
			writer.WriteEndObject();
		}
		else
		{
			writer.WriteRawValue(JsonSerializer.Serialize(value, value.GetType(), options));
		}
	}

	internal void WritePSObject(Utf8JsonWriter writer, JsonSerializerOptions options, PSObject pso, ref readonly WorkingNamingPolicy policy, IReadOnlyDictionary<string, string>? replaceNames = null)
	{
		replaceNames ??= EmptyNameDictionary.Empty<string>();
		var globalReplace = _config.GlobalReplaceNames.SerializationNames;

		writer.WriteStartObject();

		PSPropertyInfo[] props = [.. pso.Properties.Where(x => x.MemberType == PSMemberTypes.NoteProperty && x.IsGettable)];
		bool containsMetadata = props.Any(x => x.Name == "MetadataTag");

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
