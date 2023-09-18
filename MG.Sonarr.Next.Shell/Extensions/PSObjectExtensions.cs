using MG.Sonarr.Next.Shell.Components;
using Namotion.Reflection;

namespace MG.Sonarr.Next.Shell.Extensions
{
    public static class PSObjectExtensions
    {
        internal static void AddMetadataTag(this object? obj, [ConstantExpected] string tag)
        {
            if (obj is PSObject pso)
            {
                AddMetadataTag(pso: pso, tag);
            }
        }
        internal static void AddMetadataTag(this PSObject pso, [ConstantExpected] string tag)
        {
            string tagStr = string.Create(tag.Length + 1, tag, (chars, state) =>
            {
                chars[0] = Constants.META_PREFIX;
                state.CopyTo(chars.Slice(1));
                Span<char> scratch = stackalloc char[chars.Length];
                chars.CopyTo(scratch);

                ((ReadOnlySpan<char>)scratch).ToLower(chars, null);
            });

            pso.Properties.Add(new PSNoteProperty(Constants.META_PROPERTY_NAME, tagStr));
        }

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
        
        public static void AddNameAlias(this object? obj)
        {
            if (obj is PSObject pso)
            {
                AddNameAlias(pso: pso);
            }
        }
        public static void AddNameAlias(this PSObject pso)
        {
            pso.Properties.Add(new PSAliasProperty(Constants.NAME, Constants.TITLE));
        }
        private static ReadOnlySpan<char> GetTag(string tag, Span<char> span)
        {
            tag.CopyTo(span.Slice(1));
            if (tag.StartsWith(Constants.META_PREFIX))
            {
                span = span.Slice(1);
            }
            else
            {
                span[0] = Constants.META_PREFIX;
            }
            
            return span;
        }
        internal static bool IsCorrectType(this object? obj, string tag, [NotNullWhen(true)] out PSObject? pso)
        {
            if (obj is not PSObject isPso)
            {
                pso = null;
                return false;
            }

            pso = isPso;
            ReadOnlySpan<char> tagToCheck = GetTag(tag, stackalloc char[tag.Length + 1]);
            return TryGetProperty(pso, Constants.META_PROPERTY_NAME, out string? propVal) && tagToCheck.Equals(propVal, StringComparison.InvariantCultureIgnoreCase);
        }

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
