using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sonarr.Api.Enums;

namespace Sonarr.Api.Endpoints
{
    public class Series : ISonarrEndpoint
    {
        private protected const string _ep = "/api/series";
        public Uri RelativeEndpoint => new Uri(_ep, UriKind.Relative);

        public SonarrMethod[] MethodsAllowed =>
            typeof(SonarrMethod).GetEnumValues().Cast<SonarrMethod>().ToArray();    // Allow all methods

        public string Value => throw new NotImplementedException();

        public IEnumerator<string> GetEnumerator() => throw new NotImplementedException();
        IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
    }
}
