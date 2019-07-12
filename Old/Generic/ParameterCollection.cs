using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Sonarr.Api.Endpoints
{
    public class ParameterCollection : IList<Parameter>
    {
        private List<Parameter> _list;

        public ParameterCollection() => _list = new List<Parameter>();
		public ParameterCollection(IEnumerable<Parameter> ps)
        {
            _list = new List<Parameter>();
            _list.AddRange(ps);
        }
		public ParameterCollection(Parameter[] ps)
        {
            _list = new List<Parameter>();
            _list.AddRange(ps);
        }
        public ParameterCollection(Parameter p)
        {
            _list = new List<Parameter>();
            _list.Add(p);
        }

		public Parameter this[string key, Type type] => _list.Single(x => x.Name == key && x.Type.Equals(type));

		public string ToQuery()
        {
            if (_list.Count <= 0)
                return null;

            var cons = new string[_list.Count];
			for (int i = 0; i < _list.Count; i++)
            {
                var p = _list[i];
                cons[i] = p.Name.ToLower() + "=" + Convert.ToString(p.Value);
            }
            return "?" + string.Join("&", cons);
        }

        #region IList Inheritance
        public Parameter this[int index]
        {
            get => _list[index];
            set => _list[index] = value;
        }
        public int Count => _list.Count;
        public bool IsReadOnly => false;
        public void Add(Parameter item) => _list.Add(item);
        public void AddRange(IEnumerable<Parameter> items)
        {
			foreach(Parameter p in items)
            {
                if (!this.Contains(p))
                    this.Add(p);
            }
        }
        public void Clear() => _list.Clear();
        public bool Contains(Parameter item) => _list.Contains(item);
        public void CopyTo(Parameter[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);
        public IEnumerator<Parameter> GetEnumerator() => _list.GetEnumerator();
        public int IndexOf(Parameter item) => _list.IndexOf(item);
        public void Insert(int index, Parameter item) => _list.Insert(index, item);
        public bool Remove(Parameter item) => _list.Remove(item);
        public void RemoveAt(int index) => _list.RemoveAt(index);
        public Parameter[] ToArray() => _list.ToArray();
        IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();

        #endregion
    }
}
