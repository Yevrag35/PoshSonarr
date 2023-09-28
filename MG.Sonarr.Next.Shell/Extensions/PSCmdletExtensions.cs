using MG.Sonarr.Next.Services.Extensions;
using MG.Sonarr.Next.Services.Metadata;
using MG.Sonarr.Next.Shell.Components;

namespace MG.Sonarr.Next.Shell.Extensions
{
    public static class PSCmdletExtensions
    {
        public static ActionPreference GetCurrentActionPreferenceFromParam(this PSCmdlet cmdlet, [ConstantExpected] string parameterName, [ConstantExpected] string variableName)
        {
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

            return default; // silently continue
        }

        public static bool HasParameter<T>(this T cmdlet, Expression<Func<T, object?>> parameter) where T : PSCmdlet
        {
            return parameter.TryGetAsMember(out MemberExpression? memEx)
                   && 
                   cmdlet.MyInvocation.BoundParameters.ContainsKey(memEx.Member.Name);
        }
        public static bool HasParameter<T>(this T cmdlet, Expression<Func<T, SwitchParameter>> switchExpression, bool onlyIfPresent = false) where T : PSCmdlet
        {
            if (!switchExpression.TryGetAsMember(out MemberExpression? memEx))
            {
                return false;
            }
            else if (!cmdlet.MyInvocation.BoundParameters.ContainsKey(memEx.Member.Name))
            {
                return false;
            }

            var func = switchExpression.Compile();
            return !onlyIfPresent || func(cmdlet).ToBool();
        }

        public static bool ParameterSetNameIsLike(this PSCmdlet cmdlet, WildcardString wildString)
        {
            return wildString.IsMatch(cmdlet.ParameterSetName);
        }
        
        public static void WriteCollection<T>(this Cmdlet cmdlet, IEnumerable<T> collection)
        {
            cmdlet.WriteObject(collection, enumerateCollection: true);
        }
        public static void WriteCollection<T>(this Cmdlet cmdlet, MetadataTag? tag, IEnumerable<T> values)
            where T : PSObject
        {
            foreach (T value in values)
            {
                WriteObject(cmdlet, tag, value, enumerateCollection: false);
            }
        }
        public static void WriteObject<T>(this Cmdlet cmdlet, MetadataTag? tag, T value, bool enumerateCollection = true) where T : PSObject
        {
            value.AddMetadata(tag);
            cmdlet.WriteObject(value, enumerateCollection);
        }
    }
}