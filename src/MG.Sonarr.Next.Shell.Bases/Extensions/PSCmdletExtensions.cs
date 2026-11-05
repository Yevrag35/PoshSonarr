using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Extensions.Strings;
using MG.Sonarr.Next.Strings;
using System.Collections.Frozen;
using System.Runtime.CompilerServices;

namespace MG.Sonarr.Next.Shell.Extensions;

/// <summary>
/// Provides extension methods for cmdlets that read preferences and bound parameters, resolve paths, and write
/// collections and the outcomes of API calls.
/// </summary>
/// <remarks>
/// Call the methods that write to the cmdlet's streams on its pipeline thread, since PowerShell allows
/// <see cref="Cmdlet.WriteObject(object)"/> and <see cref="Cmdlet.WriteError(ErrorRecord)"/> only there.
/// </remarks>
public static partial class PSCmdletExtensions
{
	/// <summary>
	/// The members of <see cref="ActionPreference"/>, keyed by name without regard to case.
	/// </summary>
	private static readonly FrozenDictionary<string, ActionPreference> s_preferences;
	/// <summary>
	/// The values that <see cref="ActionPreference"/> defines.
	/// </summary>
	private static readonly FrozenSet<ActionPreference> s_prefSet;

	/// <summary>
	/// Initializes the lookups of <see cref="ActionPreference"/> names and values.
	/// </summary>
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

	/// <summary>
	/// Gets the preference that a common parameter such as <c>-ErrorAction</c> sets for the cmdlet, or else the
	/// preference variable that the cmdlet sees.
	/// </summary>
	/// <remarks>
	/// <para>
	/// When <paramref name="parameterName"/> is bound, as the caller's argument or a value from
	/// <c>$PSDefaultParameterValues</c>, its value is used: an <see cref="ActionPreference"/>, a number, or a name,
	/// compared without regard to case. A bound value that is none of those, or that names no member, gives
	/// <paramref name="defaultIfNotPresent"/>.
	/// </para>
	/// <para>
	/// When it isn't bound, the method reads <paramref name="variableName"/> from the cmdlet's scope and converts its
	/// value as PowerShell does, so a string that a function or a script assigned counts as well as an
	/// <see cref="ActionPreference"/>. A variable that doesn't exist, or doesn't convert, gives
	/// <paramref name="defaultIfNotPresent"/>.
	/// </para>
	/// </remarks>
	/// <param name="cmdlet">The cmdlet whose bound parameters and scope are read.</param>
	/// <param name="parameterName">The name of the common parameter, such as <c>ErrorAction</c>, without the
	/// <c>-</c>.</param>
	/// <param name="variableName">The name of the preference variable, such as <c>ErrorActionPreference</c>, without the
	/// <c>$</c>.</param>
	/// <param name="defaultIfNotPresent">The preference to return when neither the parameter nor the variable gives
	/// one.</param>
	/// <returns>The preference that applies to <paramref name="cmdlet"/>.</returns>
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

	/// <summary>
	/// Gets the preference that a common switch such as <c>-Verbose</c> or <c>-Debug</c> sets for the cmdlet, or else the
	/// preference variable that the cmdlet sees.
	/// </summary>
	/// <remarks>
	/// <para>
	/// When <paramref name="parameterName"/> is bound and set, as with <c>-Verbose</c>, the result is
	/// <see cref="ActionPreference.Continue"/>. When it's bound but not set, as with <c>-Verbose:$false</c>, the result is
	/// <paramref name="defaultIfNotPresent"/>, whatever the variable holds.
	/// </para>
	/// <para>
	/// When it isn't bound, the method reads <paramref name="variableName"/> from the cmdlet's scope and converts its
	/// value as PowerShell does, so a string that a function or a script assigned counts as well as an
	/// <see cref="ActionPreference"/>. A variable that doesn't exist, or doesn't convert, gives
	/// <paramref name="defaultIfNotPresent"/>.
	/// </para>
	/// </remarks>
	/// <param name="cmdlet">The cmdlet whose bound parameters and scope are read.</param>
	/// <param name="parameterName">The name of the common switch, such as <c>Verbose</c>, without the <c>-</c>.</param>
	/// <param name="variableName">The name of the preference variable, such as <c>VerbosePreference</c>, without the
	/// <c>$</c>.</param>
	/// <param name="defaultIfNotPresent">The preference to return when the switch is turned off, or when neither the
	/// switch nor the variable gives one.</param>
	/// <returns>The preference that applies to <paramref name="cmdlet"/>.</returns>
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

	/// <summary>
	/// Resolves the specified PowerShell path to the provider path of an item that exists.
	/// </summary>
	/// <remarks>
	/// The path is resolved against the cmdlet's current location, and wildcards in it are expanded. When it resolves to
	/// several items, the method returns the first one's path.
	/// </remarks>
	/// <param name="cmdlet">The cmdlet whose session state resolves the path.</param>
	/// <param name="path">The PowerShell path to resolve, which can be relative, hold wildcards, or be
	/// <see langword="null"/>.</param>
	/// <returns>The provider path of the first item that <paramref name="path"/> resolves to, or
	/// <see cref="string.Empty"/> when it resolves to none; or <paramref name="path"/> itself when it's
	/// <see langword="null"/> or empty.</returns>
	/// <exception cref="ItemNotFoundException">Thrown when <paramref name="path"/> names an item that doesn't
	/// exist.</exception>
	/// <exception cref="System.Management.Automation.DriveNotFoundException">Thrown when <paramref name="path"/> names a
	/// drive that doesn't exist.</exception>
	/// <exception cref="ProviderNotFoundException">Thrown when <paramref name="path"/> names a provider that isn't
	/// loaded.</exception>
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
	/// <summary>
	/// Converts the specified PowerShell path to a provider path, whether or not the item it names exists.
	/// </summary>
	/// <remarks>
	/// The path is resolved against the cmdlet's current location, and wildcards in it aren't expanded, so the method
	/// suits a file that a cmdlet is about to create.
	/// </remarks>
	/// <param name="cmdlet">The cmdlet whose session state converts the path.</param>
	/// <param name="path">The PowerShell path to convert, which can be relative or be <see langword="null"/>.</param>
	/// <returns>The provider path that <paramref name="path"/> names; or <paramref name="path"/> itself when it's
	/// <see langword="null"/> or empty.</returns>
	/// <exception cref="System.Management.Automation.DriveNotFoundException">Thrown when <paramref name="path"/> names a
	/// drive that doesn't exist.</exception>
	/// <exception cref="ProviderNotFoundException">Thrown when <paramref name="path"/> names a provider that isn't
	/// loaded.</exception>
	[return: NotNullIfNotNull(nameof(path))]
	public static string? GetUnresolvedPath(this PSCmdlet cmdlet, string? path)
	{
		return !string.IsNullOrEmpty(path) ? cmdlet.GetUnresolvedProviderPathFromPSPath(path) ?? string.Empty : path;
	}

	/// <summary>
	/// Determines whether the parameter that the specified value-type property backs is bound for the cmdlet.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Pass the parameter's property itself, as in <c>this.HasParameter(this.PageSize)</c>. The compiler supplies
	/// <paramref name="parameterName"/> from the text of that argument, and the method looks up the part after its last
	/// <c>.</c> in the cmdlet's bound parameters, without regard to case. An argument such as
	/// <c>this.PageSize.Value</c> would look up <c>Value</c> instead.
	/// </para>
	/// <para>
	/// The method doesn't read <paramref name="value"/>, so a parameter that's bound to its type's default value still
	/// counts as bound. This overload takes a value type without boxing it.
	/// </para>
	/// </remarks>
	/// <typeparam name="TValue">The type of the parameter's property.</typeparam>
	/// <param name="cmdlet">The cmdlet whose bound parameters are read.</param>
	/// <param name="value">The parameter's property, whose expression names the parameter.</param>
	/// <param name="parameterName">The parameter's name, or an expression that ends in it. Leave it out to have the
	/// compiler supply it from <paramref name="value"/>.</param>
	/// <returns><see langword="true"/> if the parameter is bound; otherwise, <see langword="false"/>.</returns>
	public static bool HasParameter<TValue>(this PSCmdlet cmdlet, TValue value, [CallerArgumentExpression(nameof(value))] string parameterName = "") where TValue : struct
	{
		return ContainsParameterKey(cmdlet.MyInvocation.BoundParameters, parameterName);
	}
	/// <summary>
	/// Determines whether the parameter that the specified property backs is bound for the cmdlet.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Pass the parameter's property itself, as in <c>this.HasParameter(this.Name)</c>. The compiler supplies
	/// <paramref name="parameterName"/> from the text of that argument, and the method looks up the part after its last
	/// <c>.</c> in the cmdlet's bound parameters, without regard to case. An argument such as
	/// <c>this.InputObject.Length</c> would look up <c>Length</c> instead.
	/// </para>
	/// <para>
	/// The method doesn't read <paramref name="value"/>, so a parameter that's bound to <see langword="null"/> still
	/// counts as bound.
	/// </para>
	/// </remarks>
	/// <param name="cmdlet">The cmdlet whose bound parameters are read.</param>
	/// <param name="value">The parameter's property, whose expression names the parameter.</param>
	/// <param name="parameterName">The parameter's name, or an expression that ends in it. Leave it out to have the
	/// compiler supply it from <paramref name="value"/>.</param>
	/// <returns><see langword="true"/> if the parameter is bound; otherwise, <see langword="false"/>.</returns>
	public static bool HasParameter(this PSCmdlet cmdlet, object? value, [CallerArgumentExpression(nameof(value))] string parameterName = "")
	{
		return ContainsParameterKey(cmdlet.MyInvocation.BoundParameters, parameterName);
	}
	/// <summary>
	/// Determines whether the parameter that the specified property backs is bound for the cmdlet to a value other than
	/// <see langword="null"/>.
	/// </summary>
	/// <remarks>
	/// The parameter's name comes from <paramref name="value"/>'s expression, as it does for
	/// <see cref="HasParameter(PSCmdlet, object?, string)"/>.
	/// </remarks>
	/// <param name="cmdlet">The cmdlet whose bound parameters are read.</param>
	/// <param name="value">The parameter's property, whose expression names the parameter.</param>
	/// <param name="parameterName">The parameter's name, or an expression that ends in it. Leave it out to have the
	/// compiler supply it from <paramref name="value"/>.</param>
	/// <returns><see langword="true"/> if <paramref name="value"/> isn't <see langword="null"/> and the parameter is
	/// bound; otherwise, <see langword="false"/>.</returns>
	public static bool HasNotNullParameter(this PSCmdlet cmdlet, [NotNullWhen(true)] object? value, [CallerArgumentExpression(nameof(value))] string parameterName = "")
	{
		return value is not null && HasParameter(cmdlet, value, parameterName);
	}

	/// <summary>
	/// Determines whether the name of the parameter set that PowerShell resolved for the cmdlet matches the specified
	/// wildcard pattern.
	/// </summary>
	/// <param name="cmdlet">The cmdlet whose parameter set name is tested.</param>
	/// <param name="wildString">The pattern to match the name against, by the rules of
	/// <see cref="Wildcard.IsMatch(string)"/>.</param>
	/// <returns><see langword="true"/> if the parameter set's name matches <paramref name="wildString"/>; otherwise,
	/// <see langword="false"/>.</returns>
	public static bool ParameterSetNameIsLike(this PSCmdlet cmdlet, Wildcard wildString)
	{
		return wildString.IsMatch(cmdlet.ParameterSetName);
	}

	/// <summary>
	/// Writes each element of the specified collection to the output stream as an object of its own.
	/// </summary>
	/// <typeparam name="T">The type of the collection's elements.</typeparam>
	/// <param name="cmdlet">The cmdlet that writes the elements.</param>
	/// <param name="collection">The collection whose elements are written.</param>
	public static void WriteCollection<T>(this Cmdlet cmdlet, IEnumerable<T> collection)
	{
		cmdlet.WriteObject(collection, enumerateCollection: true);
	}

	/// <summary>
	/// Represents a callback that turns a bound parameter's value into an <see cref="ActionPreference"/>.
	/// </summary>
	/// <remarks>
	/// <see cref="ResolveActionPreferenceFromPSCmdlet"/> takes a function pointer of this signature instead, and nothing
	/// uses the delegate type.
	/// </remarks>
	/// <param name="boundValue">The parameter's bound value, or <see langword="null"/> when it isn't bound.</param>
	/// <param name="defaultIfNotPresent">The preference to return when <paramref name="boundValue"/> gives none.</param>
	/// <returns>The preference that <paramref name="boundValue"/> gives, or <paramref name="defaultIfNotPresent"/>.</returns>
	private delegate ActionPreference ResolveFromBoundValue(object? boundValue, ActionPreference defaultIfNotPresent);
	/// <summary>
	/// Gets a preference from a bound common parameter, or else from the preference variable that the cmdlet sees.
	/// </summary>
	/// <remarks>
	/// <para>
	/// When <paramref name="parameterName"/> isn't bound, and <paramref name="variableName"/> exists and converts to an
	/// <see cref="ActionPreference"/>, the method returns the variable's preference. Otherwise it passes the bound value,
	/// or <see langword="null"/> when there's none, to <paramref name="resolution"/>.
	/// </para>
	/// <para><b>Performance:</b> <paramref name="resolution"/> is a function pointer to a static method, so the call
	/// allocates no delegate.</para>
	/// </remarks>
	/// <param name="cmdlet">The cmdlet whose bound parameters and scope are read.</param>
	/// <param name="parameterName">The name of the common parameter, without the <c>-</c>.</param>
	/// <param name="variableName">The name of the preference variable, without the <c>$</c>.</param>
	/// <param name="defaultIfNotPresent">The preference that <paramref name="resolution"/> returns when the value it's
	/// given yields none.</param>
	/// <param name="resolution">The function that turns the bound value into a preference.</param>
	/// <returns>The preference that applies to <paramref name="cmdlet"/>.</returns>
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
			TryGetPreferenceVariable(cmdlet, variableName, out ActionPreference variablePref))
		{
			return variablePref;
		}

		return resolution(boundValue, defaultIfNotPresent);
	}

	/// <summary>
	/// Reads a preference variable from the cmdlet's scope and converts its value to an <see cref="ActionPreference"/>,
	/// as PowerShell does when it applies the preference.
	/// </summary>
	/// <remarks>
	/// At the global scope, a preference variable holds an <see cref="ActionPreference"/>, but a function or a script that
	/// assigns one, as in <c>$VerbosePreference = 'Continue'</c>, creates a local variable that holds the string its author
	/// wrote, so the value is converted as an enum name rather than tested for its type.
	/// </remarks>
	/// <param name="cmdlet">The cmdlet whose session state is read.</param>
	/// <param name="variableName">The name of the preference variable, without the <c>$</c>.</param>
	/// <param name="preference">
	/// When this method returns <see langword="true"/>, the variable's value as an <see cref="ActionPreference"/>.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if the variable exists and its value converts to an <see cref="ActionPreference"/>;
	/// otherwise, <see langword="false"/>.
	/// </returns>
	private static bool TryGetPreferenceVariable(PSCmdlet cmdlet, string variableName, out ActionPreference preference)
	{
		object? value = cmdlet.SessionState.PSVariable.GetValue(variableName);
		if (value is not null && LanguagePrimitives.TryConvertTo(value, CultureInfo.InvariantCulture, out preference))
		{
			return true;
		}

		preference = default;
		return false;
	}

	/// <summary>
	/// Determines whether the specified bound parameters hold the parameter that the specified name or expression
	/// names.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The lookup uses the part of <paramref name="key"/> after its last <c>.</c>, and the dictionary's own comparer.
	/// </para>
	/// <para><b>Performance:</b> The trimmed name is a slice of <paramref name="key"/>, and the dictionary's comparer
	/// looks it up as a span, so the lookup allocates no string.</para>
	/// </remarks>
	/// <param name="dictionary">The cmdlet's bound parameters.</param>
	/// <param name="key">The parameter's name, or an expression such as <c>this.Name</c> that ends in it.</param>
	/// <returns><see langword="true"/> if <paramref name="dictionary"/> holds the parameter; otherwise,
	/// <see langword="false"/>.</returns>
	private static bool ContainsParameterKey(Dictionary<string, object?> dictionary, string key)
	{
		ReadOnlySpan<char> keySpan = TrimProperties(key);
		return dictionary.ContainsKey(keySpan);
	}
	/// <summary>
	/// Returns the part of the specified member access expression after its last <c>.</c>.
	/// </summary>
	/// <remarks>
	/// A value with no <c>.</c>, or one whose last character is the <c>.</c>, comes back whole. The result is a slice of
	/// <paramref name="value"/>, not a copy.
	/// </remarks>
	/// <param name="value">The expression, such as <c>this.Name</c>.</param>
	/// <returns>The last member name in <paramref name="value"/>, such as <c>Name</c>.</returns>
	private static ReadOnlySpan<char> TrimProperties(ReadOnlySpan<char> value)
	{
		return value.TryLastIndexOfAndNotEnd('.', out int index)
			? value.UnsafeSlice(index + 1)
			: value;
	}
}