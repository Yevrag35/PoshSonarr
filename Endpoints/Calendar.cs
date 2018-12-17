using Sonarr.Api.Enums;
using System;
using System.Collections.Generic;

namespace Sonarr.Api.Endpoints
{
    public class Calendar : ISonarrEndpoint
    {
        private const string _ep = "/api/calendar";
        public string Value { get; }
        public Uri RelativeEndpoint => new Uri(_ep, UriKind.Relative);
        public SonarrMethod[] MethodsAllowed => new SonarrMethod[1] { SonarrMethod.GET };

        public Calendar(DateTime? start = null, DateTime? end = null)
        {
            var list = new List<string>();
            if (start.HasValue)
            {
                list.Add("start=" + start.Value.ToString("yyyy-MM-dd"));
            }
            if (end.HasValue)
            {
                list.Add("end=" + end.Value.ToString("yyyy-MM-dd"));
            }
            if (list.Count <= 0)
            {
                Value = _ep;
            }
            else
            {
                var str = string.Join("&", list.ToArray());
                Value = _ep + "?" + str;
            }
        }

        public override string ToString() => this.Value;

        public static implicit operator string(Calendar cal) => cal.ToString();
    }
}
