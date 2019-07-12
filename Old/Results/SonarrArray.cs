using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;

namespace Sonarr.Api.Results
{
    public class SonarrArray<T> : IList<T> where T : SonarrResult
    {
        private List<T> _list;

        #region CONSTRUCTORS

        public SonarrArray() =>
            _list = new List<T>();

        public SonarrArray(IEnumerable<T> items) =>
            _list = new List<T>(items);

        public SonarrArray(int capacity) =>
            _list = new List<T>(capacity);

        #endregion

        #region ILIST METHODS

        public T this[int index]
        {
            get => _list[index];
            set => _list[index] = value;
        }

        public int Count => _list.Count;

        public bool IsReadOnly => false;

        public void Add(T item) => _list.Add(item);

        public void Clear() => _list.Clear();

        public bool Contains(T item) => _list.Contains(item);

        public void CopyTo(T[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);

        public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();

        public int IndexOf(T item) => _list.IndexOf(item);

        public void Insert(int index, T item) => _list.Insert(index, item);

        public bool Remove(T item) => _list.Remove(item);

        public void RemoveAt(int index) => _list.RemoveAt(index);

        IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();

        #endregion

        #region OPERATORS

        public static explicit operator SonarrArray<T>(JArray jar)
        {
            var sa = new SonarrArray<T>();
            for (int i = 0; i < jar.Count; i++)
            {
                var job = (JObject)jar[i];
                var sr = SonarrResult.FromJObject<T>(job);
                sa.Add(sr);
            }
            return sa;
        }

        #endregion
    }
}
