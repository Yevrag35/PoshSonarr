using MG.Api.Json;
using System;

namespace MG.Sonarr.Functionality
{
    /// <summary>
    /// An interface that exposing the ID property which ultimately support tag-updating operations.
    /// </summary>
    public interface ISupportsTagUpdate : IGetEndpoint, IHasTagSet, IJsonObject
    {
        /// <summary>
        /// The ID of the object to update.
        /// </summary>
        object Id { get; }
    }
}
