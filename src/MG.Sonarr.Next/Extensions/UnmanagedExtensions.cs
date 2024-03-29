﻿using MG.Sonarr.Next.Attributes;
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
        /// <typeparam name="T">The type of value.</typeparam>
        /// <param name="n">The value whose length will be calculated.</param>
        /// <returns>
        ///     The number of characters <paramref name="n"/> would consist of if converted to its default
        ///     <see cref="string"/> equivalent.
        /// </returns>
        [DebuggerStepThrough]
        public static int GetLength<T>(this T n) where T : unmanaged, INumber<T>
        {
            Guard.NotNull(in n);

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