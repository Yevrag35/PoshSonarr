using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MG.Sonarr.Next.Services.Http.Extensions
{
    /// <summary>
    /// Provides extension methods for working with metadata in <see cref="HttpResponseMessage"/> instances.
    /// </summary>
    internal static class HttpResponseMetadataExtensions
    {
        /// <summary>
        /// Adds metadata to the <see cref="HttpResponseMessage"/> by associating a specified key with a value.
        /// </summary>
        /// <remarks>The metadata is stored in the <see cref="HttpRequestMessage.Options"/> property of the associated
        /// request message. If the <see cref="HttpResponseMessage.RequestMessage"/> is <see langword="null"/>, the metadata
        /// will not be added.</remarks>
        /// <param name="response">The <see cref="HttpResponseMessage"/> to which the metadata will be added.</param>
        /// <param name="key">The key used to identify the metadata.</param>
        /// <param name="value">The value to associate with the specified key.</param>
        /// <returns>
        /// <see langword="true"/> if the metadata was successfully added; otherwise, <see langword="false"/> if the request message
        /// is <see langword="null"/> or the key already exists in the options.
        /// </returns>
        /// <inheritdoc cref="ArgumentNullException.ThrowIfNull(object, string)" path="/exception"><paramref name="key"/> is null.</inheritdoc>
        internal static bool AddMetadata(this HttpResponseMessage response, string key, object? value)
        {
            ArgumentNullException.ThrowIfNull(key);
            return response.RequestMessage?.Options is HttpRequestOptions options
                && options.TryAdd(key, value);
        }
        /// <summary>
        /// Determines whether the specified HTTP response contains metadata with the given key.
        /// </summary>
        /// <param name="response">The <see cref="HttpResponseMessage"/> to inspect for metadata.</param>
        /// <param name="key">The key of the metadata to check for.</param>
        /// <returns><see langword="true"/> if the metadata with the specified key exists in the request message options; otherwise,
        /// <see langword="false"/> if the request message is <see langword="null"/> or the key does not exist.</returns>
        /// <inheritdoc cref="ArgumentNullException.ThrowIfNull(object, string)" path="/exception"><paramref name="key"/> is null.</inheritdoc>
        internal static bool ContainsMetadata(this HttpResponseMessage response, string key)
        {
            ArgumentNullException.ThrowIfNull(key);

            return response.RequestMessage?.Options is IReadOnlyDictionary<string, object?> options
                && options.ContainsKey(key);
        }
        /// <summary>
        /// Determines whether the specified key exists in the HTTP request options.
        /// </summary>
        /// <param name="options">The <see cref="HttpRequestOptions"/> instance to search.</param>
        /// <param name="key">The key to locate in the options.</param>
        /// <returns><see langword="true"/> if the specified key exists in the options; otherwise, <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool ContainsMetadata(this HttpRequestOptions options, string key)
        {
            return ((IReadOnlyDictionary<string, object?>)options).ContainsKey(key);
        }
        /// <summary>
        /// Attempts to add a metadata key-value pair to the <see cref="HttpRequestMessage.Options"/> of the associated
        /// request.
        /// </summary>
        /// <remarks>This method updates the <see cref="HttpRequestMessage.Options"/> of the request associated with the
        /// given response. If the request message is null, the operation will fail and return <see
        /// langword="false"/>.</remarks>
        /// <param name="response">The <see cref="HttpResponseMessage"/> whose associated request's options will be updated.</param>
        /// <param name="key">The metadata key to add.</param>
        /// <param name="value">The metadata value to associate with the key.</param>
        /// <returns><see langword="true"/> if the key-value pair was successfully added to the request's options; otherwise, <see
        /// langword="false"/> if the request message is null or the key already exists.</returns>
        /// <inheritdoc cref="ArgumentNullException.ThrowIfNull(object, string)" path="/exception"><paramref name="key"/> is null.</inheritdoc>
        internal static bool TryAddMetadata(this HttpResponseMessage response, string key, object? value)
        {
            ArgumentNullException.ThrowIfNull(key);

            return response.RequestMessage?.Options.TryAdd(key, value) ?? false;
        }
        /// <summary>
        /// Attempts to retrieve a metadata value of the specified type from the <see cref="HttpResponseMessage"/>.
        /// </summary>
        /// <typeparam name="T">The type of the metadata value to retrieve.</typeparam>
        /// <param name="response">The HTTP response message from which to retrieve the metadata value. </param>
        /// <param name="key">The key associated with the metadata value to retrieve.</param>
        /// <param name="value">When this method returns, contains the metadata value associated with the specified key if found and not <see
        /// langword="null"/>; otherwise, contains the default value for the type <typeparamref name="T"/>.</param>
        /// <returns><see langword="true"/> if the metadata value was successfully retrieved and is not <see langword="null"/>;
        /// otherwise, <see langword="false"/>.</returns>
        /// <inheritdoc cref="ArgumentNullException.ThrowIfNull(object, string)" path="/exception"><paramref name="key"/> or <paramref name="response"/> is null.</inheritdoc>
        internal static bool TryGetMetadataValue<T>(this HttpResponseMessage response, string key, [NotNullWhen(true)] out T? value)
        {
            ArgumentNullException.ThrowIfNull(response);
            ArgumentNullException.ThrowIfNull(key);

            value = default;
            return
                response.RequestMessage?.Options is HttpRequestOptions options
                &&
                options.TryGetValue(new(key), out value)
                &&
                value is not null;
        }
    }
}
