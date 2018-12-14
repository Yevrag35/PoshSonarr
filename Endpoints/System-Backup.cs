using Sonarr.Api.Enums;
using System;

namespace Sonarr.Api.Endpoints
{
    public class SystemBackup : ISonarrEndpoint
    {
        private protected const string _ep = "/api/system/backup";
        private readonly string _full;
        public string Value => _full;
        public Uri RelativeEndpoint => new Uri(_ep, UriKind.Relative);
        public SonarrMethod[] MethodsAllowed => new SonarrMethod[1] { SonarrMethod.GET };

        public SystemBackup() => _full = _ep;

        public static implicit operator string(SystemBackup back) => back.Value;

        //IEnumerator<string> IEnumerable<string>.GetEnumerator() => new List<string>(1) { this.Value }.GetEnumerator();
        //IEnumerator IEnumerable.GetEnumerator() => new List<string>(1) { this.Value }.GetEnumerator();
    }
}
