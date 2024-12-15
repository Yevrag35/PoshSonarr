using System.Buffers;
using System.Text;

namespace MG.Sonarr.Next.Json.Naming;

public sealed class JsonSpanCamelCaseNamingPolicy : JsonNamingPolicy
{
    //private const int MaxStackAlloc = 128;
    private static readonly JsonNamingPolicy s_camelCase = CamelCase;
    public static readonly JsonSpanCamelCaseNamingPolicy SpanPolicy = new();

    public override string ConvertName(string name)
    {
        return s_camelCase.ConvertName(name);
    }
    public Span<char> ConvertSpan(Span<char> span)
    {
        FixCasing(span);
        return span;
    }
    public Span<byte> ConvertSpan(Span<byte> utf8Text)
    {
        FixCasing(utf8Text);
        return utf8Text;
    }

    private static void FixCasing(Span<char> chars)
    {
        for (int i = 0; i < chars.Length; i++)
        {
            ref char current = ref chars[i];

            if (i == 1 && !char.IsUpper(current))
            {
                break;
            }

            bool hasNext = i + 1 < chars.Length;

            // Stop when next char is already lowercase.
            if (i > 0 && hasNext && !char.IsUpper(chars[i + 1]))
            {
                // If the next char is a space, lowercase current char before exiting.
                if (chars[i + 1] == ' ')
                {
                    current = char.ToLowerInvariant(current);
                }

                break;
            }

            current = char.ToLowerInvariant(current);
        }
    }

    private static void FixCasing(Span<byte> utf8Bytes)
    {
        if (utf8Bytes.IsEmpty)
        {
            return;
        }

        // Decode the first rune from the span.
        var status = Rune.DecodeFromUtf8(utf8Bytes, out Rune firstRune, out int bytesConsumed);
        Debug.Assert(status == OperationStatus.Done);
        if (status != OperationStatus.Done)
        {
            throw new JsonException("Invalid UTF-8 sequence.");
        }

        // Encode the lowercase rune back into the span.
        Span<byte> tempSlice = utf8Bytes.Slice(0, bytesConsumed);

        // Convert the first rune to lowercase.
        Rune lowerRune = Rune.ToLowerInvariant(firstRune);

        // Check if a change is needed
        if (firstRune != lowerRune)
        {
            // Re-encode the lowercase rune back into the span.
            // Note: Encoding might not change byte count since TitleCase generally implies simple capital letters.
            int written = lowerRune.EncodeToUtf8(tempSlice);
            if (bytesConsumed != written)
            {
                throw new JsonException("Unexpected change in byte length when converting to lowercase");
            }
        }
    }
}