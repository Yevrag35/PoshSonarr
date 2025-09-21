using MG.Sonarr.Next.Components;

namespace MG.Sonarr.Next.Extensions;

public static class ArrayExtensions
{
    /// <summary>
    /// Determines whether the specified array is null or has a length of zero.
    /// </summary>
    /// <param name="array">The array to test for nullity or emptiness.</param>
    /// <returns>true if the array is null or has a length of zero; otherwise, false.</returns>
    public static bool IsNullOrEmpty([NotNullWhen(false)] this Array? array)
    {
        return array is null || array.Length == 0;
    }

    /// <summary>
    /// Removes all <see langword="null"/> elements from the specified array.
    /// </summary>
    /// <typeparam name="T">The type of elements in the array.</typeparam>
    /// <param name="array">The array this method is extending.</param>
    /// <returns>
    /// A new array containing only the non-<see langword="null"/> elements from the specified array -or - if 
    /// the specified array is empty or does *not* contain any <see langword="null"/> elements, the original array unchanged.
    /// </returns>
    public static T[] ToNonNullArray<T>(this T?[] array) where T : class
    {
        Span<T?> span = array;
        if (span.IsEmpty)
        {
            return array!;
        }

        RentedBuffer<T> buffer = new(array.Length);

        try
        {
            int nonNullCount = 0;

            for (int i = 0; i < span.Length; i++)
            {
                ref T? element = ref span[i];
                if (element is not null)
                {
                    buffer[nonNullCount++] = element;
                }
            }

            if (nonNullCount == span.Length)
            {
                // Non need to allocate a new array.
                return array!;
            }
            else if (nonNullCount == 0)
            {
                return [];
            }

            return buffer.Slice(0, nonNullCount).ToArray();
        }
        finally
        {
            buffer.Dispose();
        }
    }
    /// <summary>
    /// Removes all <see langword="null"/> elements from the specified array.
    /// </summary>
    /// <typeparam name="T">The type of elements in the array.</typeparam>
    /// <param name="array">The array this method is extending.</param>
    /// <returns>
    /// A new array containing only the non-<see langword="null"/> elements from the specified array -or - if the specified array:
    /// <para>
    /// 1. is <see langword="null"/>, null is returned.
    /// </para>
    /// <para>
    /// 2. is empty or does *not* contain any <see langword="null"/> elements, the original array unchanged.
    /// </para>
    /// </returns>
    [return: NotNullIfNotNull(nameof(array))]
    public static T[]? ToNonNullArrayOrNull<T>(this T?[]? array) where T : class
    {
        if (array is null)
        {
            return array!;
        }

        return ToNonNullArray(array);
    }
}
