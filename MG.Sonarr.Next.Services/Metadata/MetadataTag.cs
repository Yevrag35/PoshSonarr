namespace MG.Sonarr.Next.Services.Metadata
{
    public sealed record MetadataTag
    {
        readonly string _urlBase = null!;
        readonly string _value = null!;

        public string PSValue { get; private set; } = null!;
        public bool SupportsId { get; init; }
        public required string UrlBase
        {
            get => _urlBase;
            init
            {
                ArgumentException.ThrowIfNullOrEmpty(nameof(this.UrlBase));
                _urlBase = value;
            }
        }
        public required string Value
        {
            get => _value;
            init
            {
                ArgumentException.ThrowIfNullOrEmpty(nameof(this.Value));
                _value = value;
                this.PSValue = GetPSValue(value);
            }
        }

        private static string GetPSValue(string value)
        {
            ReadOnlySpan<char> v = value.AsSpan();
            return !v.StartsWith(new ReadOnlySpan<char>('#'), StringComparison.InvariantCulture)
                ? string.Create(value.Length + 1, value, (chars, state) =>
                {
                    Span<char> lower = stackalloc char[state.Length];
                    _ = state.AsSpan().ToLower(lower, null);

                    chars[0] = '#';
                    lower.CopyTo(chars.Slice(1));
                })
                : value;
        }
    }
}
