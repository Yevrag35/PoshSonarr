using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Services.Http.Clients;
using MG.Sonarr.Next.Models.System;
using System.Text.Json;
using MG.Sonarr.Next.Services.Auth;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Services.Jobs;

namespace MG.Sonarr.Next.Shell.Cmdlets.Systems.Server
{
    [Cmdlet(VerbsDiagnostic.Test, "SonarrInstance")]
    [Alias("Ping-Sonarr", "Test-Sonarr")]
    [OutputType(typeof(PingResult), typeof(bool))]
    public sealed class TestSonarrInstanceCmdlet : PoolableCmdlet, IApiCmdlet
    {
        ISignalRClient _client = null!;
        ApiCmdletQueue _queue = null!;

        [Parameter]
        public SwitchParameter Quiet { get; set; }

        public bool CanDebugSerializeBefore => this.DebugPreference != ActionPreference.SilentlyContinue;
        public bool CanDebugSerializeAfter => this.DebugPreference != ActionPreference.SilentlyContinue;

        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            _client = provider.GetRequiredService<ISignalRClient>();
            _queue = provider.GetRequiredService<ApiCmdletQueue>();
        }

        protected override void Process(IServiceProvider provider)
        {
            _queue.Enqueue(this);
            long timestamp = Stopwatch.GetTimestamp();

            var response = _client.SendPing();
            TimeSpan elapsed = Stopwatch.GetElapsedTime(timestamp);

            if (this.Quiet.ToBool())
            {
                this.WriteQuietResponse(in response);
                return;
            }

            PingResult result = new(in response, elapsed.Ticks);
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
                }

                _queue = null!;
                _disposed = true;
            }

            base.Dispose(disposing);
        }
        public void WriteDebugPayload(string jsonPayload)
        {
            if (this.Host?.UI is not null)
            {
                this.Host.UI.WriteDebugLine(jsonPayload);
            }
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
