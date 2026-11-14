namespace MG.Sonarr.Next.Extensions;

/// <summary>
/// Provides extension methods for performing equality checks on objects.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class RefEqualityExtensions
{
	/// <summary>
	/// Checks if two objects are reference equals or if the other object is null.
	/// </summary>
	/// <param name="this">The current non-<see langword="null"/> instance.</param>
	extension([DisallowNull] object @this)
	{
		/// <summary>
		/// Checks if two objects are reference equals or if the other object is <see langword="null"/>.
		/// </summary>
		/// <param name="other">The object to compare.</param>
		/// <param name="result">The result of the reference equality or <see langword="null"/> check.</param>
		/// <returns>
		/// <see langword="true"/> if the objects are reference equals or if <paramref name="other"/> is 
		/// <see langword="null"/>; otherwise, <see langword="false"/>.
		/// </returns>
		internal bool RefEqualsOrNull([NotNullWhen(false)] object? other, out bool result)
		{
			Debug.Assert(@this is not null, "I won't throw an exception, but this really should *NOT* be null.");
			if (ReferenceEquals(@this, other))
			{
				result = true;
				return true;
			}

			result = false;
			return other is null;
		}
	}
}
