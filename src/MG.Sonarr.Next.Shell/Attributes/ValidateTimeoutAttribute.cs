namespace MG.Sonarr.Next.Shell.Attributes;

// HttpClient.Timeout takes more than zero and at most int.MaxValue milliseconds, or an infinite timeout, and a value
// outside that range throws when the client is built. A timeout here has to be more than zero, so the infinite one goes
// too, and the error names the value, since PowerShell reads a number as ticks and a string such as '30' as days.
/// <summary>
/// Provides the validation of a timeout argument as one that the module's HTTP client accepts.
/// </summary>
/// <remarks>
/// <para>
/// The timeout must be greater than zero and at most <see cref="int.MaxValue"/> milliseconds, about 24.8 days.
/// <see cref="System.Net.Http.HttpClient"/> refuses a longer timeout, or one of zero or less, only when a scope builds
/// the connection's client, so the attribute turns it away when the arguments bind instead. It refuses
/// <see cref="System.Threading.Timeout.InfiniteTimeSpan"/> too, which the client would accept.
/// </para>
/// <para>
/// The error names the value that bound, since PowerShell reads a number, such as <c>30</c>, as ticks, and a string
/// such as <c>'30'</c> as days.
/// </para>
/// </remarks>
public sealed class ValidateTimeoutAttribute : ValidateArgumentsAttribute
{
	private static readonly TimeSpan _maxTimeout = TimeSpan.FromMilliseconds(int.MaxValue);

	/// <summary>
	/// Checks that the argument is a <see cref="TimeSpan"/> in the range that the client accepts.
	/// </summary>
	/// <param name="arguments">The argument, after the binder converted it to the parameter's type.</param>
	/// <param name="engineIntrinsics">The engine APIs of the session that binds the argument, which the method doesn't
	/// read.</param>
	/// <exception cref="ValidationMetadataException">Thrown when the argument isn't a <see cref="TimeSpan"/>, or is zero
	/// or less, or is longer than <see cref="int.MaxValue"/> milliseconds.</exception>
	protected override void Validate(object arguments, EngineIntrinsics engineIntrinsics)
	{
		if (arguments is TimeSpan timeout && timeout > TimeSpan.Zero && timeout <= _maxTimeout)
		{
			return;
		}

		throw new ValidationMetadataException($"The timeout must be greater than zero and at most {_maxTimeout}, but it was {arguments}.");
	}
}
