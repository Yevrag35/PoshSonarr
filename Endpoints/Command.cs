﻿using MG.Api;
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
        private protected const string _ep = "/api/command";
        private readonly string _full;
        public string Value => _full;
        public Dictionary<string, string> Parameters { get; }

        public Uri RelativeEndpoint => new Uri(_ep, UriKind.Relative);
        public SonarrMethod[] MethodsAllowed =>
            new SonarrMethod[2] { SonarrMethod.GET, SonarrMethod.POST };

        public Command(SonarrCommand cmd)
        {
            Parameters = new Dictionary<string, string>(1)
            {
                { "name", cmd.ToString() }
            };
            _full = _ep;
        }

        public Command(long id) => _full = _ep + "/" + id;

        public string GetPostBody() => JsonConvert.SerializeObject(Parameters);

        //IEnumerator<string> IEnumerable<string>.GetEnumerator() => new List<string>(1) { this.Value }.GetEnumerator();
        //IEnumerator IEnumerable.GetEnumerator() => new List<string>(1) { this.Value }.GetEnumerator();
    }
}
