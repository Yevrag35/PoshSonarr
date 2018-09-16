using MG.Api;
using Sonarr.Api.Enums;
using System;
using System.Collections;
using System.Collections.Generic;

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

        IEnumerator<string> IEnumerable<string>.GetEnumerator() => new List<string>(1) { this.Value }.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => new List<string>(1) { this.Value }.GetEnumerator();
    }
}
