using MG.Sonarr.Results;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;

namespace MG.Sonarr
{
    [Serializable]
    public class SonarrBodyParameters : BaseResult, IDictionary
    {
        private IDictionary _dict;

        public object this[object key] 
        { 
            get => _dict[key];
            set => _dict[key] = value;
        }

        public int Count => _dict.Count;
        bool IDictionary.IsFixedSize => _dict.IsFixedSize;
        bool IDictionary.IsReadOnly => _dict.IsReadOnly;
        bool ICollection.IsSynchronized => _dict.IsSynchronized;
        public ICollection Keys => _dict.Keys;
        object ICollection.SyncRoot => _dict.SyncRoot;
        public ICollection Values => _dict.Values;

        public SonarrBodyParameters() => _dict = new Hashtable(2);
        public SonarrBodyParameters(int capacity) => _dict = new Hashtable(capacity);
        public SonarrBodyParameters(IDictionary otherEntries) => _dict = new Hashtable(otherEntries);

        #region METHODS
        public void Add(object key, object value) => _dict.Add(key, value);
        public void Clear() => _dict.Clear();
        public bool Contains(object key) => _dict.Contains(key);
        void ICollection.CopyTo(Array array, int index) => _dict.CopyTo(array, index);
        public IDictionaryEnumerator GetEnumerator() => _dict.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _dict.GetEnumerator();
        public void Remove(object key) => _dict.Remove(key);

        #endregion
    }
}
