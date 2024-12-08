using MG.Sonarr.Next.Exceptions;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Extensions.Strings;
using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Shell.Exceptions;
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

        /// <summary><inheritdoc cref="SonarrCmdletBase.CaptureVerbosePreference"/></summary>
        /// <remarks>
        /// Implementation in the base class always returns <see langword="true"/>.
        /// </remarks>
        protected sealed override bool CaptureVerbosePreference => true;
        private protected sealed override int InternalCapacity => 0;

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
        /// <remarks>
        ///     When the method returns, the underlying <see cref="Stopwatch"/> is reset.
        /// </remarks>
        /// <exception cref="CmdletScopeNotReadyException"/>
        protected TimeSpan StopTimer()
        {
            return Stopwatch.GetElapsedTime(_timestamp);
        }

        public void WriteVerboseAfter(ISonarrResponse response, IServiceProvider provider, JsonSerializerOptions? options)
        {
            TimeSpan elapsed = this.StopTimer();
            string msg = GenerateVerboseAfter(new TimedResponse(elapsed, response, provider));
            //this.WriteVerbose(msg ?? string.Empty);
            this.Host?.UI?.WriteVerboseLine(msg);
        }

        private static string GenerateVerboseAfter(ISonarrTimedResponse response)
        {
            double rounded = Math.Round(response.Elapsed.TotalMilliseconds, 2, MidpointRounding.AwayFromZero);
            return GetAfterMessage(in rounded, response.StatusCode);
        }

        const string AFTER_MSG_FORMAT_1 = "Received response after ";
        const string AFTER_MSG_FORMAT_2 = "ms -> ";
        private static string GetAfterMessage(in double elapsedMilliseconds, HttpStatusCode statusCode)
        {
            int length = AFTER_MSG_FORMAT_1.Length + AFTER_MSG_FORMAT_2.Length
                         +
                         LengthConstants.DOUBLE_MAX + LengthConstants.HTTP_STATUS_CODE_MAX;

            Span<char> span = stackalloc char[length];
            int position = 0;

            AFTER_MSG_FORMAT_1.CopyToSlice(span, ref position);
            _ = elapsedMilliseconds.TryFormat(span.Slice(position), out int written);

            position += written;
            AFTER_MSG_FORMAT_2.CopyToSlice(span, ref position);

            _ = statusCode.TryFormatAsResponse(span.Slice(position), out int codeWritten);

            return new string(span.Slice(0, position + codeWritten));
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
