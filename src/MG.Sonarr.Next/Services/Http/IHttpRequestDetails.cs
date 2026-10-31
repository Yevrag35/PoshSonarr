namespace MG.Sonarr.Next.Services.Http;

/// <summary>
/// Provides the method and URL of an HTTP request that is about to be sent, and the services of the request's scope.
/// </summary>
/// <remarks>
/// <see cref="IApiCmdlet.WriteVerboseBefore(IHttpRequestDetails)"/> receives an instance to describe the request. Services
/// resolved through <see cref="IServiceProvider.GetService(Type)"/> come from a scope that the request owns, so they live no
/// longer than the request.
/// </remarks>
public interface IHttpRequestDetails : IServiceProvider
{
	/// <summary>
	/// Gets the HTTP method of the request, such as <c>GET</c>, <c>POST</c>, or <c>PUT</c>.
	/// </summary>
	/// <value>The name of the request's method.</value>
	string RequestMethod { get; }
	/// <summary>
	/// Gets the URL of the request.
	/// </summary>
	/// <value>The request's URL as a string.</value>
	string RequestUrl { get; }
}
