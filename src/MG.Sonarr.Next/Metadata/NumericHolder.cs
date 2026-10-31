using System.Numerics;

namespace MG.Sonarr.Next.Metadata;

/// <summary>
/// Represents a mutable wrapper around a number that compares and equates with both other wrappers and bare numbers.
/// </summary>
/// <typeparam name="T">The number type that the wrapper holds.</typeparam>
public struct NumericHolder<T> : IComparable<T>, IEquatable<T>, IComparable<NumericHolder<T>>, IEquatable<NumericHolder<T>>
	where T : unmanaged, IComparable<T>, IEquatable<T>, INumber<T>
{
	/// <summary>
	/// The number that the wrapper holds.
	/// </summary>
	private T _value;

	/// <summary>
	/// Gets or sets the number that the wrapper holds.
	/// </summary>
	/// <value>The wrapped number.</value>
	public T Value
	{
		readonly get => _value;
		set => _value = value;
	}

	/// <summary>
	/// Initializes a new <see cref="NumericHolder{T}"/> instance that holds the specified number.
	/// </summary>
	/// <param name="value">The number to hold.</param>
	public NumericHolder(in T value)
	{
		_value = value;
	}

	/// <summary>
	/// Gets a wrapper that holds the default value of <typeparamref name="T"/>, which is 0.
	/// </summary>
	/// <value>A new wrapper that holds 0.</value>
	public static NumericHolder<T> Default => new(default);

	/// <summary>
	/// Compares the wrapped number with another number.
	/// </summary>
	/// <param name="other">The number to compare with.</param>
	/// <returns>
	/// A negative number if the wrapped number is less than <paramref name="other"/>, 0 if they're equal, and a positive
	/// number if it's greater.
	/// </returns>
	public readonly int CompareTo(T other)
	{
		return other.CompareTo(_value);
	}
	/// <summary>
	/// Compares the wrapped number with another wrapper's number.
	/// </summary>
	/// <param name="other">The wrapper to compare with.</param>
	/// <returns>
	/// A negative number if the wrapped number is less than <paramref name="other"/>'s, 0 if they're equal, and a
	/// positive number if it's greater.
	/// </returns>
	public readonly int CompareTo(NumericHolder<T> other)
	{
		return this.CompareTo(other._value);
	}
	/// <summary>
	/// Determines whether the wrapped number equals another wrapper's number.
	/// </summary>
	/// <param name="other">The wrapper to compare with.</param>
	/// <returns>
	/// <see langword="true"/> if the two numbers are equal; otherwise, <see langword="false"/>.
	/// </returns>
	public readonly bool Equals(NumericHolder<T> other)
	{
		return this.Equals(other._value);
	}
	/// <summary>
	/// Determines whether the wrapped number equals another number.
	/// </summary>
	/// <param name="other">The number to compare with.</param>
	/// <returns>
	/// <see langword="true"/> if the wrapped number equals <paramref name="other"/>; otherwise,
	/// <see langword="false"/>.
	/// </returns>
	public readonly bool Equals(T other)
	{
		return other.Equals(_value);
	}
	/// <summary>
	/// Determines whether the wrapped number equals an object.
	/// </summary>
	/// <param name="obj">The object to compare with.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="obj"/> is a <see cref="NumericHolder{T}"/> or a
	/// <typeparamref name="T"/> whose number equals the wrapped one; otherwise, <see langword="false"/>.
	/// </returns>
	public readonly override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is NumericHolder<T> holder)
		{
			return this.Equals(holder.Value);
		}
		else if (obj is T other)
		{
			return this.Equals(other);
		}

		return false;
	}
	/// <summary>
	/// Gets a hash code for the wrapper.
	/// </summary>
	/// <returns>A hash code of the wrapper's contents.</returns>
	public readonly override int GetHashCode()
	{
		return base.GetHashCode();
	}

	/// <summary>
	/// Converts an <see cref="int"/> to a wrapper that holds it as a <typeparamref name="T"/>.
	/// </summary>
	/// <param name="value">The number to wrap.</param>
	/// <returns>A new wrapper that holds <paramref name="value"/>.</returns>
	/// <exception cref="OverflowException">Thrown when <paramref name="value"/> doesn't fit in a <typeparamref name="T"/>.</exception>
	public static implicit operator NumericHolder<T>(int value)
	{
		return new(T.CreateChecked(value));
	}
	/// <summary>
	/// Converts a wrapper to the number it holds.
	/// </summary>
	/// <param name="holder">The wrapper to convert.</param>
	/// <returns>The wrapped number.</returns>
	public static explicit operator T(NumericHolder<T> holder)
	{
		return holder._value;
	}

	/// <summary>
	/// Determines whether two wrappers hold equal numbers.
	/// </summary>
	/// <param name="x">The first wrapper to compare.</param>
	/// <param name="y">The second wrapper to compare.</param>
	/// <returns><see langword="true"/> if the numbers are equal; otherwise, <see langword="false"/>.</returns>
	public static bool operator ==(NumericHolder<T> x, NumericHolder<T> y)
	{
		return x.Equals(y);
	}
	/// <summary>
	/// Determines whether two wrappers hold different numbers.
	/// </summary>
	/// <param name="x">The first wrapper to compare.</param>
	/// <param name="y">The second wrapper to compare.</param>
	/// <returns><see langword="true"/> if the numbers differ; otherwise, <see langword="false"/>.</returns>
	public static bool operator !=(NumericHolder<T> x, NumericHolder<T> y)
	{
		return !(x == y);
	}
	/// <summary>
	/// Determines whether a wrapper holds a number equal to another number.
	/// </summary>
	/// <param name="x">The wrapper to compare.</param>
	/// <param name="y">The number to compare.</param>
	/// <returns><see langword="true"/> if the numbers are equal; otherwise, <see langword="false"/>.</returns>
	public static bool operator ==(NumericHolder<T> x, T y)
	{
		return x.Equals(y);
	}
	/// <summary>
	/// Determines whether a wrapper holds a number different from another number.
	/// </summary>
	/// <param name="x">The wrapper to compare.</param>
	/// <param name="y">The number to compare.</param>
	/// <returns><see langword="true"/> if the numbers differ; otherwise, <see langword="false"/>.</returns>
	public static bool operator !=(NumericHolder<T> x, T y)
	{
		return !(x == y);
	}
	/// <summary>
	/// Determines whether a number equals the number that a wrapper holds.
	/// </summary>
	/// <param name="x">The number to compare.</param>
	/// <param name="y">The wrapper to compare.</param>
	/// <returns><see langword="true"/> if the numbers are equal; otherwise, <see langword="false"/>.</returns>
	public static bool operator ==(T x, NumericHolder<T> y)
	{
		return y.Equals(x);
	}
	/// <summary>
	/// Determines whether a number differs from the number that a wrapper holds.
	/// </summary>
	/// <param name="x">The number to compare.</param>
	/// <param name="y">The wrapper to compare.</param>
	/// <returns><see langword="true"/> if the numbers differ; otherwise, <see langword="false"/>.</returns>
	public static bool operator !=(T x, NumericHolder<T> y)
	{
		return !(x == y);
	}
}
