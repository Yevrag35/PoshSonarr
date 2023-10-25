using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Services.Http.Queries;

namespace MG.Sonarr.Next.Metadata
{
    [DebuggerDisplay(@"\{{Value}, {UrlBase}\}")]
    public sealed class MetadataTag : ICloneable, IEquatable<MetadataTag>
    {
        public IReadOnlySet<string> CanPipeTo { get; }
        public bool SupportsId { get; }
        public string UrlBase { get; }
        public string Value { get; }

        private MetadataTag()
        {
            this.CanPipeTo = EmptyNameDictionary<string>.Default;
            this.SupportsId = false;
            this.UrlBase = string.Empty;
            this.Value = string.Empty;
        }
        private MetadataTag(MetadataTag copyFrom)
        {
            ArgumentNullException.ThrowIfNull(copyFrom);
            this.CanPipeTo = CopyPipeSet(copyFrom);
            this.SupportsId = copyFrom.SupportsId;
            this.UrlBase = copyFrom.UrlBase;
            this.Value = copyFrom.Value;
        }
        internal MetadataTag(string urlBase, string value, bool supportsId, IReadOnlySet<string> pipesTo)
        {
            this.UrlBase = urlBase.TrimEnd('/');
            this.Value = value;
            this.SupportsId = supportsId;
            this.CanPipeTo = pipesTo;
        }

        private static IReadOnlySet<string> CopyPipeSet(MetadataTag copyFrom)
        {
            return copyFrom.CanPipeTo.Count > 0
                ? new SortedSet<string>(copyFrom.CanPipeTo, StringComparer.InvariantCultureIgnoreCase)
                : EmptyNameDictionary<string>.Default;
        }

        object ICloneable.Clone() => this.Clone();
        public MetadataTag Clone() => new(this);

        public static readonly MetadataTag Empty = new();

        public bool Equals(MetadataTag? other)
        {
            return ReferenceEquals(this, other)
                   ||
                   (this.UrlBase == other?.UrlBase
                    &&
                    this.Value == other?.Value
                    &&
                    this.SupportsId == other?.SupportsId);
        }
        public override bool Equals(object? obj)
        {
            return obj is MetadataTag tag && this.Equals(tag);
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(this.UrlBase, this.Value, this.SupportsId);
        }
        public string GetUrl(QueryParameterCollection? parameters)
        {
            if (parameters.IsNullOrEmpty())
            {
                return this.UrlBase;
            }

            Span<char> span = stackalloc char[this.UrlBase.Length + 1 + parameters.MaxLength];
            int position = 0;
            this.UrlBase.CopyToSlice(span, ref position);
            span[position++] = '?';
            _ = parameters.TryFormat(span.Slice(position), out int written, default, Statics.DefaultProvider);
            position += written;

            return new string(span.Slice(0, position));
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
            Span<char> span = stackalloc char[this.UrlBase.Length + 1 + LengthConstants.INT128_MAX];
            int position = 0;
            this.UrlBase.CopyToSlice(span, ref position);

            span[position++] = '/';

            if (!id.TryFormat(span.Slice(position), out int written, default, Statics.DefaultProvider))
            {
                Debug.Fail($"Unable to format '{id}' into the BaseUrl.");
                return this.UrlBase + '/' + id;
            }

            return new string(span.Slice(0, position + written));
        }
        public string GetUrlForId<T>(T id, QueryParameterCollection parameters) where T : ISpanFormattable
        {
            this.ThrowIfNotSupportId();
            Span<char> span = stackalloc char[this.UrlBase.Length + 2 + parameters.MaxLength + LengthConstants.INT128_MAX];
            int position = 0;
            this.UrlBase.CopyToSlice(span, ref position);

            span[position++] = '/';

            _ = id.TryFormat(span.Slice(position), out int written, default, Statics.DefaultProvider);
            position += written;

            if (parameters.Count > 0)
            {
                span[position++] = '?';
                parameters.TryFormat(span.Slice(position), out written, default, Statics.DefaultProvider);
                position += written;
            }

            return new string(span.Slice(0, position));
        }
        private void ThrowIfNotSupportId()
        {
            if (!this.SupportsId)
            {
                throw new InvalidOperationException("This metadata tag does not support an ID in its path.");
            }
        }

        public override string ToString()
        {
            int length = this.UrlBase.Length + this.SupportsId.GetLength() + this.Value.Length + 24;
            length += nameof(this.UrlBase).Length +
                nameof(this.SupportsId).Length +
                nameof(this.Value).Length +
                nameof(this.CanPipeTo).Length;

            if (this.CanPipeTo.Count > 0)
            {
                length += this.CanPipeTo.Sum(x => x.Length) + (2 * (this.CanPipeTo.Count - 1));
            }

            return string.Create(length, this, (chars, state) =>
            {
                int position = 0;
                Span<char> sep = stackalloc char[3] { ' ', '=', ' ' };
                ref char space = ref sep[0];
                ReadOnlySpan<char> comma = stackalloc char[2] { ',', space };

                chars[position++] = '{';
                chars[position++] = space;

                nameof(state.UrlBase).CopyToSlice(chars, ref position);
                sep.CopyToSlice(chars, ref position);
                state.UrlBase.CopyToSlice(chars, ref position);
                comma.CopyToSlice(chars, ref position);

                nameof(state.Value).CopyToSlice(chars, ref position);
                sep.CopyToSlice(chars, ref position);
                state.Value.CopyToSlice(chars, ref position);
                comma.CopyToSlice(chars, ref position);

                nameof(state.SupportsId).CopyToSlice(chars, ref position);
                sep.CopyToSlice(chars, ref position);
                state.SupportsId.TryFormat(chars.Slice(position), out int written);
                position += written;
                comma.CopyToSlice(chars, ref position);

                nameof(state.CanPipeTo).CopyToSlice(chars, ref position);
                sep.CopyToSlice(chars, ref position);
                chars[position++] = '{';

                int count = 0;
                foreach (string s in state.CanPipeTo)
                {
                    s.CopyToSlice(chars, ref position);
                    if (count < state.CanPipeTo.Count - 1)
                    {
                        comma.CopyToSlice(chars, ref position);
                    }

                    count++;
                }

                chars[position++] = '}';
                chars[position++] = space;
                chars[position++] = '}';
            });
        }

        public static bool operator ==(MetadataTag? x, MetadataTag? y)
        {
            return x.IsEqualTo<MetadataTag>(y);
        }
        public static bool operator !=(MetadataTag? x, MetadataTag? y)
        {
            return !(x == y);
        }
    }
}
