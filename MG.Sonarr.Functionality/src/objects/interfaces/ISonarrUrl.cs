using System;

namespace MG.Sonarr
{
    /// <summary>
    /// An interface exposing properties used for constructing a <see cref="Uri"/> connecting to a Sonarr instance.
    /// </summary>
    public interface ISonarrUrl
    {
        string BaseUrl { get; }
        bool IncludeApiPrefix { get; set; }
        string Path { get; set; }
        Uri Url { get; }
    }
}
