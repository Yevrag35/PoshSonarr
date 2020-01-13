using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace MG.Sonarr.Results
{
    public class ErrorResultCollection : BaseResult, IList<ErrorResultException>
    {
        private List<ErrorResultException> _list;

        public int Count => _list.Count;
        public bool IsReadOnly => ((ICollection<ErrorResultException>)_list).IsReadOnly;

        public ErrorResultException this[int index]
        {
            get => _list[index];
            set => _list[index] = value;
        }

        public ErrorResultCollection() => _list = new List<ErrorResultException>();
        public ErrorResultCollection(int capacity) => _list = new List<ErrorResultException>(capacity);
        public ErrorResultCollection(IEnumerable<ErrorResultException> errorResults) => _list = new List<ErrorResultException>(errorResults);

        public void Add(ErrorResultException item) => _list.Add(item);
        public void Clear() => ((IList<ErrorResultException>)_list).Clear();
        public bool Contains(ErrorResultException item) => _list.Contains(item);
        void ICollection<ErrorResultException>.CopyTo(ErrorResultException[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);
        public int IndexOf(ErrorResultException item) => _list.IndexOf(item);
        public void Insert(int index, ErrorResultException item) => _list.Insert(index, item);
        public bool Remove(ErrorResultException item) => _list.Remove(item);
        public void RemoveAt(int index) => _list.RemoveAt(index);
        
        public IEnumerator<ErrorResultException> GetEnumerator() => _list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
    }
}
