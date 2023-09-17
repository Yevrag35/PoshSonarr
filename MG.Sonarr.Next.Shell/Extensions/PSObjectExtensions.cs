using MG.Sonarr.Next.Shell.Components;
using Namotion.Reflection;

namespace MG.Sonarr.Next.Shell.Extensions
{
    public static class PSObjectExtensions
    {
        public static bool PropertyLike(this object? obj, string propertyName, string value)
        {
            if (TryGetProperty(obj, propertyName, out string? strVal))
            {
                WildcardString ws = value;
                return ws.IsMatch(strVal);
            }

            return false;
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
