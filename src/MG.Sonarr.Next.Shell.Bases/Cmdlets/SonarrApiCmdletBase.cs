using MG.Sonarr.Next.Exceptions;
using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Services.Http.Clients;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Models;
using MG.Sonarr.Next.Services.Jobs;
using MG.Sonarr.Next.Unions;

namespace MG.Sonarr.Next.Shell.Cmdlets
{
    /// <summary>
    /// An <see langword="abstract"/>, base class for Sonarr cmdlets that issue HTTP requests to Sonarr API
    /// endpoints.
    /// </summary>
    [DebuggerStepThrough]
    public abstract class SonarrApiCmdletBase : TimedCmdlet
    {
        private ISonarrClient _client = null!;
        private ApiCmdletQueue _queue = null!;

        private protected override void OnCreatingScopeInternal(IServiceProvider provider)
        {
            base.OnCreatingScopeInternal(provider);
            _client = provider.GetRequiredService<ISonarrClient>();
            _queue = provider.GetRequiredService<ApiCmdletQueue>();
        }

        /// <summary>
        /// Sends a DELETE Http request to the specified Sonarr API endpoint.
        /// </summary>
        /// <param name="path">The relative URL path of the API endpoint.</param>
        /// <param name="token"></param>
        /// <returns>
        ///     A response object indicating the status code and any errors that may have occurred.
        /// </returns>
        protected SonarrClientResult SendDeleteRequest(string path, CancellationToken token = default)
        {
            this.StartTimer();
            _queue.Enqueue(this);

            return _client.SendDeleteAsync(path, token).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Sends a GET Http request to the specified Sonarr API endpoint.
        /// </summary>
        /// <typeparam name="T">The type of object the response's content will be deserialized as.</typeparam>
        /// <param name="path">The relative URL path of the API endpoint.</param>
        /// <param name="token"></param>
        /// <returns>
        ///     A response object indicating the status code and any errors that may have occurred.
        /// </returns>
        protected SonarrClientResult<T> SendGetRequest<T>(string path, CancellationToken token = default)
        {
            this.StartTimer();
            _queue.Enqueue(this);
            return _client.SendGetAsync<T>(path, token).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Sends a POST Http request to the specified Sonarr API endpoint with no body in the request but deserializes 
        /// the response as the specified type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of object to deserialize the response's content to.</typeparam>
        /// <param name="path"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        protected SonarrClientResult<T> SendPostRequest<T>(string path, CancellationToken token = default)
        {
            this.StartTimer();
            _queue.Enqueue(this);
            return _client.SendPostAsync<T>(path, token).GetAwaiter().GetResult();
        }
        /// <summary>
        /// Sends a POST Http request to the specified Sonarr API endpoint with the specified object 
        /// of type <typeparamref name="T"/> JSON-serialized into the request's body.
        /// </summary>
        /// <typeparam name="T">The type of object the request body will serialized from.</typeparam>
        /// <param name="path">The relative URL path of the API endpoint.</param>
        /// <param name="body">The body to serialize into the request's body.</param>
        /// <param name="token"></param>
        /// <returns>
        ///     A response object indicating the status code and any errors that may have occurred.
        /// </returns>
        protected SonarrClientResult SendPostRequest<T>(string path, T body, CancellationToken token = default) where T : notnull
        {
            this.StartTimer();
            _queue.Enqueue(this);
            return _client.SendPostAsync(path, body, token).GetAwaiter().GetResult();
        }
        /// <summary>
        /// Sends a POST Http request to the specified Sonarr API endpoint with the specified object 
        /// of type <typeparamref name="T"/> JSON-serialized into the request's body.
        /// </summary>
        /// <typeparam name="TBody">The type of object the request body will serialized from.</typeparam>
        /// <typeparam name="TOutput">The type of object the response's content will be deserialized as.</typeparam>
        /// <param name="path">The relative URL path of the API endpoint.</param>
        /// <param name="body">The body to serialize into the request's body.</param>
        /// <param name="token"></param>
        /// <returns>
        ///     A <see cref="OneOf{T0, T1}"/> object that can either be the deserialized HTTP response or
        ///     an <see cref="SonarrErrorRecord"/>.
        /// </returns>
        protected Either<TOutput, SonarrErrorRecord> SendPostRequest<TBody, TOutput>(string path, TBody body, CancellationToken token = default) where TBody : notnull
        {
            this.StartTimer();
            _queue.Enqueue(this);
            SonarrClientResult<TOutput> response = _client.SendPostAsync<TBody, TOutput>(path, body, token).GetAwaiter().GetResult();
            return !response.IsError
                ? response.Value
                : response.Error;
        }
        /// <summary>
        /// Sends a PUT Http request to the specified Sonarr API endpoint with the specified object 
        /// of type <typeparamref name="T"/> JSON-serialized into the request's body.
        /// </summary>
        /// <typeparam name="T">The type of object the request body will serialized from.</typeparam>
        /// <param name="path">The relative URL path of the API endpoint.</param>
        /// <param name="body">The body to serialize into the request's body.</param>
        /// <param name="token"></param>
        /// <returns>
        ///     A response object indicating the status code and any errors that may have occurred.
        /// </returns>
        protected SonarrClientResult SendPutRequest<T>(string path, T body , CancellationToken token = default)
            where T : notnull
        {
            this.StartTimer();
            _queue.Enqueue(this);
            return _client.SendPutAsync(path, body, token).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Attempts to commit or reset properties of the <typeparamref name="T"/> object depending on if
        /// the accompanying <see cref="SonarrClientResult"/> indicates an error occurred.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="SonarrObject"/> to process.</typeparam>
        /// <param name="sonarrObj">
        /// The <see cref="SonarrObject"/> whose properties can be
        /// either committed or reset.
        /// </param>
        /// <param name="response">
        /// The response from the Sonarr API call and whose
        /// <see cref="SonarrClientResult.IsError"/> property determines the action taken on 
        /// the properties of <paramref name="sonarrObj"/>.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="sonarrObj"/> had it properties committed, indicating
        /// a successful operation; otherwise, <see langword="false"/> indicating an error occurred.
        /// </returns>
        protected bool TryCommitFromResponse<T>(T sonarrObj, SonarrClientResult response) where T : SonarrObject
        {
            if (response.IsError)
            {
                sonarrObj.Reset();
                this.WriteError(response.Error);
                return false;
            }
            else
            {
                sonarrObj.Commit();
                return true;
            }
        }
    }
}