using MG.Api;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sonarr.Api.Enums;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Sonarr.Api.Endpoints
{
    public class Command : ISonarrPostable
    {
        private const string _ep = "/api/command";
        public string Value { get; }
        public IDictionary Parameters { get; }

        public Uri RelativeEndpoint => new Uri(_ep, UriKind.Relative);
        public SonarrMethod[] MethodsAllowed =>
            new SonarrMethod[2] { SonarrMethod.GET, SonarrMethod.POST };

        public SonarrMethod UsingMethod { get; }

        public Command(SonarrCommand cmd)
        {
            UsingMethod = SonarrMethod.POST;
            Parameters = new Dictionary<string, object>(1)
            {
                { "name", cmd.ToString() }
            };
            Value = _ep;
        }

        public Command(long id)
        {
            UsingMethod = SonarrMethod.GET;
            Value = _ep + "/" + id;
        }

        public string GetPostBody() => JsonConvert.SerializeObject(Parameters);

        public override string ToString() => this.Value;

        public static implicit operator string(Command cmd) => cmd.ToString();
    }
}
