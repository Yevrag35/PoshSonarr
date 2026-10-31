using System.Numerics;

namespace MG.Sonarr.Next.Extensions;

/// <summary>
/// Provides extension methods for integer values.
/// </summary>
public static class UnmanagedExtensions
{
	/// <summary>Returns the number of UTF‑16 characters required to format the value in decimal.</summary>
	/// <remarks>Works for any signed or unsigned integer type that implements <see cref="IBinaryInteger{T}"/>.</remarks>
	public static int GetLength<T>(this T value) where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>
	{
		// Fast‑track zero and +1 - these dominate in many counters/state machines.
		if (T.IsZero(value) || T.One.Equals(value))
			return 1;

		// Track negativity.  We need it only for the sign char.
		// (-x)  -> negativeSign = 1,  absValue = ~x + 1
		// ( x)  -> negativeSign = 0,  absValue =  x
		bool isNegative = T.IsNegative(value);
		int negativeSign = Unsafe.As<bool, byte>(ref isNegative);
		T absValue = isNegative
				   ? T.MinValue.Equals(value) ? T.MaxValue : T.Abs(value)
				   : value;

		// ---------------------------------------------------------------------
		// core digit-count - overflow-free for every unmanaged IBinaryInteger up to 128-bit (Int128/UInt128, the widest the constraint admits)
		// ---------------------------------------------------------------------

		// log2(x) ≈ bitLength - 1;  Digits ≈ ceil(log10(x))
		// A very good integer approximation: digits = ((bitLen * 1233) >> 12) + 1
		//   1233 / 2^12  ≈ 0.30103  (log10(2))
		int bitLen = absValue.GetShortestBitLength();
		int digits = (bitLen * 1233 >> 12) + 1;

		// Correct the occasional +1 error without branching.
		// If absValue < 10^(digits-1) we must subtract 1.
		// 10^(digits-1) is read from the per-T Pow10 table (built once in its static
		// constructor), so this correction costs just one comparison.
		T lookup = Pow10<T>.Get(digits - 1);
		if (lookup > absValue)
			digits--;

		return digits + negativeSign;
	}

	// -------------------------------------------------------------------------
	// Precomputed 10^n lookup so the digit-count correction needs no runtime
	// exponentiation. Built once per T in the static constructor below.
	// -------------------------------------------------------------------------
	private static class Pow10<T> where T : unmanaged, IBinaryInteger<T>
	{
		private static readonly T[] s_table;

		internal static T Get(int index) => s_table.Ref(index);

		static Pow10()
		{
			s_table = new T[40];   // 10^0..10^39; for 128-bit T only indices 0..38 are read, overflowed top entries are never indexed
			s_table[0] = T.One;
			for (int i = 1; i < s_table.Length; i++)
			{
				s_table[i] = s_table[i - 1] * T.CreateTruncating(10);
			}
		}
	}
}