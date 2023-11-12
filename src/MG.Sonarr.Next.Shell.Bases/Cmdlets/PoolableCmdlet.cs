using MG.Sonarr.Next.Collections.Pools;
using MG.Sonarr.Next.Shell.Exceptions;
using System.Buffers;

namespace MG.Sonarr.Next.Shell.Cmdlets.Bases
{
    //[DebuggerStepThrough]
    public abstract class PoolableCmdlet : SonarrCmdletBase
    {
        bool _isRented;
        object[] _rented = Array.Empty<object>();
        int _capacity;
        protected virtual int Capacity => 0;
        private protected virtual int InternalCapacity => 0;

        private static int CalculateCapacity(int capacity, int internalCapacity)
        {
            capacity += internalCapacity;
            return capacity >= internalCapacity ? capacity : internalCapacity;
        }

        private protected override void OnCreatingScopeInternal(IServiceProvider provider)
        {
            base.OnCreatingScopeInternal(provider);
            _capacity = this.Capacity;
            _rented = SetReturnables(this.InternalCapacity, ref _capacity, ref _isRented);
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

        protected virtual Span<object> GetReturnables()
        {
            return this.GetAllReturnables();
        }
        private Span<object> GetAllReturnables()
        {
            return _isRented ? _rented.AsSpan(0, _capacity) : Span<object>.Empty;
        }

        /// <summary>
        /// Returns an previously retrieved item back into its <see cref="IObjectPool{T}"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item">The item to return to the pool.</param>
        protected void ReturnPooledObject<T>(T item) where T : notnull
        {
            this.Services?.GetService<IObjectPool<T>>()?.Return(item);
        }
        private static object[] SetReturnables(int internalCapacity, ref int capacity, ref bool isRented)
        {
            capacity = CalculateCapacity(capacity, internalCapacity);
            if (capacity <= 0)
            {
                capacity = 0;
                return Array.Empty<object>();
            }

            object[] array = ArrayPool<object>.Shared.Rent(capacity);
            isRented = true;

            return array;
        }

        bool _disposed;
        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing && this.IsScopeInitialized && _isRented)
                {
                    var returner = this.Services.GetService<IPoolReturner>();
                    returner?.Return(this.GetAllReturnables());
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

