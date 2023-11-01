using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Json.Converters.Spans;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models;
using MG.Sonarr.Next.Models.Episodes;
using MG.Sonarr.Next.Models.History;
using MG.Sonarr.Next.Models.ManualImports;
using MG.Sonarr.Next.Models.Qualities;
using MG.Sonarr.Next.Models.Releases;
using MG.Sonarr.Next.Models.Series;
using MG.Sonarr.Next.PSProperties;
using System.Buffers;
using System.Management.Automation;
using System.Text;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Json.Converters
{
    public sealed class ObjectConverter : JsonConverter<object>
    {
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

        private static List<object> ConvertToListOfObjects(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            return JsonSerializer.Deserialize<List<object>>(ref reader, options) ?? 
                throw new JsonException("Unable to deserialize into an array of PSObject instances.");
        }

        internal T ConvertToObject<T>(ref Utf8JsonReader reader, JsonSerializerOptions options, IReadOnlyDictionary<string, string>? replaceNames, IReadOnlySet<string>? propertiesToCapitalize) where T : PSObject, new()
        {
            var pso = new T();
            replaceNames ??= EmptyNameDictionary<string>.Default;
            propertiesToCapitalize ??= EmptyNameDictionary<string>.Default;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string pn = ReadPropertyName(
                        ref reader, options, replaceNames, _config.GlobalReplaceNames.DeserializationNames);

                    reader.Read();

                    if (reader.TokenType == JsonTokenType.Number
                        &&
                        Constants.ID.Equals(pn, StringComparison.InvariantCultureIgnoreCase))
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

                        case JsonTokenType.String:
                            o = !reader.ValueSpan.IsEmpty
                                ? this.ReadString(ref reader, options, pn, propertiesToCapitalize)
                                : string.Empty;

                            break;

                        case JsonTokenType.Number:
                            o = ReadNumber(ref reader, options);
                            break;

                        case JsonTokenType.True:
                        case JsonTokenType.False:
                            o = ReadBoolean(ref reader, options);
                            break;

                        case JsonTokenType.None:
                        case JsonTokenType.Comment:
                        case JsonTokenType.Null:
                            o = null;
                            break;

                        case JsonTokenType.EndObject:
                        case JsonTokenType.EndArray:
                        case JsonTokenType.PropertyName:
                            goto default;

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
            ReadOnlySpan<char> quotes = stackalloc char[] { '\\', '"' };
            ReadOnlySpan<char> backs = stackalloc char[] { '\\', '\\' };
            Span<char> scratch = stackalloc char[chars.Length];

            foreach (SplitEntry section in chars.SpanSplit(quotes, backs))
            {
                section.Chars.CopyTo(scratch.Slice(position));
                position += section.Chars.Length;

                if (!section.Separator.IsEmpty)
                {
                    scratch[position++] = section.Separator[1];
                }
            }

            return new string(scratch.Slice(0, position));
        }

        private static bool ReadBoolean(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            Span<char> chars = stackalloc char[5];
            int written = Encoding.UTF8.GetChars(reader.ValueSpan, chars);

            return bool.TryParse(chars.Slice(0, written), out bool result) && result;
        }
        private static ValueType ReadNumber(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            Span<char> chars = stackalloc char[reader.ValueSpan.Length];
            int written = Encoding.UTF8.GetChars(reader.ValueSpan, chars);

            chars = chars.Slice(0, written);
            if (int.TryParse(chars, Statics.DefaultProvider, out int intNum))
            {
                return intNum;
            }
            else if (long.TryParse(chars, Statics.DefaultProvider, out long longNum))
            {
                return longNum;
            }
            else if (double.TryParse(chars, Statics.DefaultProvider, out double dubNum))
            {
                return dubNum;
            }
            else if (decimal.TryParse(chars, Statics.DefaultProvider, out decimal decNum))
            {
                return decNum;
            }
            else
            {
                return int.MinValue;
            }
        }

        private object ReadObject<TParent>(ref Utf8JsonReader reader, JsonSerializerOptions options, string pn) where TParent : PSObject
        {
            Type parentType = typeof(TParent);
            switch (pn)
            {
                case Constants.PROPERTY_DATA:
                    if (!parentType.Equals(typeof(HistoryObject)))
                    {
                        goto default;
                    }

                    return this.ReadPSObject<ReleaseObject>(ref reader, options);

                case Constants.PROPERTY_EPISODE:
                    return this.ReadPSObject<EpisodeObject>(ref reader, options);

                case Constants.PROPERTY_EPISODE_FILE:
                    return this.ReadPSObject<EpisodeFileObject>(ref reader, options);

                case Constants.PROPERTY_QUALITY:
                    if (parentType.Equals(typeof(QualityRevisionObject))
                        ||
                        parentType.Equals(typeof(QualityDefinitionObject)))
                    {
                        return this.ReadPSObject<QualityObject>(ref reader, options);
                    }
                    else if (parentType.Equals(typeof(ManualImportObject)))
                    {
                        return this.ReadPSObject<QualityRevisionObject>(ref reader, options);
                    }

                    goto default;

                case Constants.PROPERTY_REVISION:
                    if (parentType.Equals(typeof(QualityRevisionObject)))
                    {
                        return this.ReadPSObject<RevisionObject>(ref reader, options);
                    }

                    goto default;

                case Constants.PROPERTY_SERIES:
                    return this.ReadPSObject<SeriesObject>(ref reader, options);

                default:
                    return this.ConvertToObject<PSObject>(ref reader, options, null, null);
            }
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
            Span<char> chars = stackalloc char[reader.ValueSpan.Length];
            int written = Encoding.UTF8.GetChars(reader.ValueSpan, chars);

            chars = chars.Slice(0, written);
            ref char first = ref chars[0];
            if (char.IsLower(first))
            {
                first = char.ToUpper(first);
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
        private object ReadString(Span<char> chars, string propertyName)
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
                return propertyName.AsSpan().EndsWith(stackalloc char[] { 'U', 'T', 'C' }, StringComparison.InvariantCultureIgnoreCase)
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
            capitalize ??= EmptyNameDictionary<string>.Default;
            bool isRented = false;
            char[]? array = null;

            if (reader.ValueIsEscaped)
            {
                return reader.GetString() ?? string.Empty;
            }

            Span<char> span = reader.ValueSpan.Length < 1001
                ? stackalloc char[reader.ValueSpan.Length]
                : RentArray(reader.ValueSpan.Length, ref isRented, ref array);

            int written = Encoding.UTF8.GetChars(reader.ValueSpan, span);
            span = span.Slice(0, written);

            ref char firstChar = ref span[0];
            if (capitalize.Contains(propertyName) && char.IsLower(firstChar))
            {
                firstChar = char.ToUpper(firstChar);
            }

            object? result;
            if (_config.SpanConverters.TryGetValue(propertyName, out SpanConverter? converter))
            {
                result = converter.ConvertSpan(span, propertyName);
            }
            else if (TryReadAsNumber(span, out ValueType? asValueType))
            {
                result = asValueType;
            }
            else
            {
                result = this.ReadString(span, propertyName);
            }

            if (isRented)
            {
                ArrayPool<char>.Shared.Return(array!);
            }

            return result;
        }
        private static Span<T> RentArray<T>(in int length, ref bool isRented, ref T[]? array)
        {
            array = ArrayPool<T>.Shared.Rent(length);
            isRented = true;
            return array.AsSpan(0, length);
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

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            if (value is PSObject pso)
            {
                this.WritePSObject(writer, options, pso);
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

        internal void WritePSObject(Utf8JsonWriter writer, JsonSerializerOptions options, PSObject pso, IReadOnlyDictionary<string, string>? replaceNames = null)
        {
            replaceNames ??= EmptyNameDictionary<string>.Default;
            var globalReplace = _config.GlobalReplaceNames.SerializationNames;

            writer.WriteStartObject();

            foreach (var prop in pso.Properties
                .Where(x => x.MemberType == PSMemberTypes.NoteProperty
                            &&
                            x.IsGettable))
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

                writer.WritePropertyName(options.ConvertName(pn));

                if (prop.Value is null)
                {
                    writer.WriteNullValue();
                    continue;
                }
                else if (prop.Value is string strVal)
                {
                    writer.WriteStringValue(strVal);
                    continue;
                }

                string serialized = JsonSerializer.Serialize(prop.Value, prop.Value.GetType(), options);
                writer.WriteRawValue(serialized);
            }

            writer.WriteEndObject();

            return;
        }
    }
}
