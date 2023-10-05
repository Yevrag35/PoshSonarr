using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Services.Http.Clients;
using MG.Sonarr.Next.Services.Models.System;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace MG.Sonarr.Next.Shell.Cmdlets.Systems
{
    [Cmdlet(VerbsDiagnostic.Test, "SonarrInstance")]
    [Alias("Ping-Sonarr", "Test-Sonarr")]
    [OutputType(typeof(PingResult), typeof(bool))]
    public sealed class TestSonarrInstanceCmdlet : SonarrCmdletBase, IApiCmdlet
    {
        ISignalRClient Client { get; set; } = null!;
        Queue<IApiCmdlet> Queue { get; set; } = null!;
        Stopwatch Stopwatch { get; set; } = null!;

        [Parameter]
        public SwitchParameter Quiet { get; set; }

        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            this.Client = provider.GetRequiredService<ISignalRClient>();
            this.Stopwatch = this.GetPooledObject<Stopwatch>();
            this.Queue = provider.GetRequiredService<Queue<IApiCmdlet>>();
        }

        protected override void Process(IServiceProvider provider)
        {
            this.Queue.Enqueue(this);
            this.Stopwatch.Start();

            var response = this.Client.SendPing();
            this.Stopwatch.Stop();

            if (this.Quiet.ToBool())
            {
                this.WriteQuietResponse(in response);
                return;
            }

            PingResult result = new(in response, this.Stopwatch.ElapsedTicks);
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
                    this.Queue?.Clear();
                    this.ReturnPooledObject(this.Stopwatch);
                }

                this.Queue = null!;
                this.Stopwatch = null!;
                _disposed = true;
            }

            base.Dispose(disposing);
        }

        public void WriteVerboseBefore(IHttpRequestDetails request)
        {
            this.WriteVerbose($"Sending test ping (GET) request to SignalR -> {request.RequestUri}");
        }

        public void WriteVerboseAfter(ISonarrResponse response, IServiceProvider provider, JsonSerializerOptions? options = null)
        {
            this.WriteVerbose($"Received response -> {(int)response.StatusCode} ({response.StatusCode})");
        }
    }
}
