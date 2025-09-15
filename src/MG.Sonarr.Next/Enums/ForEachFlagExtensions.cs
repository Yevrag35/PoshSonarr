namespace MG.Sonarr.Next.Enums;

internal static class ForEachFlagExtensions
{
    internal static FlagEnumerator<T> Enumerate<T>(this T mask) where T : unmanaged, Enum
    {
        return new FlagEnumerator<T>(mask);
    }
}
