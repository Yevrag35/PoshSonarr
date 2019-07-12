using Sonarr.Api.Enums;
using System;

namespace Sonarr.Api.Endpoints
{
    public class SystemBackup : ISonarrEndpoint
    {
        private protected const string _ep = "/api/system/backup";
        public string Value { get; }
        public Uri RelativeEndpoint => new Uri(_ep, UriKind.Relative);
        public SonarrMethod[] MethodsAllowed => new SonarrMethod[1] { SonarrMethod.GET };

        public SystemBackup() => Value = _ep;

        public override string ToString() => this.Value;

        public static implicit operator string(SystemBackup back) => back.ToString();
    }
}
