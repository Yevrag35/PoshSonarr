using Sonarr.Api.Enums;
using System;

namespace Sonarr.Api.Endpoints
{
    public class SystemStatus : ISonarrEndpoint
    {
        private const string _ep = "/api/system/status";
        public string Value { get; }
        public Uri RelativeEndpoint => new Uri(_ep, UriKind.Relative);
        public SonarrMethod[] MethodsAllowed => new SonarrMethod[1] { SonarrMethod.GET };

        public SystemStatus() => Value = _ep;

        public override string ToString() => this.Value;

        public static implicit operator string(SystemStatus stat) => stat.ToString();
    }
}
