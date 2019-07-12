using Sonarr.Api.Enums;
using System;
using System.Linq;

namespace Sonarr.Api.Endpoints
{
    public class Series : ISonarrEndpoint
    {
        private const string _ep = "/api/series";

        public Uri RelativeEndpoint => new Uri(_ep, UriKind.Relative);
        public string Value { get; }

        public SonarrMethod[] MethodsAllowed =>
            typeof(SonarrMethod).GetEnumValues().Cast<SonarrMethod>().ToArray();    // Allow all methods

        #region CONSTRUCTORS

        public Series(int? seriesId) => 
            Value = seriesId.HasValue ? _ep + "/" + Convert.ToString(seriesId.Value) : _ep;

        #endregion

        public override string ToString() => this.Value;

        public static implicit operator string(Series ser) => ser.ToString();
    }
}
