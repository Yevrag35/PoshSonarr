namespace MG.Sonarr.Next.Services.Http.Extensions;

/// <summary>
/// Provides extension methods for <see cref="HttpContentHeaders"/> instances.
/// </summary>
internal static class HttpContentHeaderExtensions
{
	/// <summary>
	/// Provides a method that copies the headers of an <see cref="HttpContentHeaders"/> instance to another.
	/// </summary>
	/// <param name="original">The source <see cref="HttpContentHeaders"/> instance containing the headers to copy.</param>
	extension(HttpContentHeaders original)
	{
		/// <summary>
		/// Copies all headers from the current <see cref="HttpContentHeaders"/> instance to the specified target <see
		/// cref="HttpContentHeaders"/> instance.
		/// </summary>
		/// <remarks>This method attempts to add each header from <paramref name="original"/> to the target without
		/// validating the header values. Existing headers in the target instance are not overwritten.</remarks>
		/// <param name="copy">The target <see cref="HttpContentHeaders"/> instance to which the headers will be copied.</param>
		internal void CopyTo(HttpContentHeaders copy)
		{
			foreach (var kvp in original)
			{
				copy.TryAddWithoutValidation(kvp.Key, kvp.Value);
			}
		}
	}
}
