using System.Management.Automation;
using System.Runtime.CompilerServices;

namespace MG.Sonarr.Next.Extensions.PSO
{
    public static partial class PSOExtensions
    {
        [DebuggerStepThrough]
        public static T? GetValue<T>(this PSObject? pso, [CallerMemberName] string propertyName = "")
        {
            try
            {
                return (T?)pso?.Properties[propertyName]?.Value;
            }
            catch
            {
                return default;
            }
        }

        [DebuggerStepThrough]
        public static string GetStringOrEmpty(this PSObject? pso, [CallerMemberName] string propertyName = "")
        {
            string? value = pso?.Properties[propertyName]?.Value as string;
            return value ?? string.Empty;
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
