using System.Net;

namespace MG.Sonarr.Next.Services.Http.Requests
{
    internal sealed class CookieRequestMessage : SonarrRequestMessage
    {
        const string LOGIN_PART = "/login?returnUrl=/";
        const string PASSWORD = "password";
        const string USERNAME = "username";
        static readonly NetworkCredential _blankCreds = new();
        readonly NetworkCredential _creds = _blankCreds;

        public NetworkCredential Credentials
        {
            get => _creds;
            init => _creds = value ?? _blankCreds;
        }
        public bool IsCredentialsBlank => string.IsNullOrWhiteSpace(_creds.UserName)
                                          ||
                                          string.IsNullOrWhiteSpace(_creds.Password);
        public override bool IsTest => false;
        public override bool UseCookieAuthentication => true;

        public CookieRequestMessage(string requestUri)
            : base(HttpMethod.Get, requestUri)
        {
        }

        public Uri GetLoginUrl()
        {
            if (this.RequestUri is null)
            {
                return new Uri(LOGIN_PART, UriKind.Relative);
            }

            string authority = this.RequestUri
                .GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped);

            int length = authority.Length + LOGIN_PART.Length;
            string fullUri = string.Create(length, authority, (chars, state) =>
            {
                state.CopyTo(chars);
                LOGIN_PART.CopyTo(chars.Slice(state.Length));
            });

            return new Uri(fullUri, UriKind.Absolute);
        }

        /// <exception cref="UnauthorizedAccessException"></exception>
        public HttpContent GetLoginFormContent()
        {
            if (this.IsCredentialsBlank)
            {
                throw new UnauthorizedAccessException("The username or password is incorrect.");
            }

            return new FormUrlEncodedContent(GetFormContents(_creds));
        }

        private static IEnumerable<KeyValuePair<string, string>> GetFormContents(NetworkCredential credential)
        {
            yield return new KeyValuePair<string, string>(USERNAME, credential.UserName);
            yield return new KeyValuePair<string, string>(PASSWORD, credential.Password);
        }
    }
}
