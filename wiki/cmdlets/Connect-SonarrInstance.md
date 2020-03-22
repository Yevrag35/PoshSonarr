# Connect-SonarrInstance

## SYNOPSIS
Builds the connection context for subsequent cmdlets.

## SYNTAX

### ByServerName (Default)
```
Connect-SonarrInstance [[-SonarrServerName] <String>] [-ReverseProxyUriBase <String>] [-UseSSL] [-SkipCertificateCheck] [-AllowRedirects] [-Proxy <String>] [-ProxyCredential <ProxyCredential>] [-ProxyBypassOnLocal] -ApiKey <ApiKey> [-NoApiPrefix] [-PassThru] [-PortNumber <Int32>] [<CommonParameters>]
```

### BySonarrUrl
```
Connect-SonarrInstance [-SonarrUrl] <Uri> [-SkipCertificateCheck] [-AllowRedirects] [-Proxy <String>] [-ProxyCredential <ProxyCredential>] [-ProxyBypassOnLocal] -ApiKey <ApiKey> [-NoApiPrefix] [-PassThru] [<CommonParameters>]
```

## DESCRIPTION
Establishes a custom HttpClient context for use with all subsequent PoshSonarr cmdlets. The connection is created either via hostname/port/url base or by direct URL. The "/api" path is automatically appended unless the '-NoApiPrefix' parameter is used. If this command is not run first, all other cmdlets will throw an error.

## EXAMPLES

### EXAMPLE 1
Connect by 'HostName' and 'Port'
```powershell
C:\PS> Connect-Sonarr -Server "MEDIASERVER" -ApiKey "xxxxxxxxxxxxxxxx" -PassThru
```

### EXAMPLE 2
Connect by explicit URL
```powershell
C:\PS> Connect-SonarrInstance -Url 'https://sonarr-api.cloud.com/api/custom' -ApiKey "xxxxxxxxxxxxxxxx" -NoApiPrefix
```

## PARAMETERS

### ReverseProxyUriBase
Specifies a custom URL base for use with reverse proxies. If you don't use a reverse proxy with Sonarr, then you don't need this parameter :)

```yaml
Type: String
Parameter Sets: ByServerName
Aliases: 

Required: false
Position: named
Default Value: 
Pipeline Input: False
```

### UseSSL
Indicates that connection should establish over an SSL connection when using the "ByServerName" parameter set.

```yaml
Type: SwitchParameter
Parameter Sets: ByServerName
Aliases: 

Required: false
Position: named
Default Value: 
Pipeline Input: False
```

### SkipCertificateCheck
Specifies the HttpClient to ignore any certificate errors that may occur.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: 

Required: false
Position: named
Default Value: 
Pipeline Input: False
```

### AllowRedirects
Specifies the HttpClient to follow any HTTP redirects. See the wiki article for more information: https://github.com/Yevrag35/PoshSonarr/wiki/Reverse-Proxy-Information

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: 

Required: false
Position: named
Default Value: 
Pipeline Input: False
```

### Proxy
Specifies a proxy URL that HttpClient must use.

```yaml
Type: String
Parameter Sets: (All)
Aliases: 

Required: false
Position: named
Default Value: 
Pipeline Input: False
```

### ProxyCredential
Specifies a set of credentials in order to access the proxy. These can either be PSCredential or NetworkCredential objects.

```yaml
Type: ProxyCredential
Parameter Sets: (All)
Aliases: 

Required: false
Position: named
Default Value: 
Pipeline Input: False
```

### ProxyBypassOnLocal
Indicates that the proxy should be used on local connections.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: 

Required: false
Position: named
Default Value: 
Pipeline Input: False
```

### ApiKey
The API key to use for authentication. The key is 32, all lower-case, alphanumeric characters. The key can be retrieved from your Sonarr website (Settings =\> General =\> Security), or in the "Config.xml" file in the AppData directory.

The input can be a normal string and will be validated when cast to MG.Sonarr.ApiKey.

```yaml
Type: ApiKey
Parameter Sets: (All)
Aliases: 

Required: true
Position: named
Default Value: 
Pipeline Input: False
```

### NoApiPrefix
Indicates that all API requests should not append '/api' to the end of the URL path.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: 

Required: false
Position: named
Default Value: 
Pipeline Input: False
```

### PassThru
Passes through the connection testing the "/system/status" endpoint.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: 

Required: false
Position: named
Default Value: 
Pipeline Input: False
```

### SonarrServerName
The hostname of the Sonarr instance.

```yaml
Type: String
Parameter Sets: ByServerName
Aliases: HostName

Required: false
Position: 0
Default Value: 
Pipeline Input: False
```

### PortNumber
The port number for the Sonarr website.

```yaml
Type: Int32
Parameter Sets: ByServerName
Aliases: 

Required: false
Position: named
Default Value: 
Pipeline Input: False
```

### SonarrUrl
Specifies the direct URL to the Sonarr instance; including any reverse proxy bases. The "/api" path is automatically appended unless the '-NoApiPrefix' parameter is used.

```yaml
Type: Uri
Parameter Sets: BySonarrUrl
Aliases: Url

Required: true
Position: 0
Default Value: 
Pipeline Input: False
```

### \<CommonParameters\>
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see about_CommonParameters (http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None


## OUTPUTS

### MG.Sonarr.Results.StatusResult


## NOTES

## RELATED LINKS


*Generated by: PowerShell HelpWriter 2020 v2.3.46*
