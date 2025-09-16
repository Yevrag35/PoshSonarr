using MG.Sonarr.Next.Exceptions;
using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Shell.Exceptions;
using MG.Sonarr.Next.Strings;
using MG.Sonarr.Resources;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace MG.Sonarr.Next.Shell.Cmdlets.Bases
{
    /// <summary>
    /// An <see langword="abstract"/>, <see cref="PoolableCmdlet"/> that implements 
    /// <see cref="IApiCmdlet"/> and provides methods for timing the execution of Sonarr API requests.
    /// </summary>
    //[DebuggerStepThrough]
    public abstract class TimedCmdlet : PoolableCmdlet, IApiCmdlet
    {
        private long _timestamp;

        protected override bool CaptureDebugPreference => true;
        /// <summary><inheritdoc cref="SonarrCmdletBase.CaptureVerbosePreference"/></summary>
        /// <remarks>
        /// Implementation in the base class always returns <see langword="true"/>.
        /// </remarks>
        protected sealed override bool CaptureVerbosePreference => true;
        private protected sealed override int InternalCapacity => 0;
        public virtual bool CanDebugSerializeBefore => this.DebugPreference != ActionPreference.SilentlyContinue;
        public virtual bool CanDebugSerializeAfter => this.DebugPreference != ActionPreference.SilentlyContinue;

        /// <summary>
        /// Starts the timer.
        /// </summary>
        /// <exception cref="CmdletScopeNotReadyException"/>
        protected void StartTimer()
        {
            _timestamp = Stopwatch.GetTimestamp();
        }

        /// <summary>
        /// Stops the timer and returns the elapsed <see cref="TimeSpan"/>.
        /// </summary>
        /// <exception cref="CmdletScopeNotReadyException"/>
        protected TimeSpan StopTimer()
        {
            return Stopwatch.GetElapsedTime(_timestamp);
        }

        public void WriteVerboseAfter(ISonarrResponse response, IServiceProvider provider, JsonSerializerOptions? options)
        {
            if (this.Host?.UI is not null)
            {
                TimeSpan elapsed = this.StopTimer();
                string msg = GetAfterMessage(elapsed, response.StatusCode);
                this.Host.UI.WriteVerboseLine(msg);
            }
        }

        private static string GetAfterMessage(TimedValue elapsedTime, HttpStatusCode statusCode)
        {
            return Messenger.Format(
                provider: CultureInfo.CurrentCulture,
                format: Messages.Verbose_ReceivedResponse_Timed_Format,
                elapsedTime, (int)statusCode, statusCode);
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
            if (this.Host?.UI is not null)
            {
                string msg = GenerateBeforeMessage(request);
                this.Host.UI.WriteVerboseLine(msg);
            }
        }
        private static string GenerateBeforeMessage(IHttpRequestDetails request)
        {
            return $"Sending {request.RequestMethod} request -> {request.RequestUrl}";
        }

        [DebuggerStepThrough]
        [StructLayout(LayoutKind.Auto)]
        private readonly struct TimedResponse : ISonarrTimedResponse
        {
            readonly ISonarrResponse? _response;
            readonly TimeSpan _timeSpan;
            readonly IServiceProvider _provider;

            internal TimedResponse(TimeSpan elapsed, ISonarrResponse response, IServiceProvider provider)
            {
                _response = response;
                _timeSpan = elapsed;
                _provider = provider;
            }

            public TimeSpan Elapsed => _timeSpan;
            public SonarrErrorRecord? Error => _response?.Error;
            public bool IsError => _response is null || _response.IsError;
            public IServiceProvider Services => _provider;
            public HttpStatusCode StatusCode => _response?.StatusCode ?? HttpStatusCode.Unused;
            public string RequestUrl => _response?.RequestUrl ?? string.Empty;
        }
    }
}
