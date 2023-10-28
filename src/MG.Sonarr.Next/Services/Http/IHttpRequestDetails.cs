namespace MG.Sonarr.Next.Services.Http
{
    public interface IHttpRequestDetails : IServiceProvider
    {
        string RequestMethod { get; }
        string RequestUrl { get; }
    }
}
