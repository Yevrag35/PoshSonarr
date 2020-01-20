using System;

namespace MG.Sonarr.Functionality
{
    public interface IGetEndpoint
    {
        /// <summary>
        /// Gets the Sonarr uri endpoint for the implementing class.
        /// </summary>
        string GetEndpoint();
    }
}
