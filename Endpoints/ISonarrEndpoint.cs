using MG.Api;
using Sonarr.Api.Enums;
using System;

namespace Sonarr.Api.Endpoints
{
    public interface ISonarrEndpoint
    {
        Uri RelativeEndpoint { get; }
        SonarrMethod[] MethodsAllowed { get; }

        string Value { get; }
    }
}
