using Sonarr.Api.Enums;
using System;

namespace Sonarr.Api.Endpoints
{
    public class Diskspace : ISonarrEndpoint
    {
        private const string _ep = "/api/diskspace";
        public Uri RelativeEndpoint => new Uri(_ep, UriKind.Relative);

        public SonarrMethod[] MethodsAllowed => new SonarrMethod[1] { SonarrMethod.GET };

        public string Value { get; }

        public Diskspace() => Value = _ep;

        public override string ToString() => this.Value;

        public static implicit operator string(Diskspace ds) => ds.ToString();
    }
}
