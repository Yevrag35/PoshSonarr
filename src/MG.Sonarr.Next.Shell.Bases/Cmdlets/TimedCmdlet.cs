using MG.Sonarr.Next.Exceptions;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Shell.Exceptions;
using System.Net;
using System.Text.Json;

namespace MG.Sonarr.Next.Shell.Cmdlets.Bases
{
    //[DebuggerStepThrough]
    public abstract class TimedCmdlet : PoolableCmdlet, IApiCmdlet
    {
        const int TIMED_CAPACITY = 1;
        Stopwatch _timer = null!;

        /// <summary><inheritdoc cref="SonarrCmdletBase.CaptureVerbosePreference"/></summary>
        /// <remarks>
        /// Implementation in the base class always returns <see langword="true"/>.
        /// </remarks>
        protected sealed override bool CaptureVerbosePreference => true;
        private protected sealed override int InternalCapacity => 1;

        protected sealed override Span<object> GetReturnables()
        {
            Span<object> span = base.GetReturnables();
            return span.Slice(1);
        }
        protected virtual Span<object> GetReturnableSpan()
        {
            return this.GetReturnables();
        }

        private protected override void OnCreatingScopeInternal(IServiceProvider provider)
        {
            base.OnCreatingScopeInternal(provider);
            Span<object> span = base.GetReturnables();

            _timer = this.GetPooledObject<Stopwatch>();
            span[0] = _timer;
        }

        /// <summary>
        /// Starts the timer.
        /// </summary>
        /// <exception cref="CmdletScopeNotReadyException"/>
        protected void StartTimer()
        {
            try
            {
                _timer.Start();
            } 
            catch (NullReferenceException e)
            {
                this.ThrowNotYet(e);
            }
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
            try
            {
                _timer.Stop();
            }
            catch (NullReferenceException e)
            {
                this.ThrowNotYet(e);
            }

            TimeSpan elapsed = _timer.Elapsed;
            _timer.Reset();
            return elapsed;
        }

        [DoesNotReturn]
        private void ThrowNotYet(NullReferenceException e)
        {
            throw new CmdletScopeNotReadyException(this.GetType(), e);
        }

        public void WriteVerboseAfter(ISonarrResponse response, IServiceProvider provider, JsonSerializerOptions? options)
        {
            TimeSpan elapsed = this.StopTimer();
            string msg = this.GenerateVerboseAfter(new TimedResponse(elapsed, response, provider));
            this.WriteVerbose(msg ?? string.Empty);
        }

        protected virtual string GenerateVerboseAfter(ISonarrTimedResponse response)
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
            _ = elapsedMilliseconds.TryFormat(
                span.Slice(position), out int written, default, Statics.DefaultProvider);

            position += written;
            AFTER_MSG_FORMAT_2.CopyToSlice(span, ref position);

            _ = statusCode.TryFormatAsResponse(span.Slice(position), out int codeWritten);

            return new string(span.Slice(0, position + codeWritten));
        }

        public void WriteVerboseBefore(IHttpRequestDetails request)
        {
            string? msg = this.GenerateBeforeMessage(request);
            if (msg is not null)
            {
                this.WriteVerbose(msg);
            }
        }
        protected virtual string? GenerateBeforeMessage(IHttpRequestDetails request)
        {
            return $"Sending {request.RequestMethod} request -> {request.RequestUrl}";
        }

        private readonly struct TimedResponse : ISonarrTimedResponse
        {
            readonly ISonarrResponse _response;
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

        #region DISPOSAL
        bool _disposed;
        //protected override void Dispose(bool disposing, IServiceScopeFactory? scopeFactory)
        //{
        //    //if (!_disposed)
        //    //{
        //    //    if (disposing && scopeFactory is not null)
        //    //    {
        //    //        using var scope = scopeFactory.CreateScope();

        //    //        //var pool = scope.ServiceProvider.GetService<IObjectPool<Stopwatch>>();
        //    //        //pool?.Return(_timer);
        //    //    }

        //    //    _timer = null!;
        //    //    _disposed = true;
        //    //}

        //    //base.Dispose(disposing, scopeFactory);
        //}

        protected sealed override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing && this.IsScopeInitialized)
                {
                    this.ReturnPooledObject(_timer);
                }

                _timer = null!;
                _disposed = true;
            }

            this.Dispose(disposing, this.Services?.GetService<IServiceScopeFactory>());
            base.Dispose(disposing);
        }

        #endregion
    }
}
