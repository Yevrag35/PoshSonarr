using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Services.Http.Clients;
using MG.Sonarr.Next.Models.System;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using MG.Sonarr.Next.Services.Auth;

namespace MG.Sonarr.Next.Shell.Cmdlets.Systems.Server
{
    [Cmdlet(VerbsDiagnostic.Test, "SonarrInstance")]
    [Alias("Ping-Sonarr", "Test-Sonarr")]
    [OutputType(typeof(PingResult), typeof(bool))]
    public sealed class TestSonarrInstanceCmdlet : SonarrCmdletBase, IApiCmdlet
    {
        ISignalRClient _client = null!;
        Queue<IApiCmdlet> _queue = null!;
        Stopwatch _stopwatch = null!;

        [Parameter]
        public SwitchParameter Quiet { get; set; }

        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            _client = provider.GetRequiredService<ISignalRClient>();
            _stopwatch = this.GetPooledObject<Stopwatch>();
            _queue = provider.GetRequiredService<Queue<IApiCmdlet>>();
        }

        protected override void Process(IServiceProvider provider)
        {
            _queue.Enqueue(this);
            _stopwatch.Start();

            var response = _client.SendPing();
            _stopwatch.Stop();

            if (this.Quiet.ToBool())
            {
                this.WriteQuietResponse(in response);
                return;
            }

            PingResult result = new(in response, _stopwatch.ElapsedTicks);
            this.WriteObject(result);
        }

        private void WriteQuietResponse(in SonarrResponse<PingResponse> response)
        {
            this.WriteObject(!response.IsEmpty && !response.IsError);
        }

        bool _disposed;
        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _queue?.Clear();
                    this.ReturnPooledObject(_stopwatch);
                }

                _queue = null!;
                _stopwatch = null!;
                _disposed = true;
            }

            base.Dispose(disposing);
        }

        public void WriteVerboseBefore(IHttpRequestDetails request)
        {
            var settings = request.GetRequiredService<IConnectionSettings>();
            string url = request.RequestUrl.Replace(settings.ApiKey.GetValue(), "<ApiKey_Omitted>");

            this.WriteVerbose($"Sending test ping (GET) request to SignalR -> {url}");
        }

        public void WriteVerboseAfter(ISonarrResponse response, IServiceProvider provider, JsonSerializerOptions? options = null)
        {
            this.WriteVerbose($"Received response -> {(int)response.StatusCode} ({response.StatusCode})");
        }
    }
}
