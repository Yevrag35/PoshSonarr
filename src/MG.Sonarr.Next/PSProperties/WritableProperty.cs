using MG.Sonarr.Next.Extensions;
using System.Management.Automation;

namespace MG.Sonarr.Next.PSProperties
{
    public abstract class WritableProperty : PSPropertyInfo
    {
        public sealed override bool IsGettable => true;
        public sealed override bool IsSettable => true;
        public sealed override PSMemberTypes MemberType => PSMemberTypes.NoteProperty;
        protected abstract string PSTypeName { get; }

        protected virtual string? GetValueAsString()
        {
            return this.Value?.ToString();
        }

        /// <exception cref="SetValueException"></exception>
        [DoesNotReturn]
        protected T ThrowNotType<T>()
        {
            throw new SetValueException(
                $"The property '{this.Name}' only can be set with values of type '{this.TypeNameOfValue}'.");
        }
        internal static PSPropertyInfo ToProperty(string name, object? value)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            return value switch
            {
                string strVal => new StringNoteProperty(name, strVal),
                int intVal => new NumberNoteProperty<int>(name, intVal),
                bool boolVal => new StructNoteProperty<bool>(name, boolVal),
                double dubVal => new NumberNoteProperty<double>(name, dubVal),
                long longVal => new NumberNoteProperty<long>(name, longVal),
                decimal decVal => new NumberNoteProperty<decimal>(name, decVal),
                Guid guid => new StructNoteProperty<Guid>(name, guid),
                DateOnly @do => new StructNoteProperty<DateOnly>(name, @do),
                TimeOnly to => new StructNoteProperty<TimeOnly>(name, to),
                DateTimeOffset offset => new StructNoteProperty<DateTimeOffset>(name, offset),
                TimeSpan ts => new StructNoteProperty<TimeSpan>(name, ts),
                PSObject pso => new ReadOnlyPSObjectProperty(name, pso),
                SortedSet<int> set => new ReadOnlyTagsProperty(set),
                _ => new PSNoteProperty(name, value),
            };
        }

        public override string ToString()
        {
            ReadOnlySpan<char> typeName = this.PSTypeName.AsSpan().Trim(stackalloc char[] { '[', ']' });
            ReadOnlySpan<char> valueStr = this.GetValueAsString();
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

    public abstract class WritableProperty<T> : WritableProperty
    {
        protected virtual int MaxValueCharacterLength { get; }
        protected override string PSTypeName => typeof(T).GetPSTypeName();
        public sealed override object? Value
        {
            get => this.ValueAsT;
            set => this.ValueAsT = this.ConvertFromObject(value);
        }
        protected abstract T? ValueAsT { get; set; }

        protected abstract T? ConvertFromObject(object? value);
        protected override string? GetValueAsString()
        {
            return this.ValueAsT?.ToString();
        }
        public sealed override string ToString()
        {
            ReadOnlySpan<char> typeName = this.PSTypeName.AsSpan().Trim(stackalloc char[] { '[', ']' });
            ReadOnlySpan<char> name = this.Name;
            return this.MaxValueCharacterLength <= 0 || this.ValueAsT is not ISpanFormattable spanFormattable
                ? ToStringNoSpan(typeName, this.ValueAsT, name)
                : ToStringWithSpan(typeName, name, spanFormattable, this.MaxValueCharacterLength);
        }

        private static string ToStringWithSpan(ReadOnlySpan<char> typeName, ReadOnlySpan<char> name, ISpanFormattable formattable, int maxLength)
        {
            int length = 2 + typeName.Length + name.Length + maxLength;

            Span<char> chars = stackalloc char[length];
            int position = 0;

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
        private static string ToStringNoSpan(ReadOnlySpan<char> typeName, T? valueAsT, ReadOnlySpan<char> name)
        {
            ReadOnlySpan<char> valueStr = valueAsT?.ToString();
            valueStr = valueStr.Trim();

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
