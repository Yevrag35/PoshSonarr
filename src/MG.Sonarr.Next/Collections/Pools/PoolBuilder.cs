namespace MG.Sonarr.Next.Collections.Pools
{
    public interface IPoolBuilder<T> where T : notnull
    {
        int MaxPoolCapacity { set; }
        IPoolBuilder<T> SetConstructor(Func<T> implementationFactory);
        IPoolBuilder<T> SetDeconstructor(Func<T, bool> resetImplementation);
    }

    internal sealed class PoolBuilder<T> : IPoolBuilder<T> where T : notnull
    {
        internal Func<T> Constructor { get; private set; } = null!;
        public int MaxPoolCapacity { get; set; }
        internal Func<T, bool> Reset { get; private set; }

        internal PoolBuilder()
        {
            this.Reset = DefaultReset;
            this.MaxPoolCapacity = 20;
        }

        public IPoolBuilder<T> SetConstructor(Func<T> implementationFactory)
        {
            ArgumentNullException.ThrowIfNull(implementationFactory);

            this.Constructor = implementationFactory;
            return this;
        }
        public IPoolBuilder<T> SetDeconstructor(Func<T, bool> resetImplementation)
        {
            ArgumentNullException.ThrowIfNull(resetImplementation);

            this.Reset = resetImplementation;
            return this;
        }

        private static bool DefaultReset(T item)
        {
            return true;
        }
    }
}

