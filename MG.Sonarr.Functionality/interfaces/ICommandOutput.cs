using MG.Api.Json;
using System;

namespace MG.Sonarr.Functionality
{
    /// <summary>
    /// An interface representing properties from the output of a command sent to Sonarr.
    /// </summary>
    public interface ICommandOutput : IJsonObject
    {
        /// <summary>
        /// The name of the command that was executed.
        /// </summary>
        string Command { get; }
        /// <summary>
        /// The unique job ID.
        /// </summary>
        long Id { get; }
        /// <summary>
        /// The overall status of the command.
        /// </summary>
        string Status { get; }
        /// <summary>
        /// The time that the command had begun.
        /// </summary>
        DateTimeOffset? Started { get; }
    }
}