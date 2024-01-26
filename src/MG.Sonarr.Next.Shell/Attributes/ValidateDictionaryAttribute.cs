using MG.Sonarr.Next.Exceptions;
using MG.Sonarr.Next.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MG.Sonarr.Next.Shell.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class ValidateDictionaryAttribute : ValidateArgumentsAttribute
    {
        static readonly MethodInfo _validate = typeof(ValidateDictionaryAttribute)
            .GetMethod(nameof(ValidateElements), BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new MethodException("Unable to find method.");

        readonly bool _valueTypeIsObject;

        public Type KeyType { get; }
        public Type ValueType { get; }

        public ValidateDictionaryAttribute(Type keyType)
            : this(keyType, typeof(object), true)
        {
        }
        public ValidateDictionaryAttribute(Type keyType, Type valueType)
            : this(keyType, valueType, typeof(object).Equals(valueType))
        {

        }
        private ValidateDictionaryAttribute(Type keyType, Type valueType, bool valueTypeIsObject)
        {
            ArgumentNullException.ThrowIfNull(keyType);
            ArgumentNullException.ThrowIfNull(valueType);

            _valueTypeIsObject = valueTypeIsObject;
            this.KeyType = GetNonNullableKeyType(keyType);
            this.ValueType = valueType;
        }

        private static Type GetNonNullableKeyType(Type possibleNullable)
        {
            Type? possible = Nullable.GetUnderlyingType(possibleNullable);
            return possible ?? possibleNullable;
        }

        protected override void Validate(object arguments, EngineIntrinsics engineIntrinsics)
        {
            if (arguments is not IDictionary dict || dict.Count <= 0)
            {
                return;
            }

            MethodInfo genValidate = GetGenericValidation(this.KeyType, this.ValueType);

            try
            {
                genValidate.Invoke(null, new object[] { dict });
            }
            catch (TargetInvocationException ex)
            {
                if (ex.InnerException is InvalidTypeException invalidType)
                {
                    throw new ValidationMetadataException(invalidType.Message, invalidType);
                }

                throw;
            }
        }

        private static MethodInfo GetGenericValidation(Type keyType, Type valueType)
        {
            return _validate.MakeGenericMethod(new Type[] { keyType, valueType });
        }
        private static void ValidateElements<TKey, TValue>(IDictionary dictionary)
        {
            foreach (var kvp in dictionary.EnumerateAsPairs<TKey, TValue>())
            {
                Debug.Assert(kvp.Key is not null);
                Debug.Assert(kvp.Value is TValue);
            }
        }
    }
}

