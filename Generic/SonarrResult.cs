using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Sonarr.Api
{
    public class SonarrResult : IList<Dictionary<object, object>>
    {
        private List<Dictionary<object, object>> _lojob;

        public int Count => _lojob.Count;

        public bool IsReadOnly => false;

        public Dictionary<object, object> this[int index]
        {
            get => _lojob[index];
            set => _lojob[index] = value;
        }

        public SonarrResult(dynamic answer)
        {
            _lojob = new List<Dictionary<object, object>>();
            Type at = answer.GetType();
            if (at.Equals(typeof(JObject)))
            {
                var real = (JObject)answer;
                var dict = JsonConvert.DeserializeObject<Dictionary<object, object>>(JsonConvert.SerializeObject(real));
                _lojob.Add(dict);
            }
            else if (at.Equals(typeof(JArray)))
            {
                var real = (JArray)answer;
                for (int i = 0; i < real.Count; i++)
                {
                    var job = (JObject)real[i];
                    var dict = JsonConvert.DeserializeObject<Dictionary<object, object>>(JsonConvert.SerializeObject(job));
                    _lojob.Add(dict);
                }
            }
        }

        public int IndexOf(Dictionary<object, object> item) => _lojob.IndexOf(item);
        public void Insert(int index, Dictionary<object, object> item) => _lojob.Insert(index, item);
        public void RemoveAt(int index) => _lojob.RemoveAt(index);
        public void Add(Dictionary<object, object> item) => _lojob.Add(item);
        public void Clear() => _lojob.Clear();
        public bool Contains(Dictionary<object, object> item) => _lojob.Contains(item);
        public void CopyTo(Dictionary<object, object>[] array, int arrayIndex) => _lojob.CopyTo(array, arrayIndex);
        public bool Remove(Dictionary<object, object> item) => _lojob.Remove(item);
        public IEnumerator<Dictionary<object, object>> GetEnumerator() => _lojob.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _lojob.GetEnumerator();
    }
}
