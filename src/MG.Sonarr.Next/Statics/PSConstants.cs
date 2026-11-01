#pragma warning disable format, IDE0130 // Namespace does not match folder structure
namespace MG.Sonarr.Next;
#pragma warning restore format, IDE0130

/// <summary>
/// Provides PowerShell parameter set and preference variable constant names.
/// </summary>
internal static class PSConstants
{
	/// <summary>
	/// The parameter set name used when an explicit identifier is supplied.
	/// </summary>
	internal const string PSetExplicitId = "ByExplicitId";

	/// <summary>
	/// The parameter set name used when an object name is supplied.
	/// </summary>
	internal const string PSetByName = "ByName";

	/// <summary>
	/// The parameter set name used for pipeline input.
	/// </summary>
	internal const string PSetPipeline = "ByPipelineInput";

	/// <summary>
	/// The PowerShell debug preference switch name.
	/// </summary>
	internal const string Debug = "Debug";

	/// <summary>
	/// The PowerShell verbose preference switch name.
	/// </summary>
	internal const string Verbose = "Verbose";

	/// <summary>
	/// The PowerShell preference suffix.
	/// </summary>
	internal const string Preference = "Preference";

	/// <summary>
	/// The PowerShell debug preference variable name.
	/// </summary>
	/// <value><c>DebugPreference</c></value>
	internal const string DebugPreference = Debug + Preference;

	/// <summary>
	/// The PowerShell verbose preference variable name.
	/// </summary>
	/// <value><c>VerbosePreference</c></value>
	internal const string VerbosePreference = Verbose + Preference;

	/// <summary>
	/// The PowerShell error action parameter name.
	/// </summary>
	internal const string ErrorAction = "ErrorAction";

	/// <summary>
	/// The PowerShell error action preference variable name.
	/// </summary>
	/// <value><c>ErrorActionPreference</c></value>
	internal const string ErrorActionPreference = ErrorAction + Preference;
}
