using System;
using System.Collections.Generic;

namespace MG.Sonarr.Functionality
{
    /// <summary>
    /// An interface that exposes properties that support tag-updating operations.
    /// </summary>
    public interface ISupportsTagUpdate : IGetEndpoint, IJsonResult
    {
        object Id { get; }
        HashSet<int> Tags { get; set; }
    }
}
