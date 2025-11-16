using MG.Sonarr.Next.Models;
using MG.Sonarr.Next.Models.Fields;
using System.Collections.Immutable;

namespace MG.Sonarr.Next.Shell.Output;

public interface IDownloadClientOutput : IHasId, ITagOutput
{
	string ConfigContract { get; }
	bool Enable { get; }
	ImmutableArray<FieldObject> Fields { get; }
	string Implementation { get; }
	string ImplementationName { get; }
	string InfoLink { get; }
	string Name { get; }
	int Priority { get; }
	string Protocol { get; }
	bool RemoveCompletedDownloads { get; }
	bool RemoveFailedDownloads { get; }
}
