using MG.Api;
using Sonarr.Api.Enums;
using System;

namespace Sonarr.Api.Endpoints
{
    public interface ISonarrEndpoint : IApiString
    {
        Uri RelativeEndpoint { get; }
        SonarrMethod[] MethodsAllowed { get; }
    }
}
