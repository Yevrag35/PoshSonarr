using System.Net;

namespace MG.Sonarr.Next.Services.Http.Requests
{
    internal sealed class AuthedRequestMessage : SonarrRequestMessage
    {
        const string LOGIN_PART = "/login?returnUrl=/";
        const string PASSWORD = "password";
        const string USERNAME = "username";
        static readonly NetworkCredential _blankCreds = new();

        public NetworkCredential Credentials { get; }
        public bool IsCredentialsBlank => string.IsNullOrWhiteSpace(this.Credentials.UserName)
                                          ||
                                          string.IsNullOrWhiteSpace(this.Credentials.Password);
        public override bool IsTest => false;
        public override bool CanUseCookieAuthentication => true;

        public AuthedRequestMessage(HttpMethod method, string requestUri, NetworkCredential? credentials)
            : base(method, requestUri)
        {
            this.Credentials = credentials ?? _blankCreds;
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

            return new FormUrlEncodedContent(GetFormContents(this.Credentials));
        }

        private static IEnumerable<KeyValuePair<string, string>> GetFormContents(NetworkCredential credential)
        {
            yield return new KeyValuePair<string, string>(USERNAME, credential.UserName);
            yield return new KeyValuePair<string, string>(PASSWORD, credential.Password);
        }
    }
}
