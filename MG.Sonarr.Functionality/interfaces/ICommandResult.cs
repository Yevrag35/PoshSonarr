using System;

namespace MG.Sonarr.Functionality
{
    /// <summary>
    /// An interface representing the result of a command after querying its status.
    /// </summary>
    public interface ICommandResult : ICommandOutput
    {
        /// <summary>
        /// The time the command's execution completed.
        /// </summary>
        DateTimeOffset? Ended { get; }
    }
}
