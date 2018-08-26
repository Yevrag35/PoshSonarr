using Sonarr.Api.Enums;
using Sonarr.Api.Generic;
using System;
using System.Collections.Generic;

namespace Sonarr.Api.Endpoints
{
    public class Calendar : ValidatedString, ISonarrEndpoint
    {
        private protected const string _ep = "/api/calendar";
        private readonly string _full;
        internal override string Value => _full;
        public Uri RelativeEndpoint => new Uri(_ep, UriKind.Relative);
        public SonarrMethod[] MethodsAllowed => new SonarrMethod[1] { SonarrMethod.GET };
        public string FullString => Value ?? null;

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
                _full = _ep;
            }
            else
            {
                var str = string.Join("&", list.ToArray());
                _full = _ep + "?" + str;
            }
        }

        public static implicit operator string(Calendar cal) => cal.ToString();
    }
}
