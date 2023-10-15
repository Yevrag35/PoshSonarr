namespace MG.Sonarr.Next.Extensions
{
    /// <summary>
    /// Custom extension methods for <see cref="Enum"/> classes.
    /// </summary>
    public static class EnumExtensions
    {
        public static bool TryFormatToLower<TEnum>(this TEnum value, Span<char> destination, out int charsWritten)
            where TEnum : unmanaged, Enum
        {
            charsWritten = 0;
            ReadOnlySpan<char> asSpan = value.ToString();

            if (!Enum.IsDefined(value))
            {    
                return asSpan.TryCopyToSlice(destination, ref charsWritten);
            }
            else
            {
                charsWritten = asSpan.ToLower(destination, Statics.DefaultCulture);
                return charsWritten > 0;
            }
        }
    }
}
