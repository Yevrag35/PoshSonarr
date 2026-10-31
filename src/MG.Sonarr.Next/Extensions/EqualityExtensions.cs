namespace MG.Sonarr.Next.Extensions;

/// <summary>
/// Provides extension methods that compare two values for equality, either of which can be null.
/// </summary>
[DebuggerStepThrough]
public static class EqualityExtensions
{
	/// <summary>
	/// Determines whether a value equals a nullable value of the same type.
	/// </summary>
	/// <typeparam name="T">The value type to compare.</typeparam>
	/// <param name="this">The value to compare.</param>
	/// <param name="other">The nullable value to compare with.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="other"/> has a value that equals <paramref name="this"/>; otherwise,
	/// <see langword="false"/>.
	/// </returns>
	public static bool IsEqualTo<T>(this T @this, [NotNullWhen(true)] T? other) where T : struct, IEquatable<T>
	{
		return other.HasValue && @this.Equals(other.Value);
	}
	/// <summary>
	/// Determines whether two values of the same type are equal, treating two nulls as equal.
	/// </summary>
	/// <remarks>
	/// A non-null <paramref name="this"/> is compared through its <see cref="IEquatable{T}.Equals(T)"/>, which
	/// receives <paramref name="other"/> even when it's <see langword="null"/>.
	/// </remarks>
	/// <typeparam name="T">The type to compare.</typeparam>
	/// <param name="this">The value to compare, or <see langword="null"/>.</param>
	/// <param name="other">The value to compare with, or <see langword="null"/>.</param>
	/// <returns>
	/// <see langword="true"/> if both values are <see langword="null"/>, or if <paramref name="this"/> isn't
	/// <see langword="null"/> and equals <paramref name="other"/>; otherwise, <see langword="false"/>.
	/// </returns>
	public static bool IsEqualTo<T>(this T? @this, T? other) where T : IEquatable<T>
	{
		return BothAreNull(@this, other)
			   ||
			   LeftIsEqualToRight(@this, other);
	}
	/// <summary>
	/// Determines whether a value equals a value of another type, treating two nulls as equal.
	/// </summary>
	/// <remarks>
	/// A non-null <paramref name="this"/> is compared through its <see cref="IEquatable{T}.Equals(T)"/>, which
	/// receives <paramref name="other"/> even when it's <see langword="null"/>.
	/// </remarks>
	/// <typeparam name="T">The type of the value to compare, which can be compared with <typeparamref name="TOther"/>.</typeparam>
	/// <typeparam name="TOther">The type of the value to compare with.</typeparam>
	/// <param name="this">The value to compare, or <see langword="null"/>.</param>
	/// <param name="other">The value to compare with, or <see langword="null"/>.</param>
	/// <returns>
	/// <see langword="true"/> if both values are <see langword="null"/>, or if <paramref name="this"/> isn't
	/// <see langword="null"/> and equals <paramref name="other"/>; otherwise, <see langword="false"/>.
	/// </returns>
	public static bool IsEqualTo<T, TOther>(this T? @this, TOther? other) where T : IEquatable<TOther>
	{
		return BothAreNull(@this, other)
			   ||
			   LeftIsEqualToRight(@this, other);
	}

	/// <summary>
	/// Determines whether two values are both null.
	/// </summary>
	/// <typeparam name="T1">The type of the first value.</typeparam>
	/// <typeparam name="T2">The type of the second value.</typeparam>
	/// <param name="t1">The first value.</param>
	/// <param name="t2">The second value.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="t1"/> and <paramref name="t2"/> are both <see langword="null"/>;
	/// otherwise, <see langword="false"/>.
	/// </returns>
	private static bool BothAreNull<T1, T2>(T1? t1, T2? t2)
	{
		return t1 is null && t2 is null;
	}
	/// <summary>
	/// Determines whether a non-null value equals another value.
	/// </summary>
	/// <typeparam name="T1">The type of the first value, which can be compared with <typeparamref name="T2"/>.</typeparam>
	/// <typeparam name="T2">The type of the second value.</typeparam>
	/// <param name="t1">The first value.</param>
	/// <param name="t2">The second value, which can be <see langword="null"/>.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="t1"/> isn't <see langword="null"/> and equals
	/// <paramref name="t2"/>; otherwise, <see langword="false"/>.
	/// </returns>
	private static bool LeftIsEqualToRight<T1, T2>(T1? t1, T2? t2) where T1 : IEquatable<T2>
	{
		return t1 is not null && t1.Equals(t2);
	}
}
