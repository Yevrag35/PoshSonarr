using System.Collections;

namespace MG.Sonarr.Next.Shell.Checkers;

/// <summary>
/// Provides the base class of the equality comparers that
/// <see cref="MG.Sonarr.Next.Shell.Attributes.DistinctValuesAttribute"/> uses to remove repeated values from a
/// parameter's argument.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Equals(object, object)"/> settles nulls and references itself: two <see langword="null"/> values are
/// equal, a <see langword="null"/> value and any other aren't, and an instance is equal to itself. Every other pair goes
/// to <see cref="EqualsCore(object, object)"/>, and <see cref="GetHashCode(object)"/> goes to
/// <see cref="GetHashCodeCore(object)"/>, so a derived class compares only values that aren't <see langword="null"/>.
/// </para>
/// <para>
/// The attribute picks <see cref="EnumEqualityChecker{T}"/> for an enum element type,
/// <see cref="EqualityChecker{T}"/> for one that implements <see cref="IEquatable{T}"/> of itself, and
/// <see cref="Default"/> for any other. It keeps one checker for each element type for the length of a connection,
/// which every runspace shares, so a checker must be thread-safe. The module's checkers hold no state that changes
/// after they're created.
/// </para>
/// </remarks>
[DebuggerStepThrough]
public abstract class EqualityChecker : IEqualityComparer, IEqualityComparer<object>
{
	/// <summary>
	/// Determines whether two objects are equal.
	/// </summary>
	/// <param name="x">The first object, or <see langword="null"/>.</param>
	/// <param name="y">The second object, or <see langword="null"/>.</param>
	/// <returns>
	/// <see langword="true"/> if both objects are <see langword="null"/>, are the same instance, or are equal as
	/// <see cref="EqualsCore(object, object)"/> compares them; otherwise, <see langword="false"/>.
	/// </returns>
	public new bool Equals(object? x, object? y)
	{
		if (x is null)
		{
			return y is null;
		}
		else if (IsNullOrReferenceEquals(x, y, out bool result))
		{
			return result;
		}

		return this.EqualsCore(x, y);
	}

	/// <summary>
	/// When implemented in a derived class, determines whether two objects that aren't <see langword="null"/> and
	/// aren't the same instance are equal.
	/// </summary>
	/// <param name="x">The first object, which isn't <see langword="null"/>.</param>
	/// <param name="y">The second object, which isn't <see langword="null"/>.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="x"/> and <paramref name="y"/> are equal; otherwise,
	/// <see langword="false"/>.
	/// </returns>
	protected abstract bool EqualsCore([DisallowNull] object x, [DisallowNull] object y);

	/// <summary>
	/// Gets a hash code for the specified object.
	/// </summary>
	/// <param name="obj">The object, which can't be <see langword="null"/>.</param>
	/// <returns>The hash code that <see cref="GetHashCodeCore(object)"/> gives for <paramref name="obj"/>.</returns>
	public int GetHashCode([DisallowNull] object obj)
	{
		return this.GetHashCodeCore(obj);
	}
	/// <summary>
	/// When implemented in a derived class, gets a hash code for the specified object.
	/// </summary>
	/// <remarks>
	/// Two objects that <see cref="EqualsCore(object, object)"/> finds equal must get the same hash code.
	/// </remarks>
	/// <param name="obj">The object, which isn't <see langword="null"/>.</param>
	/// <returns>The hash code of <paramref name="obj"/>.</returns>
	protected abstract int GetHashCodeCore([DisallowNull] object obj);

	/// <summary>
	/// Determines whether the second of two objects settles their equality without a comparison, because it's
	/// <see langword="null"/> or the same instance as the first.
	/// </summary>
	/// <param name="x">The first object, which isn't <see langword="null"/>.</param>
	/// <param name="y">The second object, or <see langword="null"/>.</param>
	/// <param name="result">
	/// When the method returns <see langword="true"/>, <see langword="true"/> if <paramref name="y"/> is the same
	/// instance as <paramref name="x"/>, or <see langword="false"/> if it's <see langword="null"/>; otherwise,
	/// <see langword="false"/>.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="y"/> is <see langword="null"/> or the same instance as
	/// <paramref name="x"/>; otherwise, <see langword="false"/>.
	/// </returns>
	private static bool IsNullOrReferenceEquals([DisallowNull] object x, [NotNullWhen(false)] object? y, out bool result)
	{
		if (y is null)
		{
			result = false;
			return true;
		}
		else if (ReferenceEquals(x, y))
		{
			result = true;
			return true;
		}

		result = false;
		return false;
	}

	/// <summary>
	/// The checker for an element type that is neither an enum nor equatable to itself, which compares values as
	/// PowerShell does, ignoring case.
	/// </summary>
	public static readonly EqualityChecker Default = new DefaultChecker();

	/// <summary>
	/// Represents the checker that <see cref="Default"/> holds, which compares values with
	/// <see cref="LanguagePrimitives.Equals(object, object, bool, IFormatProvider)"/>.
	/// </summary>
	private sealed class DefaultChecker : EqualityChecker
	{
		/// <summary>
		/// Determines whether two objects are equal as PowerShell compares them, ignoring case and in the invariant
		/// culture.
		/// </summary>
		/// <remarks>
		/// PowerShell converts <paramref name="y"/> to the type of <paramref name="x"/> when it can before it compares
		/// them.
		/// </remarks>
		/// <param name="x">The first object, which isn't <see langword="null"/>.</param>
		/// <param name="y">The second object, which isn't <see langword="null"/>.</param>
		/// <returns>
		/// <see langword="true"/> if PowerShell finds <paramref name="x"/> and <paramref name="y"/> equal; otherwise,
		/// <see langword="false"/>.
		/// </returns>
		protected override bool EqualsCore([DisallowNull] object x, [DisallowNull] object y)
		{
			return LanguagePrimitives.Equals(x, y, ignoreCase: true, CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Gets the object's own hash code.
		/// </summary>
		/// <param name="obj">The object, which isn't <see langword="null"/>.</param>
		/// <returns>The value that <paramref name="obj"/>'s <see cref="object.GetHashCode"/> returns.</returns>
		protected override int GetHashCodeCore([DisallowNull] object obj)
		{
			return StringComparer.InvariantCultureIgnoreCase.GetHashCode(obj);
		}
	}
}