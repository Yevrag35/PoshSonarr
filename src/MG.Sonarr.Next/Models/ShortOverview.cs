using System.Buffers;

namespace MG.Sonarr.Next.Models
{
    public readonly struct ShortOverview
    {
        const int MAX_SPAN_LENGTH = 1_000;
        readonly bool _isNotEmpty;
        readonly string? _text;

        public bool IsEmpty => !_isNotEmpty;
        public string Text => _text ?? string.Empty;

        private ShortOverview(string text)
        {
            _text = text;
            _isNotEmpty = !string.IsNullOrWhiteSpace(text);
        }

        public static ShortOverview FromValue(ReadOnlySpan<char> text, int howLong)
        {
            string shorted = Truncate(text, howLong);
            return new ShortOverview(shorted);
        }

        private static string Truncate(ReadOnlySpan<char> text, int shortenedLength)
        {
            bool isRented = false;
            char[]? array = null;
            Span<char> threeDots = stackalloc char[3];
            threeDots.Fill('.');

            int useLength = text.Length + threeDots.Length;

            Span<char> span = useLength <= MAX_SPAN_LENGTH
                ? stackalloc char[useLength]
                : RentArray(useLength, ref isRented, ref array);

            text.Slice(0, shortenedLength).CopyTo(span);
            int position = shortenedLength;

            while (!char.IsWhiteSpace(span[position - 1]) && position < text.Length)
            {
                int current = position;
                span[current] = text[current];
                position++;
            }

            if (!char.IsWhiteSpace(span[position - 1]))
            {
                position++;
            }

            threeDots.CopyTo(span.Slice(position - 1));
            position += threeDots.Length - 1;

            string s = new string(span.Slice(0, position).Trim());
            if (isRented)
            {
                ArrayPool<char>.Shared.Return(array!);
            }

            return s;
        }
        private static Span<char> RentArray(int length, ref bool isRented, ref char[]? array)
        {
            array = ArrayPool<char>.Shared.Rent(length);
            isRented = true;
            return array.AsSpan(0, length);
        }
    }
}

