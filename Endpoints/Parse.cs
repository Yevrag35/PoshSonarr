using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sonarr.Api.Enums;

namespace Sonarr.Api.Endpoints
{
    public class Parse : ISonarrEndpoint
    {
        private protected const string _ep = "/api/parse";
        public Uri RelativeEndpoint => new Uri(_ep, UriKind.Relative);
        public string Value => throw new NotImplementedException();

        public SonarrMethod[] MethodsAllowed =>
            typeof(SonarrMethod).GetEnumValues().Cast<SonarrMethod>().ToArray();    // Allow all methods

        #region Constructors
        public Parse() { }


        #endregion



        public IEnumerator<string> GetEnumerator() => throw new NotImplementedException();
        IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
    }
}
