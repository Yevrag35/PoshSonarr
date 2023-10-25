using MG.Sonarr.Next.Extensions;
using System.Management.Automation;

namespace MG.Sonarr.Next.Models.PSProperties
{
    public abstract class WritableProperty<T> : PSPropertyInfo
    {
        public sealed override bool IsGettable => true;
        public sealed override bool IsSettable => true;
        public sealed override PSMemberTypes MemberType => PSMemberTypes.NoteProperty;
        protected virtual string PSTypeName => typeof(T).GetPSTypeName();
        protected abstract T? ValueAsT { get; set; }
        public sealed override object? Value
        {
            get => this.ValueAsT;
            set => this.ValueAsT = this.ConvertFromObject(value);
        }

        protected abstract T? ConvertFromObject(object? value);

        public sealed override string ToString()
        {
            ReadOnlySpan<char> typeName = this.PSTypeName.AsSpan().Trim(stackalloc char[] { '[', ']' });
            ReadOnlySpan<char> valueStr = this.ValueAsT?.ToString();
            ReadOnlySpan<char> name = this.Name;

            int length = typeName.Length + valueStr.Length + name.Length + 2; // Magic number 2 comes from: 1 space and an equals sign.
            Span<char> chars = stackalloc char[length]; 

            int position = 0;
            typeName.CopyToSlice(chars, ref position);
            chars[position++] = ' ';

            name.CopyToSlice(chars, ref position);
            chars[position++] = '=';

            valueStr.CopyToSlice(chars, ref position);

            return new string(chars.Slice(0, position));
        }
    }
}
