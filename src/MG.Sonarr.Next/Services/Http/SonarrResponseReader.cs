using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Models.Errors;
using MG.Sonarr.Next.Services.Http.Handlers;
using MG.Sonarr.Next.Unions;
using Microsoft.Extensions.DependencyInjection;
using System.Management.Automation;
using System.Net;
using System.Net.Http.Json;

namespace MG.Sonarr.Next.Services.Http;

/// <summary>
/// Provides methods that turn an HTTP exchange with Sonarr into a <see cref="SonarrClientResult"/>.
/// </summary>
/// <remarks>
/// <para>
/// The methods report a failure as a result whose <see cref="SonarrClientResult.IsError"/> is <see langword="true"/>: an
/// exchange with no response, a status code of 400 or above, and, for a request that returns a value, a body that can't
/// be deserialized.
/// </para>
/// <para>
/// The methods read the response's body but don't dispose the response or its request, which stay the caller's.
/// </para>
/// </remarks>
public interface IResponseReader
{
	/// <summary>
	/// Reads the outcome of a request that returns no value.
	/// </summary>
	/// <param name="call">The exchange to read.</param>
	/// <param name="targetObj">The object that the request acted on, which an error record names as its target, or <see langword="null"/>.</param>
	/// <param name="token">A token that cancels reading the response's body.</param>
	/// <returns>A task that completes with the result of the request.</returns>
	Task<SonarrClientResult> ReadNoResultAsync(HttpCall call, object? targetObj = null, CancellationToken token = default);
	/// <summary>
	/// Reads the outcome of a request that returns a value, and deserializes the value from a successful response's body.
	/// </summary>
	/// <remarks>
	/// A 404 response gives the ignorable result that <see cref="SonarrClientResult.NotFound{T}"/> creates.
	/// </remarks>
	/// <typeparam name="T">The type to deserialize the body as.</typeparam>
	/// <param name="call">The exchange to read.</param>
	/// <param name="targetObj">The object that the request acted on, which an error record names as its target, or <see langword="null"/>.</param>
	/// <param name="token">A token that cancels reading the response's body.</param>
	/// <returns>A task that completes with the result of the request, which holds the deserialized value on success.</returns>
	Task<SonarrClientResult<T>> ReadResultAsync<T>(HttpCall call, object? targetObj = null, CancellationToken token = default);
}

/// <summary>
/// Provides the <see cref="IResponseReader"/> that every client of a connection shares, which deserializes with the
/// connection's JSON options.
/// </summary>
/// <remarks>
/// The reader holds no per-request state, so one instance serves requests from many threads at once.
/// </remarks>
internal sealed class SonarrResponseReader : IResponseReader
{
	/// <summary>
	/// The JSON options that deserialize response bodies.
	/// </summary>
	readonly JsonSerializerOptions _options;

	/// <summary>
	/// Initializes a new <see cref="SonarrResponseReader"/> instance with the deserializing options of the specified JSON
	/// options.
	/// </summary>
	/// <param name="options">The connection's JSON options, whose <see cref="ISonarrJsonOptions.ForDeserializing"/> the reader uses.</param>
	public SonarrResponseReader(ISonarrJsonOptions options)
	{
		_options = options.ForDeserializing;
	}

	/// <inheritdoc/>
	public async Task<SonarrClientResult> ReadNoResultAsync(HttpCall call, object? targetObj = null, CancellationToken token = default)
	{
		if (TryGetInvalidResult(call, call.Response, out SonarrClientResult? result))
		{
			return result;
		}

		if (IsSuccessCode(call.Response.StatusCode, call.Method, out bool isIgnorable))
		{
			return new(call.Response, call.RequestUri);
		}

		string? content = await call.Response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
		IErrorCollection deserializedError = GetErrorFromContent(content, _options);
		SonarrHttpException httpEx = new(call.Request, call.Response, deserializedError, null);
		SonarrErrorRecord record = new(httpEx, targetObj);

		return new(record);
	}
	/// <inheritdoc/>
	public async Task<SonarrClientResult<T>> ReadResultAsync<T>(HttpCall call, object? targetObj = null, CancellationToken token = default)
	{
		if (((IReadOnlyDictionary<string, object?>)call.Request.Options).ContainsKey(ErrorHandler.Is404))
		{
			return SonarrClientResult.NotFound<T>();
		}

		if (TryGetInvalidResult(call, call.Response, out SonarrClientResult<T>? result))
		{
			return result;
		}

		if (IsSuccessCode(call.Response.StatusCode, call.Method, out bool isIgnorable))
		{
			var oneOf = await this.ReadContentAsync<T>(call.Response, targetObj, token).ConfigureAwait(false);

			unsafe
			{
				return oneOf.Match(call,
					f1: &ReadErrorFromOneOf<T>,
					f2: &ReadContentFromOneOf);
			}
		}

		string? content = await call.Response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
		IErrorCollection deserializedError = GetErrorFromContent(content, _options);
		SonarrHttpException httpEx = new(call.Request, call.Response, deserializedError, null);
		SonarrErrorRecord record = new(httpEx, targetObj);

		return new(record);
	}

	/// <summary>
	/// Reads the server's errors from the body of an error response.
	/// </summary>
	/// <remarks>
	/// A body that is one JSON object gives one error, and a JSON array, such as Sonarr's validation failures, gives one
	/// error for each item. Any other body, and one that doesn't deserialize, gives an empty collection.
	/// </remarks>
	/// <param name="content">The body of the response, or <see langword="null"/>.</param>
	/// <param name="options">The JSON options that deserialize the errors, or <see langword="null"/> for the defaults.</param>
	/// <returns>The errors that the body holds, or an empty collection when it holds none.</returns>
	private static IErrorCollection GetErrorFromContent(string? content, JsonSerializerOptions? options)
	{
		if (string.IsNullOrWhiteSpace(content))
		{
			return ErrorCollection.Empty;
		}

		IErrorCollection list;
		try
		{
			if (content.StartsWith('{'))
			{
				var error = JsonSerializer.Deserialize<SonarrServerError>(content, options);
				list = error is not null
					? ErrorCollection.FromOne(error)
					: ErrorCollection.Empty;
			}
			else if (content.StartsWith('['))
			{
				list = JsonSerializer.Deserialize<ErrorCollection>(content, options)
					?? ErrorCollection.Empty;
			}
			else
			{
				list = ErrorCollection.Empty;
			}
		}
		catch (Exception e)
		{
			Debug.Fail(e.Message);
			list = ErrorCollection.Empty;
		}

		return list;
	}

	/// <summary>
	/// Deserializes the body of a successful response as the specified type.
	/// </summary>
	/// <remarks>
	/// The method doesn't throw for a body it can't read: a body that is JSON <c>null</c>, or that fails to deserialize,
	/// gives an error record with the category <see cref="ErrorCategory.ParserError"/>.
	/// </remarks>
	/// <typeparam name="T">The type to deserialize the body as.</typeparam>
	/// <param name="response">The response whose body is read.</param>
	/// <param name="targetObj">The object that an error record names as its target, or <see langword="null"/>.</param>
	/// <param name="token">A token that cancels reading the body.</param>
	/// <returns>A task that completes with the deserialized value, or with an error record when the body can't be read.</returns>
	private async Task<Either<SonarrErrorRecord, T>> ReadContentAsync<T>(HttpResponseMessage response, object? targetObj, CancellationToken token)
	{
		try
		{
			return await response.Content.ReadFromJsonAsync<T>(_options, token).ConfigureAwait(false)
				?? throw new JsonException("Unable to deserialize the response content.");
		}
		catch (Exception e)
		{
			SonarrErrorRecord record = new(e, e.GetTypeName(), ErrorCategory.ParserError, targetObj);
			return record;
		}
	}

	/// <summary>
	/// Determines whether an exchange can't be read, and creates the failed result for it when it can't.
	/// </summary>
	/// <remarks>
	/// An exchange without a response gives an <see cref="EmptyHttpResponseException"/> error with the category
	/// <see cref="ErrorCategory.InvalidResult"/> and a status code of <see cref="ErrorHandler.NoResponseCode"/>.
	/// </remarks>
	/// <param name="call">The exchange to check.</param>
	/// <param name="msg">The response of <paramref name="call"/>.</param>
	/// <param name="result">When this method returns, the failed result, or <see langword="null"/> when the exchange can be read.</param>
	/// <returns>
	/// <see langword="true"/> if the exchange can't be read; otherwise, <see langword="false"/>.
	/// </returns>
	private static bool TryGetInvalidResult(HttpCall call, [NotNullWhen(false)] HttpResponseMessage? msg, [NotNullWhen(true)] out SonarrClientResult? result)
	{
		result = null;
		if (!call.HasResponse)
		{
			var ex = new EmptyHttpResponseException(call.RequestUri);
			result = SonarrClientResult.FromException(ex, ErrorCategory.InvalidResult, ErrorHandler.NoResponseCode, msg);
			return true;
		}

		return !ReferenceEquals(call.Response, msg);
	}
	/// <summary>
	/// Determines whether an exchange for a request that returns a value can't be read, and creates the failed result for
	/// it when it can't.
	/// </summary>
	/// <remarks>
	/// An exchange without a response gives an <see cref="EmptyHttpResponseException"/> error with the category
	/// <see cref="ErrorCategory.InvalidResult"/> and a status code of <see cref="ErrorHandler.NoResponseCode"/>.
	/// </remarks>
	/// <typeparam name="T">The type of the value that the request returns.</typeparam>
	/// <param name="call">The exchange to check.</param>
	/// <param name="msg">The response of <paramref name="call"/>.</param>
	/// <param name="result">When this method returns, the failed result, or <see langword="null"/> when the exchange can be read.</param>
	/// <returns>
	/// <see langword="true"/> if the exchange can't be read; otherwise, <see langword="false"/>.
	/// </returns>
	private static bool TryGetInvalidResult<T>(HttpCall call, [NotNullWhen(false)] HttpResponseMessage? msg, [NotNullWhen(true)] out SonarrClientResult<T>? result)
	{
		result = null;

		if (!call.HasResponse)
		{
			EmptyHttpResponseException ex = new(call.RequestUri);
			result = SonarrClientResult.FromException<T>(ex, ErrorCategory.InvalidResult, ErrorHandler.NoResponseCode, msg);
			return true;
		}

		return !ReferenceEquals(call.Response, msg);
	}
	/// <summary>
	/// Determines whether a status code reports success.
	/// </summary>
	/// <param name="code">The status code of the response, or <see langword="null"/>.</param>
	/// <param name="method">The method of the request.</param>
	/// <param name="isIgnorable">
	/// When this method returns, <see langword="true"/> if <paramref name="code"/> is 404 and <paramref name="method"/> is
	/// <see cref="HttpMethod.Get"/>; otherwise, <see langword="false"/>.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="code"/> is below 400; otherwise, <see langword="false"/>.
	/// </returns>
	private static bool IsSuccessCode(HttpStatusCode? code, HttpMethod method, out bool isIgnorable)
	{
		isIgnorable = false;
		int? status = (int?)code;
		switch (status)
		{
			case <= 399:
				return true;

			case 404:
				isIgnorable = method == HttpMethod.Get;
				return false;

			default:
				return false;
		}
	}

	/// <summary>
	/// Creates the failed result for a successful response whose body couldn't be deserialized.
	/// </summary>
	/// <typeparam name="T">The type of the value that the request returns.</typeparam>
	/// <param name="error">The record that describes why the body couldn't be read.</param>
	/// <param name="call">The exchange whose response was read.</param>
	/// <returns>
	/// A failed result with the status code of <paramref name="error"/>, or else that of the response.
	/// </returns>
	private static SonarrClientResult<T> ReadErrorFromOneOf<T>(SonarrErrorRecord error, HttpCall call)
	{
		return new(error)
		{
			StatusCode = error.StatusCode ?? call.Response!.StatusCode,
		};
	}
	/// <summary>
	/// Creates the successful result for a value deserialized from a response's body.
	/// </summary>
	/// <typeparam name="T">The type of the value.</typeparam>
	/// <param name="content">The deserialized value.</param>
	/// <param name="call">The exchange whose response was read.</param>
	/// <returns>A result that holds <paramref name="content"/>, with the status code and URL of the response.</returns>
	private static SonarrClientResult<T> ReadContentFromOneOf<T>(T content, HttpCall call)
	{
		return SonarrClientResult.Create(content, call.Response!, call.RequestUri);
	}
}

/// <summary>
/// Provides the registration of the module's <see cref="IResponseReader"/>.
/// </summary>
internal static class SonarrResponseReaderDependencyInjection
{
	/// <summary>
	/// Registers <see cref="SonarrResponseReader"/> as the singleton <see cref="IResponseReader"/>.
	/// </summary>
	/// <param name="services">The service collection to add the registration to.</param>
	/// <returns><paramref name="services"/>, so that calls can be chained.</returns>
	public static IServiceCollection AddResponseReader(this IServiceCollection services)
	{
		return services.AddSingleton<IResponseReader, SonarrResponseReader>();
	}
}
