using System.Collections.ObjectModel;

namespace MG.Sonarr.Next.Extensions.Reflection;

public sealed class PSTypeAcceleratorNames
{
    public static readonly PSTypeAcceleratorNames Shared = new(InitializeDictionary());

    private readonly ReadOnlyDictionary<Type, string> _dict;

    public string this[Type key] => this.TryGetName(key, out string? name)
        ? name
        : key.GetName();

    private PSTypeAcceleratorNames(Dictionary<Type, string> keyValuePairs)
    {
        _dict = new(keyValuePairs);
    }

    public string GetName([DisallowNull] Type type)
    {
        return _dict.TryGetValue(type, out string? acceleratedName)
            ? acceleratedName
            : type.GetName();
    }
    [return: NotNullIfNotNull(nameof(otherName))]
    public string? GetNameOr(Type? type, string? otherName)
    {
        if (type is null || !_dict.TryGetValue(type, out string? acceleratedName))
        {
            return otherName;
        }

        return acceleratedName;
    }

    public bool TryGetName(Type type, [NotNullWhen(true)] out string? acceleratedName)
    {
        return _dict.TryGetValue(type, out acceleratedName);
    }

    private static Dictionary<Type, string> InitializeDictionary()
    {
        return new()
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
            { typeof(short), "int16" },
            { typeof(int), "int32" },
            { typeof(long), "int64" },
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
            { typeof(System.Management.Automation.PSObject), "pscustomobject" },
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
            { typeof(float), "single" },
            { typeof(string), "string" },
            { typeof(System.Management.Automation.SupportsWildcardsAttribute), "SupportsWildcards" },
            { typeof(System.Management.Automation.SwitchParameter), "switch" },
            { typeof(System.TimeSpan), "timespan" },
            { typeof(System.Type), "type" },
            { typeof(uint), "uint" },
            { typeof(ushort), "uint16" },
            { typeof(uint), "uint32" },
            { typeof(ulong), "uint64" },
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
        };
    }
}