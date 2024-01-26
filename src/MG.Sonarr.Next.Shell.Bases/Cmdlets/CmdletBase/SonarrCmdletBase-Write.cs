using MG.Sonarr.Next.Exceptions;
using MG.Sonarr.Next.Services.Http;
using System.Collections;
using System.Text.Json;

namespace MG.Sonarr.Next.Shell.Cmdlets
{
    public abstract partial class SonarrCmdletBase
    {
        static readonly Type _enumerableType = typeof(IEnumerable);
        static readonly Type _stringType = typeof(string);

        private static bool IsEnumerableType(Type type)
        {
            return (type.IsArray || _enumerableType.IsAssignableFrom(type)) 
                   && 
                   !_stringType.IsAssignableFrom(type);
        }

        /// <summary>
        /// Serializes a given object to the cmdlet's Debug output stream but only if the
        /// <see cref="DebugPreference"/> preference would allow for writing it. This possibly saves not 
        /// calling on expensive serialization operations if the output would not be shared.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value">The object to serialize.</param>
        /// <param name="message">An optional message to display before the serialization.</param>
        /// <param name="options">Options to provide the <see cref="JsonSerializer"/>.</param>
        [DebuggerStepThrough]
        protected void SerializeIfDebug<T>(T value, string? message = null, bool includeType = true, JsonSerializerOptions? options = null)
        {
            if (this.DebugPreference != ActionPreference.SilentlyContinue)
            {
                if (!string.IsNullOrWhiteSpace(message))
                {
                    this.WriteDebug(message);
                }

                Type type = value is not null
                    ? typeof(T)
                    : typeof(object);

                if (includeType)
                {
                    this.WriteDebug($"Serializing 'value' of type: {type.FullName ?? type.Name}");
                }

                this.WriteDebug(JsonSerializer.Serialize(value, type, options));
            }
        }

        /// <inheritdoc cref="TryWriteObject{T, TOutput}(in SonarrResponse{T}, bool, bool, Func{T, TOutput})"
        ///     path="/*[not(self::summary)]"/>
        /// <summary>
        /// Attempts to write the response object to the output stream, applying default conditions for 
        /// writing the object based on the response status and its type.
        /// </summary>
        [DebuggerStepThrough]
        protected bool TryWriteObject<T>(in SonarrResponse<T> response)
        {
            return this.TryWriteObject(
                in response,
                writeConditionally: true,
                enumerateCollection: IsEnumerableType(typeof(T)));
        }

        //[DebuggerStepThrough]
        //protected bool TryWriteObject<T, TOutput>(in SonarrResponse<T> response, Func<T, TOutput?> writeSelector)
        //{
        //    return this.TryWriteObject(in response,
        //        writeConditionally: true,
        //        enumerateCollection: IsEnumerableType(typeof(TOutput)),
        //        writeSelector);
        //}
        //[DebuggerStepThrough]
        //protected bool TryWriteObject<T>(in SonarrResponse<T> response, bool enumerateCollection)
        //{
        //    return this.TryWriteObject(in response, writeConditionally: true, enumerateCollection);
        //}
        //[DebuggerStepThrough]
        //protected bool TryWriteObject<T>(in SonarrResponse<T> response, bool enumerateCollection, Func<T, object?> writeSelector)
        //{
        //    return this.TryWriteObject(in response, writeConditionally: true, enumerateCollection, writeSelector);
        //}

        /// <inheritdoc cref="TryWriteObject{T, TOutput}(in SonarrResponse{T}, bool, bool, Func{T, TOutput})"
        ///     path="/*[not(self::summary)]"/>
        /// <summary>
        /// Attempts to write the response object to the output stream, applying conditional logic based on 
        /// the response status and whether to enumerate collections.
        /// </summary>
        [DebuggerStepThrough]
        protected bool TryWriteObject<T>(in SonarrResponse<T> response, bool writeConditionally, bool enumerateCollection)
        {
            return this.TryWriteObject(in response, writeConditionally, enumerateCollection, x => x);
        }
        /// <summary>
        /// Attempts to write the response object to the output stream after transforming it using
        /// the provided selector, applying conditional logic based on the response status and 
        /// whether to enumerate collections.
        /// </summary>
        /// <typeparam name="T">The type of the data in the Sonarr response.</typeparam>
        /// <typeparam name="TOutput">The type of the output after applying the transformation.
        /// </typeparam>
        /// <param name="response">The Sonarr response to process.</param>
        /// <param name="writeConditionally">Indicates whether to write the response conditionally
        /// based on its error status.</param>
        /// <param name="enumerateCollection">Indicates whether to enumerate the collection if the
        /// transformed data is a collection type.</param>
        /// <param name="writeSelector">A function to transform the response data before writing 
        /// to the output stream.</param>
        /// <returns><see langword="true"/> if the response was written successfully; otherwise, 
        /// <see langword="false"/>.</returns>
        protected bool TryWriteObject<T, TOutput>(in SonarrResponse<T> response, bool writeConditionally, bool enumerateCollection, Func<T, TOutput?> writeSelector)
        {
            if (!response.IsError)
            {
                this.WriteObject(writeSelector(response.Data), enumerateCollection);
                return true;
            }
            else if (writeConditionally)
            {
                this.WriteConditionalError(response.Error);
            }
            else
            {
                this.WriteError(response.Error);
            }

            return false;
        }

        /// <summary>
        /// Writes the given error record to the host, but only if 
        /// <see cref="SonarrErrorRecord.IsIgnorable"/> is <see langword="false"/>.
        /// </summary>
        /// <param name="error">The error record to write.</param>
        [DebuggerStepThrough]
        protected void WriteConditionalError(SonarrErrorRecord error)
        {
            if (error.IsIgnorable)
            {
                return;
            }

            this.WriteError(error);
        }
    }
}

