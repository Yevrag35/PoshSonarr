using MG.Sonarr.Next.Generators;

namespace MG.Sonarr.Next.Models.Commands;

[GenerateEnumToString]
public enum CommandPriority
{
	Normal = 0,
	Low = 1,
	High = 2,
}
