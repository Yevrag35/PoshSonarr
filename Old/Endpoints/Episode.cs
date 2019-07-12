using Sonarr.Api.Enums;
using System;

namespace Sonarr.Api.Endpoints
{
    public class Episode : ISonarrEndpoint
    {
        private const string _ep = "/api/episode";

        public string Value { get; }

        public Uri RelativeEndpoint => new Uri(_ep, UriKind.Relative);
        public SonarrMethod[] MethodsAllowed => new SonarrMethod[2] { SonarrMethod.GET, SonarrMethod.PUT };

        public Episode() => Value = _ep;

        public Episode(long id, bool isEpisodeId)
        {
            var ending = isEpisodeId ? 
                "/" + Convert.ToString(id) : 
                "?seriesId=" + Convert.ToString(id);

            Value = _ep + ending;
        }

        public override string ToString() => this.Value;

        public static implicit operator string(Episode ep) => ep.ToString();
    }
}
