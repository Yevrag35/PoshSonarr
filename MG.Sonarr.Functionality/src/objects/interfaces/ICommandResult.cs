using System;

namespace MG.Sonarr.Functionality
{
    public interface ICommandResult : ICommandOutput
    {
        DateTimeOffset? Ended { get; }
    }
}
