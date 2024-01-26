using MG.Sonarr.Next.Collections.Pools;
using MG.Sonarr.Next.Shell.Exceptions;
using System.Buffers;

namespace MG.Sonarr.Next.Shell.Cmdlets.Bases
{
    //[DebuggerStepThrough]
    /// <summary>
    /// An <see langword="abstract"/>, <see cref="SonarrCmdletBase"/> class that allows for retrieving 
    /// and returning objects from an <see cref="IObjectPool{T}"/> instance.
    /// </summary>
    /// <remarks>
    ///     Objects pulled from pools can either be defined up-front and automatically returned, or can 
    ///     be created on-demand and returned manually.
    /// </remarks>
    public abstract class PoolableCmdlet : SonarrCmdletBase
    {
        private bool _isRented;
        private object[] _rented = [];
        private int _capacity;

        /// <summary>
        /// In dervied cmdlets, gets the capacity of the number of rented objects that will be allocated
        /// and disposed.
        /// </summary>
        /// <remarks>
        ///     Default implementation returns 0.
        /// </remarks>
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
        /// <para>
        ///     This method should only be called once execution of the cmdlet has started (i.e. - in either
        ///     the <see cref="Cmdlet.BeginProcessing"/>, <see cref="Cmdlet.ProcessRecord"/>, or
        ///     <see cref="Cmdlet.EndProcessing"/> invocations). Calling it at any other time will throw a 
        ///     <see cref="PipelineStoppedException"/>.
        /// </para>
        /// <para>
        ///     The caller is responsible for returning the object back to the pool by calling
        ///     <see cref="ReturnPooledObject{T}(T)"/>.
        /// </para>
        /// </remarks>
        /// <typeparam name="T">
        ///     The type of object that is pulled from an <see cref="IObjectPool{T}"/>.
        /// </typeparam>
        /// <returns>
        ///     An object of type <typeparamref name="T"/> that has been pulled from one of the registered
        ///     <see cref="IObjectPool{T}"/> instances.
        /// </returns>
        /// <exception cref="PipelineStoppedException"/>
        protected T GetPooledObject<T>() where T : notnull
        {
            if (!this.IsScopeInitialized)
            {
                return CmdletScopeNotReadyException.ThrowAsInnerTo<T>(this, ex =>
                {
                    return new PipelineStoppedException(ex.Message, ex);
                });
            }

            try
            {
                return this.Services.GetRequiredService<IObjectPool<T>>().Get();
            }
            catch (InvalidOperationException e)
            {
                return ThrowPipelineStopped<T>("Unable to find the required service.", e);
            }
        }

        /// <summary>
        /// Returns a span of all the returnable objects that were pre-defined and allocated 
        /// during the execution of <see cref="SonarrCmdletBase.OnCreatingScope(IServiceProvider)"/>.
        /// </summary>
        /// <returns>
        /// <para>
        ///     A <see cref="Span{T}"/> of <see cref="object"/> instances that were pre-defined and allocated
        ///     and will be returned to their respective <see cref="IObjectPool{T}"/> instances when this
        ///     cmdlet finishes execution whose length is equal to <see cref="Capacity"/>.
        /// </para>
        /// <para>
        ///     If <see cref="Capacity"/> is equal to 0, then <see cref="Span{T}.Empty"/> is returned.
        /// </para>
        /// </returns>
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
                return [];
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

