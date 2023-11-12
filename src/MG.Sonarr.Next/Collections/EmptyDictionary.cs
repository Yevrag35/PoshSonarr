using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MG.Sonarr.Next.Collections
{
    internal static class EmptyNameDictionary
    {
        /// <summary>
        /// Gets an empty read-only dictionary and set.
        /// </summary>
        /// <typeparam name="T">The type of elements in the dictionary and set.</typeparam>
        /// <returns>An empty read-only dictionary and set.</returns>
        internal static EmptyNameDictionary<T> Create<T>(params ReadOnlySpan<KeyValuePair<string, T>> values)
        {
            if (!values.IsEmpty)
            {
                throw new ArgumentException("The collection must be empty.", nameof(values));
            }

            return new();
        }

        internal static EmptyNameDictionary<T> Empty<T>()
        {
            return new();
        }
    }

    /// <summary>
    /// Represents an empty read-only dictionary and set.
    /// </summary>
    /// <typeparam name="T">The type of elements in the dictionary and set.</typeparam>
    [DebuggerStepThrough]
    [StructLayout(LayoutKind.Auto)]
    [CollectionBuilder(typeof(EmptyNameDictionary), nameof(EmptyNameDictionary.Create))]
    internal readonly struct EmptyNameDictionary<T> : IEquatable<EmptyNameDictionary<T>>, IReadOnlyDictionary<string, T>, IReadOnlySet<T>
    {
        /// <inheritdoc/>
        T IReadOnlyDictionary<string, T>.this[string key] => throw new KeyNotFoundException("This is a read-only, empty collection.");

        /// <summary>
        /// Gets the number of elements in the collection, which is always zero.
        /// </summary>
        public int Count => 0;

        /// <summary>
        /// Gets an empty collection of keys.
        /// </summary>
        public IEnumerable<string> Keys => [];

        /// <summary>
        /// Gets an empty collection of values.
        /// </summary>
        public IEnumerable<T> Values => [];

        /// <summary>
        /// Determines whether the set contains a specific value.
        /// </summary>
        /// <param name="key">The value to locate in the set.</param>
        /// <returns><see langword="false"/> because the set is empty.</returns>
        bool IReadOnlySet<T>.Contains(T key)
        {
            return false;
        }

        /// <summary>
        /// Determines whether the dictionary contains a specific key.
        /// </summary>
        /// <param name="key">The key to locate in the dictionary.</param>
        /// <returns><see langword="false"/> because the dictionary is empty.</returns>
        public bool ContainsKey(string key)
        {
            return false;
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns><see langword="true"/> because all instances of <see cref="EmptyNameDictionary{T}"/> are equal.</returns>
        public bool Equals(EmptyNameDictionary<T> other)
        {
            return true;
        }

        /// <inheritdoc/>
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is EmptyNameDictionary<T> empty && this.Equals(empty);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<string, T>> GetEnumerator()
        {
            return Enumerable.Empty<KeyValuePair<string, T>>().GetEnumerator();
        }

        /// <summary>
        /// Attempts to get the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key, if the key is found; otherwise, the default value for the type of the value parameter. This parameter is passed uninitialized.</param>
        /// <returns><see langword="false"/> because the dictionary is empty.</returns>
        public bool TryGetValue(string key, [MaybeNullWhen(false)] out T value)
        {
            value = default;
            return false;
        }

        /// <inheritdoc/>
        [DebuggerStepThrough]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>
        /// Determines whether the current set is a proper subset of a specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current set.</param>
        /// <returns><see langword="false"/> because the set is empty.</returns>
        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            return !other.Any();
        }

        /// <summary>
        /// Determines whether the current set is a proper superset of a specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current set.</param>
        /// <returns><see langword="false"/> because the set is empty.</returns>
        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            return false;
        }

        /// <summary>
        /// Determines whether the current set is a subset of a specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current set.</param>
        /// <returns><see langword="true"/> because the set is empty.</returns>
        public bool IsSubsetOf(IEnumerable<T> other)
        {
            return true;
        }

        /// <summary>
        /// Determines whether the current set is a superset of a specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current set.</param>
        /// <returns>
        /// <see langword="true"/> if the specified other set is empty; otherwise, <see langword="false"/>.
        /// </returns>
        public bool IsSupersetOf(IEnumerable<T> other)
        {
            return !other.Any();
        }

        /// <summary>
        /// Determines whether the current set overlaps with a specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current set.</param>
        /// <returns><see langword="false"/> because the set is empty.</returns>
        public bool Overlaps(IEnumerable<T> other)
        {
            return false;
        }

        /// <summary>
        /// Determines whether the current set and a specified collection contain the same elements.
        /// </summary>
        /// <param name="other">The collection to compare to the current set.</param>
        /// <returns><see langword="true"/> if the specified collection is also empty; otherwise, <see langword="false"/>.</returns>
        public bool SetEquals(IEnumerable<T> other)
        {
            return other is not null && !other.Any();
        }

        /// <inheritdoc/>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return Enumerable.Empty<T>().GetEnumerator();
        }
    }
}