using MG.Sonarr.Next.Extensions;
using System.Management.Automation;

namespace MG.Sonarr.Next.Models.PSProperties
{
    public sealed class ReadOnlyIntProperty : ReadOnlyProperty<int>
    {
        internal static readonly string IntTypeName = typeof(int).GetTypeName();

        public int IntValue { get; }
        public override string TypeNameOfValue => IntTypeName;
        protected override int ValueAsT => this.IntValue;

        public ReadOnlyIntProperty(string propertyName, int value)
            : base(propertyName)
        {
            this.IntValue = value;
        }

        public override PSMemberInfo Copy()
        {
            return new ReadOnlyIntProperty(this.Name, this.ValueAsT);
        }
    }
}
