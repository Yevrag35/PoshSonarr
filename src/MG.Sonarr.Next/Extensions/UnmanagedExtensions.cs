using MG.Sonarr.Next.Attributes;
using System.Numerics;

namespace MG.Sonarr.Next.Extensions
{
    public static class UnmanagedExtensions
    {
        /// <summary>
        /// Returns the length of how many <see cref="char"/> elements make 
        /// up <typeparamref name="T"/> value if it were formatted as a 
        /// <see cref="string"/> instance without actually converting it.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="n"></param>
        /// <returns></returns>
        [DebuggerStepThrough]
        public static int GetLength<T>([ValidatedNotNull] this T n) where T : unmanaged, INumber<T>
        {
            if (T.IsZero(n))
            {
                return 1;
            }

            int length = 1;
            // For negative numbers, add one to count for the '-' sign.
            if (T.IsNegative(n))
            {
                length++;
                n = T.Abs(n);
            }

            // Compute floor of log base 10 of the number.
            int flooredLogBase10 = (int)Math
                .Floor(
                    Math.Log10(
                        double.CreateChecked(n)));

            // Add the number of digits in the absolute value of the number.
            length += flooredLogBase10;

            return length;
        }
    }
}