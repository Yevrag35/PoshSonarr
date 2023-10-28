using MG.Sonarr.Next.Extensions;
using System.Management.Automation;

namespace MG.Sonarr.Next.PSProperties
{
    public sealed class StructNoteProperty<T> : WritableProperty<T> where T : struct
    {
        public override string TypeNameOfValue => typeof(T).GetTypeName();
        public T StructValue { get; set; }

        protected override T ValueAsT
        {
            get => this.StructValue;
            set => this.StructValue = value;
        }

        public StructNoteProperty(string propertyName)
        {
            this.SetMemberName(propertyName);
        }
        public StructNoteProperty(string propertyName, T value)
            : this(propertyName)
        {
            this.StructValue = value;
        }

        protected override T ConvertFromObject(object? value)
        {
            return value is T tVal ? tVal : this.ThrowNotType<T>();
        }
        public override PSMemberInfo Copy()
        {
            return new StructNoteProperty<T>(this.Name, this.StructValue);
        }

        public override bool ValueIsProper(object? value)
        {
            return value is T;
        }
    }
}

