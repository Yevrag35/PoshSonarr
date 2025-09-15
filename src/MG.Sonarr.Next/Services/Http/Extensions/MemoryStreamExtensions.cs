using System.Runtime.CompilerServices;

namespace MG.Sonarr.Next.Services.Http.Extensions
{
    /// <summary>
    /// Provides extension methods for the <see cref="MemoryStream"/> class.
    /// </summary>
    internal static class MemoryStreamExtensions
    {
        /// <summary>
        /// Resets the position of the specified <see cref="MemoryStream"/> to the beginning.
        /// </summary>
        /// <remarks>If the <paramref name="stream"/> is not seekable, the method does nothing.</remarks>
        /// <param name="stream">The <see cref="MemoryStream"/> to rewind..</param>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> is null.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Rewind(this MemoryStream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);
            Debug.Assert(stream.CanSeek, "Attempted to rewind a non-seekable stream.");
            _ = stream.Seek(0, SeekOrigin.Begin);
        }
    }
}
