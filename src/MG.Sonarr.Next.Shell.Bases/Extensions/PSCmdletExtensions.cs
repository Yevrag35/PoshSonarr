using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Reflection;
using MG.Sonarr.Next.Shell.Cmdlets;
using MG.Sonarr.Next.Strings;
using System.Runtime.CompilerServices;

namespace MG.Sonarr.Next.Shell.Extensions
{
    public static class PSCmdletExtensions
    {
        public static ActionPreference GetCurrentActionPreferenceFromParam(this PSCmdlet cmdlet, [ConstantExpected] string parameterName, [ConstantExpected] string variableName)
        {
            if (cmdlet.MyInvocation?.BoundParameters is null || cmdlet.MyInvocation.BoundParameters.Count == 0)
            {
                return ActionPreference.SilentlyContinue;
            }

            if (cmdlet.MyInvocation.BoundParameters.TryGetValueAs(parameterName, out ActionPreference actionPref))
            {
                return actionPref;
            }
            else if (cmdlet.SessionState.PSVariable.TryGetVariableValue(variableName, out actionPref))
            {
                return actionPref;
            }

            return ActionPreference.SilentlyContinue;
        }

        public static ActionPreference GetCurrentActionPreferenceFromSwitch(this PSCmdlet cmdlet, [ConstantExpected] string parameterName, [ConstantExpected] string variableName)
        {
            if (cmdlet.MyInvocation.BoundParameters.TryGetValueAs(parameterName, out SwitchParameter result)
                &&
                result.ToBool())
            {
                return ActionPreference.Continue;
            }
            else if (cmdlet.SessionState.PSVariable.TryGetVariableValue(variableName, out ActionPreference actionPref))
            {
                return actionPref;
            }

            return ActionPreference.SilentlyContinue;
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

        public static bool HasParameter<T, TValue>(this T cmdlet, [NotNullWhen(true)] TValue? value, [CallerArgumentExpression(nameof(value))] string parameterName = "")
            where T : PSCmdlet
            where TValue : class
        {
            return cmdlet.MyInvocation.BoundParameters.ContainsKey(parameterName)
                   &&
                   value is not null;
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
    }
}