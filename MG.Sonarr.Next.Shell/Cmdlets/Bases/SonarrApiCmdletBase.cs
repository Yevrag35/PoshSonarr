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
    /// <summary>
    /// An <see langword="abstract"/>, base class for Sonarr cmdlets that issue HTTP requests to Sonarr API
    /// endpoints.
    /// </summary>
    public abstract class SonarrApiCmdletBase : SonarrCmdletBase, IApiCmdlet
    {
        Stopwatch _timer = null!;

        ISonarrClient Client { get; set; } = null!;
        protected SonarrJsonOptions? Options { get; private set; } = null!;
        Queue<IApiCmdlet> Queue { get; set; } = null!;

        /// <summary>
        /// Overridden to retrieve a scoped <see cref="ISonarrClient"/> instance to be used for any API
        /// requests.
        /// </summary>
        /// <param name="provider">The scoped service provider for use by derived cmdlets.</param>
        /// <exception cref="Exception"/>
        /// <exception cref="InvalidOperationException"/>
        protected override void OnCreatingScope(IServiceProvider provider)
        {
            this.Client = provider.GetRequiredService<ISonarrClient>();
            this.Options = provider.GetService<SonarrJsonOptions>();
            this.Queue = provider.GetRequiredService<Queue<IApiCmdlet>>();
            var pool = provider.GetRequiredService<IObjectPool<Stopwatch>>();
            _timer = pool.Get();
        }

        /// <summary>
        /// Sends a DELETE Http request to the specified Sonarr API endpoint.
        /// </summary>
        /// <param name="path">The relative URL path of the API endpoint.</param>
        /// <param name="token"></param>
        /// <returns>
        ///     A response object indicating the status code and any errors that may have occurred.
        /// </returns>
        protected virtual SonarrResponse SendDeleteRequest(string path, CancellationToken token = default)
        {
            _timer.Start();
            this.Queue.Enqueue(this);

            var response = this.Client.SendDelete(path, token);

            return response;
        }

        /// <summary>
        /// Sends a GET Http request to the specified Sonarr API endpoint.
        /// </summary>
        /// <param name="path">The relative URL path of the API endpoint.</param>
        /// <param name="token"></param>
        /// <returns>
        ///     A response object indicating the status code and any errors that may have occurred.
        /// </returns>
        protected virtual SonarrResponse<T> SendGetRequest<T>(string path, CancellationToken token = default)
        {
            _timer.Start();
            this.Queue.Enqueue(this);
            SonarrResponse<T> response = this.Client.SendGet<T>(path, token);
            return response;
        }

        /// <summary>
        /// Sends a POST Http request to the specified Sonarr API endpoint with the specified object 
        /// of type <typeparamref name="T"/> JSON-serialized into the request's body.
        /// </summary>
        /// <param name="path">The relative URL path of the API endpoint.</param>
        /// <param name="body">The body to serialize into the request's body.</param>
        /// <param name="token"></param>
        /// <returns>
        ///     A response object indicating the status code and any errors that may have occurred.
        /// </returns>
        protected virtual SonarrResponse SendPostRequest<T>(string path, T body, CancellationToken token = default)
            where T : notnull
        {
            _timer.Start();
            this.Queue.Enqueue(this);
            SonarrResponse response = this.Client.SendPost(path, body, token);
            return response;
        }
        /// <summary>
        /// Sends a POST Http request to the specified Sonarr API endpoint with the specified object 
        /// of type <typeparamref name="T"/> JSON-serialized into the request's body.
        /// </summary>
        /// <param name="path">The relative URL path of the API endpoint.</param>
        /// <param name="body">The body to serialize into the request's body.</param>
        /// <param name="token"></param>
        /// <returns>
        ///     A <see cref="OneOf{T0, T1}"/> object that can either be the deserialized HTTP response or
        ///     an <see cref="SonarrErrorRecord"/>.
        /// </returns>
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
        /// <summary>
        /// Sends a PUT Http request to the specified Sonarr API endpoint with the specified object 
        /// of type <typeparamref name="T"/> JSON-serialized into the request's body.
        /// </summary>
        /// <param name="path">The relative URL path of the API endpoint.</param>
        /// <param name="body">The body to serialize into the request's body.</param>
        /// <param name="token"></param>
        /// <returns>
        ///     A response object indicating the status code and any errors that may have occurred.
        /// </returns>
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