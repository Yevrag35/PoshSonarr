using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Sonarr.Api
{
    namespace Authentication
    {
        public class AuthHeader : Object
        {
            private const string _hn = "X-Api-Key";
            private ApiKey _key;
            public WebHeaderCollection Headers { get; set; }
            private Uri _url;
            public ApiKey Key
            {
                get { return _key; }
                set { _key = value; }
            }
            public string SonarrUrl
            {
                get { return _url.ToString(); }
            }

            // Constructors
            public AuthHeader(string sonarrUrl, string apiKey)
            {
                _url = new Uri(sonarrUrl);
                _key = new ApiKey(apiKey);

                Headers = new WebHeaderCollection()
                {
                    { _hn, Key.Key }
                };
            }
        }
    }
}
