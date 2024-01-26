using MG.Http.Urls.Queries;
using MG.Sonarr.Next.Extensions;
using System.Management.Automation;

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
    public sealed class MetadataTag : ICloneable, IEquatable<MetadataTag>
    {
        /// <summary>
        /// Gets the array of cmdlet names that data tagged with this instance can be piped to in PowerShell.
        /// </summary>
        public string[] CanPipeTo { get; }
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
        {
            this.CanPipeTo = [];
            this.SupportsId = false;
            this.UrlBase = string.Empty;
            this.Value = string.Empty;
        }
        private MetadataTag(MetadataTag copyFrom)
        {
            ArgumentNullException.ThrowIfNull(copyFrom);
            this.CanPipeTo = CopyOriginal(copyFrom.CanPipeTo);
            this.SupportsId = copyFrom.SupportsId;
            this.UrlBase = copyFrom.UrlBase;
            this.Value = copyFrom.Value;
        }
        internal MetadataTag(string urlBase, string value, bool supportsId, IReadOnlySet<string> pipesTo)
        {
            this.UrlBase = urlBase.TrimEnd('/');
            this.Value = value;
            this.SupportsId = supportsId;
            this.CanPipeTo = CopyFromSet(pipesTo);
        }

        private static string[] CopyFromSet(IReadOnlySet<string> pipesTo)
        {
            if (pipesTo.Count <= 0)
            {
                return [];
            }

            string[] canPipeTo = new string[pipesTo.Count];
            int i = 0;
            foreach (string s in pipesTo)
            {
                canPipeTo[i++] = s;
            }

            return canPipeTo;
        }

        private static string[] CopyOriginal(scoped Span<string> canPipeTo)
        {
            if (canPipeTo.Length <= 0)
            {
                return [];
            }

            string[] copyInto = new string[canPipeTo.Length];
            canPipeTo.CopyTo(copyInto);
            return copyInto;
        }

        object ICloneable.Clone() => this.Clone();
        public MetadataTag Clone() => new(this);

        /// <summary>
        /// An empty <see cref="MetadataTag"/> instance that has no value and cannot used in the
        /// PowerShell pipeline. It may also represent data from an unknown Sonarr API endpoint.
        /// </summary>
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
