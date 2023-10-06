using MG.Sonarr.Next.Exceptions;
using MG.Sonarr.Next.Services.Auth;
using MG.Sonarr.Next.Services.Collections;
using MG.Sonarr.Next.Services.Http.Requests;
using MG.Sonarr.Next.Services.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Net;

namespace MG.Sonarr.Next.Services.Http.Handlers
{
    public sealed class CookieHandler : DelegatingHandler
    {
        const string SONARR_COOKIE = "_SonarrCookie";
        const string FORM_TYPE = "application/x-www-form-urlencoded";
        const string USERNAME = "username";
        const string PASSWORD = "password";
        public static readonly HttpRequestOptionsKey<NetworkCredential?> CredentialKey = new("Credentials");
        public static readonly HttpRequestOptionsKey<bool> NoCookie = new("nocookie");

        readonly bool _isForms;
        IMemoryCache Cache { get; }

        public CookieHandler(IConnectionSettings settings, IMemoryCache cache)
        {
            _isForms = settings.AuthType == SonarrAuthType.Forms;
            this.Cache = cache;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!_isForms)
            {
                return base.SendAsync(request, cancellationToken);
            }

            if (request is not CookieRequestMessage cookieMsg || cookieMsg.IsCredentialsBlank)
            {
                throw new SonarrHttpException(request, null, ErrorCollection.Empty,
                    new UnauthorizedAccessException("The Sonarr server requires Forms-based authentication and no credentials were provided."));
            }

            return this.ProcessFormsLogin(cookieMsg, cancellationToken);
        }

        private async Task<HttpResponseMessage> ProcessFormsLogin(CookieRequestMessage request, CancellationToken token)
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
            return await base.SendAsync(request, token);
        }

        private async Task<string> PerformLogin(CookieRequestMessage original, CancellationToken token)
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
        private static IEnumerable<KeyValuePair<string, string>> GetFormContents(NetworkCredential credentials)
        {
            yield return new(USERNAME, credentials.UserName);
            yield return new(PASSWORD, credentials.Password);
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
    }
}
