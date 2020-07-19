using MG.Sonarr.Results;
using System;

namespace MG.Sonarr.Functionality
{
    public interface IPastJob
    {
        string Command { get; }
        DateTimeOffset? Ended { get; }
        long Id { get; }
        DateTimeOffset Started { get; }
        CommandStatus Status { get; }
    }
}
