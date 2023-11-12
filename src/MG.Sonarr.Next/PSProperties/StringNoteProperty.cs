using System.Management.Automation;

namespace MG.Sonarr.Next.PSProperties
{
    public sealed class StringNoteProperty : WritableProperty<string>
    {
        internal const string STRING_TYPE = "System.String";
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

        protected override PSPropertyInfo CopyIntoNew(string name)
        {
            return new StringNoteProperty(name, _value);
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
        protected override ReadOnlyProperty<string> CopyToReadOnly()
        {
            return new ReadOnlyStringProperty(this.Name, _value);
        }

        public override bool ValueIsProper(object? value)
        {
            return value is null || value is string || value is IFormattable || value is IConvertible;
        }
    }
}
