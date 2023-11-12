using Microsoft.Extensions.DependencyInjection;

namespace MG.Sonarr.Next.Collections.Pools
{
    file sealed class GenericObjectPool<T> : SonarrObjectPool<T> where T : notnull
    {
        readonly PoolBuilder<T> _builder;
        protected override int MaxPoolCapacity { get; }

        private GenericObjectPool(PoolBuilder<T> builder)
        {
            _builder = builder;
            this.MaxPoolCapacity = builder.MaxPoolCapacity;
        }

        protected override T Construct()
        {
            return _builder.Constructor.Invoke();
        }

        protected override bool ResetObject(T obj)
        {
            return _builder.Reset.Invoke(obj);
        }

        internal static GenericObjectPool<T> Create(Action<IPoolBuilder<T>> configureBuilder)
        {
            PoolBuilder<T> builder = new();
            configureBuilder(builder);

            return builder.Constructor is not null
                ? new(builder)
                : throw new InvalidOperationException("The constructor for the pool must at least be defined.");
        }
        internal static GenericObjectPool<T> Create(PoolBuilder<T> builder)
        {
            return builder.Constructor is not null
                ? new(builder)
                : throw new InvalidOperationException("The constructor for the pool must at least be defined.");
        }
    }

    public static class GenericObjectPoolDependencyInjection
    {
        public static IServiceCollection AddGenericObjectPool<T>(this IServiceCollection services, Action<IPoolBuilder<T>> configurePool) where T : notnull
        {
            var pool = GenericObjectPool<T>.Create(configurePool);

            return RegisterPoolTypes<T, GenericObjectPool<T>>(services, pool);
        }
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

