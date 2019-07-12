using Sonarr.Api.Components;
using Sonarr.Api.Enums;
using System;

namespace Sonarr.Api.Endpoints
{
    public class WantedMissing : ISonarrEndpoint
    {
        private const string _ep = "/api/wanted/missing";

        private Resolver _res;

        public Uri RelativeEndpoint => new Uri(_ep, UriKind.Relative);

        public SonarrMethod[] MethodsAllowed => new SonarrMethod[1] { SonarrMethod.GET };

        public string Value { get; }

        public WantedMissing(WantedMissingSortKey sortKey, SortDirection direction, int page = 1, int pageSize = 10)
        {
            _res = new Resolver();
            var strs = new string[4]
            {
                "sortKey=" + _res.GetNameAttribute(sortKey),
                "page=" + Convert.ToString(page),
                "pageSize=" + Convert.ToString(pageSize),
                "sortDir=" + _res.GetNameAttribute(direction)
            };
            var str = string.Join("&", strs);
            Value = _ep + "?" + str;
        }

        public override string ToString() => this.Value;

        public static implicit operator string(WantedMissing wm) => wm.ToString();
    }
}
