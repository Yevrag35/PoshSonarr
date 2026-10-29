namespace MG.Sonarr.Next.Services.Http;

/// <summary>
/// Provides the cmdlet that a DI scope belongs to, so that the clients that the scope resolves can hand it to the requests
/// they send.
/// </summary>
/// <remarks>
/// <para>
/// The container gives each scope its own instance, and a cmdlet's scope receives the cmdlet when the scope is created. The
/// HTTP handlers read the cmdlet back from each request to write its verbose and debug output, so cmdlets that run at once,
/// such as in the runspaces of <c>ForEach-Object -Parallel</c>, each receive the output of their own requests only.
/// </para>
/// <para>
/// A scope that no <see cref="IApiCmdlet"/> owns keeps a <see langword="null"/> cmdlet, and its requests write no output.
/// </para>
/// </remarks>
internal sealed class ApiCmdletAccessor
{
	/// <summary>
	/// The key of the request option that holds the cmdlet that sent the request.
	/// </summary>
	static readonly HttpRequestOptionsKey<IApiCmdlet> _cmdletKey = new(nameof(IApiCmdlet));

	/// <summary>
	/// Gets or sets the cmdlet that the scope belongs to.
	/// </summary>
	/// <value>
	/// The cmdlet that the scope belongs to, or <see langword="null"/> when no <see cref="IApiCmdlet"/> owns the scope.
	/// </value>
	public IApiCmdlet? Cmdlet { get; set; }

	/// <summary>
	/// Stores the scope's cmdlet in the options of the specified request, where the HTTP handlers find it.
	/// </summary>
	/// <remarks>
	/// The request's options stay unchanged when no cmdlet owns the scope.
	/// </remarks>
	/// <param name="request">The request that a client of the scope is about to send.</param>
	public void AttachTo(HttpRequestMessage request)
	{
		if (this.Cmdlet is IApiCmdlet cmdlet)
		{
			request.Options.Set(_cmdletKey, cmdlet);
		}
	}

	/// <summary>
	/// Gets the cmdlet that sent the specified request.
	/// </summary>
	/// <param name="request">The request to read the cmdlet from.</param>
	/// <param name="cmdlet">
	/// When this method returns, the cmdlet that sent <paramref name="request"/>, or <see langword="null"/> when no cmdlet
	/// did.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if a cmdlet sent <paramref name="request"/>; otherwise, <see langword="false"/>.
	/// </returns>
	public static bool TryGetCmdlet(HttpRequestMessage request, [NotNullWhen(true)] out IApiCmdlet? cmdlet)
	{
		return request.Options.TryGetValue(_cmdletKey, out cmdlet);
	}
}
