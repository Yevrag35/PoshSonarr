using System.Collections;
using System.Management.Automation;
using System.Runtime.CompilerServices;

namespace MG.Sonarr.Next.Extensions.PSO
{
    public static partial class PSOExtensions
    {
        [DebuggerStepThrough]
        public static T? GetValue<T>(this PSObject pso, [CallerMemberName] string propertyName = "")
        {
            object? value = pso.Properties[propertyName]?.Value;
            return value switch
            {
                T tVal => tVal,
                null => default,
                _ when LanguagePrimitives.TryConvertTo(value, out T? convertedTo) && convertedTo is not null => convertedTo,
                _ => default,
            };
        }

        /// <summary>
        /// Gets the value of the property as a string or an empty string if the value is null or cannot be converted to a string.
        /// </summary>
        /// <param name="pso">The <see cref="PSObject"/> this method is extending.</param>
        /// <param name="propertyName">The name of the property to get the value of.</param>
        /// <returns>The value of the property as a string or an empty string if the value is null or cannot be converted to a string.</returns>
        [DebuggerStepThrough]
        public static string GetStringOrEmpty(this PSObject pso, [CallerMemberName] string propertyName = "")
        {
            object? value = pso.Properties[propertyName]?.Value;
            return value switch
            {
                string strValue => strValue,
                null => string.Empty,
                _ when LanguagePrimitives.TryConvertTo(value, out string? convertedTo) && convertedTo is not null => convertedTo,
                _ => string.Empty,
            };
        }

        [DebuggerStepThrough]
        public static void SetValue<T>(this PSObject pso, T? value, [CallerMemberName] string propertyName = "")
        {
            ArgumentNullException.ThrowIfNull(pso);

            PSPropertyInfo? prop = pso.Properties[propertyName];
            if (prop is null)
            {
                pso.AddProperty(propertyName, value);
                return;
            }

            prop.Value = value;
        }

        [return: NotNullIfNotNull(nameof(pso))]
        public static Dictionary<string, object?>? ToDictionary(this PSObject? pso, params ReadOnlySpan<string> ignoreKeys)
        {
            if (pso is null) return null;

            Dictionary<string, object?> dic = new(StringComparer.OrdinalIgnoreCase);
            foreach (PSPropertyInfo prop in pso.Properties)
            {
                if (ignoreKeys.Contains(prop.Name))
                {
                    continue;
                }

                if (prop.Value is null)
                {
                    dic.TryAdd(prop.Name, prop.Value);
                    continue;
                }

                object val = prop.Value switch
                {
                    PSObject innerPso => ToDictionary(innerPso, ignoreKeys),
                    string str => str,
                    IEnumerable collection => ProcessCollection(collection, ignoreKeys),
                    _ => prop.Value
                };

                dic.TryAdd(prop.Name, val);
            }

            return dic;
        }

        private static List<object?> ProcessCollection(IEnumerable collection, ReadOnlySpan<string> ignoreKeys)
        {
            List<object?> list = [];
            foreach (object? item in collection)
            {
                object? val = item switch
                {
                    PSObject innerPso => ToDictionary(innerPso, ignoreKeys),
                    string str => str,
                    IEnumerable innerCollection => ProcessCollection(innerCollection, ignoreKeys),
                    _ => item
                };

                list.Add(val);
            }
            return list;
        }

        [DebuggerStepThrough]
        public static bool TryGetNonNullProperty<T>(this PSObject? pso, string propertyName, [NotNullWhen(true)] out T? value)
        {
            return TryGetProperty<T>(pso, propertyName, out value) && value is not null;
        }

        [DebuggerStepThrough]
        public static bool TryGetProperty<T>(this PSObject? pso, string propertyName, [MaybeNull] out T value)
        {
            value = default;
            PSMemberInfo? prop = pso?.Properties[propertyName];
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
    }
}
