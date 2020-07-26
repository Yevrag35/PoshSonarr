using MG.Sonarr.Functionality;
using MG.Sonarr.Functionality.Strings;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace MG.Sonarr
{
    public class ReplaceHashtable
    {
        #region FIELDS/CONSTANTS
        private const string IGNORED = "Ignored";
        private const string REQUIRED = "Required";

        #endregion

        #region PROPERTIES
        public int Count => this.Ignored.Count + this.Required.Count;

        public JsonStringSet Ignored { get; }
        public JsonStringSet Required { get; }

        #endregion

        #region CONSTRUCTORS
        public ReplaceHashtable(IDictionary dict)
        {
            IEqualityComparer<string> ieq = SonarrFactory.NewIgnoreCase();
            this.Ignored = new JsonStringSet();
            this.Required = new JsonStringSet();

            IEnumerable<string> keys = dict.Keys.Cast<string>();
            if (keys.Contains(IGNORED, ieq))
            {
                string ikey = keys.FirstOrDefault(x => x.Equals(IGNORED, StringComparison.CurrentCultureIgnoreCase));
                if (dict[ikey] is IEnumerable enumerable1)
                {
                    foreach (IConvertible icon1 in enumerable1)
                    {
                        this.Ignored.Add(Convert.ToString(icon1));
                    }
                }
                else if (dict[ikey] is string str1)
                    this.Ignored.Add(str1);
            }
            if (keys.Contains(REQUIRED, ieq))
            {
                string rkey = keys.FirstOrDefault(x => x.Equals(REQUIRED, StringComparison.CurrentCultureIgnoreCase));
                if (dict[rkey] is IEnumerable enumerable2)
                {
                    foreach (IConvertible icon2 in enumerable2)
                    {
                        this.Required.Add(Convert.ToString(icon2));
                    }
                }
                else if (dict[rkey] is string str2)
                    this.Required.Add(str2);
            }
        }

        #endregion

        #region PUBLIC METHODS

        //public void Add(string key, object value) => _dict.Add(key, value);
        //public void Add(KeyValuePair<string, object> kvp) => _dict.Add(kvp.Key, kvp.Value);
        //private void AddRange(IEnumerable<DictionaryEntry> dictionaryEntries)
        //{
        //    foreach (DictionaryEntry de in dictionaryEntries)
        //    {
        //        ((IDictionary)_dict).Add(de.Key, de.Value);
        //    }
        //}
        //void IDictionary.Add(object key, object value) => ((IDictionary)_dict).Add(key, value);
        //public void Clear() => _dict.Clear();
        //public bool Contains(object key) => ((IDictionary)_dict).Contains(key);
        //public bool ContainsKey(string key) => _dict.ContainsKey(key);
        //public bool ContainsValue(object value) => _dict.ContainsValue(value);
        //void ICollection.CopyTo(Array array, int index) => ((ICollection)_dict).CopyTo(array, index);
        //public IDictionaryEnumerator GetEnumerator() => ((IDictionary)_dict).GetEnumerator();
        //IEnumerator IEnumerable.GetEnumerator() => ((IDictionary)_dict).GetEnumerator();
        //private bool KeyIsIgnored(object key) => Convert.ToString(key).IndexOf("ignored", StringComparison.CurrentCultureIgnoreCase) >= 0;
        //public bool KeyIsRequired(object key) => Convert.ToString(key).IndexOf("required", StringComparison.CurrentCultureIgnoreCase) >= 0;
        //public void Remove(string key) => _dict.Remove(key);
        //public void Remove(KeyValuePair<string, object> kvp) => _dict.Remove(kvp.Key);
        //void IDictionary.Remove(object key) => ((IDictionary)_dict).Remove(key);
        //public bool TryGetValue(string key, out object value) => _dict.TryGetValue(key, out value);

        #endregion

        #region OPERATORS/CASTS
        public static implicit operator ReplaceHashtable(Hashtable ht) => new ReplaceHashtable(ht);
        public static implicit operator ReplaceHashtable(OrderedDictionary orderedDict) => new ReplaceHashtable(orderedDict);

        #endregion
    }
}