using System.Collections;
using System.Management.Automation;

namespace MG.Sonarr.Next.Services.Collections
{
    internal readonly struct EmptyNameDictionary : IEquatable<EmptyNameDictionary>, IReadOnlyDictionary<string, string>
    {
        public string this[string key] => string.Empty;

        public int Count => 0;
        public IEnumerable<string> Keys => Enumerable.Empty<string>();
        public IEnumerable<string> Values => Enumerable.Empty<string>();

        public bool ContainsKey(string key)
        {
            return false;
        }
        public bool Equals(EmptyNameDictionary other)
        {
            return true;
        }
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is EmptyNameDictionary empty && this.Equals(empty);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            yield break;
        }
        public bool TryGetValue(string key, [MaybeNullWhen(false)] out string value)
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
