namespace MG.Sonarr.Next.Shell.Checkers;

/// <summary>
/// Represents an equality checker for an element type that is equatable to itself, which converts both values to that
/// type and compares them.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="MG.Sonarr.Next.Shell.Attributes.DistinctValuesAttribute"/> picks this checker for an element type that
/// isn't an enum and implements <see cref="IEquatable{T}"/> of itself, such as <see cref="int"/>. Values are converted
/// as PowerShell converts them, and a value that doesn't convert to <typeparamref name="T"/> is equal to nothing but
/// itself.
/// </para>
/// <para>
/// The checker holds no state, so it's thread-safe. A derived class changes the comparison by overriding
/// <see cref="Equals(T, T)"/>, and the hash code by overriding <see cref="GetHashCodeCore(object)"/>.
/// </para>
/// </remarks>
/// <typeparam name="T">The element type, which implements <see cref="IEquatable{T}"/> of itself.</typeparam>
[DebuggerStepThrough]
public class EqualityChecker<T> : EqualityChecker where T : IEquatable<T>
{
	/// <summary>
	/// When overridden in a derived class, determines whether two values of the element type are equal.
	/// </summary>
	/// <remarks>
	/// The base implementation compares them with <see cref="IEquatable{T}.Equals(T)"/>.
	/// </remarks>
	/// <param name="x">The first value, which isn't <see langword="null"/>.</param>
	/// <param name="y">The second value, which isn't <see langword="null"/>.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="x"/> and <paramref name="y"/> are equal; otherwise,
	/// <see langword="false"/>.
	/// </returns>
	protected virtual bool Equals([DisallowNull] T x, [DisallowNull] T y)
	{
		return x.Equals(y);
	}

	/// <summary>
	/// Gets a hash code for the specified value of the element type.
	/// </summary>
	/// <remarks>
	/// A value type is boxed to pass it to <see cref="GetHashCodeCore(object)"/>.
	/// </remarks>
	/// <param name="obj">The value, which can't be <see langword="null"/>.</param>
	/// <returns>The hash code that <see cref="GetHashCodeCore(object)"/> gives for <paramref name="obj"/>.</returns>
	public int GetHashCode([DisallowNull] T obj)
	{
		return this.GetHashCodeCore(obj);
	}

	/// <summary>
	/// Converts both objects to the element type and determines whether the values are equal.
	/// </summary>
	/// <param name="x">The first object, which isn't <see langword="null"/>.</param>
	/// <param name="y">The second object, which isn't <see langword="null"/>.</param>
	/// <returns>
	/// <see langword="true"/> if both objects convert to <typeparamref name="T"/> and <see cref="Equals(T, T)"/> finds
	/// the values equal; otherwise, <see langword="false"/>.
	/// </returns>
	protected override bool EqualsCore([DisallowNull] object x, [DisallowNull] object y)
	{
		if (LanguagePrimitives.TryConvertTo(x, out T tX) && LanguagePrimitives.TryConvertTo(y, out T tY))
		{
			return this.Equals(tX!, tY!);
		}

		return false;
	}

	/// <summary>
	/// Gets the object's own hash code.
	/// </summary>
	/// <remarks>
	/// The attribute converts each value to the element type before it compares them, so the object is already a
	/// <typeparamref name="T"/>, and its hash code is that of the value.
	/// </remarks>
	/// <param name="obj">The object, which isn't <see langword="null"/>.</param>
	/// <returns>The value that <paramref name="obj"/>'s <see cref="object.GetHashCode"/> returns.</returns>
	protected override int GetHashCodeCore([DisallowNull] object obj)
	{
		return obj.GetHashCode();
	}
}
