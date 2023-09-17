using MG.Sonarr.Next.Services.Auth;

namespace MG.Sonarr.Next.Services.Http
{
    public sealed class OhHandler : DelegatingHandler
    {
        public OhHandler()
            : base()
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            string json = await response.Content.ReadAsStringAsync(cancellationToken);
            Debug.WriteLine(json);

            return response;
        }
    }
}
