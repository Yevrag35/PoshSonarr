using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace MG.Sonarr
{
    public class ReplaceHashtable : IDictionary
    {
        #region FIELDS/CONSTANTS
        private Dictionary<string, object> _dict;
        private JsonStringCollection _ig;
        private JsonStringCollection _req;
        private bool ContainsIgnored => _dict.Any(x => x.Key.IndexOf("ignored", StringComparison.CurrentCultureIgnoreCase) >= 0);
        private bool ContainsRequired => _dict.Any(x => x.Key.IndexOf("required", StringComparison.CurrentCultureIgnoreCase) >= 0);

        #endregion

        #region INDEXERS
        public object this[object key]
        {
            get => _dict[Convert.ToString(key)];
            set => _dict[Convert.ToString(key)] = value;
        }

        #endregion

        #region PROPERTIES
        public int Count => _dict.Count;
        public JsonStringCollection Ignored => _ig;

        public JsonStringCollection Required => _req;

        bool IDictionary.IsFixedSize => false;
        bool IDictionary.IsReadOnly => false;
        bool ICollection.IsSynchronized => ((ICollection)_dict).IsSynchronized;
        ICollection IDictionary.Keys => _dict.Keys;
        object ICollection.SyncRoot => ((ICollection)_dict).SyncRoot;
        ICollection IDictionary.Values => _dict.Values;

        #endregion

        #region CONSTRUCTORS
        public ReplaceHashtable(IDictionary dict)
        {
            _ig = new JsonStringCollection();
            _req = new JsonStringCollection();
            _dict = new Dictionary<string, object>(dict.Count);
            this.AddRange(dict.Cast<DictionaryEntry>());
            if (this.ContainsIgnored)
                _ig.Add(_dict.Single(x => x.Key.IndexOf("ignored", StringComparison.CurrentCultureIgnoreCase) >= 0).Value);

            if (this.ContainsRequired)
                _req.Add(_dict.Single(x => x.Key.IndexOf("required", StringComparison.CurrentCultureIgnoreCase) >= 0).Value);
        }

        #endregion

        #region PUBLIC METHODS

        public void Add(string key, object value) => _dict.Add(key, value);
        public void Add(KeyValuePair<string, object> kvp) => _dict.Add(kvp.Key, kvp.Value);
        private void AddRange(IEnumerable<DictionaryEntry> dictionaryEntries)
        {
            foreach (DictionaryEntry de in dictionaryEntries)
            {
                ((IDictionary)_dict).Add(de.Key, de.Value);
            }
        }
        void IDictionary.Add(object key, object value) => ((IDictionary)_dict).Add(key, value);
        public void Clear() => _dict.Clear();
        public bool Contains(object key) => ((IDictionary)_dict).Contains(key);
        public bool ContainsKey(string key) => _dict.ContainsKey(key);
        public bool ContainsValue(object value) => _dict.ContainsValue(value);
        void ICollection.CopyTo(Array array, int index) => ((ICollection)_dict).CopyTo(array, index);
        public IDictionaryEnumerator GetEnumerator() => ((IDictionary)_dict).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IDictionary)_dict).GetEnumerator();
        private bool KeyIsIgnored(object key) => Convert.ToString(key).IndexOf("ignored", StringComparison.CurrentCultureIgnoreCase) >= 0;
        public bool KeyIsRequired(object key) => Convert.ToString(key).IndexOf("required", StringComparison.CurrentCultureIgnoreCase) >= 0;
        public void Remove(string key) => _dict.Remove(key);
        public void Remove(KeyValuePair<string, object> kvp) => _dict.Remove(kvp.Key);
        void IDictionary.Remove(object key) => ((IDictionary)_dict).Remove(key);
        public bool TryGetValue(string key, out object value) => _dict.TryGetValue(key, out value);

        #endregion

        #region OPERATORS/CASTS
        public static implicit operator ReplaceHashtable(Hashtable ht) => new ReplaceHashtable(ht);
        public static implicit operator ReplaceHashtable(OrderedDictionary orderedDict) => new ReplaceHashtable(orderedDict);

        #endregion

        #region BACKEND/PRIVATE METHODS


        #endregion
    }
}