using MG.Api;
using Sonarr.Api.Enums;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Sonarr.Api.Endpoints
{
    public class Command : ISonarrEndpoint
    {
        private protected const string _ep = "/api/command";
        private readonly string _full;
        //private readonly RequestParameters _rp;
        public string Value => _full;

        //public RequestParameters RequestBody => _rp;

        public Uri RelativeEndpoint => new Uri(_ep, UriKind.Relative);
        public SonarrMethod[] MethodsAllowed =>
            new SonarrMethod[2] { SonarrMethod.GET, SonarrMethod.POST };

        public Command(SonarrCommand cmd, RequestParameters parameters)
        {
            parameters.Add("name", cmd.ToString());
            _rp = parameters;
            _full = _ep;
        }

        public Command(long id) => _full = _ep + "/" + id;

        IEnumerator<string> IEnumerable<string>.GetEnumerator() => new List<string>(1) { this.Value }.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => new List<string>(1) { this.Value }.GetEnumerator();
    }
}
