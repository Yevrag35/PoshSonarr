namespace MG.Sonarr.Next.Services.Http
{
    public readonly struct HttpCall
    {
        readonly HttpMethod? _method;
        readonly string? _path;
        readonly HttpRequestMessage? _request;
        readonly HttpResponseMessage? _response;
        readonly string? _requestUri;
        readonly bool _hasResponse;
        readonly bool _isNotEmpty;

        [MemberNotNullWhen(true, nameof(_response), nameof(Response))]
        public bool HasResponse => _hasResponse;
        [MemberNotNullWhen(false, nameof(_method), nameof(Method), nameof(_request), nameof(Request))]
        public bool IsEmpty => !_isNotEmpty;
        public HttpMethod? Method => _method;
        public HttpRequestMessage? Request => _request;
        public string RequestUri => _requestUri ?? _path ?? string.Empty;
        public HttpResponseMessage? Response => _response;

        public HttpCall(string path, HttpRequestMessage request, HttpResponseMessage? response)
        {
            ArgumentException.ThrowIfNullOrEmpty(path);
            ArgumentNullException.ThrowIfNull(request);
            _method = request.Method;
            _request = request;
            _requestUri = request.RequestUri?.ToString();
            _isNotEmpty = true;
            _hasResponse = response is not null;
            _response = response;
            _path = path;
        }

        public static implicit operator HttpCall(ValueTuple<string, HttpRequestMessage, HttpResponseMessage?> tuple)
        {
            return new(tuple.Item1, tuple.Item2, tuple.Item3);
        }
    }
}
