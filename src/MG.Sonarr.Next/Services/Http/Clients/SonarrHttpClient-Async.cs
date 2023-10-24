using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Services.Http.Requests;
using System.Management.Automation;
using System.Net;
using System.Net.Http.Json;

namespace MG.Sonarr.Next.Services.Http.Clients
{
    internal sealed partial class SonarrHttpClient
    {
        public Task<SonarrResponse> SendDeleteAsync(string path, CancellationToken token = default)
        {
            using ApiKeyRequestMessage request = new(HttpMethod.Delete, path);

            return this.SendNoResultRequestAsync(request, path, token);
        }
        public async Task<SonarrResponse<T>> SendGetAsync<T>(string path, CancellationToken token = default)
        {
            using ApiKeyRequestMessage request = new(HttpMethod.Get, path);

            var response = await this.SendResultRequestAsync<T>(request, path, token);
            if (response.IsDataTaggable(out IJsonMetadataTaggable? taggable))
            {
                taggable.SetTag(_resolver);
            }

            if (response.IsDataSortable(out ISortable? sortable) && sortable.Count > 1)
            {
                sortable.Sort();
            }

            return response;
        }
        public Task<SonarrResponse> SendPostAsync<T>(string path, T body, CancellationToken token = default) where T : notnull
        {
            using ApiKeyRequestMessage request = new(HttpMethod.Post, path);
            request.Content = JsonContent.Create(body, body.GetType(), options: _options.ForSerializing);

            return this.SendNoResultRequestAsync(request, path, token);
        }
        public async Task<SonarrResponse<TOutput>> SendPostAsync<TBody, TOutput>(string path, TBody body, CancellationToken token = default) where TBody : notnull
        {
            using ApiKeyRequestMessage request = new(HttpMethod.Post, path);

            request.Content = JsonContent.Create(body, body.GetType(), options: _options.ForSerializing);

            var response = await this.SendResultRequestAsync<TOutput>(request, path, token);
            if (response.IsDataTaggable(out IJsonMetadataTaggable? taggable))
            {
                taggable.SetTag(_resolver);
            }

            return response;
        }
        public Task<SonarrResponse> SendPutAsync<T>(string path, T body, CancellationToken token = default) where T : notnull
        {
            using ApiKeyRequestMessage request = new(HttpMethod.Put, path);
            request.Content = JsonContent.Create(body, body.GetType(), options: _options.ForSerializing);

            return this.SendNoResultRequestAsync(request, path, token);
        }

        private async Task<SonarrResponse> SendNoResultRequestAsync(HttpRequestMessage request, string path, CancellationToken token)
        {
            HttpResponseMessage? response = null;
            try
            {
                response = await _client.SendAsync(request, token);
                return await _responseReader.ReadNoResultAsync((path, request, response), path, token);
            }
            catch (HttpRequestException httpEx)
            {
                var pso = ParseResponseForError(response, _options.ForDeserializing, token);
                SonarrHttpException sonarrEx = new(request, response, ErrorCollection.FromOne(pso), httpEx);

                var result = SonarrResponse.FromException(path, sonarrEx, ErrorCategory.InvalidResult, response?.StatusCode ?? HttpStatusCode.Unused, response);

                return result;
            }
            catch (Exception ex)
            {
                var result = SonarrResponse.FromException(path, ex, ErrorCategory.ConnectionError, response?.StatusCode ?? HttpStatusCode.Unused, response);

                return result;
            }
            finally
            {
                response?.Dispose();
            }
        }
        private async Task<SonarrResponse<T>> SendResultRequestAsync<T>(HttpRequestMessage request, string path, CancellationToken token)
        {
            HttpResponseMessage? response = null;

            try
            {
                response = await _client.SendAsync(request, token);
                return await _responseReader.ReadResultAsync<T>((path, request, response), path, token);
            }
            catch (HttpRequestException httpEx)
            {
                var pso = ParseResponseForError(response, _options.ForDeserializing, token);
                SonarrHttpException sonarrEx = new(request, response, ErrorCollection.FromOne(pso), httpEx);

                var result = SonarrResponse.FromException<T>(path, sonarrEx, ErrorCategory.InvalidResult, response?.StatusCode ?? HttpStatusCode.Unused, response);

                return result;
            }
            catch (TaskCanceledException cancelled)
            {
                ErrorCategory cat = cancelled.InnerException is TimeoutException tout
                    ? ErrorCategory.OperationTimeout
                    : ErrorCategory.OperationStopped;

                return ReturnFromException<T>(path, response, cat, cancelled);
            }
            catch (Exception e)
            {
                return ReturnFromException<T>(path, response, ErrorCategory.ConnectionError, e);
            }
        }
    }
}
