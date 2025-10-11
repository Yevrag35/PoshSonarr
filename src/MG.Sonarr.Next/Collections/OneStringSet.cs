using MG.Sonarr.Next.Components;
using System.Collections;

namespace MG.Sonarr.Next.Collections
{
    /// <summary>
    /// Represents a read-only set containing zero or one string value, providing efficient set operations for
    /// single-item scenarios.
    /// </summary>
    /// <remarks>This struct is optimized for cases where a set contains at most one string. All set
    /// operations are performed using ordinal, case-insensitive string comparison.</remarks>
    internal readonly struct OneStringSet : IReadOnlySet<string>
    {
        const int COUNT = 1;

        readonly string? _value;

        [MemberNotNullWhen(false, nameof(Value), nameof(_value))]
        public bool IsEmpty => string.IsNullOrEmpty(_value);
        public string Value => _value ?? string.Empty;

        int IReadOnlyCollection<string>.Count => COUNT;

        internal OneStringSet(string value)
        {
            _value = value;
        }

        public IEnumerator<string> GetEnumerator()
        {
            if (!this.IsEmpty)
            {
                yield return _value;
            }
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        internal static OneStringSet FromEnumerable(IEnumerable<string> collection)
        {
            ArgumentNullException.ThrowIfNull(collection);

            foreach (string str in collection)
            {
                return new OneStringSet(str);
            }

            return default;
        }

        bool IReadOnlySet<string>.Contains(string item)
        {
            return !this.IsEmpty && _value.Equals(item, StringComparison.OrdinalIgnoreCase);
        }
        bool IReadOnlySet<string>.IsProperSubsetOf(IEnumerable<string> other)
        {
            ArgumentNullException.ThrowIfNull(other);
            if (this.IsEmpty)
            {
                return true;
            }

            DoubleBool dub = DoubleBool.InitializeNew();

            foreach (string s in other)
            {
                if (dub)
                {
                    break;
                }

                if (_value.Equals(s, StringComparison.OrdinalIgnoreCase))
                {
                    dub.Bool1 = true;
                }
                else
                {
                    dub.Bool2 = true;
                }
            }

            return dub;
        }
        bool IReadOnlySet<string>.IsProperSupersetOf(IEnumerable<string> other)
        {
            if (this.IsEmpty)
            {
                return false;
            }

            return !other.Any();
        }
        bool IReadOnlySet<string>.IsSubsetOf(IEnumerable<string> other)
        {
            ArgumentNullException.ThrowIfNull(other);
            if (this.IsEmpty)
            {
                return true;
            }

            foreach (string s in other)
            {
                if (_value.Equals(s, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
        bool IReadOnlySet<string>.IsSupersetOf(IEnumerable<string> other)
        {
            if (this.IsEmpty)
            {
                return !other.Any();
            }

            foreach (string s in other)
            {
                if (!_value.Equals(s, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }
        bool IReadOnlySet<string>.Overlaps(IEnumerable<string> other)
        {
            ArgumentNullException.ThrowIfNull(other);
            if (this.IsEmpty)
            {
                return false;
            }

            foreach (string s in other)
            {
                if (_value.Equals(s, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
        bool IReadOnlySet<string>.SetEquals(IEnumerable<string> other)
        {
            ArgumentNullException.ThrowIfNull(other);
            if (this.IsEmpty)
            {
                return !other.Any();
            }

            foreach (string s in other)
            {
                if (!_value.Equals(s, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }
    }
}

