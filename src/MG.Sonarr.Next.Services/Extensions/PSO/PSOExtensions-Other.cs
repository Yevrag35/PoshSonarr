﻿using MG.Sonarr.Next.Services.Metadata;
using System.Management.Automation;
using System.Runtime.CompilerServices;

namespace MG.Sonarr.Next.Services.Extensions.PSO
{
    public static partial class PSOExtensions
    {
        public static bool IsCorrectType(this object? obj, string propName, [ConstantExpected] string tag, [NotNullWhen(true)] out PSObject? pso)
        {
            if (obj is not PSObject isPso)
            {
                pso = null;
                return false;
            }

            pso = isPso;
            return pso.TryGetNonNullProperty(MetadataResolver.META_PROPERTY_NAME, out MetadataTag? mt)
                   &&
                   mt.Value == tag;
        }

        public static T? GetValue<T>(this PSObject pso, [CallerMemberName] string propertyName = "")
        {
            try
            {
                return (T?)pso.Properties[propertyName]?.Value;
            }
            catch
            {
                return default;
            }
        }
        public static void SetValue<T>(this PSObject pso, T? value, [CallerMemberName] string propertyName = "")
        {
            PSPropertyInfo? prop = pso.Properties[propertyName];
            if (prop is null)
            {
                pso.AddProperty(propertyName, value);
                return;
            }

            prop.Value = value;
        }

        public static bool PropertyEquals<T>(this object? obj, string propertyName, T mustEqual) where T : IEquatable<T>
        {
            return !obj.TryGetProperty(propertyName, out T? oVal)
                   ||
                   (mustEqual?.Equals(oVal)).GetValueOrDefault();
        }

        public static bool PropertyEquals<T>(this PSObject pso, string propertyName, T mustEqual)
        {
            if (pso.TryGetProperty(propertyName, out T? value))
            {
                return mustEqual is null && value is null
                       ||
                       (mustEqual?.Equals(value)).GetValueOrDefault();
            }

            return false;
        }

        public static bool TryGetNonNullProperty<T>([NotNullWhen(true)] this object? obj, string propertyName, [NotNullWhen(true)] out T? value)
        {
            return obj.TryGetProperty(propertyName, out value) && value is not null;
        }
        public static bool TryGetNonNullProperty<T>(this PSObject pso, string propertyName, [NotNullWhen(true)] out T? value)
        {
            return pso.TryGetProperty(propertyName, out value) && value is not null;
        }
        public static bool TryGetProperty<T>([NotNullWhen(true)] this object? obj, string propertyName, [MaybeNull] out T value)
        {
            value = default;
            return obj is PSObject pso && pso.TryGetProperty(propertyName, out value);
        }
        public static bool TryGetProperty<T>(this PSObject pso, string propertyName, [MaybeNull] out T value)
        {
            value = default;
            PSMemberInfo? prop = pso.Properties[propertyName];
            return prop is not null && TrySafeCast(prop.Value, out value);
        }

        /// <exception cref="InvalidCastException"/>
        [return: NotNullIfNotNull(nameof(obj))]
        private static T? Cast<T>(object? obj)
        {
            return (T?)obj;
        }
        private static bool TrySafeCast<T>([NotNullWhen(true)] object? obj, [MaybeNull] out T tVal)
        {
            tVal = default;
            if (obj is null)
            {
                return true;
            }

            Type genType = typeof(T);
            Type oType = obj.GetType();
            if (oType.IsAssignableTo(genType))
            {
                tVal = Cast<T>(obj);
                return true;
            }
            else
            {
                try
                {
                    tVal = Cast<T>(obj);
                    return true;
                }
                catch (InvalidCastException)
                {
                    return false;
                }
            }
        }
        //private static bool TryUnsafeCast<T>([NotNullWhen(true)] object? obj, [NotNullWhen(true)] out T? tVal)
        //{
        //    if (obj is null)
        //    {
        //        tVal = default;
        //        return false;
        //    }

        //    tVal = Cast<T>(obj);
        //    return true;
        //}
    }
}
