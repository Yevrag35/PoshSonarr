using Newtonsoft.Json;
using Sonarr.Api.Enums;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Sonarr.Api.Endpoints
{
    public class EpisodeFile : ISonarrPostable
    {
        private const string _ep = "/api/episodefile";
        public IDictionary Parameters { get; }

        public Uri RelativeEndpoint => new Uri("/api/episodefile");

        public SonarrMethod[] MethodsAllowed => new SonarrMethod[3]
        {
            SonarrMethod.GET, SonarrMethod.PUT, SonarrMethod.DELETE
        };

        public SonarrMethod UsingMethod { get; }

        public string Value { get; }

        public EpisodeFile(long id, bool isEpisodeId)
        {
            var ending = isEpisodeId ? "/" + Convert.ToString(id) : "?seriesId=" + Convert.ToString(id);
            UsingMethod = SonarrMethod.GET;
            Value = _ep + ending;
        }
        public EpisodeFile(long id)
        {
            UsingMethod = SonarrMethod.DELETE;
            Value = _ep + "/" + Convert.ToString(id);
        }
        public EpisodeFile(long id, IDictionary parameters)
        {
            UsingMethod = SonarrMethod.PUT;
            Value = _ep + "/" + Convert.ToString(id);
            Parameters = parameters;
        }

        public string GetPostBody() => JsonConvert.SerializeObject(Parameters);
    }
}
