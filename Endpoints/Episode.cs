using MG.Api;
using Sonarr.Api.Enums;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Sonarr.Api.Endpoints
{
    public class Episode : ISonarrEndpoint
    {
        private const string _ep = "/api/episode";
        private readonly string _full;

        public string Value => _full;

        public Uri RelativeEndpoint => new Uri(_ep, UriKind.Relative);
        public SonarrMethod[] MethodsAllowed => new SonarrMethod[2] { SonarrMethod.GET, SonarrMethod.PUT };

        public Episode() => _full = _ep;

        public Episode(long id, bool isEpisodeId)
        {
            var ending = isEpisodeId ? "/" + Convert.ToString(id) : "?seriesId=" + Convert.ToString(id);
            _full = _ep + ending;
        }

        public static implicit operator string(Episode ep) => ep.Value;

        IEnumerator<string> IEnumerable<string>.GetEnumerator() => new List<string>(1) { this.Value }.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => new List<string>(1) { this.Value }.GetEnumerator();
    }
}
