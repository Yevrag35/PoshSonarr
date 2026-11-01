using MG.Sonarr.Next.Generators;

namespace MG.Sonarr.Next.Models.Commands;

/// <summary>
/// Specifies the priority that Sonarr gives a command that it queues.
/// </summary>
[GenerateEnumToString]
public enum CommandPriority
{
	/// <summary>
	/// The default priority.
	/// </summary>
	Normal = 0,
	/// <summary>
	/// A priority below the default.
	/// </summary>
	Low = 1,
	/// <summary>
	/// A priority above the default.
	/// </summary>
	High = 2,
}
