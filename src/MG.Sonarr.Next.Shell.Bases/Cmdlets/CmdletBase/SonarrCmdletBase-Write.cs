using MG.Sonarr.Next.Exceptions;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Services.Http;
using System.Text.Json;

namespace MG.Sonarr.Next.Shell.Cmdlets;

public abstract partial class SonarrCmdletBase
{
	/// <summary>
	/// Writes the JSON of the specified object to the debug stream when the cmdlet's <see cref="DebugPreference"/>
	/// allows it, and skips serializing it otherwise.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The object is serialized by its runtime type, as the HTTP client serializes a request's body, and with
	/// <see cref="ISonarrJsonOptions.ForDebugging"/> unless <paramref name="options"/> names others, so the JSON is the
	/// body that a request sends, indented.
	/// </para>
	/// <para>
	/// Serializing a model runs its <see cref="System.Text.Json.Serialization.IJsonOnSerializing.OnSerializing"/>, which
	/// only the <see cref="MG.Sonarr.Next.Models.SonarrObject.Commit"/> or
	/// <see cref="MG.Sonarr.Next.Models.SonarrObject.Reset"/> after a request undoes. So a cmdlet writes a model here
	/// only once <see cref="Cmdlet.ShouldProcess(string, string)"/> has agreed to send it, or <c>-WhatIf</c> would leave
	/// the model changed.
	/// </para>
	/// </remarks>
	/// <param name="value">The object to serialize, or <see langword="null"/>.</param>
	/// <param name="message">A message to write before the JSON, or <see langword="null"/> to write none.</param>
	/// <param name="includeType">
	/// <see langword="true"/> to write the name of the object's type before the JSON; otherwise, <see langword="false"/>.
	/// </param>
	/// <param name="options">
	/// The options to serialize with, or <see langword="null"/> for <see cref="ISonarrJsonOptions.ForDebugging"/>.
	/// </param>
	[DebuggerStepThrough]
	protected void SerializeIfDebug(object? value, string? message = null, bool includeType = true, JsonSerializerOptions? options = null)
	{
		if (this.DebugPreference != ActionPreference.SilentlyContinue)
		{
			if (!string.IsNullOrWhiteSpace(message))
			{
				this.WriteDebug(message);
			}

			options ??= this.Services?.GetService<ISonarrJsonOptions>()?.ForDebugging;

			Type type = value?.GetType() ?? typeof(object);

			if (includeType)
			{
				this.WriteDebug($"Serializing 'value' of type: {type.FullName ?? type.Name}");
			}

			this.WriteDebug(JsonSerializer.Serialize(value, type, options));
		}
	}

	/// <inheritdoc cref="TryWriteObject{T, TOutput}(SonarrClientResult{T}, bool, bool, Func{T, TOutput})"
	///     path="/*[not(self::summary)]"/>
	/// <summary>
	/// Attempts to write the response object to the output stream, applying default conditions for 
	/// writing the object based on the response status and its type.
	/// </summary>
	[DebuggerStepThrough]
	protected bool TryWriteObject<T>(SonarrClientResult<T> response)
	{
		return this.TryWriteObject(
			response,
			writeConditionally: true,
			enumerateCollection: true);
	}

	/// <inheritdoc cref="TryWriteObject{T, TOutput}(SonarrClientResult{T}, bool, bool, Func{T, TOutput})"
	///     path="/*[not(self::summary)]"/>
	/// <summary>
	/// Attempts to write the response object to the output stream, applying conditional logic based on 
	/// the response status and whether to enumerate collections.
	/// </summary>
	[DebuggerStepThrough]
	protected bool TryWriteObject<T>(SonarrClientResult<T> response, bool writeConditionally, bool enumerateCollection)
	{
		return this.TryWriteObject(response, writeConditionally, enumerateCollection, writeSelector: null);

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
	protected bool TryWriteObject<T>(SonarrClientResult<T> response, bool writeConditionally, bool enumerateCollection, Func<T, object?>? writeSelector)
	{
		if (!response.IsError)
		{
			object? data = writeSelector is null
				? response.Value
				: writeSelector(response.Value);

			this.WriteObject(data, enumerateCollection);

			return true;
		}

		if (writeConditionally)
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
	//[DebuggerStepThrough]
	protected void WriteConditionalError(SonarrErrorRecord error)
	{
		if (error.IsIgnorable)
		{
			return;
		}

		this.WriteError(error);
	}
}

