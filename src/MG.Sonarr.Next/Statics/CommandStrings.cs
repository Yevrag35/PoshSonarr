#pragma warning disable format, IDE0130 // Namespace does not match folder structure
namespace MG.Sonarr.Next;
#pragma warning restore format, IDE0130

/// <summary>
/// Provides Sonarr command name constants used for command submission.
/// </summary>
internal static class CommandStrings
{
	/// <summary>
	/// The Sonarr display command name for an RSS sync command.
	/// </summary>
	internal const string RssSyncCommand = "Rss Sync";

	/// <summary>
	/// The Sonarr command name for file rename operations.
	/// </summary>
	internal const string RenameFiles = "RenameFiles";

	/// <summary>
	/// The Sonarr command name for RSS sync operations.
	/// </summary>
	internal const string RssSync = "RssSync";
}
