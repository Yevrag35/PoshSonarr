using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Shell.Exceptions;
using System.Buffers;

namespace MG.Sonarr.Next.Shell.Cmdlets.Bases
{
    [DebuggerStepThrough]
    public abstract class PoolableCmdlet : SonarrCmdletBase
    {
        bool _isRented;
        object[] _rented = Array.Empty<object>();
        protected virtual int ReturnableCapacity => 0;

        private void SetReturnables()
        {
            if (this.ReturnableCapacity <= 0)
            {
                return;
            }

            _rented = ArrayPool<object>.Shared.Rent(this.ReturnableCapacity);
            _isRented = true;
        }

        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            this.SetReturnables();
        }

        /// <summary>
        /// Retrieves a pooled item from the one of the <see cref="IObjectPool{T}"/> instances
        /// registered.
        /// </summary>
        /// <remarks>
        ///     This method should only be called once execution of the cmdlet has started.
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        /// <exception cref="PipelineStoppedException"/>
        [DebuggerStepThrough]
        protected T GetPooledObject<T>() where T : notnull
        {
            if (!this.IsScopeInitialized)
            {
                var ex = new CmdletScopeNotReadyException(this.GetType());
                throw new PipelineStoppedException(ex.Message, ex);
            }

            try
            {
                return this.Services.GetRequiredService<IObjectPool<T>>().Get();
            }
            catch (InvalidOperationException e)
            {
                var pipelineStopped = new PipelineStoppedException("Unable to find the required service.", e);
                throw pipelineStopped;
            }
        }

        protected Span<object> GetReturnables()
        {
            return _isRented ? _rented.AsSpan(0, this.ReturnableCapacity) : Span<object>.Empty;
        }

        /// <summary>
        /// Returns an previously retrieved item back into its <see cref="IObjectPool{T}"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item">The item to return to the pool.</param>
        [DebuggerStepThrough]
        protected void ReturnPooledObject<T>(T item) where T : notnull
        {
            this.Services?.GetService<IObjectPool<T>>()?.Return(item);
        }

        bool _disposed;
        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing && this.IsScopeInitialized && _isRented)
                {
                    var returner = this.Services.GetService<IPoolReturner>();
                    returner?.Return(this.GetReturnables());
                    ArrayPool<object>.Shared.Return(_rented);
                }

                _rented = null!;
                _isRented = false;
                _disposed = true;
            }

            base.Dispose(disposing);
        }
    }
}

