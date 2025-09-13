using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Extensions.Strings;
using MG.Sonarr.Next.Services.Http.Queries;
using System.Collections.Immutable;

namespace MG.Sonarr.Next.Metadata
{
    /// <summary>
    /// A class that represents an identifier for organizing the output data returned from Sonarr API 
    /// endpoints.
    /// </summary>
    /// <remarks>
    /// Instances of this class are immutable and should be retrieved from an 
    /// <see cref="IMetadataResolver"/> implementing service.
    /// </remarks>
    [DebuggerDisplay(@"\{{Value}, {UrlBase}\}")]
    public sealed class MetadataTag : ICloneable, IComparable<MetadataTag>, IEquatable<MetadataTag>
    {
        /// <summary>
        /// Gets the array of cmdlet names that data tagged with this instance can be piped to in PowerShell.
        /// </summary>
        public ImmutableArray<string> CanPipeTo { get; }
        /// <summary>
        /// Indicates whether the API endpoint this tag represents supports an ID in the URL path.
        /// </summary>
        public bool SupportsId { get; }
        /// <summary>
        /// Gets the URI path of the Sonarr API endpoint that this tag represents.
        /// </summary>
        public string UrlBase { get; }
        /// <summary>
        /// Gets the identifier of the tag that is easily visible in the PowerShell console.
        /// </summary>
        public string Value { get; }

        private MetadataTag()
            : this(string.Empty, string.Empty, supportsId: false, [])
        {
        }
        private MetadataTag(MetadataTag copyFrom)
            : this(copyFrom.UrlBase, copyFrom.Value, copyFrom.SupportsId, copyFrom.CanPipeTo)
        {
        }
        internal MetadataTag(string urlBase, string value, bool supportsId, ImmutableArray<string> pipesTo)
        {
            this.UrlBase = urlBase.TrimEnd('/');
            this.Value = value;
            this.SupportsId = supportsId;
            this.CanPipeTo = pipesTo;
        }

        [DebuggerStepThrough]
        object ICloneable.Clone() => this.Clone();
        public MetadataTag Clone() => new(this);

        /// <summary>
        /// An empty <see cref="MetadataTag"/> instance that has no value and cannot used in the
        /// PowerShell pipeline. It may also represent data from an unknown Sonarr API endpoint.
        /// </summary>
        public static readonly MetadataTag Empty = new();

        /// <summary>
        /// Compares this instance to another <see cref="MetadataTag"/> instance based on the
        /// <see cref="Value"/> property.
        /// </summary>
        /// <param name="other">The <see cref="MetadataTag"/> instance to compare to.</param>
        /// <returns>A value indicating the relative order of the two instances.</returns>
        public int CompareTo(MetadataTag? other)
        {
            if (this.IsNullOrReferenceEquals(other, out bool isEqual))
            {
                return isEqual ? 0 : -1;
            }

            return this.Value.CompareTo(other.Value);
        }

        public bool Equals(MetadataTag? other)
        {
            if (!this.IsNullOrReferenceEquals(other, out bool isEqual))
            {
                isEqual = this.SupportsId == other.SupportsId
                          &&
                          this.UrlBase.Equals(other.UrlBase, StringComparison.OrdinalIgnoreCase)
                          &&
                          this.Value.Equals(other.Value, StringComparison.OrdinalIgnoreCase);
            }

            return isEqual;
        }
        public override bool Equals(object? obj)
        {
            return obj is MetadataTag tag && this.Equals(tag);
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(this.UrlBase, this.Value, this.SupportsId);
        }

        /// <summary>
        /// Constructs a new URL for an API request using <see cref="UrlBase"/> and appending the
        /// provided query parameters.
        /// </summary>
        /// <param name="parameters">The collection of query parameters to append as the URL
        /// query string.</param>
        /// <returns>
        /// The constructed URL string to the endpoint defined by this tag with the appended query 
        /// parameters.
        /// </returns>
        public string GetUrl(QueryCol? parameters)
        {
            return parameters.GetUrl(this.UrlBase);
        }
        public string GetUrl(IQueryField parameter)
        {
            Span<char> chars = stackalloc char[parameter.MaxLength + 1];
            chars[0] = '?';
            _ = parameter.TryFormat(chars[1..], out int written, default, null);

            return string.Concat(this.UrlBase, chars.Slice(0, written + 1));
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
        public string GetUrlForId(ReadOnlySpan<char> id)
        {
            this.ThrowIfNotSupportId();
            if (id.IsWhiteSpace())
            {
                return this.UrlBase;
            }

            Span<char> chars = stackalloc char[this.UrlBase.Length + 1 + id.Length];

            int position = 0;
            this.UrlBase.CopyToSlice(chars, ref position);
            chars[position++] = '/';

            id.CopyTo(chars.Slice(position));

            return new string(chars);
        }
        
        /// <exception cref="InvalidOperationException"/>
        public string GetUrlForId<T>(T id) where T : ISpanFormattable
        {
            this.ThrowIfNotSupportId();
            Span<char> span = stackalloc char[this.UrlBase.Length + 1 + LengthConstants.INT128_MAX];
            int position = 0;
            this.UrlBase.CopyToSlice(span, ref position);

            span[position++] = '/';

            if (!id.TryCopyToSlice(span, ref position, provider: Statics.DefaultProvider))
            {
                Debug.Fail($"Unable to format '{id}' into the BaseUrl.");
                position = this.UrlBase.Length + 1;

                id.ToString(null, formatProvider: Statics.DefaultProvider)
                  .AsSpan()
                  .CopyToSlice(span, ref position);
            }

            return new string(span.Slice(0, position));
        }
        public string GetUrlForId<T>(T id, QueryCol parameters) where T : ISpanFormattable
        {
            this.ThrowIfNotSupportId();
            Span<char> span = stackalloc char[this.UrlBase.Length + 2 + parameters.MaxLength + LengthConstants.INT128_MAX];

            int position = 0;
            this.UrlBase.CopyToSlice(span, ref position);

            span[position++] = '/';

            id.CopyToSlice(span, ref position, provider: Statics.DefaultProvider);

            if (parameters.Count > 0)
            {
                span[position++] = '?';
                parameters.CopyToSlice(span, ref position, provider: Statics.DefaultProvider);
            }

            return new string(span.Slice(0, position));
        }

        private bool IsNullOrReferenceEquals([NotNullWhen(false)] MetadataTag? other, out bool isEqual)
        {
            if (other is null)
            {
                isEqual = false;
                return true;
            }
            else if (ReferenceEquals(this, other))
            {
                isEqual = true;
                return true;
            }

            isEqual = false;
            return false;
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
            int length = this.Value.Length + 22 + nameof(this.Value).Length + nameof(this.CanPipeTo).Length;

            if (this.CanPipeTo.Length > 0)
            {
                length += this.CanPipeTo.Sum(x => x.Length) + (2 * (this.CanPipeTo.Length - 1));
            }

            return string.Create(length, this, (chars, state) =>
            {
                int position = 0;
                Span<char> sep = [' ', '=', ' '];
                ref char space = ref sep[0];

                Span<char> comma = [',', space];

                chars[position++] = '{';

                nameof(state.Value).CopyToSlice(chars, ref position);
                sep.CopyToSlice(chars, ref position);
                state.Value.CopyToSlice(chars, ref position);
                comma.CopyToSlice(chars, ref position);

                nameof(state.CanPipeTo).CopyToSlice(chars, ref position);
                sep.CopyToSlice(chars, ref position);
                chars[position++] = '{';

                int count = 0;
                foreach (string s in state.CanPipeTo)
                {
                    s.CopyToSlice(chars, ref position);
                    if (count < state.CanPipeTo.Length - 1)
                    {
                        comma.CopyToSlice(chars, ref position);
                    }

                    count++;
                }

                chars[position++] = '}';
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
