using MG.Sonarr.Resources;
using System.Runtime.CompilerServices;

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
	public static void BufferTooSmall(int requiredMinimum, int actualValue, string? parameterName = null, IFormatProvider? provider = null)
	{
		Debug.Assert(requiredMinimum > actualValue);
		provider ??= CultureInfo.CurrentCulture;

		string message = string.Format(
			provider: provider,
			format: ExMessages.Buffer_TooSmall,
			arg0: requiredMinimum,
			arg1: actualValue);

		throw new ArgumentException(message, parameterName);
	}

	/// <summary>
	/// Throws an <see cref="ArgumentException"/> with a message that indicates that the collection is empty when it should not be.
	/// </summary>
	/// <param name="paramName">
	/// The parameter name of the collection that is empty.
	/// </param>
	/// <exception cref="ArgumentException"/>
	[DoesNotReturn]
	public static void CollectionIsEmpty(string? paramName)
	{
		throw new ArgumentException(ExMessages.Collections_ExpectedOneOrMore, paramName);
	}
	/// <summary>
	/// Throws an <see cref="ArgumentException"/> with a message that indicates an element in the collection/span is <see langword="null"/>.
	/// </summary>
	/// <param name="paramName">
	/// The parameter name of collection/span that contains a <see langword="null"/> element.
	/// </param>
	/// <exception cref="ArgumentException"/>
	[DoesNotReturn]
	public static void ItemInCollectionIsNull(string? paramName)
	{
		throw new ArgumentException(ExMessages.Collections_ItemIsNull, paramName);
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
        Debug.Assert(other is <= int.MaxValue and not 0, "The other value should never be 0 and always than or equal to int.MaxValue.");
        if ((uint)value > other)
        {
            throw new ArgumentOutOfRangeException(paramName, value,
                message: string.Format(
                    CultureInfo.CurrentCulture,
                    "{0} ('{1}') must not be negative but also not greater than '{2}'. (Parameter '{0}')\r\nActual value was {1}.",
                    paramName,
                    value,
                    other));
        }
    }
}
