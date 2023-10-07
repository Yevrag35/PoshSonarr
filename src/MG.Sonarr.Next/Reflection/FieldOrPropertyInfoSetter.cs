using System.Reflection;

namespace MG.Sonarr.Next.Services.Reflection
{
    public readonly struct FieldOrPropertyInfoSetter
    {
        readonly bool _isNotEmpty;
        readonly bool _isField;
        readonly FieldInfo? _fi;
        readonly PropertyInfo? _pi;

        public bool IsEmpty => !_isNotEmpty;

        [MemberNotNullWhen(true, nameof(_fi), nameof(_pi))]
        public bool IsField => _isNotEmpty && _isField;

        [MemberNotNullWhen(true, nameof(_pi))]
        public bool IsProperty => _isNotEmpty && !_isField;

        public FieldOrPropertyInfoSetter(FieldInfo fieldInfo)
        {
            _isNotEmpty = true;
            _fi = fieldInfo;
            _isField = true;
            _pi = null;
        }
        public FieldOrPropertyInfoSetter(PropertyInfo propertyInfo)
        {
            _isNotEmpty = true;
            _pi = propertyInfo;
            _isField = false;
            _fi = null;
        }

        public void SetValue(object instance, object? value)
        {
            if (this.IsField)
            {
                _fi.SetValue(instance, value);
            }
            else if (this.IsProperty)
            {
                _pi.SetValue(instance, value);
            }
        }
    }
}
