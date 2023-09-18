using MG.Sonarr.Next.Services.Extensions;
using Microsoft.PowerShell.Commands;
using NJsonSchema;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Services.Json.Converters
{
    public sealed class PSObjectConverter : JsonConverter<object>
    {
        static readonly Type _objType = typeof(object);
        static readonly Type _psObjType = typeof(PSObject);
        static readonly Type _psCusType = typeof(PSCustomObject);
        readonly HashSet<string> _ignore;

        public PSObjectConverter(string[] ignoreProperties)
        {
            _ignore = new HashSet<string>(ignoreProperties, StringComparer.InvariantCultureIgnoreCase);
        }
        public PSObjectConverter(ReadOnlySpan<string> span)
        {
            _ignore = new HashSet<string>(span.Length, StringComparer.InvariantCultureIgnoreCase);
            foreach (string s in span)
            {
                _ = _ignore.Add(s);
            }
        }

        public override bool CanConvert(Type typeToConvert)
        {
            return _objType.Equals(typeToConvert) || _psObjType.Equals(typeToConvert) || _psCusType.Equals(typeToConvert);
        }

        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.StartArray => ConvertToListOfObjects(ref reader, options),
                JsonTokenType.StartObject => this.ConvertToObject(ref reader, options),
                JsonTokenType.String => ReadString(ref reader, options),
                JsonTokenType.Number => ReadNumber(ref reader, options),
                JsonTokenType.True or JsonTokenType.False => ReadBoolean(ref reader, options),
                JsonTokenType.None or JsonTokenType.Null => null,
                _ => throw new JsonException($"Unable to process object with token type '{reader.TokenType}'."),
            };
        }

        private static object[] ConvertToListOfObjects(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            return JsonSerializer.Deserialize<object[]>(ref reader, options) ?? throw new JsonException("Unable to deserialize into an array of PSObject instances.");
        }

        private object ConvertToObject(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            var pso = new PSObject(2);
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string pn = ReadPropertyName(ref reader, options);
                    reader.Read();

                    object? o = null;
                    if (!reader.ValueSpan.IsEmpty)
                    {
                        switch (reader.TokenType)
                        {
                            case JsonTokenType.StartObject:
                                o = this.ConvertToObject(ref reader, options);
                                break;

                            case JsonTokenType.StartArray:
                                o = ConvertToListOfObjects(ref reader, options);
                                break;

                            case JsonTokenType.String:
                                o = ReadString(ref reader, options);
                                break;

                            case JsonTokenType.Number:
                                o = ReadNumber(ref reader, options);
                                break;

                            case JsonTokenType.True:
                            case JsonTokenType.False:
                                o = ReadBoolean(ref reader, options);
                                break;

                            case JsonTokenType.Null:
                            case JsonTokenType.Comment:
                            case JsonTokenType.None:
                                o = null;
                                break;

                            case JsonTokenType.EndObject:
                            case JsonTokenType.EndArray:
                            case JsonTokenType.PropertyName:
                                goto default;

                            default:
                                throw new JsonException("Unable to deserialize the value(s).");
                        }
                    }

                    pso.Properties.Add(new PSNoteProperty(pn, o));
                }
            }

            return pso;
        }

        private static bool ReadBoolean(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            Span<char> chars = stackalloc char[5];
            int written = Encoding.UTF8.GetChars(reader.ValueSpan, chars);

            return bool.TryParse(chars.Slice(0, written), out bool result) && result;
        }
        private static object ReadString(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            bool isRented = false;
            char[]? array = null;

            Span<char> span = reader.ValueSpan.Length < 1001
                ? stackalloc char[reader.ValueSpan.Length]
                : RentArray(reader.ValueSpan.Length, ref isRented, ref array);

            int written = Encoding.UTF8.GetChars(reader.ValueSpan, span);

            object result = ReadString(span.Slice(0, written));
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

        private static object ReadString(Span<char> chars)
        {
            if (Guid.TryParse(chars, null, out Guid guidStr))
            {
                return guidStr;
            }
            else if (DateTimeOffset.TryParse(chars, null, out DateTimeOffset offset))
            {
                return offset;
            }
            else if (DateTime.TryParse(chars, null, out DateTime dt))
            {
                return dt;
            }
            else if (Version.TryParse(chars, out Version? version))
            {
                return version;
            }
            else
            {
                return ProcessQuotes(chars);
            }
        }

        private static string ProcessQuotes(ReadOnlySpan<char> chars)
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

        private static ValueType ReadNumber(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            Span<char> chars = stackalloc char[reader.ValueSpan.Length];
            int written = Encoding.UTF8.GetChars(reader.ValueSpan, chars);

            chars = chars.Slice(0, written);
            if (int.TryParse(chars, null, out int intNum))
            {
                return intNum;
            }
            else if (long.TryParse(chars, null, out long longNum))
            {
                return longNum;
            }
            else if (double.TryParse(chars, null, out double dubNum))
            {
                return dubNum;
            }
            else if (decimal.TryParse(chars, null, out decimal decNum))
            {
                return decNum;
            }
            else
            {
                return int.MinValue;
            }
        }

        private static string ReadPropertyName(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            Span<char> chars = stackalloc char[reader.ValueSpan.Length];
            int written = Encoding.UTF8.GetChars(reader.ValueSpan, chars);

            chars = chars.Slice(0, written);

            if (char.IsLower(chars[0]))
            {
                chars[0] = char.ToUpper(chars[0]);
            }

            return new string(chars);
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

        private void WritePSObject(Utf8JsonWriter writer, JsonSerializerOptions options, PSObject pso)
        {
            writer.WriteStartObject();

            foreach (var prop in pso.Properties
                .Where(x => x.MemberType == PSMemberTypes.NoteProperty
                            &&
                            x.IsGettable))
            {
                if (_ignore.Contains(prop.Name))
                {
                    continue;
                }

                writer.WritePropertyName(options.ConvertName(prop.Name));

                if (prop.Value is null)
                {
                    writer.WriteNullValue();
                    continue;
                }

                writer.WriteRawValue(JsonSerializer.Serialize(prop.Value, prop.Value.GetType(), options));
            }

            writer.WriteEndObject();

            return;
        }
    }
}
