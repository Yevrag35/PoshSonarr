using System;

namespace MG.Sonarr
{
    public interface ISonarrUrl
    {
        string BaseUrl { get; }
        bool IncludeApiPrefix { get; set; }
        string Path { get; set; }
        Uri Url { get; }
    }
}
