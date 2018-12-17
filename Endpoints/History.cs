using Sonarr.Api.Components;
using Sonarr.Api.Enums;
using System;

namespace Sonarr.Api.Endpoints
{
    public class History : ISonarrEndpoint
    {
        private const string _ep = "/api/history";
        private Resolver _res;

        public Uri RelativeEndpoint => new Uri(_ep, UriKind.Relative);
        public SonarrMethod[] MethodsAllowed => new SonarrMethod[1] { SonarrMethod.GET };
        public string Value { get; }

        private History() => _res = new Resolver();

        public History(HistorySortKey hsk, SortDirection direction, int page, int pageSize)
            : this()
        {
            var strs = new string[4]
            {
                "sortKey=" + _res.GetNameAttribute(hsk),
                "page=" + Convert.ToString(page),
                "pageSize=" + Convert.ToString(pageSize),
                "sortDir=" + _res.GetNameAttribute(direction)
            };
            Value = MashTogether(strs);
        }

        public History(HistorySortKey hsk, long episodeId)
            : this()
        {
            var strs = new string[2]
            {
                "sortKey=" + _res.GetNameAttribute(hsk),
                "episodeId=" + Convert.ToString(episodeId)
            };
            Value = MashTogether(strs);
        }

        private string MashTogether(params string[] inStrs) =>
            _ep + "?" + string.Join("&", inStrs);

        public override string ToString() => this.Value;

        public static implicit operator string(History hstry) => hstry.ToString();
    }
}
