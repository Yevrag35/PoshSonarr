using MG.Sonarr.Next.Services.Auth;
using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Services.Http.Requests;
using Microsoft.Extensions.Caching.Memory;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace MG.Sonarr.Next.Services.Http.Handlers
{
    public sealed class AuthHandler : DelegatingHandler
    {
        public static readonly HttpRequestOptionsKey<NetworkCredential?> CredentialKey = new("Credentials");

        SonarrAuthType AuthType { get; }
        IMemoryCache Cache { get; }

        public AuthHandler(IConnectionSettings settings, IMemoryCache cache)
        {
            this.AuthType = settings.AuthType;
            this.Cache = cache;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            switch (this.AuthType)
            {
                case SonarrAuthType.None:
                case SonarrAuthType.External:
                    return base.SendAsync(request, cancellationToken);

                case SonarrAuthType.Basic:
                    return this.DoBasicAuthProcedure(request, cancellationToken);

                case SonarrAuthType.Forms:
                    return this.DoFormsAuthProcedure(request, cancellationToken);

                default:
                    goto case SonarrAuthType.None;
            }
        }

        private static bool TryGetAsAuthedRequest(HttpRequestMessage request, [NotNullWhen(true)] out AuthedRequestMessage? result)
        {
            result = null;
            if (request is not AuthedRequestMessage authedMsg || authedMsg.IsCredentialsBlank)
            {
                return false;
            }

            result = authedMsg;
            return true;
        }

        #region BASIC AUTH
        const string AUTHORIZATION = "Authorization";

        private Task<HttpResponseMessage> DoBasicAuthProcedure(HttpRequestMessage request, CancellationToken token)
        {
            if (!TryGetAsAuthedRequest(request, out AuthedRequestMessage? authedMsg))
            {
                throw new SonarrHttpException(request, null, ErrorCollection.Empty,
                    new UnauthorizedAccessException("The Sonarr server requires Basic Authentication and no credentials were provided."));
            }

            AddBasicToRequest(authedMsg);
            return base.SendAsync(authedMsg, token);
        }

        private static void AddBasicToRequest(AuthedRequestMessage request)
        {
            NetworkCredential creds = request.Credentials;
            string coupled = string.Create(creds.UserName.Length + creds.Password.Length + 1, creds,
                (chars, state) =>
            {
                int position = 0;
                state.UserName.CopyToSlice(chars, ref position);
                chars[position++] = ':';
                state.Password.CopyTo(chars.Slice(position));
            });

            string base64Auth = Convert.ToBase64String(Encoding.UTF8.GetBytes(coupled));
            request.Headers.Authorization = new AuthenticationHeaderValue(AUTHORIZATION, base64Auth);
        }

        #endregion

        #region FORMS AUTH
        const string SONARR_COOKIE = "_SonarrCookie";

        private async Task<HttpResponseMessage> DoFormsAuthProcedure(HttpRequestMessage request, CancellationToken token)
        {
            if (!TryGetAsAuthedRequest(request, out AuthedRequestMessage? authedMsg))
            {
                throw new SonarrHttpException(request, null, ErrorCollection.Empty,
                    new UnauthorizedAccessException("The Sonarr server requires Forms-based authentication and no credentials were provided."));
            }

            request = await this.ProcessFormsLogin(authedMsg, token);
            return await base.SendAsync(request, token);
        }

        private async Task<HttpRequestMessage> ProcessFormsLogin(AuthedRequestMessage request, CancellationToken token)
        {
            if (!TryGetCookieFromCache(this.Cache, out string? cookie))
            {
                cookie = await this.PerformLogin(request, token);
                if (string.IsNullOrWhiteSpace(cookie))
                {
                    throw new Exception("shit");
                }
            }

            request.Headers.Add("Cookie", cookie);
            return request;
        }
        private async Task<string> PerformLogin(AuthedRequestMessage original, CancellationToken token)
        {
            Uri loginUrl = original.GetLoginUrl();
            ApiKeyRequestMessage request = new(HttpMethod.Post, loginUrl);
            try
            {
                request.Content = original.GetLoginFormContent();
            }
            catch (UnauthorizedAccessException e)
            {
                throw new SonarrHttpException(original, null, ErrorCollection.Empty, e);
            }

            using HttpResponseMessage response = await base.SendAsync(request, token);

            if (request.RequestUri!.ToString().Contains("loginFailed=true", StringComparison.OrdinalIgnoreCase))
            {
                response.StatusCode = HttpStatusCode.Unauthorized;
                response.ReasonPhrase = response.StatusCode.ToString();
                throw new SonarrHttpException(request, response, ErrorCollection.Empty,
                    new UnauthorizedAccessException("The username or password is incorrect."));
            }

            if ((response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.SeeOther)
                &&
                response.Headers.TryGetValues("Set-Cookie", out var values))
            {
                return StoreCookieInCache(this.Cache, values.FirstOrDefault() ?? string.Empty);
            }

            return string.Empty;
        }

        private static string StoreCookieInCache(IMemoryCache cache, string cookie)
        {
            return cache.Set(SONARR_COOKIE, cookie, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1d),
                Priority = CacheItemPriority.Low,
                Size = 1L,
            });
        }
        private static bool TryGetCookieFromCache(IMemoryCache cache, [NotNullWhen(true)] out string? cookie)
        {
            return cache.TryGetValue(SONARR_COOKIE, out cookie);
        }

        #endregion
    }
}
