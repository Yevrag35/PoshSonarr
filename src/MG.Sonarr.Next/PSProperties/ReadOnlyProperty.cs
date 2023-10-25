using MG.Sonarr.Next.Extensions;
using System.Management.Automation;

namespace MG.Sonarr.Next.PSProperties
{
    public abstract class ReadOnlyProperty : PSPropertyInfo
    {
        public sealed override bool IsGettable => true;
        public sealed override bool IsSettable => false;
    }

    public abstract class ReadOnlyProperty<T> : ReadOnlyProperty
    {
        protected virtual int MaxValueCharacterLength { get; }
        public sealed override PSMemberTypes MemberType => PSMemberTypes.NoteProperty;
        protected virtual string PSTypeName => typeof(T).GetPSTypeName();
        public override string TypeNameOfValue => typeof(T).GetTypeName();

        /// <summary>
        ///     Gets the value of the <see cref="ReadOnlyProperty{T}"/>.
        /// </summary>
        /// <remarks>
        ///     Even though this property has a setter, a <see cref="SetValueException"/> will be thrown if 
        ///     trying to overwrite the initial value set from the constructor.
        /// </remarks>
        /// <exception cref="SetValueException"/>
        public sealed override object? Value
        {
            get => this.ValueAsT;
            set
            {
                ReadOnlyPropertyException ex = new(this.Name);
                throw new SetValueException(ex.Message, ex);
            }
        }

        protected abstract T ValueAsT { get; }

        protected ReadOnlyProperty(string propertyName)
        {
            ArgumentException.ThrowIfNullOrEmpty(propertyName);
            this.SetMemberName(propertyName);
        }

        protected virtual bool TryWriteValueToSpan(Span<char> span, out int written)
        {
            ReadOnlySpan<char> valueAsString = this.ValueAsT?.ToString();
            valueAsString = valueAsString.Trim();

            written = valueAsString.Length;
            return valueAsString.TryCopyTo(span);
        }

        /// <exception cref="SetValueException"></exception>
        [DoesNotReturn]
        protected T ThrowNotType()
        {
            throw new SetValueException(
                $"The property '{this.Name}' only can be set with values of type '{this.TypeNameOfValue}'.");
        }

        const string READ_ONLY = "read-only ";
        public sealed override string ToString()
        {
            ReadOnlySpan<char> typeName = this.PSTypeName.AsSpan().Trim(stackalloc char[] { '[', ']' });
            ReadOnlySpan<char> name = this.Name;
            if (this.MaxValueCharacterLength <= 0 || this.ValueAsT is not ISpanFormattable spanFormattable)
            {
                return ToStringNoSpan(typeName, this.ValueAsT, name);
            }
            else
            {
                return ToStringWithSpan(typeName, name, spanFormattable, this.MaxValueCharacterLength);
            }
        }

        private static string ToStringNoSpan(ReadOnlySpan<char> typeName, T? valueAsT, ReadOnlySpan<char> name)
        {
            ReadOnlySpan<char> valueStr = valueAsT?.ToString();
            valueStr = valueStr.Trim();

            int length = READ_ONLY.Length + typeName.Length + valueStr.Length + name.Length + 2; // Magic number 2 comes from: 1 space and an equals sign.
            Span<char> chars = stackalloc char[length];

            int position = 0;
            READ_ONLY.CopyToSlice(chars, ref position);
            typeName.CopyToSlice(chars, ref position);
            chars[position++] = ' ';

            name.CopyToSlice(chars, ref position);
            chars[position++] = '=';

            valueStr.CopyToSlice(chars, ref position);

            return new string(chars.Slice(0, position));
        }
        private static string ToStringWithSpan(ReadOnlySpan<char> typeName, ReadOnlySpan<char> name, ISpanFormattable formattable, int maxLength)
        {
            int length = 2 + READ_ONLY.Length + typeName.Length + name.Length + maxLength;

            Span<char> chars = stackalloc char[length];
            int position = 0;

            READ_ONLY.CopyToSlice(chars, ref position);
            typeName.CopyToSlice(chars, ref position);

            chars[position++] = ' ';
            name.CopyToSlice(chars, ref position);

            chars[position++] = '=';
            if (!formattable.TryFormat(chars.Slice(position), out int written, default, Statics.DefaultProvider))
            {
                chars.Slice(position, written).Fill(' ');
            }

            return new string(chars.Slice(0, position + written).TrimEnd());
        }
    }
}
