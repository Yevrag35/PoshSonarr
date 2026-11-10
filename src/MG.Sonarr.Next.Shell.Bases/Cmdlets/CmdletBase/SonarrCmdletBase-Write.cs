using MG.Sonarr.Next.Exceptions;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Services.Http;
using System.Text.Json;

namespace MG.Sonarr.Next.Shell.Cmdlets;

public partial class SonarrCmdletBase
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

	/// <summary>
	/// Attempts to write the response's value to the output stream, enumerating it when it's a collection, or else
	/// writes the response's error unless the error is ignorable.
	/// </summary>
	/// <remarks>
	/// The method calls <see cref="TryWriteObject{T}(SonarrClientResult{T}, bool, bool, Func{T, object})"/> with
	/// <c>writeConditionally</c> and <c>enumerateCollection</c> both <see langword="true"/> and no selector.
	/// </remarks>
	/// <typeparam name="T">The type of the data in the Sonarr response.</typeparam>
	/// <param name="response">The Sonarr response to process.</param>
	/// <returns>
	/// <see langword="true"/> if the response succeeded and its value was written; otherwise, <see langword="false"/>.
	/// </returns>
	/// <exception cref="PipelineStoppedException">
	/// Thrown when the error is written under <c>-ErrorAction Stop</c>, and when a downstream command such as
	/// <c>Select-Object -First</c> has all the objects it needs.
	/// </exception>
	[DebuggerStepThrough]
	protected bool TryWriteObject<T>(SonarrClientResult<T> response)
	{
		return this.TryWriteObject(
			response,
			writeConditionally: true,
			enumerateCollection: true);
	}

	/// <summary>
	/// Attempts to write the response's value to the output stream, or else writes the response's error, as
	/// <paramref name="writeConditionally"/> says.
	/// </summary>
	/// <remarks>
	/// The method calls <see cref="TryWriteObject{T}(SonarrClientResult{T}, bool, bool, Func{T, object})"/> with no
	/// selector.
	/// </remarks>
	/// <typeparam name="T">The type of the data in the Sonarr response.</typeparam>
	/// <param name="response">The Sonarr response to process.</param>
	/// <param name="writeConditionally">
	/// <see langword="true"/> to skip writing an error that is ignorable, such as a 404's; otherwise,
	/// <see langword="false"/> to write every error.
	/// </param>
	/// <param name="enumerateCollection">
	/// <see langword="true"/> to write each item of the value when it's a collection; otherwise,
	/// <see langword="false"/> to write it as one object.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if the response succeeded and its value was written; otherwise, <see langword="false"/>.
	/// </returns>
	/// <exception cref="PipelineStoppedException">
	/// Thrown when the error is written under <c>-ErrorAction Stop</c>, and when a downstream command such as
	/// <c>Select-Object -First</c> has all the objects it needs.
	/// </exception>
	[DebuggerStepThrough]
	protected bool TryWriteObject<T>(SonarrClientResult<T> response, bool writeConditionally, bool enumerateCollection)
	{
		return this.TryWriteObject(response, writeConditionally, enumerateCollection, writeSelector: null);

	}
	/// <summary>
	/// Attempts to write the response's value, or what the specified selector makes of it, to the output stream, or
	/// else writes the response's error, as <paramref name="writeConditionally"/> says.
	/// </summary>
	/// <remarks>
	/// <para>
	/// When the response succeeded, the method writes its value, or the selector's result, through
	/// <see cref="Cmdlet.WriteObject(object, bool)"/>. When it failed, the method writes its error through
	/// <see cref="WriteConditionalError(SonarrErrorRecord)"/> or <see cref="Cmdlet.WriteError(ErrorRecord)"/>, so
	/// <c>-ErrorAction</c> decides what happens to it.
	/// </para>
	/// </remarks>
	/// <typeparam name="T">The type of the data in the Sonarr response.</typeparam>
	/// <param name="response">The Sonarr response to process.</param>
	/// <param name="writeConditionally">
	/// <see langword="true"/> to skip writing an error that is ignorable, such as a 404's; otherwise,
	/// <see langword="false"/> to write every error.
	/// </param>
	/// <param name="enumerateCollection">
	/// <see langword="true"/> to write each item of the written object when it's a collection; otherwise,
	/// <see langword="false"/> to write it as one object.
	/// </param>
	/// <param name="writeSelector">
	/// A function that turns the response's value into the object to write, or <see langword="null"/> to write the
	/// value itself.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if the response succeeded and its value was written; otherwise, <see langword="false"/>.
	/// </returns>
	/// <exception cref="PipelineStoppedException">
	/// Thrown when the error is written under <c>-ErrorAction Stop</c>, and when a downstream command such as
	/// <c>Select-Object -First</c> has all the objects it needs.
	/// </exception>
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
	/// Writes the specified error record to the error stream, but only if
	/// <see cref="SonarrErrorRecord.IsIgnorable"/> is <see langword="false"/>.
	/// </summary>
	/// <remarks>
	/// A record that stands for a 404 is ignorable, so a cmdlet that looks up something Sonarr doesn't have writes
	/// nothing instead of an error.
	/// </remarks>
	/// <param name="error">The error record to write.</param>
	/// <exception cref="PipelineStoppedException">
	/// Thrown when the error is written under <c>-ErrorAction Stop</c>.
	/// </exception>
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

