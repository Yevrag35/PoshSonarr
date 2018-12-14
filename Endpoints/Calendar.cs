using Sonarr.Api.Enums;
using System;
using System.Collections.Generic;

namespace Sonarr.Api.Endpoints
{
    public class Calendar : ISonarrEndpoint
    {
        private protected const string _ep = "/api/calendar";
        private readonly string _full;
        public string Value => _full;
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
                _full = _ep;
            }
            else
            {
                var str = string.Join("&", list.ToArray());
                _full = _ep + "?" + str;
            }
        }

        public static implicit operator string(Calendar cal) => cal.Value;

        //IEnumerator<string> IEnumerable<string>.GetEnumerator() => new List<string>(1) { this.Value }.GetEnumerator();
        //IEnumerator IEnumerable.GetEnumerator() => new List<string>(1) { this.Value }.GetEnumerator();
    }
}
