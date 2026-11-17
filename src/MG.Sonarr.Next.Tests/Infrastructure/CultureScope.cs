namespace MG.Sonarr.Next.Tests.Infrastructure;

/// <summary>
/// Sets the current culture until the scope is disposed, which puts the earlier one back.
/// </summary>
/// <remarks>
/// <see cref="CultureInfo.CurrentCulture"/> flows with the execution context, so a scope set in a test reaches the
/// tasks and threads that the test starts, and no other test.
/// </remarks>
internal sealed class CultureScope : IDisposable
{
	/// <summary>
	/// The cultures that broke the module's reading and writing of text, and the two that a session most often runs
	/// under.
	/// </summary>
	/// <remarks>
	/// <list type="bullet">
	/// <item>The invariant culture, written as empty text, and en-US.</item>
	/// <item>tr-TR, whose <c>i</c> uppercases to a dotted <c>İ</c> (U+0130).</item>
	/// <item>sv-SE and fi-FI, whose negative sign is U+2212, and fi-FI's time separator is a period.</item>
	/// <item>de-DE and fr-FR, whose decimal separator is a comma.</item>
	/// <item>th-TH, whose calendar is the Buddhist one, 543 years ahead of the Gregorian.</item>
	/// <item>ar-SA, whose signs carry a directional mark and whose calendar is the Hijri one.</item>
	/// </list>
	/// </remarks>
	internal static readonly string[] Names = ["", "en-US", "tr-TR", "sv-SE", "fi-FI", "de-DE", "fr-FR", "th-TH", "ar-SA"];

	private readonly CultureInfo _previous;

	private CultureScope(CultureInfo culture)
	{
		_previous = CultureInfo.CurrentCulture;
		CultureInfo.CurrentCulture = culture;
	}

	/// <summary>
	/// Makes the culture with the specified name the current culture until the returned scope is disposed.
	/// </summary>
	/// <param name="name">The culture's name, or empty text for the invariant culture.</param>
	/// <returns>The scope, which puts the earlier culture back when it's disposed.</returns>
	internal static CultureScope Set(string name)
	{
		return new(CultureInfo.GetCultureInfo(name));
	}

	/// <summary>
	/// Puts back the culture that was current when the scope was set.
	/// </summary>
	public void Dispose()
	{
		CultureInfo.CurrentCulture = _previous;
	}
}
