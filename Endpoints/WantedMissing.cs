using MG.Api;
using Sonarr.Api.Components;
using Sonarr.Api.Enums;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Sonarr.Api.Endpoints
{
    public class WantedMissing : ISonarrEndpoint
    {
        private const string _ep = "/api/wanted/missing";
        private readonly string _full;

        private Resolver _res;

        public Uri RelativeEndpoint => new Uri(_ep, UriKind.Relative);

        public SonarrMethod[] MethodsAllowed => new SonarrMethod[1] { SonarrMethod.GET };

        public string Value => _full;

        public WantedMissing(SortKey sortKey, SortDirection direction, int page = 1, int pageSize = 10)
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
            _full = _ep + "?" + str;
        }

        public static implicit operator string(WantedMissing wm) => wm.Value;

        IEnumerator<string> IEnumerable<string>.GetEnumerator() => new List<string>(1) { this.Value }.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => new List<string>(1) { this.Value }.GetEnumerator();
    }
}
