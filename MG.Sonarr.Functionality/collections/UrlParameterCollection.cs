using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MG.Sonarr.Functionality.Collections
{
    public class UrlParameterCollection :  IUrlParameterCollection
    {
        private List<IUrlParameter> _list;
        private const string SEPARATOR = "&";
        private const string START_FILTER = "?";

        public IUrlParameter this[int index] 
        {
            get => _list[index]; 
            set => _list[index] = value; 
        }

        public int Count => _list.Count;
        bool ICollection<IUrlParameter>.IsReadOnly => ((ICollection<IUrlParameter>)_list).IsReadOnly;
        public int Length => 1 + _list.Sum(x => x.Length);

        public UrlParameterCollection() => _list = new List<IUrlParameter>();
        public UrlParameterCollection(int capacity) => _list = new List<IUrlParameter>(capacity);
        public UrlParameterCollection(IEnumerable<IUrlParameter> items) => _list = new List<IUrlParameter>(items.Where(x => x != null));

        public void Add(IUrlParameter item) => _list.Add(item);
        public void AddRange(IEnumerable<IUrlParameter> items)
        {
            if (items != null)
                _list.AddRange(items.Where(x => x != null));
        }
        public void Clear() => _list.Clear();
        public bool Contains(IUrlParameter item) => _list.Contains(item);
        void ICollection<IUrlParameter>.CopyTo(IUrlParameter[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);
        public IEnumerator<IUrlParameter> GetEnumerator() => _list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
        public int IndexOf(IUrlParameter item) => _list.IndexOf(item);
        public void Insert(int index, IUrlParameter item)
        {
            if (item != null)
            {
                _list.Insert(index, item);
            }
        }
        public bool Remove(IUrlParameter item) => _list.Remove(item);
        public void RemoveAt(int index) => _list.RemoveAt(index);

        public void ToQueryString(ref StringBuilder builder, params IUrlParameter[] oneOffs)
        {
            for (int i = 0; i < this.Count; i++)
            {
                builder.Append(this[i].AsString());
                if (i < this.Count - 1)
                    builder.Append(SEPARATOR);
            }

            if (oneOffs != null && oneOffs.Length > 0)
            {
                if (builder.Length > 0)
                    builder.Append(SEPARATOR);

                for (int n = 0; n < oneOffs.Length; n++)
                {
                    builder.Append(oneOffs[n].AsString());
                    if (n < oneOffs.Length - 1)
                        builder.Append(SEPARATOR);
                }
            }

            builder.Insert(0, START_FILTER);
        }
        public string ToQueryString(params IUrlParameter[] oneOffs)
        {
            int length = this.Length + (oneOffs?.Sum(x => x.Length)).GetValueOrDefault();
            var builder = new StringBuilder(length);
            this.ToQueryString(ref builder, oneOffs);
            return builder.ToString();
        }
    }
}
