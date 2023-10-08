using System.Collections.Concurrent;

namespace MG.Sonarr.Next.Collections
{
    /// <summary>
    /// An interface exposing a property and method of an <see cref="IObjectPool{T}"/> to be able to return
    /// non-generic object instances.
    /// </summary>
    public interface IObjectPoolReturnable
    {
        /// <summary>
        /// The type of objects the <see cref="IObjectPoolReturnable"/> can return.
        /// </summary>
        Type ReturnsType { get; }

        /// <summary>
        /// Returns an object back into the pool.
        /// </summary>
        /// <param name="obj">The object to return.</param>
        void Return(object? obj);
    }

    /// <summary>
    /// An interface exposing methods for retrieving and returning to and from a pool of objects.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IObjectPool<T> where T : notnull
    {
        /// <summary>
        /// Retrieves a single object from the pool. If no items are present in the pool, a new object will 
        /// constructed instead.
        /// </summary>
        /// <returns>A cached or constructed object instance from the pool.</returns>
        T Get();

        /// <summary>
        /// Returns an object back into the pool.
        /// </summary>
        /// <param name="item">The object to return.</param>
        void Return(T item);
    }

    /// <summary>
    /// A base class allowing for pooling of objects.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class SonarrObjectPool<T> : IObjectPoolReturnable, IObjectPool<T> where T : notnull
    {
        ConcurrentBag<T> Bag { get; }
        /// <summary>
        /// The maximum number of objects to kept in the pool at any one time.
        /// </summary>
        protected abstract int MaxPoolCapacity { get; }
        public Type ReturnsType => typeof(T);

        /// <summary>
        /// The default constructor initializing an empty pool.
        /// </summary>
        protected SonarrObjectPool()
        {
            this.Bag = new();
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="SonarrObjectPool{T}"/> class that contains an initial
        /// pool of objects copied from the specified collection.
        /// </summary>
        /// <param name="initialItems">
        ///     The collection whose elements are copied into the
        ///     new <see cref="SonarrObjectPool{T}"/>.
        /// </param>
        protected SonarrObjectPool(IEnumerable<T> initialItems)
        {
            initialItems ??= Enumerable.Empty<T>();
            this.Bag = new(initialItems);
        }

        public T Get()
        {
            return this.GetItemFromBag();
        }

        /// <summary>
        /// An overridable method that retrieves an item from the pool or constructs a new one.
        /// </summary>
        /// <returns>A new or retrieved item from the pool.</returns>
        protected virtual T GetItemFromBag()
        {
            return this.GetItemFromBag(out _);
        }
        /// <summary>
        /// Retrieves or constructs an item from the pool.
        /// </summary>
        /// <param name="wasConstructed">A flag that indicates whether the item was constructed.</param>
        /// <returns>A new or retrieved item from the pool.</returns>
        protected T GetItemFromBag(out bool wasConstructed)
        {
            wasConstructed = false;
            if (!this.Bag.TryTake(out T? item))
            {
                item = this.Construct();
                wasConstructed = true;
            }

            return item;
        }

        /// <summary>
        /// Returns an item back to the pool.
        /// </summary>
        /// <remarks>
        ///     If the item fails to be reset or the pool is at its max capacity, it will not be returned.
        /// </remarks>
        /// <param name="item">The object to return.</param>
        public void Return([MaybeNull] T item)
        {
            if (item is null)
            {
                return;
            }

            int count = this.Bag.Count;
            if (count < this.MaxPoolCapacity && this.ResetObject(item))
            {
                this.Bag.Add(item);
            }
        }
        void IObjectPoolReturnable.Return(object? obj)
        {
            if (obj is T item)
            {
                this.Return(item: item);
            }
        }

        /// <summary>
        /// Constructs a new item when one is not available from the pool.
        /// </summary>
        /// <returns>
        ///     A newly, constructed instance of <typeparamref name="T"/>.
        /// </returns>
        protected abstract T Construct();
        /// <summary>
        /// Resets an object being requested to be returned to the pool.
        /// </summary>
        /// <param name="obj">The item being returned.</param>
        /// <returns>
        ///     <see langword="true"/> if the item has been reset successfully and should be returned
        ///     to the pool; otherwise, <see langword="false"/>.
        /// </returns>
        protected abstract bool ResetObject(T obj);
    }
}
