using System.Management.Automation;

namespace MG.Sonarr.Next.PSProperties
{
    public sealed class ReadOnlyStructProperty<T> : ReadOnlyProperty<T> where T : struct
    {
        public T StructValue { get; }
        protected override T ValueAsT => this.StructValue;

        public ReadOnlyStructProperty(string propertyName, T value)
            : base(propertyName)
        {
            this.StructValue = value;
        }

        public override PSMemberInfo Copy()
        {
            return new ReadOnlyStructProperty<T>(this.Name, this.StructValue);
        }
    }
}

