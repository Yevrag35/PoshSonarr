namespace MG.Sonarr.Next.Attributes
{
    /// <summary>
    /// An attribute for Codacy to recognize that decorated method parameters have been validated against 
    /// being null.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
    public sealed class ValidatedNotNullAttribute : AnalysisAttribute
    {
    }

    /// <summary>
    /// Because Codacy is stupid.
    /// </summary>
    [DebuggerStepThrough]
    public static class Guard
    {
        /// <summary>
        /// Because Codacy is stupid.
        /// </summary>
        public static ReadOnlySpan<char> AsSpan([ValidatedNotNull] string? value)
        {
            NotNull(value);
            return value.AsSpan();
        }

        /// <summary>
        /// Because Codacy is stupid.
        /// </summary>
        public static void NotNull<T>([ValidatedNotNull] T value)
        {
            return;
        }

        /// <summary>
        /// Because Codacy is stupid.
        /// </summary>
        public static void NotNull<T>([ValidatedNotNull] in T value) where T : struct
        {
            return;
        }

        /// <summary>
        /// Because Codacy is stupid.
        /// </summary>
        public static void IsSpan<T>([ValidatedNotNull] ReadOnlySpan<T> value)
        {
            return;
        }

        /// <summary>
        /// Because Codacy is stupid.
        /// </summary>
        public static void IsSpan<T>([ValidatedNotNull] ReadOnlySpan<T> value1, [ValidatedNotNull] ReadOnlySpan<T> value2)
        {
            return;
        }

        /// <summary>
        /// Because Codacy is stupid.
        /// </summary>
        public static void IsSpan<T>([ValidatedNotNull] ReadOnlySpan<T> value1, [ValidatedNotNull] ReadOnlySpan<T> value2, [ValidatedNotNull] ReadOnlySpan<T> value3)
        {
            return;
        }

        /// <summary>
        /// Because Codacy is stupid.
        /// </summary>
        public static void IsSpan<T>([ValidatedNotNull] ReadOnlySpan<T> value1, [ValidatedNotNull] ReadOnlySpan<T> value2, [ValidatedNotNull] Span<T> value3)
        {
            return;
        }

        /// <summary>
        /// Because Codacy is stupid.
        /// </summary>
        public static void IsSpan<T>([ValidatedNotNull] ReadOnlySpan<T> value1, [ValidatedNotNull] Span<T> value2)
        {
            return;
        }

        /// <summary>
        /// Because Codacy is stupid.
        /// </summary>
        public static void IsSpan<T>([ValidatedNotNull] Span<T> value)
        {
            return;
        }

        /// <summary>
        /// Because Codacy is stupid.
        /// </summary>
        public static void IsSpan<T>([ValidatedNotNull] Span<T> value1, [ValidatedNotNull] Span<T> value2)
        {
            return;
        }

        /// <summary>
        /// Because Codacy is stupid.
        /// </summary>
        public static void IsSpan<T>([ValidatedNotNull] Span<T> value1, [ValidatedNotNull] ReadOnlySpan<T> value2)
        {
            return;
        }
    }
}
