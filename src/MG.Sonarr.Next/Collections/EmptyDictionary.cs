namespace MG.Sonarr.Next.Collections;

/// <summary>
/// Provides the one <see cref="EmptyNameDictionary{T}"/> instance for each element type.
/// </summary>
internal static class EmptyNameDictionary
{
	/// <summary>
	/// Gets the empty read-only dictionary and set for <typeparamref name="T"/>.
	/// </summary>
	/// <remarks>
	/// The <see cref="CollectionBuilderAttribute"/> on <see cref="EmptyNameDictionary{T}"/> names this method, so the
	/// collection expression <c>[]</c> calls it.
	/// </remarks>
	/// <typeparam name="T">The type of elements in the dictionary and set.</typeparam>
	/// <param name="values">The collection expression's elements, which must be empty.</param>
	/// <returns>The one <see cref="EmptyNameDictionary{T}"/> instance for <typeparamref name="T"/>.</returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="values"/> isn't empty.</exception>
	internal static EmptyNameDictionary<T> Create<T>(params ReadOnlySpan<KeyValuePair<string, T>> values)
	{
		if (!values.IsEmpty)
		{
			throw new ArgumentException("The collection must be empty.", nameof(values));
		}

		return EmptyInstance<T>.Instance;
	}

	/// <summary>
	/// Gets the empty read-only dictionary and set for <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The type of elements in the dictionary and set.</typeparam>
	/// <returns>The one <see cref="EmptyNameDictionary{T}"/> instance for <typeparamref name="T"/>.</returns>
	internal static EmptyNameDictionary<T> Empty<T>()
	{
		return EmptyInstance<T>.Instance;
	}

	/// <summary>
	/// Provides the one <see cref="EmptyNameDictionary{T}"/> instance for <typeparamref name="T"/>.
	/// </summary>
	/// <remarks>
	/// The constructor of <see cref="EmptyNameDictionary{T}"/> is private, so that no other instance can exist, and
	/// <see cref="CreateInstance"/> reaches it through <see cref="UnsafeAccessorAttribute"/>. Each
	/// <typeparamref name="T"/> gets its own copy of <see cref="Instance"/>.
	/// </remarks>
	/// <typeparam name="T">The type of elements in the dictionary and set.</typeparam>
	private static class EmptyInstance<T>
	{
		/// <summary>
		/// The one instance for <typeparamref name="T"/>.
		/// </summary>
		internal static readonly EmptyNameDictionary<T> Instance = CreateInstanceCore();

		/// <summary>
		/// Creates the one instance for <typeparamref name="T"/>, and wraps a failure in an exception that names
		/// <typeparamref name="T"/>.
		/// </summary>
		/// <remarks>
		/// It runs in the class's type initializer, so a failure reaches callers as a
		/// <see cref="TypeInitializationException"/> that holds the <see cref="InvalidOperationException"/>, and every
		/// later read of <see cref="Instance"/> for the same <typeparamref name="T"/> throws again.
		/// </remarks>
		/// <returns>The new <see cref="EmptyNameDictionary{T}"/> instance.</returns>
		/// <exception cref="InvalidOperationException">Thrown when the private constructor can't be called.</exception>
		private static EmptyNameDictionary<T> CreateInstanceCore()
		{
			try
			{
				return CreateInstance();
			}
			catch (Exception e)
			{
				throw createInvalidException(e);
			}

			[DebuggerStepThrough] static InvalidOperationException createInvalidException(Exception e)
			{
				return new InvalidOperationException($"Failed to create the empty read-only dictionary and set for {typeof(T).FullName}.", e);
			}
		}

		// The cref to EmptyNameDictionary{T}() in the doc comment below is a build-time check on the private
		// constructor that [UnsafeAccessor] binds to only at run time. Neither one depends on the constructor's
		// accessibility, and both fail only when EmptyNameDictionary<T> has no parameterless constructor.
		// src/MG.Sonarr.Next/Collections/.editorconfig makes CS1574 an error in this file, so that fails the build.
		/// <summary>
		/// Calls the private constructor: <see cref="EmptyNameDictionary{T}()"/>.
		/// </summary>
		/// <remarks>
		/// The runtime supplies the body, which <see cref="UnsafeAccessorAttribute"/> binds to the constructor that takes no
		/// parameters.
		/// </remarks>
		/// <returns>A new <see cref="EmptyNameDictionary{T}"/> instance.</returns>
		/// <exception cref="MissingMethodException">
		/// Thrown when <see cref="EmptyNameDictionary{T}"/> has no constructor that takes no parameters.
		/// </exception>
		[UnsafeAccessor(UnsafeAccessorKind.Constructor)]
		private static extern EmptyNameDictionary<T> CreateInstance();
	}
}

/// <summary>
/// Represents an empty read-only dictionary and set.
/// </summary>
/// <remarks>
/// Each element type has one instance, which <see cref="EmptyNameDictionary.Empty{T}"/> and the collection expression
/// <c>[]</c> return.
/// </remarks>
/// <typeparam name="T">The type of elements in the dictionary and set.</typeparam>
[DebuggerStepThrough, DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicConstructors)]
[CollectionBuilder(typeof(EmptyNameDictionary), nameof(EmptyNameDictionary.Create))]
internal sealed class EmptyNameDictionary<T> : IEquatable<EmptyNameDictionary<T>>, IReadOnlyDictionary<string, T>, IReadOnlySet<T>
{
	// EmptyNameDictionary.EmptyInstance<T>.CreateInstance calls this constructor through [UnsafeAccessor]. Its
	// accessibility doesn't matter to that call, but its parameters do: giving it a parameter fails the build with
	// CS1574 (see src/MG.Sonarr.Next/Collections/.editorconfig). It stays private so that no other instance can exist.
	/// <summary>
	/// Initializes a new <see cref="EmptyNameDictionary{T}"/> instance.
	/// </summary>
	private EmptyNameDictionary()
	{
	}

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
	/// <returns>
	/// <see langword="true"/> if <paramref name="other"/> isn't <see langword="null"/>, since it's then this same
	/// instance; otherwise, <see langword="false"/>.
	/// </returns>
	public bool Equals(EmptyNameDictionary<T>? other)
	{
		return ReferenceEquals(this, other);
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
	/// <returns>
	/// <see langword="true"/> if the specified collection has at least one element; otherwise, <see langword="false"/>.
	/// </returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="other"/> is null.</exception>
	public bool IsProperSubsetOf(IEnumerable<T> other)
	{
		ArgumentNullException.ThrowIfNull(other);
		return other.Any();
	}

	/// <summary>
	/// Determines whether the current set is a proper superset of a specified collection.
	/// </summary>
	/// <param name="other">The collection to compare to the current set.</param>
	/// <returns><see langword="false"/> because the set is empty.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="other"/> is null.</exception>
	public bool IsProperSupersetOf(IEnumerable<T> other)
	{
		ArgumentNullException.ThrowIfNull(other);
		return false;
	}

	/// <summary>
	/// Determines whether the current set is a subset of a specified collection.
	/// </summary>
	/// <param name="other">The collection to compare to the current set.</param>
	/// <returns><see langword="true"/> because the set is empty.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="other"/> is null.</exception>
	public bool IsSubsetOf(IEnumerable<T> other)
	{
		ArgumentNullException.ThrowIfNull(other);
		return true;
	}

	/// <summary>
	/// Determines whether the current set is a superset of a specified collection.
	/// </summary>
	/// <param name="other">The collection to compare to the current set.</param>
	/// <returns>
	/// <see langword="true"/> if the specified collection is empty; otherwise, <see langword="false"/>.
	/// </returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="other"/> is null.</exception>
	public bool IsSupersetOf(IEnumerable<T> other)
	{
		ArgumentNullException.ThrowIfNull(other);
		return !other.Any();
	}

	/// <summary>
	/// Determines whether the current set overlaps with a specified collection.
	/// </summary>
	/// <param name="other">The collection to compare to the current set.</param>
	/// <returns><see langword="false"/> because the set is empty.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="other"/> is null.</exception>
	public bool Overlaps(IEnumerable<T> other)
	{
		ArgumentNullException.ThrowIfNull(other);
		return false;
	}

	/// <summary>
	/// Determines whether the current set and a specified collection contain the same elements.
	/// </summary>
	/// <param name="other">The collection to compare to the current set.</param>
	/// <returns><see langword="true"/> if the specified collection is also empty; otherwise, <see langword="false"/>.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="other"/> is null.</exception>
	public bool SetEquals(IEnumerable<T> other)
	{
		ArgumentNullException.ThrowIfNull(other);
		return !other.Any();
	}

	/// <inheritdoc/>
	IEnumerator<T> IEnumerable<T>.GetEnumerator()
	{
		return Enumerable.Empty<T>().GetEnumerator();
	}
}