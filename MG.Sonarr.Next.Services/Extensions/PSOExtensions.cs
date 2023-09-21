using MG.Sonarr.Next.Services.Metadata;
using Microsoft.VisualBasic;
using Namotion.Reflection;
using System.Management.Automation;

namespace MG.Sonarr.Next.Services.Extensions
{
    public static class PSOExtensions
    {
        public static void AddProperty<T>(this object? obj, string propertyName, T value)
        {
            if (obj is not PSObject pso)
            {
                return;
            }

            AddProperty(pso: pso, propertyName, value);
        }
        public static void AddProperty<T>(this PSObject pso, string propertyName, T value)
        {
            pso.Properties.Add(new PSNoteProperty(propertyName, value));
        }
        public static bool IsCorrectType(this object? obj, string propName, [ConstantExpected] string tag, [NotNullWhen(true)] out PSObject? pso)
        {
            if (obj is not PSObject isPso)
            {
                pso = null;
                return false;
            }

            pso = isPso;
            return TryGetNonNullProperty(pso, MetadataResolver.META_PROPERTY_NAME, out MetadataTag? mt)
                   &&
                   mt.Value == tag;
        }

        public static bool PropertyEquals<T>(this object? obj, string propertyName, T mustEqual) where T : IEquatable<T>
        {
            return !TryGetProperty(obj, propertyName, out T? oVal)
                   ||
                   (mustEqual?.Equals(oVal)).GetValueOrDefault();
        }

        public static bool PropertyEquals<T>(this PSObject pso, string propertyName, T mustEqual)
        {
            if (TryGetProperty(pso, propertyName, out T? value))
            {
                return (mustEqual is null && value is null)
                       ||
                       (mustEqual?.Equals(value)).GetValueOrDefault();
            }

            return false;
        }

        public static bool TryGetNonNullProperty<T>([NotNullWhen(true)] this object? obj, string propertyName, [NotNullWhen(true)] out T? value)
        {
            return TryGetProperty(obj, propertyName, out value) && value is not null;
        }
        public static bool TryGetNonNullProperty<T>(this PSObject pso, string propertyName, [NotNullWhen(true)] out T? value)
        {
            return TryGetProperty(pso, propertyName, out value) && value is not null;
        }
        public static bool TryGetProperty<T>([NotNullWhen(true)] this object? obj, string propertyName, [MaybeNull] out T value)
        {
            value = default;
            return obj is PSObject pso && TryGetProperty(pso, propertyName, out value);
        }
        public static bool TryGetProperty<T>(this PSObject pso, string propertyName, [MaybeNull] out T value)
        {
            PSMemberInfo? prop = pso.Properties[propertyName];
            return TrySafeCast(prop?.Value, out value);
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
        private static bool TryUnsafeCast<T>([NotNullWhen(true)] object? obj, [NotNullWhen(true)] out T? tVal)
        {
            if (obj is null)
            {
                tVal = default;
                return false;
            }

            tVal = Cast<T>(obj);
            return true;
        }
    }
}
