using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Services.Json;
using Microsoft.Extensions.DependencyInjection;
using OneOf;
using System.Text.Json;

namespace MG.Sonarr.Next.Shell.Cmdlets
{
    public abstract class SonarrApiCmdletBase : SonarrCmdletBase, IApiCmdlet
    {
        ISonarrClient Client { get; }
        protected SonarrJsonOptions? Options { get; }
        Queue<IApiCmdlet> Queue { get; }

        protected SonarrApiCmdletBase()
            : base()
        {
            this.Client = this.Services.GetRequiredService<ISonarrClient>();
            this.Options = this.Services.GetService<SonarrJsonOptions>();
            this.Queue = this.Services.GetRequiredService<Queue<IApiCmdlet>>();
        }

        protected virtual SonarrResponse SendDeleteRequest(string path, CancellationToken token = default)
        {
            this.Queue.Enqueue(this);

            var response = this.Client.SendDelete(path, token);
            return response;
        }

        protected virtual SonarrResponse<T> SendGetRequest<T>(string path, CancellationToken token = default)
        {
            this.Queue.Enqueue(this);
            SonarrResponse<T> response = this.Client.SendGet<T>(path, token);
            return response;
        }

        protected virtual SonarrResponse SendPostRequest<T>(string path, T body, CancellationToken token = default)
            where T : notnull
        {
            this.Queue.Enqueue(this);
            SonarrResponse response = this.Client.SendPost(path, body, token);
            return response;
        }
        protected virtual OneOf<TOutput, ErrorRecord> SendPostRequest<TBody, TOutput>(string path, TBody body, CancellationToken token = default)
            where TBody : notnull
        {
            this.Queue.Enqueue(this);
            SonarrResponse<TOutput> response = this.Client.SendPost<TBody, TOutput>(path, body, token);
            return !response.IsError
                ? response.Data
                : response.Error;
        }

        protected virtual SonarrResponse SendPutRequest<T>(string path, T body , CancellationToken token = default)
            where T : notnull
        {
            this.Queue.Enqueue(this);
            SonarrResponse response = this.Client.SendPut(path, body, token);
            return response;
        }

        public virtual void WriteVerbose(HttpRequestMessage request)
        {
            this.WriteVerbose($"Sending {request.Method.Method} request -> {request.RequestUri?.ToString()}");
        }
    }
}