using System.Reflection;

namespace MG.Sonarr.Next.Reflection
{
    public interface IMemberGetter
    {
        object? GetValue(object? instance);
    }
    public interface IMemberSetter
    {
        void SetValue(object? instance, object? value);
    }

    public readonly struct FieldOrPropertyInfo : IMemberGetter, IMemberSetter
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

        public FieldOrPropertyInfo(FieldInfo fieldInfo)
        {
            ArgumentNullException.ThrowIfNull(fieldInfo);

            _isNotEmpty = true;
            _fi = fieldInfo;
            _isField = true;
            _pi = null;
        }
        public FieldOrPropertyInfo(PropertyInfo propertyInfo)
        {
            ArgumentNullException.ThrowIfNull(propertyInfo);

            _isNotEmpty = true;
            _pi = propertyInfo;
            _isField = false;
            _fi = null;
        }

        public object? GetValue(object? instance)
        {
            if (this.IsProperty)
            {
                return _pi.GetValue(instance);
            }
            else if (this.IsField)
            {
                return _fi.GetValue(instance);
            }

            return null;
        }
        public void SetValue(object? instance, object? value)
        {
            if (this.IsProperty)
            {
                _pi.SetValue(instance, value);
            }
            else if (this.IsField)
            {
                _fi.SetValue(instance, value);
            }
        }

        //public static implicit operator FieldOrPropertyInfo(FieldInfo fieldInfo)
        //{
        //    return fieldInfo is not null
        //        ? new FieldOrPropertyInfo(fieldInfo)
        //        : default;
        //}
        //public static implicit operator FieldOrPropertyInfo(PropertyInfo propertyInfo)
        //{
        //    return propertyInfo is not null
        //        ? new FieldOrPropertyInfo(propertyInfo)
        //        : default;
        //}
    }
}
