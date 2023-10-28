using MG.Sonarr.Next.Extensions;
using System.Management.Automation;
using System.Numerics;

namespace MG.Sonarr.Next.PSProperties
{
    public sealed class NumberNoteProperty<T> : WritableProperty<T> where T : unmanaged, INumber<T>
    {
        protected override int MaxValueCharacterLength => LengthConstants.INT128_MAX;
        public T NumValue { get; set; }

        public override string TypeNameOfValue => typeof(T).GetTypeName();
        protected override T ValueAsT
        {
            get => this.NumValue;
            set => this.NumValue = value;
        }

        public NumberNoteProperty(string propertyName)
        {
            this.SetMemberName(propertyName);
        }
        public NumberNoteProperty(string propertyName, T value)
            : this(propertyName)
        {
            this.NumValue = value;
        }
        protected override T ConvertFromObject(object? value)
        {
            if (value is T intVal)
            {
                return intVal;
            }
            else if (value is string strVal && T.TryParse(strVal, Statics.DefaultProvider, out T fromStr))
            {
                return fromStr;
            }
            else if (value is ISpanFormattable spanFormattable)
            {
                Span<char> chars = stackalloc char[LengthConstants.INT128_MAX];
                if (spanFormattable.TryFormat(chars, out int written, default, Statics.DefaultProvider)
                    &&
                    T.TryParse(chars.Slice(0, written), Statics.DefaultProvider, out T intRes))
                {
                    return intRes;
                }

                Debug.Fail("Shit");
            }

            return this.ThrowNotType<T>();
        }
        public override PSMemberInfo Copy()
        {
            return new NumberNoteProperty<T>(this.Name, this.NumValue);
        }
        public override bool ValueIsProper([NotNullWhen(true)] object? value)
        {
            return value is T || value is IConvertible;
        }
    }
}
