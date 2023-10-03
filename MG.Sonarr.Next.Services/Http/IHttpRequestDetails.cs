namespace MG.Sonarr.Next.Services.Http
{
    public interface IHttpRequestDetails
    {
        string Method { get; }
        string RequestUri { get; }
    }
}
