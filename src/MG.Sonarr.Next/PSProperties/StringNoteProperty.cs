using MG.Sonarr.Next.Extensions;
using System.Management.Automation;

namespace MG.Sonarr.Next.PSProperties
{
    public sealed class StringNoteProperty : WritableProperty<string>
    {
        const string STRING_TYPE = "System.String";
        string _value;

        public string StringValue
        {
            get => _value;
            set => _value = value ?? string.Empty;
        }
        public override string TypeNameOfValue => STRING_TYPE;
        protected override string? ValueAsT
        {
            get => this.StringValue;
            set => this.StringValue = value!;
        }

        public StringNoteProperty(string propertyName)
        {
            this.SetMemberName(propertyName);
            _value = string.Empty;
        }
        public StringNoteProperty(string name, string? value)
        {
            this.SetMemberName(name);
            _value = value ?? string.Empty;
        }

        /// <exception cref="SetValueException"></exception>
        public override PSMemberInfo Copy()
        {
            return new StringNoteProperty(this.Name, _value);
        }

        /// <exception cref="SetValueException"></exception>
        protected override string ConvertFromObject(object? value)
        {
            return value switch
            {
                null => string.Empty,
                string strVal => strVal,
                IFormattable formattable => formattable.ToString(null, Statics.DefaultProvider),
                IConvertible icon => Convert.ToString(icon) ?? string.Empty,

                _ => this.ThrowNotType<string>(),
            };
        }
    }
}
