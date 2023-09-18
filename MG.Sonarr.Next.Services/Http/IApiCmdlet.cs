namespace MG.Sonarr.Next.Services.Http
{
    public interface IApiCmdlet
    {
        void WriteVerbose(HttpRequestMessage request);
        void WriteVerboseSonarrResult(ISonarrResponse response, JsonSerializerOptions? options = null);
    }
}
