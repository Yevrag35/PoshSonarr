namespace MG.Sonarr.Next.Collections.Pools
{
    /// <summary>
    /// Defines the interface for building a pool of objects.
    /// </summary>
    /// <typeparam name="T">The type of objects in the pool.</typeparam>
    public interface IPoolBuilder<T> where T : notnull
    {
        /// <summary>
        /// Sets the maximum capacity of the pool.
        /// </summary>
        int MaxPoolCapacity { set; }

        /// <summary>
        /// Sets the constructor function for creating new instances of <typeparamref name="T"/>.
        /// </summary>
        /// <param name="implementationFactory">The function to create new instances.</param>
        /// <returns>The current <see cref="IPoolBuilder{T}"/> instance.</returns>
        IPoolBuilder<T> SetConstructor(Func<T> implementationFactory);

        /// <summary>
        /// Sets the deconstructor function for resetting instances of <typeparamref name="T"/>.
        /// </summary>
        /// <param name="resetImplementation">The function to reset instances.</param>
        /// <returns>The current <see cref="IPoolBuilder{T}"/> instance.</returns>
        IPoolBuilder<T> SetDeconstructor(Func<T, bool> resetImplementation);
    }

    /// <summary>
    /// Provides an implementation of <see cref="IPoolBuilder{T}"/> for building a pool of objects.
    /// </summary>
    /// <typeparam name="T">The type of objects in the pool.</typeparam>
    internal sealed class PoolBuilder<T> : IPoolBuilder<T> where T : notnull
    {
        /// <summary>
        /// Gets the constructor function for creating new instances of <typeparamref name="T"/>.
        /// </summary>
        internal Func<T> Constructor { get; private set; } = null!;

        /// <summary>
        /// Gets or sets the maximum capacity of the pool.
        /// </summary>
        public int MaxPoolCapacity { get; set; }

        /// <summary>
        /// Gets the deconstructor function for resetting instances of <typeparamref name="T"/>.
        /// </summary>
        internal Func<T, bool> Reset { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PoolBuilder{T}"/> class.
        /// </summary>
        internal PoolBuilder()
        {
            this.Reset = DefaultReset;
            this.MaxPoolCapacity = 10;
        }

        /// <summary>
        /// Sets the constructor function for creating new instances of <typeparamref name="T"/>.
        /// </summary>
        /// <param name="implementationFactory">The function to create new instances.</param>
        /// <returns>The current <see cref="IPoolBuilder{T}"/> instance.</returns>
        public IPoolBuilder<T> SetConstructor(Func<T> implementationFactory)
        {
            ArgumentNullException.ThrowIfNull(implementationFactory);

            this.Constructor = implementationFactory;
            return this;
        }

        /// <summary>
        /// Sets the deconstructor function for resetting instances of <typeparamref name="T"/>.
        /// </summary>
        /// <param name="resetImplementation">The function to reset instances.</param>
        /// <returns>The current <see cref="IPoolBuilder{T}"/> instance.</returns>
        public IPoolBuilder<T> SetDeconstructor(Func<T, bool> resetImplementation)
        {
            ArgumentNullException.ThrowIfNull(resetImplementation);

            this.Reset = resetImplementation;
            return this;
        }

        /// <summary>
        /// The default reset function which always returns <see langword="true"/>.
        /// </summary>
        /// <param name="item">The item to reset.</param>
        /// <returns><see langword="true"/>.</returns>
        private static bool DefaultReset(T item)
        {
            return true;
        }
    }
}

