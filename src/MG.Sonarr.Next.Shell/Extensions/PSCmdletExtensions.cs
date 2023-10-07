using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Reflection;
using MG.Sonarr.Next.Shell.Cmdlets;
using MG.Sonarr.Next.Shell.Components;

namespace MG.Sonarr.Next.Shell.Extensions
{
    public static class PSCmdletExtensions
    {
        public static ActionPreference GetCurrentActionPreferenceFromParam([ValidatedNotNull] this PSCmdlet cmdlet, [ConstantExpected] string parameterName, [ConstantExpected] string variableName)
        {
            if (cmdlet?.MyInvocation?.BoundParameters is null)
            {
                return default;
            }

            if (cmdlet.MyInvocation.BoundParameters.TryGetValueAs(parameterName, out ActionPreference actionPref))
            {
                return actionPref;
            }
            else if (cmdlet.SessionState.PSVariable.TryGetVariableValue(variableName, out actionPref))
            {
                return actionPref;
            }

            return default;
        }

        public static ActionPreference GetCurrentActionPreferenceFromSwitch([ValidatedNotNull] this PSCmdlet cmdlet, [ConstantExpected] string parameterName, [ConstantExpected] string variableName)
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

            return default; // silently continue
        }

        public static bool HasParameter<T>([ValidatedNotNull] this T cmdlet, [ValidatedNotNull] Expression<Func<T, object?>> parameter) where T : PSCmdlet
        {
            return parameter.TryGetAsMember(out MemberExpression? memEx)
                   && 
                   cmdlet.MyInvocation.BoundParameters.ContainsKey(memEx.Member.Name);
        }
        public static bool HasParameter<T>([ValidatedNotNull] this T cmdlet, [ValidatedNotNull] Expression<Func<T, SwitchParameter>> switchExpression, bool onlyIfPresent = false) where T : PSCmdlet
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

        public static bool ParameterSetNameIsLike([ValidatedNotNull] this PSCmdlet cmdlet, Wildcard wildString)
        {
            return wildString.IsMatch(cmdlet.ParameterSetName);
        }

        public static void SetValue<TCmdlet, TObj, TValue>([ValidatedNotNull] this TCmdlet cmdlet, TValue? value, Expression<Func<TCmdlet, TObj?>> getSetting, Action<TValue, TObj> setValue)
            where TCmdlet : SonarrCmdletBase
            where TObj : class, new()
        {
            ArgumentNullException.ThrowIfNull(getSetting);
            ArgumentNullException.ThrowIfNull(setValue);

            if (value is not null)
            {
                var func = getSetting.Compile();
                TObj? obj = func(cmdlet);
                if (obj is null)
                {
                    if (!getSetting.TryGetAsSetter(out FieldOrPropertyInfoSetter setter))
                    {
                        throw new InvalidOperationException("TObj must resolve to a field or property.");
                    }

                    obj = new();
                    setter.SetValue(cmdlet, obj);
                }

                setValue.Invoke(value, obj);
            }
        }
        
        public static void WriteCollection<T>([ValidatedNotNull] this Cmdlet cmdlet, IEnumerable<T> collection)
        {
            cmdlet.WriteObject(collection, enumerateCollection: true);
        }
    }
}