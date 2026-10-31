using System.Numerics;

namespace MG.Sonarr.Next.Extensions;

public static class UnmanagedExtensions
{
	/// <summary>
	/// Returns the number of characters that the specified integer takes when it's written in decimal, without
	/// formatting it.
	/// </summary>
	/// <remarks>
	/// The count includes a one-character minus sign for a negative value, as the invariant culture writes it.
	/// </remarks>
	/// <typeparam name="T">The integer type.</typeparam>
	/// <param name="n">The value whose length will be calculated.</param>
	/// <returns>
	///     The number of characters in <paramref name="n"/>'s decimal form, its minus sign included.
	/// </returns>
	[DebuggerStepThrough]
	public static int GetLength<T>(this T n) where T : unmanaged, IBinaryInteger<T>
	{
		// Division truncates toward zero for a negative value too, so the loop never needs the absolute value, which
		// overflows for the minimum value of a signed type.
		T ten = T.CreateTruncating(10);
		int length = T.IsNegative(n) ? 2 : 1;
		while ((n /= ten) != T.Zero)
		{
			length++;
		}

		return length;
	}
}