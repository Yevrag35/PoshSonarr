using Sonarr.Api.Enums;
using System;
using System.Collections;

namespace Sonarr.Api.Endpoints
{
    public interface ISonarrPostable : ISonarrEndpoint
    {
        IDictionary Parameters { get; }

        SonarrMethod UsingMethod { get; }

        string GetPostBody();
    }
}
