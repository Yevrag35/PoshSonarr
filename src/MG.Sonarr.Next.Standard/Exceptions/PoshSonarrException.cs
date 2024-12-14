using MG.Sonarr.Next.Attributes;
using System;
using System.Diagnostics.CodeAnalysis;

#nullable enable

namespace MG.Sonarr.Next.Exceptions
{
    /// <summary>
    /// An <see langword="abstract"/> base class for all <see cref="Exception"/> instances thrown by PoshSonarr libraries.
    /// </summary>
    public class PoshSonarrException : Exception
    {
        private const string THIS_DOT = "this.";

        /// <summary>
        /// Initializes a new instance of the <see cref="PoshSonarrException"/> class with a specified
        /// error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        protected PoshSonarrException(string? message)
            : base(message)
        {
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="PoshSonarrException"/> class with a specified error
        /// message and a reference to an inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">
        ///     The exception that is the casue of the current exception, or a <see langword="null"/> reference
        ///     if no inner exception is specified.
        /// </param>
        protected PoshSonarrException(string? message, Exception? innerException)
            : base(message, innerException)
        {
        }

        private static bool ThisDotIsLonger(ReadOnlySpan<char> argumentName)
        {
            return argumentName.IsEmpty || THIS_DOT.Length > argumentName.Length;
        }
        /// <summary>
        /// Trims leading "this." from the specified <paramref name="argumentName"/> if it is present.
        /// </summary>
        /// <param name="argumentName">The argument name to trim.</param>
        /// <returns>
        /// The trimmed <paramref name="argumentName"/> if it starts with "this."; otherwise, the original string unchanged.
        /// </returns>
        [return: NotNullIfNotNull(nameof(argumentName))]
        protected static string? TrimThisDot(string? argumentName)
        {
            ReadOnlySpan<char> name = argumentName.AsSpan();
            if (ThisDotIsLonger(name) || !name.StartsWith(THIS_DOT.AsSpan(), StringComparison.Ordinal))
            {
                return argumentName;
            }

            return name.Slice(THIS_DOT.Length).Trim().ToString();
        }
    }
}
