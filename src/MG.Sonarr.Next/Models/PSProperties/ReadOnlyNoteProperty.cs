using MG.Sonarr.Next.Extensions;
using System.Data;
using System.Management.Automation;

namespace MG.Sonarr.Next.Models.PSProperties
{
    public sealed class ReadOnlyNoteProperty<T> : PSPropertyInfo
    {
        public override bool IsSettable => false;
        public override bool IsGettable => true;

        public override PSMemberTypes MemberType => PSMemberTypes.NoteProperty;

        public T? ValueAsT { get; }
        /// <summary>
        /// The value of the property.  Can NOT modify this value.
        /// </summary>
        /// <exception cref="ReadOnlyException"/>
        public override object? Value
        {
            get => this.ValueAsT;
            set => throw new ReadOnlyPropertyException(this.Name);
        }

        public ReadOnlyNoteProperty(string name, T? value)
            : this(name, value, typeof(T).GetTypeName())
        {
        }
        private ReadOnlyNoteProperty(ReadOnlyNoteProperty<T> copyFrom)
            : this(copyFrom.Name, copyFrom.ValueAsT, copyFrom.TypeNameOfValue)
        {
        }
        private ReadOnlyNoteProperty(string name, T? value, string typeName)
        {
            this.SetMemberName(name);
            this.ValueAsT = value;
            this.TypeNameOfValue = typeName;
        }

        public override string TypeNameOfValue { get; }

        public override PSMemberInfo Copy()
        {
            return new ReadOnlyNoteProperty<T>(this);
        }

        const string READ_ONLY = "read-only ";
        public override string ToString()
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
