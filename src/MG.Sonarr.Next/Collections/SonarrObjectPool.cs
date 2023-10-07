using System.Collections.Concurrent;

namespace MG.Sonarr.Next.Collections
{
    public interface IObjectPoolReturnable
    {
        Type ReturnsType { get; }
        void Return(object? obj);
    }

    public interface IObjectPool<T> where T : notnull
    {
        T Get();
        void Return(T item);
    }

    public abstract class SonarrObjectPool<T> : IObjectPoolReturnable, IObjectPool<T> where T : notnull
    {
        ConcurrentBag<T> Bag { get; }
        protected abstract int MaxPoolCapacity { get; }
        Type IObjectPoolReturnable.ReturnsType => typeof(T);

        protected SonarrObjectPool()
        {
            this.Bag = new();
        }
        protected SonarrObjectPool(IEnumerable<T> initialItems)
        {
            this.Bag = new(initialItems);
        }

        public T Get()
        {
            return this.GetItem();
        }

        protected virtual T GetItem()
        {
            return this.GetItem(out _);
        }
        protected T GetItem(out bool wasConstructed)
        {
            wasConstructed = false;
            if (!this.Bag.TryTake(out T? item))
            {
                item = this.Construct();
                wasConstructed = true;
            }

            return item;
        }

        public void Return(T item)
        {
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

        protected abstract T Construct();
        protected abstract bool ResetObject(T obj);
    }
}
