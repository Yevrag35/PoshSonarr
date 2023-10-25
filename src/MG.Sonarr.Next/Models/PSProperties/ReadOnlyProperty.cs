using MG.Sonarr.Next.Extensions;
using System.Management.Automation;

namespace MG.Sonarr.Next.Models.PSProperties
{
    public abstract class ReadOnlyProperty<T> : PSPropertyInfo
    {
        public sealed override bool IsGettable => true;
        public sealed override bool IsSettable => false;
        public sealed override PSMemberTypes MemberType => PSMemberTypes.NoteProperty;

        /// <summary>
        ///     Gets the value of the <see cref="ReadOnlyProperty{T}"/>.
        /// </summary>
        /// <remarks>
        ///     Even though this property has a setter, a <see cref="SetValueException"/> will be thrown if 
        ///     trying to overwrite the initial value set from the constructor.
        /// </remarks>
        /// <exception cref="SetValueException"/>
        public sealed override object? Value
        {
            get => this.ValueAsT;
            set
            {
                ReadOnlyPropertyException ex = new(this.Name);
                throw new SetValueException(ex.Message, ex);
            }
        }

        protected abstract T ValueAsT { get; }

        protected ReadOnlyProperty(string propertyName)
        {
            base.SetMemberName(propertyName);
        }

        const string READ_ONLY = "read-only ";
        public sealed override string ToString()
        {
            string typeName = typeof(T).GetPSTypeName();
            string valueStr = this.ValueAsT?.ToString() ?? string.Empty;
            string name = this.Name;

            int length = 2 + READ_ONLY.Length; // Magic number 2 comes from: 1 space and an equals sign.
            length += typeName.Length + valueStr.Length + name.Length;

            return string.Create(length, (typeName, valueStr, name), (chars, state) =>
            {
                int position = 0;
                READ_ONLY.CopyToSlice(chars, ref position);

                state.typeName.CopyToSlice(chars, ref position);
                chars[position++] = ' ';

                state.name.CopyToSlice(chars, ref position);
                chars[position++] = '=';

                state.valueStr.CopyTo(chars.Slice(position));
            });
        }
    }
}
