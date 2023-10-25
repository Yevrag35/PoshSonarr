using MG.Sonarr.Next.Extensions;
using System.Management.Automation;

namespace MG.Sonarr.Next.Models.PSProperties
{
    public sealed class StringNoteProperty : PSPropertyInfo
    {
        const string STRING_TYPE = "System.String";
        const string STRING_PS_TYPE = "string ";
        string _value;

        public override bool IsGettable => true;
        public override bool IsSettable => true;
        public override PSMemberTypes MemberType => PSMemberTypes.NoteProperty;

        public string StringValue
        {
            get => _value;
            set => _value = value ?? string.Empty;
        }
        public override string TypeNameOfValue => STRING_TYPE;

        [NotNull]
        public override object Value
        {
            get => _value;
            set => _value = this.SetValueAsString(value);
        }

        public StringNoteProperty(string name, string? value)
        {
            base.SetMemberName(name);
            _value = value ?? string.Empty;
        }

        /// <exception cref="SetValueException"></exception>
        public override PSMemberInfo Copy()
        {
            return new StringNoteProperty(this.Name, _value);
        }

        /// <exception cref="SetValueException"></exception>
        private string SetValueAsString(object? value)
        {
            return value switch
            {
                null => string.Empty,
                string strVal => strVal,
                IFormattable formattable => formattable.ToString(null, Statics.DefaultProvider),
                IConvertible icon => Convert.ToString(icon) ?? string.Empty,

                _ => throw new SetValueException($"The value for property '{this.Name}' must be of the type 'System.String'."),
            };
        }

        internal static PSPropertyInfo ToProperty(string name, object? value)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            return value switch
            {
                string strVal => new StringNoteProperty(name, strVal),
                int intVal => new NumberNoteProperty<int>(name, intVal),
                double dubVal => new NumberNoteProperty<double>(name, dubVal),
                long longVal => new NumberNoteProperty<long>(name, longVal),
                decimal decVal => new NumberNoteProperty<decimal>(name, decVal),
                _ => new PSNoteProperty(name, value),
            };
        }
        public override string ToString()
        {
            int length = STRING_PS_TYPE.Length + this.Name.Length + this.StringValue.Length + 1;

            return string.Create(length, this, (chars, state) =>
            {
                int position = 0;

                STRING_PS_TYPE.CopyToSlice(chars, ref position);
                state.Name.CopyToSlice(chars, ref position);

                chars[position++] = '=';
                state.StringValue.CopyTo(chars.Slice(position));
            });
        }
    }
}
