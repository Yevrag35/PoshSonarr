using System;

namespace MG.Sonarr.Functionality
{
    /// <summary>
    /// An interface exposing properties used for constructing a <see cref="Uri"/> connecting to a Sonarr instance.
    /// </summary>
    public interface ISonarrUrl
    {
        /// <summary>
        /// The <see cref="Uri"/> base of the Sonarr instance.
        /// </summary>
        string BaseUrl { get; }
        /// <summary>
        /// Indicates whether to include the "/api" prefix to the <see cref="Uri.PathAndQuery"/>.
        /// </summary>
        bool IncludeApiPrefix { get; set; }
        /// <summary>
        /// The path.
        /// </summary>
        string Path { get; set; }
        /// <summary>
        /// The resulting constructed <see cref="Uri"/> that points to the Sonarr instance.
        /// </summary>
        Uri Url { get; }
    }
}
