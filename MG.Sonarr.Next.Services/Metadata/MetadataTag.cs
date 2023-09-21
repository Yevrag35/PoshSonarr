namespace MG.Sonarr.Next.Services.Metadata
{
    public sealed record MetadataTag : ICloneable
    {
        public bool SupportsId { get; }
        public string UrlBase { get; }
        public string Value { get; }

        public MetadataTag(string urlBase, string value, bool supportsId)
        {
            this.UrlBase = urlBase.TrimEnd('/');
            this.Value = value;
            this.SupportsId = supportsId;
        }

        object ICloneable.Clone()
        {
            return new MetadataTag(this);
        }
        public static MetadataTag Copy(MetadataTag tag)
        {
            return new(tag);
        }
        /// <exception cref="InvalidOperationException"/>
        public string GetUrlForId(string? id)
        {
            this.ThrowIfNotSupportId();
            if (string.IsNullOrWhiteSpace(id))
            {
                return this.UrlBase;
            }

            int length = this.UrlBase.Length + 1 + id.Length;

            return string.Create(length, (this.UrlBase, id), (chars, state) =>
            {
                state.UrlBase.CopyTo(chars);
                int position = state.UrlBase.Length;
                chars[position++] = '/';

                state.id.CopyTo(chars.Slice(position));
            });
        }
        /// <exception cref="InvalidOperationException"/>
        public string GetUrlForId<T>(T id) where T : ISpanFormattable
        {
            this.ThrowIfNotSupportId();
            Span<char> chars = stackalloc char[this.UrlBase.Length + 1 + LengthConstants.INT128_MAX];
            this.UrlBase.CopyTo(chars);

            int position = this.UrlBase.Length;
            chars[position++] = '/';

            if (!id.TryFormat(chars.Slice(position), out int written, default, Statics.DefaultProvider))
            {
                Debug.Fail($"Unable to format '{id}' into the BaseUrl.");
                return this.UrlBase + '/' + id.ToString();
            }

            return new string(chars.Slice(0, position + written));
        }
        public bool IsUrlForThis([NotNullWhen(true)] Uri? uri)
        {
            return this.IsUrlForThis(uri?.ToString());
        }
        public bool IsUrlForThis([NotNullWhen(true)] string? url)
        {
            ReadOnlySpan<char> path = url.AsSpan();
            ReadOnlySpan<char> thisUrl = this.UrlBase.AsSpan();
            if (!path.StartsWith(new ReadOnlySpan<char>('/'), StringComparison.InvariantCultureIgnoreCase))
            {
                thisUrl = thisUrl.TrimStart('/');
            }

            return path.StartsWith(thisUrl, StringComparison.InvariantCultureIgnoreCase);
        }
        private void ThrowIfNotSupportId()
        {
            if (!this.SupportsId)
            {
                throw new InvalidOperationException("This metadata tag does not support an ID in its path.");
            }
        }

        public static readonly MetadataTag Empty = new(string.Empty, string.Empty, false);
    }
}
