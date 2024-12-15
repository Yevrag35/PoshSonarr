using Microsoft.Extensions.DependencyInjection;

namespace MG.Sonarr.Next.Collections.Pools
{
    /// <summary>
    /// Represents a generic object pool.
    /// </summary>
    /// <typeparam name="T">The type of objects to pool.</typeparam>
    internal class GenericObjectPool<T> : SonarrObjectPool<T> where T : notnull
    {
        readonly PoolBuilder<T> _builder;

        /// <summary>
        /// Gets the maximum pool capacity.
        /// </summary>
        protected override int MaxPoolCapacity { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericObjectPool{T}"/> class.
        /// </summary>
        /// <param name="builder">The pool builder.</param>
        [DebuggerStepThrough]
        private GenericObjectPool(PoolBuilder<T> builder)
        {
            _builder = builder;
            this.MaxPoolCapacity = builder.MaxPoolCapacity;
        }

        /// <summary>
        /// Constructs a new object.
        /// </summary>
        /// <returns>A new instance of <typeparamref name="T"/>.</returns>
        protected override T Construct()
        {
            return _builder.Constructor.Invoke();
        }

        /// <summary>
        /// Resets the specified object.
        /// </summary>
        /// <param name="obj">The object to reset.</param>
        /// <returns><see langword="true"/> if the object was successfully reset; otherwise, <see langword="false"/>.</returns>
        protected override bool ResetObject(T obj)
        {
            return _builder.Reset.Invoke(obj);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="GenericObjectPool{T}"/> class.
        /// </summary>
        /// <param name="configureBuilder">The action to configure the pool builder.</param>
        /// <returns>A new instance of <see cref="GenericObjectPool{T}"/>.</returns>
        internal static GenericObjectPool<T> Create(Action<IPoolBuilder<T>> configureBuilder)
        {
            PoolBuilder<T> builder = new();
            configureBuilder(builder);

            return builder.Constructor is not null
                ? new(builder)
                : throw new InvalidOperationException("The constructor for the pool must at least be defined.");
        }

        /// <summary>
        /// Creates a new instance of the <see cref="GenericObjectPool{T}"/> class.
        /// </summary>
        /// <param name="builder">The pool builder.</param>
        /// <returns>A new instance of <see cref="GenericObjectPool{T}"/>.</returns>
        internal static GenericObjectPool<T> Create(PoolBuilder<T> builder)
        {
            return builder.Constructor is not null
                ? new(builder)
                : throw new InvalidOperationException("The constructor for the pool must at least be defined.");
        }
    }

    /// <summary>
    /// Represents a generic resettable object pool.
    /// </summary>
    /// <typeparam name="T">The type of objects to pool.</typeparam>
    internal sealed class GenericResettableObjectPool<T> : SonarrObjectPool<T>, IQuickPool<T> where T : notnull, IResettable
    {
        private readonly IServiceProvider _provider;

        /// <summary>
        /// Gets the maximum pool capacity.
        /// </summary>
        protected override int MaxPoolCapacity => 10;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericResettableObjectPool{T}"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        public GenericResettableObjectPool(IServiceProvider serviceProvider)
        {
            _provider = serviceProvider;
        }

        /// <summary>
        /// Constructs a new object.
        /// </summary>
        /// <returns>A new instance of <typeparamref name="T"/>.</returns>
        protected override T Construct()
        {
            return _provider.GetRequiredService<T>();
        }

        /// <summary>
        /// Resets the specified object.
        /// </summary>
        /// <param name="obj">The object to reset.</param>
        /// <returns><see langword="true"/> if the object was successfully reset; otherwise, <see langword="false"/>.</returns>
        protected override bool ResetObject(T obj)
        {
            return obj.TryReset();
        }
    }

    /// <summary>
    /// Provides extension methods for adding generic object pools to the service collection.
    /// </summary>
    public static class GenericObjectPoolDependencyInjection
    {
        /// <summary>
        /// Adds a generic object pool to the service collection.
        /// </summary>
        /// <typeparam name="T">The type of objects to pool.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="configurePool">The action to configure the pool builder.</param>
        /// <returns>The updated service collection.</returns>
        public static IServiceCollection AddGenericObjectPool<T>(this IServiceCollection services, Action<IPoolBuilder<T>> configurePool) where T : notnull
        {
            var pool = GenericObjectPool<T>.Create(configurePool);

            return RegisterPoolTypes<T, GenericObjectPool<T>>(services, pool);
        }

        /// <summary>
        /// Adds a generic object pool to the service collection.
        /// </summary>
        /// <typeparam name="T">The type of objects to pool.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="resetImplementation">The reset implementation.</param>
        /// <returns>The updated service collection.</returns>
        public static IServiceCollection AddGenericObjectPool<T>(this IServiceCollection services, Func<T, bool> resetImplementation) where T : notnull, new()
        {
            PoolBuilder<T> builder = new();
            builder.SetConstructor(() => new());
            builder.SetDeconstructor(resetImplementation);

            GenericObjectPool<T> pool = GenericObjectPool<T>.Create(builder);

            return RegisterPoolTypes<T, GenericObjectPool<T>>(services, pool);
        }

        private static IServiceCollection RegisterPoolTypes<T, TPool>(IServiceCollection services, TPool pool)
            where TPool : class, IObjectPool<T>
            where T : notnull
        {
            return services
                .AddSingleton<TPool>(pool)
                .AddSingleton<IObjectPool<T>>(p => p.GetRequiredService<GenericObjectPool<T>>())
                .AddSingleton<IObjectPoolReturnable>(p => p.GetRequiredService<GenericObjectPool<T>>());
        }
    }
}

