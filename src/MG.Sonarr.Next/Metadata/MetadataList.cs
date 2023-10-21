using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Json;
using System.Collections;

namespace MG.Sonarr.Next.Metadata
{
    public sealed class MetadataList<T> : IList<T>, IJsonMetadataTaggable, ISortable
        where T : IComparable<T>, IJsonMetadataTaggable
    {
        readonly List<T> _list;

        public T this[int index]
        {
            get => _list[index];
            set => _list[index] = value;
        }

        public int Count => _list.Count;
        bool ICollection<T>.IsReadOnly => false;
        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => _list;

        public MetadataList()
            : this(0)
        {
        }
        public MetadataList(int capacity)
        {
            _list = new(capacity);
        }
        public MetadataList(IEnumerable<T> items)
        {
            items ??= Enumerable.Empty<T>();
            _list = new(items);
        }

        public void Add(T item)
        {
            if (item is not null)
            {
                _list.Add(item);
            }
        }

        /// <inheritdoc cref="List{T}.AddRange(IEnumerable{T})"/>
        public void AddRange(IEnumerable<T> collection)
        {
            collection ??= Enumerable.Empty<T>();
            _list.AddRange(collection);
        }
        public void Clear()
        {
            _list.Clear();
        }
        public bool Contains(T item)
        {
            return _list.Contains(item);
        }
        public void CopyTo(T[] array, int index)
        {
            _list.CopyTo(array, index);
        }
        void ICollection.CopyTo(Array array, int index)
        {
            ArgumentNullException.ThrowIfNull(array);
            ((ICollection)_list).CopyTo(array, index);
        }
        public IEnumerator<T> GetEnumerator()
        {
            return _list.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
        public int IndexOf(T item)
        {
            return _list.IndexOf(item);
        }
        public void Insert(int index, T item)
        {
            _list.Insert(index, item);
        }
        public bool Remove(T item)
        {
            return _list.Remove(item);
        }
        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
        }

        public void SetTag(IMetadataResolver resolver)
        {
            if (this.Count <= 0)
            {
                return;
            }

            foreach (T item in this)
            {
                item.SetTag(resolver);
            }
        }

        /// <inheritdoc cref="List{T}.Sort()"/>
        public void Sort()
        {
            _list.Sort();
        }
    }
}
