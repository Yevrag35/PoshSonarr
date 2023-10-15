namespace MG.Sonarr.Next.Services.Http
{
    public interface IHttpRequestDetails
    {
        string RequestMethod { get; }
        string RequestUrl { get; }
    }
}
