namespace MG.Sonarr.Next.Guarding;

/// <summary>
/// A <see langword="static"/> class that provides methods for throwing exceptions with messages that are formatted.
/// </summary>
[DebuggerStepThrough]
public static partial class ThrowHelper
{
	/// <summary>
	/// Throws an <see cref="ArgumentException"/> with a message that indicates that the buffer is too small with the 
	/// optional parameter name and an format provider.
	/// </summary>
	/// <param name="actualValue">The actual length/size of the buffer which is less than the required minimum.</param>
	/// <param name="requiredMinimum">
	/// The minimum required length/size of the buffer that is greater than <paramref name="actualValue"/>.
	/// </param>
	/// <param name="parameterName">
	/// The parameter name of the buffer that is too small. If <see langword="null"/>, the parameter name is not included in the message.
	/// </param>
	/// <param name="provider">
	/// The format provider to use when formatting the message. 
	/// If <see langword="null"/>, the <see cref="CultureInfo.CurrentCulture"/> is used.
	/// </param>
	/// <exception cref="ArgumentException">The buffer is too small for the operation to be performed.</exception>
	[DoesNotReturn]
	public static void BufferTooSmall(int requiredMinimum, int actualValue, [CallerArgumentExpression(nameof(actualValue))] string? parameterName = null, IFormatProvider? provider = null)
	{
		const string msg = "The buffer is too small to be used as a copy destination. Expected minimum length: {0}, Received length: {1}";
		Debug.Assert(requiredMinimum > actualValue);

		string message = string.Format(
			provider: provider,
			format: msg,
			arg0: requiredMinimum,
			arg1: actualValue);

		throw new ArgumentException(message, parameterName);
	}

	/// <summary>
	/// Throws an <see cref="ArgumentOutOfRangeException"/> if the specified <paramref name="value"/> is negative  or
	/// greater than the specified <paramref name="other"/>.
	/// </summary>
	/// <param name="value">The integer value to validate. Must not be negative and must not exceed <paramref name="other"/>.</param>
	/// <param name="other">The upper limit, inclusive, that <paramref name="value"/> must not exceed. Must be less than or equal to <see
	/// cref="int.MaxValue"/>.</param>
	/// <param name="paramName">The name of the parameter being validated. This is automatically populated by the compiler if not explicitly
	/// provided.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="value"/> is negative or greater than <paramref name="other"/>.</exception>
	public static void ThrowIfNegativeOrGreaterThan(int value, uint other, [CallerArgumentExpression(nameof(value))] string? paramName = null)
	{
		Debug.Assert(other <= int.MaxValue, "The other value should always be less than or equal to int.MaxValue.");
		if ((uint)value > other)
		{
			// The exception appends the parameter's name and the actual value to the message itself.
			throw new ArgumentOutOfRangeException(paramName, value,
				message: string.Format(
					CultureInfo.CurrentCulture,
					"{0} ('{1}') must not be negative but also not greater than '{2}'.",
					paramName,
					value,
					other));
		}
	}
	/// <summary>
	/// Throws an <see cref="ArgumentOutOfRangeException"/> if the specified <paramref name="value"/> is negative  or
	/// greater than or equal to the specified <paramref name="other"/>.
	/// </summary>
	/// <remarks>This method is typically used to validate input parameters to ensure they fall within an acceptable
	/// range.</remarks>
	/// <param name="value">The integer value to validate.</param>
	/// <param name="other">The upper bound that <paramref name="value"/> must be less than. Must be less than or equal to <see
	/// cref="int.MaxValue"/>.</param>
	/// <param name="paramName">The name of the parameter being validated. This is automatically populated by the compiler when using  <see
	/// cref="CallerArgumentExpressionAttribute"/>.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="value"/> is negative or greater than or equal to <paramref name="other"/>.</exception>
	[StackTraceHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ThrowIfNegativeOrGreaterThanOrEqualTo(int value, uint other, [CallerArgumentExpression(nameof(value))] string? paramName = null)
	{
		ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(unchecked((uint)value), other, paramName);
	}
	/// <summary>
	/// Throws an <see cref="ArgumentOutOfRangeException"/> if the specified <paramref name="value"/> is negative  or
	/// greater than the specified <paramref name="other"/>.
	/// </summary>
	/// <remarks>This method is typically used to validate input parameters to ensure they fall within an acceptable
	/// range.</remarks>
	/// <param name="value">The integer value to validate. Must not be negative and must not exceed <paramref name="other"/>.</param>
	/// <param name="other">The upper limit, inclusive, that <paramref name="value"/> must not exceed. Must be less than or equal to <see
	/// cref="long.MaxValue"/>.</param>
	/// <param name="paramName">The name of the parameter being validated. This is automatically populated by the compiler if not explicitly
	/// provided.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="value"/> is negative or greater than <paramref name="other"/>.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ThrowIfNegativeOrGreaterThan(long value, ulong other, [CallerArgumentExpression(nameof(value))] string? paramName = null)
	{
		ArgumentOutOfRangeException.ThrowIfGreaterThan(unchecked((ulong)value), other, paramName);
	}

	/// <summary>
	/// Casts the specified value to the given type parameter if possible, or throws an exception if the value is not of
	/// the expected type.
	/// </summary>
	/// <remarks>Use this method to ensure that an object is of a specific type at runtime. If the value is <see
	/// langword="null"/>, the method returns the default value of <typeparamref name="T"/>, which is <see langword="null"/>
	/// for a reference type, without throwing an exception.</remarks>
	/// <typeparam name="T">The type to which the value is expected to be cast.</typeparam>
	/// <param name="value">The object to validate and cast to type <typeparamref name="T"/>. May be <see langword="null"/>.</param>
	/// <param name="paramName">The name of the parameter being validated. This is typically provided automatically by the compiler and is used in
	/// exception messages.</param>
	/// <returns>The value cast to type <typeparamref name="T"/>, or the default value of <typeparamref name="T"/> if
	/// <paramref name="value"/> is <see langword="null"/>.</returns>
	/// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is not null and is not of type <typeparamref name="T"/>.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[return: NotNullIfNotNull(nameof(value))]
	public static T? ThrowIfNot<T>(object? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
	{
		const string msg = "Value of parameter '{0}' is of type '{1}' but must be of type '{2}'.";

		return value switch
		{
			null => default,
			not T => throwMsg(paramName, value),
			_ when typeof(T).IsValueType => (T)value,
			_ => Unsafe.As<object, T>(ref value),
		};

		[DoesNotReturn, StackTraceHidden, MethodImpl(MethodImplOptions.NoInlining)]
		static T? throwMsg(string? paramName, object value)
		{
			throw new ArgumentException(
				paramName: paramName,
				message: string.Format(
					CultureInfo.CurrentCulture,
					msg,
					paramName,
					value.GetType().FullName,
					typeof(T).FullName));
		}
	}
}
