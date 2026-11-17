namespace MG.Sonarr.Next.Services.Http;

/// <summary>
/// Represents the error object that Sonarr returns in the body of an error response.
/// </summary>
/// <remarks>
/// Each member holds a field of the JSON object, and a field that the body leaves out reads as <see langword="null"/>.
/// </remarks>
public interface IServerError
{
	/// <summary>
	/// Gets the error's description.
	/// </summary>
	/// <value>The description, or <see langword="null"/> when the body has none.</value>
	string? Description { get; }
	/// <summary>
	/// Gets the error's message.
	/// </summary>
	/// <value>The message, or <see langword="null"/> when the body has none.</value>
	string? Message { get; }
	/// <summary>
	/// Gets the error's title.
	/// </summary>
	/// <value>The title, or <see langword="null"/> when the body has none.</value>
	string? Title { get; }
	/// <summary>
	/// Gets the HTTP status code that the body's <c>status</c> field gives.
	/// </summary>
	/// <value>The status code, or <see langword="null"/> when the body has none.</value>
	int? StatusCode { get; }
	/// <summary>
	/// Gets the identifier of the server's trace of the failed request.
	/// </summary>
	/// <value>The trace identifier, or <see langword="null"/> when the body has none.</value>
	string? TraceId { get; }
}
