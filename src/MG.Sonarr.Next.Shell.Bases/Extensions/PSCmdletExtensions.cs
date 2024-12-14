using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Reflection;
using MG.Sonarr.Next.Shell.Cmdlets;
using MG.Sonarr.Next.Strings;
using System.Runtime.CompilerServices;

namespace MG.Sonarr.Next.Shell.Extensions
{
    public static class PSCmdletExtensions
    {
        public static ActionPreference GetActionPreferenceFromParam(this PSCmdlet cmdlet, [ConstantExpected] string parameterName, [ConstantExpected] string variableName, ActionPreference defaultIfNotPresent = ActionPreference.SilentlyContinue)
        {
            return ResolveActionPreferenceFromPSCmdlet(
                cmdlet,
                parameterName,
                variableName,
                in defaultIfNotPresent,
                resolution: (object? boundValue, in ActionPreference defValue) => boundValue switch
                {
                    ActionPreference preference when Enum.IsDefined(preference) => preference,
                    int numberValue when Enum.IsDefined((ActionPreference)numberValue) => (ActionPreference)numberValue,
                    string strValue when Enum.TryParse(strValue, ignoreCase: true, out ActionPreference pref) => pref,
                    _ => defValue,
                });
        }

        public static ActionPreference GetActionPreferenceFromSwitch(this PSCmdlet cmdlet, [ConstantExpected] string parameterName, [ConstantExpected] string variableName, ActionPreference defaultIfNotPresent = ActionPreference.SilentlyContinue)
        {
            return ResolveActionPreferenceFromPSCmdlet(
                cmdlet,
                parameterName,
                variableName,
                in defaultIfNotPresent,
                resolution: (object? boundValue, in ActionPreference defValue) => boundValue switch
                {
                    SwitchParameter swParam when swParam.ToBool() => ActionPreference.Continue,
                    bool justBool when justBool => ActionPreference.Continue,
                    _ => defValue,
                });
        }

        [return: NotNullIfNotNull(nameof(path))]
        public static string? GetResolvedPath(this PSCmdlet cmdlet, string? path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            var col = cmdlet.GetResolvedProviderPathFromPSPath(path, out _);
            return col.Count switch
            {
                > 0 => col[0],
                _ => string.Empty,
            };
        }
        [return: NotNullIfNotNull(nameof(path))]
        public static string? GetUnresolvedPath(this PSCmdlet cmdlet, string? path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            return cmdlet.GetUnresolvedProviderPathFromPSPath(path) ?? string.Empty;
        }

        public static bool HasParameter<T>(this T cmdlet, Expression<Func<T, object?>> parameter) where T : PSCmdlet
        {
            return parameter.TryGetAsMember(out MemberExpression? memEx)
                   && 
                   cmdlet.MyInvocation.BoundParameters.ContainsKey(memEx.Member.Name);
        }
        public static bool HasParameter<T>(this T cmdlet, Expression<Func<T, SwitchParameter>> switchExpression, bool onlyIfPresent) where T : PSCmdlet
        {
            if (!switchExpression.TryGetAsMember(out MemberExpression? memEx))
            {
                return false;
            }
            else if (!cmdlet.MyInvocation.BoundParameters.ContainsKey(memEx.Member.Name))
            {
                return false;
            }
            else if (onlyIfPresent)
            {
                return true;
            }

            var func = switchExpression.Compile();
            return func(cmdlet).ToBool();
        }

        public static bool HasParameter<TValue>(this PSCmdlet cmdlet, TValue value, [CallerArgumentExpression(nameof(value))] string parameterName = "") where TValue : struct
        {
            return ContainsParameterKey(cmdlet.MyInvocation.BoundParameters, parameterName);
        }
        public static bool HasParameter(this PSCmdlet cmdlet, object? value, [CallerArgumentExpression(nameof(value))] string parameterName = "")
        {
            return ContainsParameterKey(cmdlet.MyInvocation.BoundParameters, parameterName);
        }
        public static bool HasNotNullParameter(this PSCmdlet cmdlet, [NotNullWhen(true)] object? value, [CallerArgumentExpression(nameof(value))] string parameterName = "")
        {
            return value is not null && HasParameter(cmdlet, value, parameterName);
        }

        public static bool ParameterSetNameIsLike(this PSCmdlet cmdlet, Wildcard wildString)
        {
            return wildString.IsMatch(cmdlet.ParameterSetName);
        }

        public static void SetValue<TCmdlet, TObj, TValue>(this TCmdlet cmdlet, TValue? value, Expression<Func<TCmdlet, TObj?>> getSetting, Action<TValue, TObj> setValue)
            where TCmdlet : SonarrCmdletBase
            where TObj : class, new()
        {
            if (value is null)
            {
                return;
            }

            var func = getSetting.Compile();
            TObj? obj = func(cmdlet);
            if (obj is null)
            {
                if (!getSetting.TryGetAsSetter(out IMemberSetter? setter))
                {
                    throw new InvalidOperationException("TObj must resolve to a field or property.");
                }

                obj = new();
                setter.SetValue(cmdlet, obj);
            }

            setValue.Invoke(value, obj);
        }
        
        public static void WriteCollection<T>(this Cmdlet cmdlet, IEnumerable<T> collection)
        {
            cmdlet.WriteObject(collection, enumerateCollection: true);
        }

        private delegate ActionPreference ResolveFromBoundValue(object? boundValue, in ActionPreference defaultIfNotPresent);
        private static ActionPreference ResolveActionPreferenceFromPSCmdlet(
            PSCmdlet cmdlet,
            string parameterName,
            string variableName,
            in ActionPreference defaultIfNotPresent,
            ResolveFromBoundValue resolution)
        {
            object? boundValue = null;

            if (false == cmdlet.MyInvocation?.BoundParameters?.TryGetValue(parameterName, out boundValue)
                &&
                cmdlet.SessionState.PSVariable.TryGetVariableValue(variableName, out ActionPreference variablePref))
            {
                return variablePref;
            }

            return resolution(boundValue, in defaultIfNotPresent);
        }

        private static bool ContainsParameterKey(Dictionary<string, object?> dictionary, string key)
        {
            ReadOnlySpan<char> keySpan = TrimProperties(key);
            return dictionary.ContainsKey(keySpan);
        }
        private static bool TryGetParameterValue(Dictionary<string, object?> dictionary, string key, out object? value)
        {
            ReadOnlySpan<char> keySpan = TrimProperties(key);
            return dictionary.TryGetValue(keySpan, out value);
        }
        private static bool TryGetParameterNonNullValue(Dictionary<string, object?> dictionary, string key, [NotNullWhen(true)] out object? value)
        {
            bool result = TryGetParameterValue(dictionary, key, out value);
            return result && value is not null;
        }
        private static ReadOnlySpan<char> TrimProperties(ReadOnlySpan<char> value)
        {
            int index = value.LastIndexOf('.');
            return index >= 0 && index < value.Length - 1
                ? value.Slice(index + 1)
                : value;
        }
    }
}