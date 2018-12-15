using Sonarr.Api.Enums;
using System;

namespace Sonarr.Api.Endpoints
{
    public class SystemStatus : ISonarrEndpoint
    {
        private protected const string _ep = "/api/system/status";
        private readonly string _full;
        public string Value => _full;
        public Uri RelativeEndpoint => new Uri(_ep, UriKind.Relative);
        public SonarrMethod[] MethodsAllowed => new SonarrMethod[1] { SonarrMethod.GET };

        public SystemStatus() => _full = _ep;

        public static implicit operator string(SystemStatus stat) => stat.Value;
    }
}
