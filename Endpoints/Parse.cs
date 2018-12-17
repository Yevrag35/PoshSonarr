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
        private const string _ep = "/api/parse";
        public Uri RelativeEndpoint => new Uri(_ep, UriKind.Relative);
        public string Value { get; }

        public SonarrMethod[] MethodsAllowed =>
            typeof(SonarrMethod).GetEnumValues().Cast<SonarrMethod>().ToArray();    // Allow all methods

        #region CONSTRUCTORS

        public Parse() { }

        #endregion

        public override string ToString() => this.Value;

        public static implicit operator string(Parse p) => p.ToString();
    }
}
