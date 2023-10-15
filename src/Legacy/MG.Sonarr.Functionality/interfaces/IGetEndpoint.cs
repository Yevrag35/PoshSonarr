using System;

namespace MG.Sonarr.Functionality
{
    /// <summary>
    /// An interface that provides a method for retrieving the API Uri endpoint for the implementing class.
    /// </summary>
    public interface IGetEndpoint
    {
        /// <summary>
        /// Gets the Sonarr uri endpoint for the implementing class.
        /// </summary>
        string GetEndpoint();
    }
}
