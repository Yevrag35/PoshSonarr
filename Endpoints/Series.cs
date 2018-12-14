using Sonarr.Api.Enums;
using System;
using System.Linq;

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
        public Series(int? seriesId) => _full = seriesId.HasValue ? _ep + "/" + Convert.ToString(seriesId.Value) : _ep;

        #endregion

        #region Methods

        //public IEnumerator<string> GetEnumerator() => new List<string>(1) { this.Value }.GetEnumerator();
        //IEnumerator IEnumerable.GetEnumerator() => new List<string>(1) { this.Value }.GetEnumerator();

        #endregion
    }
}
