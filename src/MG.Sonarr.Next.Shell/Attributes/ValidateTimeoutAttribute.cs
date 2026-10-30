namespace MG.Sonarr.Next.Shell.Attributes;

// HttpClient.Timeout takes more than zero and at most int.MaxValue milliseconds, or an infinite timeout, and a value
// outside that range throws when the client is built. A timeout here has to be more than zero, so the infinite one goes
// too, and the error names the value, since PowerShell reads a number as ticks and a string such as '30' as days.
public sealed class ValidateTimeoutAttribute : ValidateArgumentsAttribute
{
	static readonly TimeSpan _maxTimeout = TimeSpan.FromMilliseconds(int.MaxValue);

	protected override void Validate(object arguments, EngineIntrinsics engineIntrinsics)
	{
		if (arguments is TimeSpan timeout && timeout > TimeSpan.Zero && timeout <= _maxTimeout)
		{
			return;
		}

		throw new ValidationMetadataException($"The timeout must be greater than zero and at most {_maxTimeout}, but it was {arguments}.");
	}
}
