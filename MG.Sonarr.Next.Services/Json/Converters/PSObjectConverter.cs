using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Services.Json.Converters
{
    public sealed class PSObjectConverter : JsonConverter<object>
    {
        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.StartArray => ConvertToListOfObjects(ref reader, options),
                JsonTokenType.StartObject => this.ConvertToObject(ref reader, options),
                JsonTokenType.String => PSObject.AsPSObject(ReadString(ref reader, options)),
                JsonTokenType.Number => PSObject.AsPSObject(ReadNumber(ref reader, options)),
                JsonTokenType.True or JsonTokenType.False => ReadBoolean(ref reader, options),
                JsonTokenType.None or JsonTokenType.Null => null,
                _ => throw new JsonException($"Unable to process object with token type '{reader.TokenType}"),
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
            if (reader.ValueSpan.Length > 400)
            {
                return ReadStringSlow(reader);
            }

            Span<char> span = stackalloc char[reader.ValueSpan.Length];
            int written = Encoding.UTF8.GetChars(reader.ValueSpan, span);

            span = span.Slice(0, written);

            foreach ()

            //span.
            //return ReadString(ref span);
        }

        private static object ReadStringSlow(Utf8JsonReader reader)
        {
            char[] array = ArrayPool<char>.Shared.Rent(reader.ValueSpan.Length);
            Span<char> chars = array;
            int writtenCount = Encoding.UTF8.GetChars(reader.ValueSpan, chars);

            chars = chars.Slice(0, writtenCount);
            


            object read = ReadString(ref chars);
            ArrayPool<char>.Shared.Return(array);

            return read;
        }

        private static object ReadString(ref Span<char> chars)
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
                return new string(chars);
            }
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
            throw new NotSupportedException();
        }
    }
}
