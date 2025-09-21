using System.Net.Http.Headers;

namespace MG.Sonarr.Next.Services.Http.Extensions
{
    internal static class HttpContentHeaderExtensions
    {
        /// <summary>
        /// Copies all headers from the current <see cref="HttpContentHeaders"/> instance to the specified target <see
        /// cref="HttpContentHeaders"/> instance.
        /// </summary>
        /// <remarks>This method attempts to add each header from the source to the target without
        /// validating the header values. Existing headers in the target instance are not overwritten.</remarks>
        /// <param name="original">The source <see cref="HttpContentHeaders"/> instance containing the headers to copy.</param>
        /// <param name="copy">The target <see cref="HttpContentHeaders"/> instance to which the headers will be copied.</param>
        internal static void CopyTo(this HttpContentHeaders original, HttpContentHeaders copy)
        {
            foreach (var kvp in original)
            {
                copy.TryAddWithoutValidation(kvp.Key, kvp.Value);
            }
        }
    }
}

