using MG.Sonarr.Next.Guarding;
using System.Numerics;

namespace MG.Sonarr.Next.Buffers;

/// <summary>
/// A helper class for working with bits.
/// </summary>
[DebuggerStepThrough]
public static class BitHelper
{
	/// <summary>
	/// The largest power of 2 that can be represented by an <see cref="int"/>.
	/// </summary>
	/// <value>
	/// <c>2^30</c> -or- <c>1,073,741,824</c>.
	/// </value>
	public const uint LargestIntPowerOf2 = 1_073_741_824;
	private const int MINIMUM_POWER = 2;

	/// <summary>
	/// Rounds the given <see cref="int"/> value up to the next power of 2.
	/// </summary>
	/// <param name="value">The value to round up.</param>
	/// <returns>The smallest power of 2 that is at least 2 and greater than or equal to <paramref name="value"/>.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is negative or greater than
	/// <see cref="LargestIntPowerOf2"/>.</exception>
	public static int RoundUpToPowerOf2(int value)
	{
		ThrowHelper.ThrowIfNegativeOrGreaterThan(value, LargestIntPowerOf2);

		return value switch
		{
			< MINIMUM_POWER => MINIMUM_POWER,
			_ => (int)BitOperations.RoundUpToPowerOf2((uint)value),
		};
	}
	/// <summary>
	/// Rounds and sets the given <see cref="int"/> value up to the next power of 2.
	/// </summary>
	/// <remarks>
	/// This method does not check for negative or overflowable values.
	/// </remarks>
	/// <param name="value">The value to round up.</param>
	public static int RoundUpToPowerOf2Unsafe(int value)
	{
		Debug.Assert(value > 0, "Value is 0 or negative!");
		return unchecked((int)BitOperations.RoundUpToPowerOf2((uint)value));
	}
}