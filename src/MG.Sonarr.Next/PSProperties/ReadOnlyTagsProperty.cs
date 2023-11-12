using MG.Sonarr.Next.Extensions;
using System.Management.Automation;

namespace MG.Sonarr.Next.PSProperties
{
    public sealed class ReadOnlyTagsProperty : ReadOnlyProperty<SortedSet<int>>
    {
        const string AS_STRING = "read-only SortedSet[int] Tags=";
        const string AS_EMPTY = AS_STRING + "{}";
        const int MAX_STRING_LENGTH = 1000;

        public SortedSet<int> Tags { get; internal set; }
        protected override SortedSet<int> ValueAsT => this.Tags;

        public ReadOnlyTagsProperty(SortedSet<int> set)
            : base(Constants.TAGS)
        {
            this.Tags = set;
        }

        public override PSMemberInfo Copy()
        {
            return this;
            //return new ReadOnlyTagsProperty(new SortedSet<int>(this.Tags));
        }

        public override string ToString()
        {
            if (this.Tags.Count <= 0)
            {
                return AS_EMPTY;
            }

            int length = AS_STRING.Length + 2
                +
                (this.Tags.Count * LengthConstants.INT_MAX)
                +
                (this.Tags.Count - 1);

            if (length > MAX_STRING_LENGTH)
            {
                return string.Concat(AS_STRING, '{', string.Join(',', this.Tags), '}');
            }

            Span<char> span = stackalloc char[length];
            int position = 0;
            AS_STRING.AsSpan().CopyToSlice(span, ref position);

            span[position++] = '{';

            int i = 0;
            foreach (int id in this.Tags)
            {
                _ = id.TryFormat(span.Slice(position), out int written);
                position += written;

                if (i < this.Tags.Count - 1)
                {
                    span[position++] = ',';
                }

                i++;
            }

            span[position++] = '}';

            return new string(span.Slice(0, position));
        }
    }
}

