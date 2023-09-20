using MG.Sonarr.Next.Services.Extensions;
using System.Numerics;

namespace MG.Sonarr.Next.Services.Metadata
{
    public sealed record MetadataTag
    {
        readonly string _urlBase = null!;
        readonly string _value = null!;

        public bool SupportsId { get; init; }
        public required string UrlBase
        {
            get => _urlBase;
            init
            {
                ArgumentException.ThrowIfNullOrEmpty(nameof(this.UrlBase));
                _urlBase = value.TrimEnd('/');
            }
        }
        public required string Value
        {
            get => _value;
            init
            {
                ArgumentException.ThrowIfNullOrEmpty(nameof(this.Value));
                _value = value;
            }
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
        public string GetUrlForId<T>(T id) where T : INumber<T>
        {
            this.ThrowIfNotSupportId();
            int length = this.UrlBase.Length + 1 + id.GetLength();
            return string.Create(
                length: length,
                state: (this.UrlBase, id),
                action: (chars, state) =>
                {
                    state.UrlBase.CopyTo(chars);
                    int position = state.UrlBase.Length;
                    chars[position++] = '/';

                    _ = state.id.TryFormat(
                        chars.Slice(position), out int written, default, Statics.DefaultProvider);
                });
        }
        private void ThrowIfNotSupportId()
        {
            if (!this.SupportsId)
            {
                throw new InvalidOperationException("This metadata tag does not support an ID in its path.");
            }
        }
    }
}
