using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace MG.Sonarr.Functionality
{
    public class UrlParameterCollection : IUrlParameterCollection
    {
        private List<IUrlParameter> _list;
        private const string SEPARATOR = "&";

        public IUrlParameter this[int index] 
        {
            get => _list[index]; 
            set => _list[index] = value; 
        }

        public int Count => _list.Count;
        bool ICollection<IUrlParameter>.IsReadOnly => ((ICollection<IUrlParameter>)_list).IsReadOnly;

        public UrlParameterCollection() => _list = new List<IUrlParameter>();
        public UrlParameterCollection(int capacity) => _list = new List<IUrlParameter>(capacity);
        public UrlParameterCollection(IEnumerable<IUrlParameter> items) => _list = new List<IUrlParameter>(items);

        public void Add(IUrlParameter item) => _list.Add(item);
        public void AddRange(IEnumerable<IUrlParameter> items) => _list.AddRange(items);
        public void Clear() => _list.Clear();
        public bool Contains(IUrlParameter item) => _list.Contains(item);
        void ICollection<IUrlParameter>.CopyTo(IUrlParameter[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);
        public IEnumerator<IUrlParameter> GetEnumerator() => _list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
        public int IndexOf(IUrlParameter item) => _list.IndexOf(item);
        public void Insert(int index, IUrlParameter item) => _list.Insert(index, item);
        public bool Remove(IUrlParameter item) => _list.Remove(item);
        public void RemoveAt(int index) => _list.RemoveAt(index);

        public string ToQueryString()
        {
            var strs = new List<string>(_list.Count);
            _list.ForEach((x) =>
            {
                strs.Add(x.AsString());
            });

            return "?" + string.Join(SEPARATOR, strs);
        }
    }
}
