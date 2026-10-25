using System.Collections.Frozen;

namespace MG.Sonarr.Next.Extensions.Reflection;

/// <summary>
/// Provides a mapping between .NET types and their corresponding PowerShell type accelerator names.
/// </summary>
/// <remarks>Use this class to retrieve PowerShell type accelerator names for supported .NET types, or to obtain
/// bracketed type accelerator representations. This is useful when generating or analyzing PowerShell scripts that
/// reference types by their accelerator names. The set of mappings is based on common PowerShell type accelerators and
/// is case-insensitive for name lookups.</remarks>
public sealed class PSTypeAcceleratorNames
{
	public static readonly PSTypeAcceleratorNames Shared = new(InitializeDictionaries());

	private readonly FrozenDictionary<Type, string> _typeToNames;
	private readonly FrozenDictionary<string, string> _namesToBrackets;

	public string this[Type key] => this.TryGetName(key, out string? name)
		? name
		: key.GetName();

	private PSTypeAcceleratorNames((Dictionary<Type, string> typeToNames, Dictionary<string, string> namesToBrackets) tuple)
	{
		_typeToNames = tuple.typeToNames.ToFrozenDictionary();
		_namesToBrackets = tuple.namesToBrackets.ToFrozenDictionary(tuple.namesToBrackets.Comparer);
	}

	[DebuggerStepThrough]
	public string GetName([DisallowNull] Type type)
	{
		return this.TryGetName(type, out string? acceleratedName)
			? acceleratedName
			: type.GetName();
	}
	public string GetName([DisallowNull] Type type, bool includeBrackets)
	{
		return this.TryGetName(type, includeBrackets, out string? acceleratedName)
			? acceleratedName
			: type.GetName();
	}
	[DebuggerStepThrough]
	[return: NotNullIfNotNull(nameof(otherName))]
	public string? GetNameOr(Type? type, string? otherName)
	{
		return this.GetNameOr(type, otherName, includeBrackets: false);
	}
	[return: NotNullIfNotNull(nameof(otherName))]
	public string? GetNameOr(Type? type, string? otherName, bool includeBrackets)
	{
		return type is not null && this.TryGetName(type, includeBrackets, out string? acceleratedName)
			? acceleratedName
			: otherName;
	}

	public bool TryGetName(Type type, [NotNullWhen(true)] out string? acceleratedName)
	{
		return _typeToNames.TryGetValue(type, out acceleratedName);
	}
	public bool TryGetName(Type type, bool includeBrackets, [NotNullWhen(true)] out string? acceleratedName)
	{
		return this.TryGetName(type, out acceleratedName)
			   &&
			   (
					!includeBrackets || this.TryGetBracketName(acceleratedName, out acceleratedName)
			   );
	}
	public bool TryGetBracketName(string name, [NotNullWhen(true)] out string? bracketName)
	{
		return _namesToBrackets.TryGetValue(name, out bracketName);
	}

	private static (Dictionary<Type, string>, Dictionary<string, string>) InitializeDictionaries()
	{
		return (new()
		{
			{ typeof(System.DirectoryServices.DirectoryEntry), "adsi" },
			{ typeof(System.DirectoryServices.DirectorySearcher), "adsisearcher" },
			{ typeof(System.Management.Automation.AliasAttribute), "Alias" },
			{ typeof(System.Management.Automation.AllowEmptyCollectionAttribute), "AllowEmptyCollection" },
			{ typeof(System.Management.Automation.AllowEmptyStringAttribute), "AllowEmptyString" },
			{ typeof(System.Management.Automation.AllowNullAttribute), "AllowNull" },
			{ typeof(System.Management.Automation.ArgumentCompleterAttribute), "ArgumentCompleter" },
			{ typeof(System.Management.Automation.ArgumentCompletionsAttribute), "ArgumentCompletions" },
			{ typeof(System.Array), "array" },
			{ typeof(System.Numerics.BigInteger), "bigint" },
			{ typeof(bool), "bool" },
			{ typeof(byte), "byte" },
			{ typeof(char), "char" },
			{ typeof(Microsoft.Management.Infrastructure.CimClass), "cimclass" },
			{ typeof(Microsoft.Management.Infrastructure.CimConverter), "cimconverter" },
			{ typeof(Microsoft.Management.Infrastructure.CimInstance), "ciminstance" },
			{ typeof(Microsoft.Management.Infrastructure.CimSession), "CimSession" },
			{ typeof(Microsoft.Management.Infrastructure.CimType), "cimtype" },
			{ typeof(System.Management.Automation.CmdletBindingAttribute), "CmdletBinding" },
			{ typeof(System.Globalization.CultureInfo), "cultureinfo" },
			{ typeof(System.DateTime), "datetime" },
			{ typeof(decimal), "decimal" },
			{ typeof(double), "double" },
			{ typeof(System.Management.Automation.DscLocalConfigurationManagerAttribute), "DscLocalConfigurationManager" },
			{ typeof(System.Management.Automation.DscPropertyAttribute), "DscProperty" },
			{ typeof(System.Management.Automation.DscResourceAttribute), "DscResource" },
			{ typeof(System.Management.Automation.ExperimentAction), "ExperimentAction" },
			{ typeof(System.Management.Automation.ExperimentalAttribute), "Experimental" },
			{ typeof(System.Management.Automation.ExperimentalFeature), "ExperimentalFeature" },
			{ typeof(float), "float" },
			{ typeof(System.Guid), "guid" },
			{ typeof(System.Collections.Hashtable), "hashtable" },
			{ typeof(System.Management.Automation.Runspaces.InitialSessionState), "initialsessionstate" },
			{ typeof(int), "int" },
			{ typeof(System.Net.IPAddress), "ipaddress" },
			{ typeof(System.Net.IPEndPoint), "IPEndpoint" },
			{ typeof(long), "long" },
			{ typeof(System.Net.Mail.MailAddress), "mailaddress" },
			{ typeof(System.Management.Automation.Language.NullString), "NullString" },
			{ typeof(System.Security.AccessControl.ObjectSecurity), "ObjectSecurity" },
			{ typeof(System.Collections.Specialized.OrderedDictionary), "ordered" },
			{ typeof(System.Management.Automation.OutputTypeAttribute), "OutputType" },
			{ typeof(System.Management.Automation.ParameterAttribute), "Parameter" },
			{ typeof(System.Net.NetworkInformation.PhysicalAddress), "PhysicalAddress" },
			{ typeof(System.Management.Automation.PowerShell), "powershell" },
			{ typeof(System.Management.Automation.PSAliasProperty), "psaliasproperty" },
			{ typeof(System.Management.Automation.PSCredential), "pscredential" },
			{ typeof(System.Management.Automation.PSCustomObject), "pscustomobject" },
			{ typeof(System.Management.Automation.PSDefaultValueAttribute), "PSDefaultValue" },
			{ typeof(System.Management.Automation.PSListModifier), "pslistmodifier" },
			{ typeof(System.Management.Automation.PSModuleInfo), "psmoduleinfo" },
			{ typeof(System.Management.Automation.PSNoteProperty), "psnoteproperty" },
			{ typeof(System.Management.Automation.PSObject), "psobject" },
			{ typeof(System.Management.Automation.PSPrimitiveDictionary), "psprimitivedictionary" },
			{ typeof(Microsoft.PowerShell.Commands.PSPropertyExpression), "pspropertyexpression" },
			{ typeof(System.Management.Automation.PSScriptMethod), "psscriptmethod" },
			{ typeof(System.Management.Automation.PSScriptProperty), "psscriptproperty" },
			{ typeof(System.Management.Automation.PSTypeNameAttribute), "PSTypeNameAttribute" },
			{ typeof(System.Management.Automation.PSVariable), "psvariable" },
			{ typeof(System.Management.Automation.PSVariableProperty), "psvariableproperty" },
			{ typeof(System.Management.Automation.PSReference), "ref" },
			{ typeof(System.Text.RegularExpressions.Regex), "regex" },
			{ typeof(System.Management.Automation.Runspaces.Runspace), "runspace" },
			{ typeof(System.Management.Automation.Runspaces.RunspaceFactory), "runspacefactory" },
			{ typeof(sbyte), "sbyte" },
			{ typeof(System.Management.Automation.ScriptBlock), "scriptblock" },
			{ typeof(System.Security.SecureString), "securestring" },
			{ typeof(System.Management.Automation.SemanticVersion), "semver" },
			{ typeof(short), "short" },
			{ typeof(string), "string" },
			{ typeof(System.Management.Automation.SupportsWildcardsAttribute), "SupportsWildcards" },
			{ typeof(System.Management.Automation.SwitchParameter), "switch" },
			{ typeof(System.TimeSpan), "timespan" },
			{ typeof(System.Type), "type" },
			{ typeof(uint), "uint" },
			{ typeof(ulong), "ulong" },
			{ typeof(System.Uri), "uri" },
			{ typeof(ushort), "ushort" },
			{ typeof(System.Management.Automation.ValidateCountAttribute), "ValidateCount" },
			{ typeof(System.Management.Automation.ValidateDriveAttribute), "ValidateDrive" },
			{ typeof(System.Management.Automation.ValidateLengthAttribute), "ValidateLength" },
			{ typeof(System.Management.Automation.ValidateNotNullAttribute), "ValidateNotNull" },
			{ typeof(System.Management.Automation.ValidateNotNullOrEmptyAttribute), "ValidateNotNullOrEmpty" },
			{ typeof(System.Management.Automation.ValidatePatternAttribute), "ValidatePattern" },
			{ typeof(System.Management.Automation.ValidateRangeAttribute), "ValidateRange" },
			{ typeof(System.Management.Automation.ValidateScriptAttribute), "ValidateScript" },
			{ typeof(System.Management.Automation.ValidateSetAttribute), "ValidateSet" },
			{ typeof(System.Management.Automation.ValidateTrustedDataAttribute), "ValidateTrustedData" },
			{ typeof(System.Management.Automation.ValidateUserDriveAttribute), "ValidateUserDrive" },
			{ typeof(System.Version), "version" },
			{ typeof(void), "void" },
			{ typeof(System.Management.Automation.WildcardPattern), "WildcardPattern" },
			{ typeof(System.Management.ManagementObject), "wmi" },
			{ typeof(System.Management.ManagementClass), "wmiclass" },
			{ typeof(System.Management.ManagementObjectSearcher), "wmisearcher" },
			{ typeof(System.Security.Cryptography.X509Certificates.X500DistinguishedName), "X500DistinguishedName" },
			{ typeof(System.Security.Cryptography.X509Certificates.X509Certificate), "X509Certificate" },
			{ typeof(System.Xml.XmlDocument), "xml" },
		},
		new(StringComparer.OrdinalIgnoreCase)
		{
			{ "adsi", "[adsi]" },
			{ "adsisearcher", "[adsisearcher]" },
			{ "Alias", "[Alias]" },
			{ "AllowEmptyCollection", "[AllowEmptyCollection]" },
			{ "AllowEmptyString", "[AllowEmptyString]" },
			{ "AllowNull", "[AllowNull]" },
			{ "ArgumentCompleter", "[ArgumentCompleter]" },
			{ "ArgumentCompletions", "[ArgumentCompletions]" },
			{ "array", "[array]" },
			{ "bigint", "[bigint]" },
			{ "bool", "[bool]" },
			{ "byte", "[byte]" },
			{ "char", "[char]" },
			{ "cimclass", "[cimclass]" },
			{ "cimconverter", "[cimconverter]" },
			{ "ciminstance", "[ciminstance]" },
			{ "CimSession", "[CimSession]" },
			{ "cimtype", "[cimtype]" },
			{ "CmdletBinding", "[CmdletBinding]" },
			{ "cultureinfo", "[cultureinfo]" },
			{ "datetime", "[datetime]" },
			{ "decimal", "[decimal]" },
			{ "double", "[double]" },
			{ "DscLocalConfigurationManager", "[DscLocalConfigurationManager]" },
			{ "DscProperty", "[DscProperty]" },
			{ "DscResource", "[DscResource]" },
			{ "ExperimentAction", "[ExperimentAction]" },
			{ "Experimental", "[Experimental]" },
			{ "ExperimentalFeature", "[ExperimentalFeature]" },
			{ "float", "[float]" },
			{ "guid", "[guid]" },
			{ "hashtable", "[hashtable]" },
			{ "initialsessionstate", "[initialsessionstate]" },
			{ "int", "[int]" },
			{ "ipaddress", "[ipaddress]" },
			{ "IPEndpoint", "[IPEndpoint]" },
			{ "long", "[long]" },
			{ "mailaddress", "[mailaddress]" },
			{ "NullString", "[NullString]" },
			{ "ObjectSecurity", "[ObjectSecurity]" },
			{ "ordered", "[ordered]" },
			{ "OutputType", "[OutputType]" },
			{ "Parameter", "[Parameter]" },
			{ "PhysicalAddress", "[PhysicalAddress]" },
			{ "powershell", "[powershell]" },
			{ "psaliasproperty", "[psaliasproperty]" },
			{ "pscredential", "[pscredential]" },
			{ "pscustomobject", "[pscustomobject]" },
			{ "PSDefaultValue", "[PSDefaultValue]" },
			{ "pslistmodifier", "[pslistmodifier]" },
			{ "psmoduleinfo", "[psmoduleinfo]" },
			{ "psnoteproperty", "[psnoteproperty]" },
			{ "psobject", "[psobject]" },
			{ "psprimitivedictionary", "[psprimitivedictionary]" },
			{ "pspropertyexpression", "[pspropertyexpression]" },
			{ "psscriptmethod", "[psscriptmethod]" },
			{ "psscriptproperty", "[psscriptproperty]" },
			{ "PSTypeNameAttribute", "[PSTypeNameAttribute]" },
			{ "psvariable", "[psvariable]" },
			{ "psvariableproperty", "[psvariableproperty]" },
			{ "ref", "[ref]" },
			{ "regex", "[regex]" },
			{ "runspace", "[runspace]" },
			{ "runspacefactory", "[runspacefactory]" },
			{ "sbyte", "[sbyte]" },
			{ "scriptblock", "[scriptblock]" },
			{ "securestring", "[securestring]" },
			{ "semver", "[semver]" },
			{ "short", "[short]" },
			{ "single", "[single]" },
			{ "string", "[string]" },
			{ "SupportsWildcards", "[SupportsWildcards]" },
			{ "switch", "[switch]" },
			{ "timespan", "[timespan]" },
			{ "type", "[type]" },
			{ "uint", "[uint]" },
			{ "ulong", "[ulong]" },
			{ "uri", "[uri]" },
			{ "ushort", "[ushort]" },
			{ "ValidateCount", "[ValidateCount]" },
			{ "ValidateDrive", "[ValidateDrive]" },
			{ "ValidateLength", "[ValidateLength]" },
			{ "ValidateNotNull", "[ValidateNotNull]" },
			{ "ValidateNotNullOrEmpty", "[ValidateNotNullOrEmpty]" },
			{ "ValidatePattern", "[ValidatePattern]" },
			{ "ValidateRange", "[ValidateRange]" },
			{ "ValidateScript", "[ValidateScript]" },
			{ "ValidateSet", "[ValidateSet]" },
			{ "ValidateTrustedData", "[ValidateTrustedData]" },
			{ "ValidateUserDrive", "[ValidateUserDrive]" },
			{ "version", "[version]" },
			{ "void", "[void]" },
			{ "WildcardPattern", "[WildcardPattern]" },
			{ "wmi", "[wmi]" },
			{ "wmiclass", "[wmiclass]" },
			{ "wmisearcher", "[wmisearcher]" },
			{ "X500DistinguishedName", "[X500DistinguishedName]" },
			{ "X509Certificate", "[X509Certificate]" },
			{ "xml", "[xml]" },
		});
	}
}