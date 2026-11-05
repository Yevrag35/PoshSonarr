using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Extensions.Strings;
using MG.Sonarr.Next.Strings;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace MG.Sonarr.Next.Shell.Extensions;

public static partial class PSCmdletExtensions
{
	private static readonly FrozenDictionary<string, ActionPreference> s_preferences;
	private static readonly FrozenSet<ActionPreference> s_prefSet;

	static PSCmdletExtensions()
	{
		string[] names = Enum.GetNames<ActionPreference>();
		var dic = new Dictionary<string, ActionPreference>(names.Length, StringComparer.OrdinalIgnoreCase);

		foreach (string name in names)
		{
			dic.Add(name, Enum.Parse<ActionPreference>(name));
		}

		s_preferences = dic.ToFrozenDictionary();
		s_prefSet = dic.Values.ToFrozenSet();
	}

	public static ActionPreference GetActionPreferenceFromParam(this PSCmdlet cmdlet, [ConstantExpected] string parameterName, [ConstantExpected] string variableName, ActionPreference defaultIfNotPresent = ActionPreference.SilentlyContinue)
	{
		unsafe
		{
			return ResolveActionPreferenceFromPSCmdlet(
				cmdlet,
				parameterName,
				variableName,
				defaultIfNotPresent,
				&getPreference);
		}

		static ActionPreference getPreference(object? boundValue, ActionPreference defaultIfNotPresent)
		{
			return boundValue switch
			{
				ActionPreference preference when s_prefSet.Contains(preference) => preference,
				int numberValue when s_prefSet.Contains((ActionPreference)numberValue) => (ActionPreference)numberValue,
				string strValue when s_preferences.TryGetValue(strValue, out ActionPreference pref) => pref,
				_ => defaultIfNotPresent,
			};
		}
	}

	public static ActionPreference GetActionPreferenceFromSwitch(this PSCmdlet cmdlet, [ConstantExpected] string parameterName, [ConstantExpected] string variableName, ActionPreference defaultIfNotPresent = ActionPreference.SilentlyContinue)
	{
		unsafe
		{
			return ResolveActionPreferenceFromPSCmdlet(
				cmdlet,
				parameterName,
				variableName,
				defaultIfNotPresent,
				&getPreference);
		}

		static ActionPreference getPreference(object? boundValue, ActionPreference defaultIfNotPresent)
		{
			return boundValue switch
			{
				SwitchParameter swParam when swParam.ToBool() => ActionPreference.Continue,
				bool justBool when justBool => ActionPreference.Continue,
				_ => defaultIfNotPresent,
			};
		}
	}

	[return: NotNullIfNotNull(nameof(path))]
	public static string? GetResolvedPath(this PSCmdlet cmdlet, string? path)
	{
		if (string.IsNullOrEmpty(path))
		{
			return path;
		}

		var col = cmdlet.GetResolvedProviderPathFromPSPath(path, out _);
		return col.Count > 0 ? col[0] : string.Empty;
	}
	[return: NotNullIfNotNull(nameof(path))]
	public static string? GetUnresolvedPath(this PSCmdlet cmdlet, string? path)
	{
		return !string.IsNullOrEmpty(path) ? cmdlet.GetUnresolvedProviderPathFromPSPath(path) ?? string.Empty : path;
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

	public static void WriteCollection<T>(this Cmdlet cmdlet, IEnumerable<T> collection)
	{
		cmdlet.WriteObject(collection, enumerateCollection: true);
	}

	private delegate ActionPreference ResolveFromBoundValue(object? boundValue, ActionPreference defaultIfNotPresent);
	private static unsafe ActionPreference ResolveActionPreferenceFromPSCmdlet(
		PSCmdlet cmdlet,
		string parameterName,
		string variableName,
		ActionPreference defaultIfNotPresent,
		delegate*<object?, ActionPreference, ActionPreference> resolution)
	{
		object? boundValue = null;

		if (false == cmdlet.MyInvocation?.BoundParameters?.TryGetValue(parameterName, out boundValue)
			&&
			cmdlet.SessionState.PSVariable.TryGetVariableValue(variableName, out ActionPreference variablePref))
		{
			return variablePref;
		}

		return resolution(boundValue, defaultIfNotPresent);
	}

	private static bool ContainsParameterKey(Dictionary<string, object?> dictionary, string key)
	{
		ReadOnlySpan<char> keySpan = TrimProperties(key);
		return dictionary.ContainsKey(keySpan);
	}
	private static ReadOnlySpan<char> TrimProperties(ReadOnlySpan<char> value)
	{
		return value.TryLastIndexOfAndNotEnd('.', out int index)
			? value.UnsafeSlice(index + 1)
			: value;
	}
}