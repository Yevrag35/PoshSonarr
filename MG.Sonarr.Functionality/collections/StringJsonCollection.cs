using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Functionality
{
    public class JsonStringSet : ISet<string>
    {
        private HashSet<string> _set;

        public JsonStringSet() : this(true) { }
        public JsonStringSet(bool ignoreCase) => _set = new HashSet<string>(ignoreCase ? new IgnoreCase() : null);
        public JsonStringSet(IEnumerable<string> other) : this(other, true) { }
        public JsonStringSet(IEnumerable<string> other, bool ignoreCase) => _set = new HashSet<string>(other, ignoreCase ? new IgnoreCase() : null);


        public int Count => _set.Count;
        bool ICollection<string>.IsReadOnly => ((ICollection<string>)_set).IsReadOnly;

        public bool Add(string item) => _set.Add(item);
        void ICollection<string>.Add(string item) => _set.Add(item);
        public void Clear() => _set.Clear();
        public bool Contains(string item) => _set.Contains(item);
        void ICollection<string>.CopyTo(string[] array, int arrayIndex) => _set.CopyTo(array, arrayIndex);
        public void ExceptWith(IEnumerable<string> other) => _set.ExceptWith(other);
        public IEnumerator<string> GetEnumerator() => _set.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _set.GetEnumerator();
        public void IntersectWith(IEnumerable<string> other) => _set.IntersectWith(other);
        public bool IsProperSubsetOf(IEnumerable<string> other) => _set.IsProperSubsetOf(other);
        public bool IsProperSupersetOf(IEnumerable<string> other) => _set.IsProperSupersetOf(other);
        public bool IsSubsetOf(IEnumerable<string> other) => _set.IsSubsetOf(other);
        public bool IsSupersetOf(IEnumerable<string> other) => _set.IsSupersetOf(other);
        public bool Overlaps(IEnumerable<string> other) => _set.Overlaps(other);
        public bool Remove(string item) => _set.Remove(item);
        public bool SetEquals(IEnumerable<string> other) => _set.SetEquals(other);
        public void SymmetricExceptWith(IEnumerable<string> other) => _set.SymmetricExceptWith(other);
        public void UnionWith(IEnumerable<string> other) => _set.UnionWith(other);
        
    }
}
