using MG.Sonarr.Next.Services.Collections;
using MG.Sonarr.Next.Services.Exceptions;
using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Services.Http.Clients;
using MG.Sonarr.Next.Services.Json;
using Microsoft.Extensions.DependencyInjection;
using OneOf;
using System.Text.Json;

namespace MG.Sonarr.Next.Shell.Cmdlets
{
    public abstract class SonarrApiCmdletBase : SonarrCmdletBase, IApiCmdlet
    {
        Stopwatch _timer = null!;

        ISonarrClient Client { get; set; } = null!;
        protected SonarrJsonOptions? Options { get; private set; } = null!;
        Queue<IApiCmdlet> Queue { get; set; } = null!;

        protected override void OnCreatingScope(IServiceProvider provider)
        {
            this.Client = provider.GetRequiredService<ISonarrClient>();
            this.Options = provider.GetService<SonarrJsonOptions>();
            this.Queue = provider.GetRequiredService<Queue<IApiCmdlet>>();
            var pool = provider.GetRequiredService<IObjectPool<Stopwatch>>();
            _timer = pool.Get();
        }

        protected virtual SonarrResponse SendDeleteRequest(string path, CancellationToken token = default)
        {
            _timer.Start();
            this.Queue.Enqueue(this);

            var response = this.Client.SendDelete(path, token);

            return response;
        }

        protected virtual SonarrResponse<T> SendGetRequest<T>(string path, CancellationToken token = default)
        {
            _timer.Start();
            this.Queue.Enqueue(this);
            SonarrResponse<T> response = this.Client.SendGet<T>(path, token);
            return response;
        }

        protected virtual SonarrResponse SendPostRequest<T>(string path, T body, CancellationToken token = default)
            where T : notnull
        {
            _timer.Start();
            this.Queue.Enqueue(this);
            SonarrResponse response = this.Client.SendPost(path, body, token);
            return response;
        }
        protected virtual OneOf<TOutput, SonarrErrorRecord> SendPostRequest<TBody, TOutput>(string path, TBody body, CancellationToken token = default)
            where TBody : notnull
        {
            _timer.Start();
            this.Queue.Enqueue(this);
            SonarrResponse<TOutput> response = this.Client.SendPost<TBody, TOutput>(path, body, token);
            return !response.IsError
                ? response.Data
                : response.Error;
        }

        protected virtual SonarrResponse SendPutRequest<T>(string path, T body , CancellationToken token = default)
            where T : notnull
        {
            _timer.Start();
            this.Queue.Enqueue(this);
            SonarrResponse response = this.Client.SendPut(path, body, token);
            return response;
        }

        public virtual void WriteVerboseBefore(IHttpRequestDetails request)
        {
            this.WriteVerbose($"Sending {request.Method} request -> {request.RequestUri}");
        }
        public void WriteVerboseAfter(ISonarrResponse response, IServiceProvider provider, JsonSerializerOptions? options = null)
        {
            _timer.Stop();
            string msg = $"Received response after {_timer.ElapsedMilliseconds}ms -> {(int)response.StatusCode} ({response.StatusCode})";

            _timer.Reset();
            this.WriteVerbose(msg);
        }

        bool _disposed;
        protected override void Dispose(bool disposing, IServiceScopeFactory? scopeFactory)
        {
            if (!_disposed && disposing && scopeFactory is not null)
            {
                using var scope = scopeFactory.CreateScope();
                var pool = scope.ServiceProvider.GetService<IObjectPool<Stopwatch>>();

                pool?.Return(_timer);
                _timer = null!;

                _disposed = true;
            }
        }
    }
}