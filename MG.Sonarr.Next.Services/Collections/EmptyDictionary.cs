using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MG.Sonarr.Next.Services.Collections
{
    internal readonly struct EmptyNameDictionary : IReadOnlyDictionary<string, string>
    {
        public string this[string key] => string.Empty;

        public int Count => 0;
        IEnumerable<string> IReadOnlyDictionary<string, string>.Keys => Enumerable.Empty<string>();
        IEnumerable<string> IReadOnlyDictionary<string, string>.Values => Enumerable.Empty<string>();

        bool IReadOnlyDictionary<string, string>.ContainsKey(string key)
        {
            return false;
        }
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            yield break;
        }
        bool IReadOnlyDictionary<string, string>.TryGetValue(string key, [MaybeNullWhen(false)] out string value)
        {
            value = null;
            return false;
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        internal static EmptyNameDictionary Default => default;
    }
}
