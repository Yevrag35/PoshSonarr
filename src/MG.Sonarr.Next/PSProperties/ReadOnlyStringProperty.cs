using System.Management.Automation;

namespace MG.Sonarr.Next.PSProperties
{
    public sealed class ReadOnlyStringProperty : ReadOnlyProperty<string>
    {
        public string StringValue { get; }
        public override string TypeNameOfValue => StringNoteProperty.STRING_TYPE;
        protected override string ValueAsT => this.StringValue;

        public ReadOnlyStringProperty(string propertyName, string? value)
            : base(propertyName)
        {
            this.StringValue = value ?? string.Empty;
        }

        public override PSMemberInfo Copy()
        {
            return new ReadOnlyStringProperty(this.Name, this.StringValue);
        }
    }
}

