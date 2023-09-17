using System;

namespace MG.Sonarr.Functionality.Jobs
{
    public interface IPastJob
    {
        string Command { get; }
        DateTimeOffset? Ended { get; }
        long Id { get; }
        DateTimeOffset Started { get; }
        string Status { get; }
    }
}
