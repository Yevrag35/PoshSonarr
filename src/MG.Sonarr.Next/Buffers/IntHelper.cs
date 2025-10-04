namespace MG.Sonarr.Next.Buffers;

internal static class IntHelper
{
	/// <summary>
	/// Branch‑free 3‑way comparison that returns –1, 0, or +1,
	/// matching the contract of <see cref="IComparable{T}.CompareTo(T)"/>.
	/// </summary>
	/// <remarks>
	/// The method relies on two facts about two‑complement arithmetic:
	/// <list type="number">
	/// <item><description>When <c>b &lt; a</c>, the subtraction <c>(b - a)</c> has its sign‑bit set; shifting the unsigned result right by 31 places yields <c>1</c>, otherwise <c>0</c>.</description></item>
	/// <item><description>We can obtain the complementary test (<c>a &lt; b</c>) by swapping the operands.  Subtracting the two 0/1 flags produces the required –1, 0, +1 range.</description></item>
	/// </list>
	/// No branches are emitted; the JIT lowers the shifts to a single <c>shr</c> on x86‑64.
	/// </remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int Compare(int a, int b)
	{
		// 1) Flag is 1 when b < a, otherwise 0
		uint greaterFlag = (uint)(b - a) >> 31;

		// 2) Flag is 1 when a < b, otherwise 0
		uint lessFlag = (uint)(a - b) >> 31;

		// 3) Subtract the flags:  1‑0 = +1  |  0‑0 = 0  |  0‑1 = -1
		return (int)greaterFlag - (int)lessFlag;
	}
}