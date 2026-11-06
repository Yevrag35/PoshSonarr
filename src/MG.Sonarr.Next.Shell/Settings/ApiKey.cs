using MG.Sonarr.Next.Exceptions.Shell;
using MG.Sonarr.Next.Services.Auth;
using System.Runtime.InteropServices;
using System.Security;

namespace MG.Sonarr.Next.Shell.Settings;

/// <summary>
/// Represents the API key that a connection sends to Sonarr, which converts implicitly from a <see cref="string"/> or a
/// <see cref="SecureString"/>.
/// </summary>
/// <remarks>
/// <para>
/// The conversions let the <c>-ApiKey</c> parameter of <c>Connect-SonarrInstance</c> and <c>Test-SonarrHost</c> take
/// either a plain string or a secure string, such as one that <c>Read-Host -AsSecureString</c> returns. A secure string
/// is read into a plain string when it converts, since the key goes out in every request's <c>X-Api-Key</c> header.
/// </para>
/// <para>
/// The default value holds no key, as <see cref="Empty"/> does.
/// </para>
/// </remarks>
[StructLayout(LayoutKind.Sequential), DebuggerDisplay("{_key}")]
public readonly struct ApiKey : IApiKey
{
	private readonly string? _key;

	/// <summary>
	/// Gets a value that indicates whether the key is missing, empty, or only white space.
	/// </summary>
	/// <value><see langword="true"/> if the key is <see langword="null"/>, empty, or only white space; otherwise,
	/// <see langword="false"/>.</value>
	public bool IsEmpty => string.IsNullOrWhiteSpace(_key);

	/// <summary>
	/// Gets the number of characters in the key.
	/// </summary>
	/// <value>The number of characters in the key, or 0 when there's no key.</value>
	public int Length => _key?.Length ?? 0;

	/// <summary>
	/// Initializes a new <see cref="ApiKey"/> instance with the specified plain-text key.
	/// </summary>
	/// <param name="key">The key, or <see langword="null"/> for an empty one.</param>
	private ApiKey(string? key)
	{
		_key = key ?? string.Empty;
	}
	/// <summary>
	/// Initializes a new <see cref="ApiKey"/> instance with the key that the specified secure string holds.
	/// </summary>
	/// <param name="secureString">The secure string that holds the key.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="secureString"/> is null.</exception>
	private ApiKey(SecureString secureString)
		: this(ConvertFromSecureString(secureString))
	{
	}

	/// <summary>
	/// Reads the plain text that a secure string holds.
	/// </summary>
	/// <remarks>
	/// The text is copied into an unmanaged BSTR, read from it, and the BSTR is zeroed and freed.
	/// </remarks>
	/// <param name="ss">The secure string to read.</param>
	/// <returns>The plain text of <paramref name="ss"/>.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="ss"/> is null.</exception>
	private static string ConvertFromSecureString(SecureString ss)
	{
		nint bstr = Marshal.SecureStringToBSTR(ss);
		try
		{
			return Marshal.PtrToStringBSTR(bstr);
		}
		finally
		{
			Marshal.ZeroFreeBSTR(bstr);
		}
	}

	/// <summary>
	/// The key that holds an empty string.
	/// </summary>
	public static readonly ApiKey Empty = new(string.Empty);

	/// <summary>
	/// Gets the plain text of the key.
	/// </summary>
	/// <returns>The key, or <see cref="string.Empty"/> when there's no key.</returns>
	public string GetValue()
	{
		return _key ?? string.Empty;
	}

	/// <summary>
	/// Checks that the key isn't missing, empty, or only white space.
	/// </summary>
	/// <exception cref="InvalidApiKeyException">Thrown when the key is missing, empty, or only white space.</exception>
	public void Validate()
	{
		if (string.IsNullOrWhiteSpace(_key))
		{
			throw new InvalidApiKeyException();
		}
	}

	/// <summary>
	/// Converts a plain-text key to an <see cref="ApiKey"/>.
	/// </summary>
	/// <param name="key">The key, or <see langword="null"/> for an empty one.</param>
	/// <returns>The key that holds <paramref name="key"/>.</returns>
	public static implicit operator ApiKey(string? key) => new(key);
	/// <summary>
	/// Converts the key that a secure string holds to an <see cref="ApiKey"/>.
	/// </summary>
	/// <param name="secureString">The secure string that holds the key.</param>
	/// <returns>The key that holds the plain text of <paramref name="secureString"/>.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="secureString"/> is null.</exception>
	public static implicit operator ApiKey(SecureString secureString) => new(secureString);
}
