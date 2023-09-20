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
            return TryGetProperty(pso, propName, out string? propVal) && tag.Equals(propVal, StringComparison.InvariantCultureIgnoreCase);
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

        public static bool TryGetProperty<T>([NotNullWhen(true)] this object? obj, string propertyName, [NotNullWhen(true)] out T? value)
        {
            if (obj is PSObject pso)
            {
                return TryGetProperty(pso, propertyName, out value);
            }

            value = obj.TryGetPropertyValue<T>(propertyName);
            return value is not null;
        }
        public static bool TryGetProperty<T>(this PSObject pso, string propertyName, [NotNullWhen(true)] out T? value)
        {
            var col = pso.Properties.Match(propertyName, PSMemberTypes.NoteProperty);
            if (col.Count == 1 && col[0].Value is T tVal)
            {
                value = tVal;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }
    }
}
