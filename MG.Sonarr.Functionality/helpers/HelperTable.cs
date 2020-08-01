using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace MG.Sonarr.Functionality.Helpers
{
    public class HelperTable : IDictionary<string, List<object>>
    {
        private const string NO_KEY = "nokey";
        private Dictionary<string, List<object>> _dict;

        public List<object> this[string key]
        { 
            get => _dict[key];
            set => _dict[key] = value;
        }

        public IEqualityComparer<string> KeyComparer { get; }
        public IEqualityComparer<object> ValueComparer { get; }
        public int Count => _dict.Count;
        /// <summary>
        /// Indicates whether the <see cref="HelperTable"/> currently contains a key called "nokey" which is used for uncategorized values.
        /// </summary>
        public bool HasNoKey => _dict.ContainsKey(NO_KEY);
        public ICollection<string> Keys => _dict.Keys;

        protected HelperTable(int capacity)
        {
            this.KeyComparer = SonarrFactory.NewIgnoreCase();
            this.ValueComparer = SonarrFactory.NewObjectIgnoreCase();
            _dict = new Dictionary<string, List<object>>(capacity);
        }

        public void Add(string key, List<object> value) => _dict.Add(key.ToLower(), value);
        public void Clear() => _dict.Clear();
        public bool ContainsKey(string key) => _dict.ContainsKey(key.ToLower());
        public IEnumerator<KeyValuePair<string, List<object>>> GetEnumerator() => _dict.GetEnumerator();
        public IList<object> GetValues() => _dict.Values.SelectMany(x => x).ToList();
        public IList<object> GetNoKeyValues() => _dict[NO_KEY];
        public IEnumerable<object> GetUniqueNoKeyValues() => _dict[NO_KEY].Distinct(this.ValueComparer);
        public bool Remove(string key) => _dict.Remove(key.ToLower());
        public bool TryGetValue(string key, out List<object> value) => _dict.TryGetValue(key, out value);

        #region PROCESSING
        private static HelperTable EnumerateDictionary(IDictionary dict)
        {
            var table = new HelperTable(dict.Count);
            foreach (DictionaryEntry de in dict)
            {
                var list = new List<object>(EnumerateObject(de.Value));

                table.Add(Convert.ToString(de.Key), list);
            }
            return table;
        }
        private static IEnumerable<object> EnumerateObjects(object[] objs)
        {
            foreach (object o in objs)
            {
                foreach (object innerO in EnumerateObject(o))
                {
                    yield return innerO;
                }
            }
        }
        private static IEnumerable<object> EnumerateObject(object o)
        {
            if (!(o is string) && o is IEnumerable enumerable)
            {
                foreach (object io in enumerable)
                {
                    yield return io;
                }
            }
            else
                yield return o;
        }

        private static IEnumerable<HelperTable> ProcessObjects(object[] objs)
        {
            foreach (object o in objs)
            {
                if (o is IDictionary dict)
                {
                    yield return EnumerateDictionary(dict);
                }
                else
                {
                    IEnumerable<object> flatten = EnumerateObjects(objs);

                    HelperTable table = new HelperTable(1)
                    {
                        { NO_KEY, new List<object>(flatten) }
                    };
                    yield return table;
                }
            }
        }

        public static HelperTable NewTable(object[] objs)
        {
            if (objs == null || objs.Length <= 0)
                return null;

            var table = new HelperTable(objs.Length);
            IEnumerable<HelperTable> tables = ProcessObjects(objs);
            HashSet<string> set = new HashSet<string>(tables.SelectMany(x => x.Keys), table.KeyComparer);
            foreach (string s in set)
            {
                IEnumerable<object> getThese = tables.Where(x => x.ContainsKey(s)).SelectMany(x => x[s]);
                table.Add(s, new List<object>(getThese));
            }
            return table;
        }

        #endregion


        #region IDICTIONARY INTERFACE MEMBERS

        bool ICollection<KeyValuePair<string, List<object>>>.IsReadOnly => ((ICollection<KeyValuePair<string, List<object>>>)_dict).IsReadOnly;
        ICollection<List<object>> IDictionary<string, List<object>>.Values => _dict.Values;

        IEnumerator IEnumerable.GetEnumerator() => _dict.GetEnumerator();
        void ICollection<KeyValuePair<string, List<object>>>.Add(KeyValuePair<string, List<object>> item) => _dict.Add(item.Key, item.Value);
        bool ICollection<KeyValuePair<string, List<object>>>.Contains(KeyValuePair<string, List<object>> item) => ((ICollection<KeyValuePair<string, List<object>>>)_dict).Contains(item);
        void ICollection<KeyValuePair<string, List<object>>>.CopyTo(KeyValuePair<string, List<object>>[] array, int arrayIndex) => ((ICollection<KeyValuePair<string, List<object>>>)_dict).CopyTo(array, arrayIndex);
        bool ICollection<KeyValuePair<string, List<object>>>.Remove(KeyValuePair<string, List<object>> item) => ((ICollection<KeyValuePair<string, List<object>>>)_dict).Remove(item);


        #endregion
    }
}
