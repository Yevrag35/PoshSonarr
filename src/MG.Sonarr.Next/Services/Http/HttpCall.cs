namespace MG.Sonarr.Next.Services.Http
{
    public sealed class HttpCall
    {
        [MemberNotNullWhen(true, nameof(Response))]
        public bool HasResponse { get; }
        public HttpMethod Method { get; }
        public HttpRequestMessage Request { get; }
        public string RequestUri { get; }
        public HttpResponseMessage? Response { get; }

        public HttpCall(string path, HttpRequestMessage request, HttpResponseMessage? response)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);
            ArgumentNullException.ThrowIfNull(request);

            this.Method = request.Method;
            this.Request = request;
            this.RequestUri = request.RequestUri?.ToString() ?? path;
            this.HasResponse = response is not null;
            this.Response = response;
        }
    }
}
