namespace MG.Sonarr.Next.Extensions
{
    [DebuggerStepThrough]
    public static class EqualityExtensions
    {
        public static bool IsEqualTo<T>(this T @this, [NotNullWhen(true)] T? other) where T : struct, IEquatable<T>
        {
            return other.HasValue && @this.Equals(other.Value);
        }
        public static bool IsEqualTo<T>(this T? @this, T? other) where T : IEquatable<T>
        {
            return BothAreNull(@this, other)
                   ||
                   LeftIsEqualToRight(@this, other);
        }
        public static bool IsEqualTo<T, TOther>(this T? @this, TOther? other) where T : IEquatable<TOther>
        {
            return BothAreNull(@this, other)
                   ||
                   LeftIsEqualToRight(@this, other);
        }

        private static bool BothAreNull<T1, T2>(T1? t1, T2? t2)
        {
            return t1 is null && t2 is null;
        }
        private static bool LeftIsEqualToRight<T1, T2>(T1? t1, T2? t2) where T1 : IEquatable<T2>
        {
            return t1 is not null && t1.Equals(t2);
        }
    }
}
