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

		private static bool ThisDotIsLonger(int length)
		{
			return THIS_DOT.Length > length;
		}
		/// <summary>
		/// Trims leading "this." from the specified <paramref name="argumentName"/> if it is present.
		/// </summary>
		/// <param name="argumentName">The argument name to trim.</param>
		/// <returns>
		/// The trimmed <paramref name="argumentName"/> if it starts with "this."; otherwise, the original string unchanged.
		/// </returns>
		[return: NotNullIfNotNull(nameof(argumentName))]
		protected static unsafe string? TrimThisDot(string? argumentName)
		{
			if (string.IsNullOrWhiteSpace(argumentName))
				return argumentName;

			int len = argumentName!.Length;

			// Preserve original behavior: if length <= "this.".Length, do not trim.
			if (len <= THIS_DOT.Length)
				return argumentName;

			// Fast ordinal check for "this." prefix without allocations.
			fixed (char* p = argumentName)
			{
				for (int i = 0; i < THIS_DOT.Length; i++)
				{
					if (p[i] != THIS_DOT[i])
						return argumentName;
				}

				int start = THIS_DOT.Length;     // position just after "this."
				int end = len - 1;

				// Left trim (skip whitespace after the prefix)
				while (start <= end && char.IsWhiteSpace(p[start]))
					start++;

				// Right trim (skip trailing whitespace)
				while (end >= start && char.IsWhiteSpace(p[end]))
					end--;

				// If only whitespace remained after the prefix, return empty string.
				if (start > end)
					return string.Empty;

				int newLen = end - start + 1;
				return argumentName.Substring(start, newLen);
			}
		}
	}
}
