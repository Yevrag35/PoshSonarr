using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sonarr.Api.Enums;

namespace Sonarr.Api.Endpoints
{
    public class Series : ISonarrEndpoint
    {
        private protected const string _ep = "/api/series";
        private readonly string _full;

        public Uri RelativeEndpoint => new Uri(_ep, UriKind.Relative);
        public string Value => _full;

        public SonarrMethod[] MethodsAllowed =>
            typeof(SonarrMethod).GetEnumValues().Cast<SonarrMethod>().ToArray();    // Allow all methods

        #region Constructors
        public Series(int? seriesId)
        {
            if (seriesId.HasValue)
                _full = _ep + "/" + Convert.ToString(seriesId.Value);

            else
                _full = _ep;
        }

        #endregion

        #region Methods

        public IEnumerator<string> GetEnumerator() => new List<string>(1) { this.Value }.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => new List<string>(1) { this.Value }.GetEnumerator();

        #endregion
    }
}
