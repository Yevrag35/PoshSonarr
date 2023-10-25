using MG.Sonarr.Next.Extensions;
using System.Management.Automation;
using System.Numerics;

namespace MG.Sonarr.Next.PSProperties
{
    public sealed class ReadOnlyNumberProperty<T> : ReadOnlyProperty<T> where T : unmanaged, INumber<T>
    {
        protected override int MaxValueCharacterLength => LengthConstants.INT128_MAX;
        public T NumberValue { get; }
        public override string TypeNameOfValue => typeof(T).GetTypeName();
        protected override T ValueAsT => this.NumberValue;

        public ReadOnlyNumberProperty(string propertyName, T value)
            : base(propertyName)
        {
            this.NumberValue = value;
        }
        public override PSMemberInfo Copy()
        {
            return new ReadOnlyNumberProperty<T>(this.Name, this.NumberValue);
        }
    }
}
