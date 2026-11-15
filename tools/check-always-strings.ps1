#Requires -Version 7.6

<#
.SYNOPSIS
	Check that every string property of a body that the module sends back to Sonarr is on the
	always-string list, or held in a form that the module can't read as a number.

.DESCRIPTION
	The module reads a JSON string that parses as a number, a Guid, a date, or a version as that
	type, unless a span converter that AddAllConverters registers for the property's name claims it
	(src/MG.Sonarr.Next/Json/SonarrJsonOptions.cs). When a request sends the object back, such a
	string goes to Sonarr as a number, and Sonarr 4 turns away a number for a string with a 400,
	even for a property that it then ignores.

	Sonarr leaves a null value out of its responses, so no response shows every property. The
	script reads them from Sonarr's source instead. For each resource class in its table of bodies,
	and each class that the resource nests or derives from, it finds the public properties that
	System.Text.Json fills from a request's body and that can hold a string - a string, a
	collection of strings, a dictionary of them, or an object - and sorts each one:

		claimed   The name that the module reads it as has a span converter, or, for an array, a
		          type in EnumerateConverterProperties that holds strings. That name is the
		          property's JSON name, capitalized, then renamed by the model's own
		          GetDeserializedNames at the top of the body, or else by
		          EnumerateGlobalReplaceNames.
		held      The script's table of held properties names it, with the reason that Sonarr keeps
		          its text in a form that can't be read as a number, such as a path that Sonarr
		          always checks is rooted.
		neither   Reported, with where it sits in the body and the cmdlets that send the body.

	The script also reports a resource, model, or property that its tables name and the source or
	the module lacks, and a held property that's claimed too. It warns when no tag on the
	checkout's HEAD names the Sonarr version that sandbox/sonarr/compose.yaml pins, as when it
	checks the next version before the sandbox moves to it.

	It reads C# with regular expressions rather than a compiler, which suits Sonarr's plain
	resource classes. A provider field's Value is an object whose shape comes from the provider's
	settings class, which the script doesn't follow.

	Exit codes: 0 when every property is claimed or held and the tables match the source and the
	module; 1 when a property is neither, or a table doesn't match; 2 when the checkout or the
	module's files can't be read or parsed.

.EXAMPLE
	.\tools\check-always-strings.ps1
	# the checkout in .temp/sonarr-src
.EXAMPLE
	.\tools\check-always-strings.ps1 ~/src/Sonarr -Verbose
	# another checkout, listing what claims or holds each property
.EXAMPLE
	pwsh -NoProfile -File tools/check-always-strings.ps1
	# from Bash
#>
[CmdletBinding()]
param (
	# The root of a clone of Sonarr's repository, the folder that holds src/, checked out at the tag of
	# the version to check, which is v followed by the version. A relative path resolves against the
	# current directory. The default is .temp/sonarr-src in the repository, which git ignores, so each
	# machine needs a clone of its own there.
	[Parameter(Mandatory = $false, Position = 0)]
	[string] $SonarrSource = (Join-Path $PSScriptRoot '..' '.temp' 'sonarr-src')
)

$ErrorActionPreference = 'Stop'

# The bodies that cmdlets send back to Sonarr: the resource class that Sonarr reads each one as, the
# module's models that hold what the cmdlets read, and the cmdlets. Properties limits a body that a
# cmdlet builds itself to the properties that it fills from what it read.
$bodies = @(
	@{ Resource = 'SeriesResource'; Models = 'SeriesObject', 'AddSeriesObject'; Cmdlets = 'Update-SonarrSeries', 'Add-SonarrSeries', 'Add-SonarrTag', 'Clear-SonarrTag' }
	@{ Resource = 'EpisodeResource'; Models = 'EpisodeObject'; Cmdlets = 'Update-SonarrEpisode' }
	@{ Resource = 'HostConfigResource'; Models = 'HostObject'; Cmdlets = 'Update-SonarrHostConfig' }
	@{ Resource = 'NamingConfigResource'; Models = 'NamingConfigObject'; Cmdlets = 'Update-SonarrNamingConfig' }
	@{ Resource = 'MediaManagementConfigResource'; Models = 'MediaManagementObject'; Cmdlets = 'Update-SonarrMediaManagement' }
	@{ Resource = 'DownloadClientResource'; Models = 'DownloadClientObject', 'DownloadClientSchemaObject'; Cmdlets = 'Update-SonarrDownloadClient', 'New-SonarrDownloadClient', 'Add-SonarrTag', 'Clear-SonarrTag' }
	@{ Resource = 'IndexerResource'; Models = 'IndexerObject'; Cmdlets = 'Update-SonarrIndexer', 'Add-SonarrTag', 'Clear-SonarrTag' }
	@{ Resource = 'NotificationResource'; Models = 'NotificationObject', 'NotificationSchemaObject'; Cmdlets = 'Update-SonarrNotification', 'Add-SonarrTag', 'Clear-SonarrTag' }
	@{ Resource = 'DelayProfileResource'; Models = 'DelayProfileObject'; Cmdlets = 'Update-SonarrDelayProfile', 'Add-SonarrTag', 'Clear-SonarrTag' }
	@{ Resource = 'ReleaseProfileResource'; Models = 'ReleaseProfileObject'; Cmdlets = 'Update-SonarrReleaseProfile', 'Add-SonarrTag', 'Clear-SonarrTag' }
	@{ Resource = 'RemotePathMappingResource'; Models = 'RemotePathObject'; Cmdlets = 'Update-SonarrRemotePathMapping' }
	@{ Resource = 'ManualImportFile'; Models = 'ManualImportObject'; Cmdlets = 'Invoke-SonarrManualImport'; Properties = 'Path', 'SeriesId', 'EpisodeIds', 'Quality', 'Languages', 'ReleaseGroup', 'IndexerFlags', 'ReleaseType' }
	@{ Resource = 'ReleaseResource'; Models = 'ReleaseObject'; Cmdlets = 'Add-SonarrRelease'; Properties = 'Guid', 'IndexerId' }
)

# Properties that can hold a string but stay off the always-string list, each with the reason that
# its text can't be read as a number. Keyed by the class that declares the property.
$held = [ordered]@{
	'AlternateTitleResource.SceneOrigin'                 = "Sonarr's own: where a scene mapping came from"
	'CustomFormatSpecificationSchema.Implementation'     = "Sonarr's own: a specification's class name"
	'CustomFormatSpecificationSchema.ImplementationName' = "Sonarr's own: a specification's kind"
	'CustomFormatSpecificationSchema.InfoLink'           = "Sonarr's own URL"
	'EpisodeFileResource.Path'                           = "Sonarr's own: the file's rooted path"
	'EpisodeFileResource.RelativePath'                   = "Sonarr's own: the file's path in the series' folder, which ends in an extension"
	'EpisodeResource.FinaleType'                         = 'a finale type from the metadata: series, season, or midseason'
	'Field.HelpLink'                                     = "Sonarr's own URL"
	'Field.HelpText'                                     = "Sonarr's own text"
	'Field.HelpTextWarning'                              = "Sonarr's own text"
	'Field.Hidden'                                       = "Sonarr's own: a HiddenType's name"
	'Field.Section'                                      = "Sonarr's own text"
	'Field.SelectOptionsProviderAction'                  = "Sonarr's own: an action's name"
	'Field.Type'                                         = "Sonarr's own: a FieldType's name"
	'Field.Unit'                                         = "Sonarr's own text"
	'HostConfigResource.ConsoleLogLevel'                 = "a log level, which Sonarr's logging reads with NLog's LogLevel.FromString, so only a level's name works"
	'HostConfigResource.LogLevel'                        = "a log level, which Sonarr's logging reads with NLog's LogLevel.FromString, so only a level's name works"
	'HostConfigResource.UrlBase'                         = 'Sonarr returns it empty or starting with /'
	'ManualImportFile.Path'                              = "a file's rooted path, from Sonarr's list of the files to import"
	'MediaCover.RemoteUrl'                               = 'a URL from the metadata'
	'MediaCover.Url'                                     = "Sonarr's own path, starting with /, or a URL from the metadata"
	'MediaInfoResource.AudioCodec'                       = "Sonarr's name for the codec, or FFmpeg's"
	'MediaInfoResource.Resolution'                       = "Sonarr's own: the width and height, joined by x"
	'MediaInfoResource.RunTime'                          = "Sonarr's own: a length of time with colons"
	'MediaInfoResource.ScanType'                         = "Sonarr's own: a scan type such as Progressive"
	'MediaInfoResource.VideoCodec'                       = "Sonarr's name for the codec, or FFmpeg's"
	'MediaInfoResource.VideoDynamicRange'                = "Sonarr's own: HDR or empty"
	'MediaInfoResource.VideoDynamicRangeType'            = "Sonarr's own: an HDR format's name"
	'MediaManagementConfigResource.RecycleBin'           = 'Sonarr checks that it is rooted whenever it is set'
	'NotificationResource.Link'                          = "Sonarr's own URL"
	'NotificationResource.TestCommand'                   = "Sonarr's own: a command's name"
	'ProviderMessage.Message'                            = "Sonarr's own text"
	'ProviderResource.ConfigContract'                    = "Sonarr's own: a settings class's name"
	'ProviderResource.Implementation'                    = "Sonarr's own: a provider's class name"
	'ProviderResource.ImplementationName'                = "Sonarr's own: a provider's kind"
	'ProviderResource.InfoLink'                          = "Sonarr's own URL"
	'RemotePathMappingResource.LocalPath'                = 'Sonarr checks that it is rooted'
	'SeriesResource.ImdbId'                              = 'an IMDb ID from the metadata, which starts with tt'
	'SeriesResource.Path'                                = 'Sonarr checks that it is rooted'
	'SeriesResource.ProfileName'                         = 'Sonarr never sets it, so no response has it'
	'SeriesResource.RemotePoster'                        = 'a URL from the metadata'
	'SeriesResource.RootFolderPath'                      = "Sonarr's own: the rooted folder that holds the series"
}

# Sonarr's projects that hold the resource classes and every class that they nest or derive from.
$projects = 'Sonarr.Api.V3', 'Sonarr.Http', 'NzbDrone.Core', 'NzbDrone.Common'

# Generic types whose elements System.Text.Json reads from an array, and those whose values it reads
# from an object's properties, whose names are data rather than a property's name.
$collectionTypes = 'List', 'IList', 'IEnumerable', 'ICollection', 'IReadOnlyList', 'IReadOnlyCollection', 'HashSet', 'ISet', 'SortedSet', 'Collection', 'ImmutableArray', 'ImmutableList', 'IImmutableList'
$dictionaryTypes = 'Dictionary', 'IDictionary', 'IReadOnlyDictionary', 'SortedDictionary', 'ConcurrentDictionary'

# Classes of Sonarr's that a converter of its own writes as a single string.
$valueClasses = 'HttpUri'

# Compiled, since it runs over each of the roughly 2,000 files in Sonarr's projects.
$declarationRegex = [regex]::new('(?m)^[ \t]*(?:\[[^\]\r\n]*\][ \t]*)*(?:(?:public|internal|private|protected|static|sealed|abstract|partial|readonly|unsafe|new|file)[ \t]+)*(?<kind>record(?:[ \t]+(?:class|struct))?|class|enum|struct|interface)[ \t]+(?<name>\w+)', 'Compiled')
$namespaceRegex = [regex]::new('(?m)^[ \t]*namespace[ \t]+(?<name>[\w.]+)')
$usingRegex = [regex]::new('(?m)^[ \t]*using[ \t]+(?<name>[\w.]+)[ \t]*;')

# Comments and string and character literals, which can hold braces, semicolons, and words that look
# like code.
$literalRegex = [regex]::new('//[^\n]*|/\*.*?\*/|\$?@\$?"(?:[^"]|"")*"|\$?"(?:[^"\\\n]|\\.)*"|''(?:[^''\\\n]|\\.)+''', 'Singleline')

# A member's attributes, then the rest of its declaration, up to its block.
$memberRegex = [regex]::new('^(?<attributes>\s*(?:\[(?:[^\[\]]|\[[^\[\]]*\])*\]\s*)*)(?<declaration>.*)$', 'Singleline')
$propertyRegex = [regex]::new('^(?<modifiers>(?:(?:public|private|protected|internal|static|virtual|override|abstract|sealed|new|required|readonly|unsafe|extern)\s)*)(?<type>[^\s(=:][^(=]*?)\s(?<name>@?[A-Za-z_]\w*)$')
$publicSetterRegex = [regex]::new('(?<!\w)(?<!\b(?:private|protected|internal)\s+)(?:set|init)\b')
$anySetterRegex = [regex]::new('(?<!\w)(?:set|init)\b')

function Note([string] $Message, [ConsoleColor] $Color = [ConsoleColor]::Cyan) {
	Write-Host "[check-always-strings] $Message" -ForegroundColor $Color
}

# A path for a message: relative to the repo, with forward slashes, when it's inside it.
function Get-DisplayPath([string] $FullPath) {

	$relative = [System.IO.Path]::GetRelativePath($repoRoot, $FullPath)
	if ($relative -eq '..' -or $relative.StartsWith('..' + [System.IO.Path]::DirectorySeparatorChar) -or [System.IO.Path]::IsPathRooted($relative)) {
		return $FullPath
	}
	return $relative.Replace('\', '/')
}

# A source file: its text and namespace, and, once something asks, its using directives and its code.
function New-SourceFile([string] $Path, [string] $Text) {

	return [pscustomobject]@{
		Path      = $Path
		Text      = $Text
		Namespace = $namespaceRegex.Match($Text).Groups['name'].Value
		Usings    = $null
		Code      = $null
	}
}

function Get-Usings($File) {

	if ($null -eq $File.Usings) {
		$File.Usings = [string[]]@(foreach ($match in $usingRegex.Matches($File.Text)) { $match.Groups['name'].Value })
	}
	return $File.Usings
}

# The file's text with every comment and literal blanked to spaces, line breaks kept, so that each
# brace and semicolon in it is code, and each offset in it is the same offset in the text.
function Get-Code($File) {

	if ($null -eq $File.Code) {
		$File.Code = $literalRegex.Replace($File.Text, [System.Text.RegularExpressions.MatchEvaluator] { param($match) $match.Value -replace '[^\r\n]', ' ' })
	}
	return $File.Code
}

# The offset of the bracket that closes the one at $Open, counting nested pairs, or -1.
function Find-Close([string] $Code, [int] $Open) {

	$opener = $Code[$Open]
	$closer = switch ($opener) { '{' { '}' } '(' { ')' } '[' { ']' } '<' { '>' } }
	$pair = [char[]]@($opener, $closer)
	$depth = 0
	$at = $Open
	while ($at -ge 0) {
		if ($Code[$at] -eq $opener) {
			$depth++
		}
		else {
			$depth--
			if ($depth -eq 0) {
				return $at
			}
		}
		$at = $Code.IndexOfAny($pair, $at + 1)
	}
	return -1
}

# The types in a comma-separated list, split only where no angle bracket is open.
function Split-TypeList([string] $Text) {

	$parts = [System.Collections.Generic.List[string]]::new()
	$depth = 0
	$start = 0
	for ($i = 0; $i -lt $Text.Length; $i++) {
		switch ($Text[$i]) {
			'<' { $depth++ }
			'>' { $depth-- }
			',' {
				if ($depth -eq 0) {
					$parts.Add($Text.Substring($start, $i - $start).Trim())
					$start = $i + 1
				}
			}
		}
	}
	$parts.Add($Text.Substring($start).Trim())
	return , $parts.ToArray()
}

# The declaration that a type's name means in a file: one in the file's namespace or a namespace
# around it, or one that a using directive brings in, or, for a qualified name, one in that
# namespace. $null for a name that isn't Sonarr's, such as System.Version.
function Resolve-Type([string] $Name, $File) {

	$qualifier = ''
	$dot = $Name.LastIndexOf('.')
	if ($dot -ge 0) {
		$qualifier = $Name.Substring(0, $dot)
		$Name = $Name.Substring($dot + 1)
	}
	if (-not $index.ContainsKey($Name)) {
		return $null
	}

	$usings = Get-Usings $File
	$visible = @(foreach ($candidate in $index[$Name]) {

		$namespace = $candidate.File.Namespace
		$isVisible = if ($qualifier) {
			$namespace -ceq $qualifier -or $namespace.EndsWith(".$qualifier", [System.StringComparison]::Ordinal)
		}
		else {
			[object]::ReferenceEquals($candidate.File, $File) -or $namespace -ceq $File.Namespace -or
				$File.Namespace.StartsWith("$namespace.", [System.StringComparison]::Ordinal) -or $usings -ccontains $namespace
		}
		if ($isVisible) {
			$candidate
		}
	})

	if ($visible.Count -eq 1) {
		return $visible[0]
	}
	if ($visible.Count -gt 1 -and $ambiguous.Add("$Name|$($File.Path)")) {

		Note "can't tell which $Name $(Get-DisplayPath $File.Path) means: one in each of $(($visible | ForEach-Object { $_.File.Namespace }) -join ', ')" Red
		$script:unreadable++
	}
	return $null
}

# A class's generic parameters, its base class, and the properties that it declares, which
# System.Text.Json fills from a request's body. $null when its declaration can't be read.
function Get-ClassInfo($Entry) {

	$key = "$($Entry.File.Path)|$($Entry.Name)"
	if ($classes.ContainsKey($key)) {
		return $classes[$key]
	}

	$file = $Entry.File
	$code = Get-Code $file
	$header = [regex]::Match($code, '(?<![\w.])(?:class|struct|record)\s+(?:(?:class|struct)\s+)?' + [regex]::Escape($Entry.Name) + '\b\s*(?<generics><[^{]*?>)?\s*(?::(?<bases>[^{]*?))?\s*(?:where\b[^{]*)?\{')
	$open = $header.Success ? $header.Index + $header.Length - 1 : -1
	$close = ($open -ge 0) ? (Find-Close $code $open) : -1
	if ($close -lt 0) {

		Note "can't read the declaration of $($Entry.Name) in $(Get-DisplayPath $file.Path)" Red
		$script:unreadable++
		$classes[$key] = $null
		return $null
	}

	$info = [pscustomobject]@{
		Key        = $key
		Name       = $Entry.Name
		Entry      = $Entry
		Generics   = [string[]]@()
		Base       = $null
		Properties = [System.Collections.Generic.List[object]]::new()
	}
	$classes[$key] = $info

	if ($header.Groups['generics'].Success) {
		$info.Generics = [string[]]@(foreach ($parameter in (Split-TypeList $header.Groups['generics'].Value.Trim().Trim('<', '>'))) { ($parameter -split '\s+')[-1] })
	}

	# The base class is the first type in the base list that's a class of Sonarr's. The rest are
	# interfaces.
	if ($header.Groups['bases'].Success) {
		foreach ($baseType in (Split-TypeList $header.Groups['bases'].Value)) {

			$baseEntry = Resolve-Type ($baseType -replace '<.*$', '').Trim() $file
			if ($null -ne $baseEntry -and $baseEntry.Kind -eq 'class') {
				$info.Base = $baseEntry
				break
			}
		}
	}

	# Each member at the class's own level ends at a semicolon or at the block that follows its
	# declaration, which is a property's accessors, a method's body, or a nested type's body.
	$stops = [char[]]@('{', '}', ';')
	$start = $open + 1
	$at = $code.IndexOfAny($stops, $start)
	while ($at -ge 0 -and $at -lt $close) {

		if ($code[$at] -eq '{') {

			$end = Find-Close $code $at
			if ($end -lt 0) {

				Note "can't find where a member of $($Entry.Name) ends in $(Get-DisplayPath $file.Path)" Red
				$script:unreadable++
				break
			}
			$property = Read-Property $info $file $code $start $at $end
			if ($null -ne $property) {
				$info.Properties.Add($property)
			}
			$start = $end + 1
		}
		else {
			$start = $at + 1
		}
		$at = $code.IndexOfAny($stops, $start)
	}
	return $info
}

# The property that a member declares, when it's one that System.Text.Json fills from a request's
# body: public, not static, with a public set or init accessor, or any with [JsonInclude], and not
# ignored. $null for any other member.
function Read-Property($Info, $File, [string] $Code, [int] $Start, [int] $Open, [int] $Close) {

	# A preprocessor line, such as #region or #pragma, can sit between members.
	$parts = $memberRegex.Match($Code.Substring($Start, $Open - $Start))
	$match = $propertyRegex.Match((($parts.Groups['declaration'].Value -replace '(?m)^[ \t]*#.*$', '') -replace '\s+', ' ').Trim())
	if (-not $match.Success) {
		return $null
	}
	$modifiers = $match.Groups['modifiers'].Value
	$type = $match.Groups['type'].Value.Trim()
	if ($modifiers -notmatch '\bpublic\b' -or $modifiers -match '\bstatic\b' -or $type -match '^(?:class|struct|record|interface|enum|event|delegate)\b') {
		return $null
	}

	$attributes = $File.Text.Substring($Start + $parts.Groups['attributes'].Index, $parts.Groups['attributes'].Length)
	$accessors = $Code.Substring($Open + 1, $Close - $Open - 1)
	$included = $attributes -match '(?<![\w.])JsonInclude(?:Attribute)?\b'
	if (-not ($publicSetterRegex.IsMatch($accessors) -or ($included -and $anySetterRegex.IsMatch($accessors)))) {
		return $null
	}
	if (Test-JsonIgnored $attributes $File) {
		return $null
	}

	$name = $match.Groups['name'].Value.TrimStart('@')
	$jsonName = [regex]::Match($attributes, 'JsonPropertyName(?:Attribute)?\s*\(\s*"(?<name>[^"]+)"\s*\)').Groups['name'].Value
	return [pscustomobject]@{
		Name     = $name
		JsonName = $jsonName ? $jsonName : $name
		Type     = $type
		Class    = $Info
	}
}

# Whether System.Text.Json's JsonIgnore keeps the property from being read: it does with no
# condition or with JsonIgnoreCondition.Always. Newtonsoft's JsonIgnore, which some of Sonarr's
# files use, doesn't.
function Test-JsonIgnored([string] $Attributes, $File) {

	$match = [regex]::Match($Attributes, '(?<![\w.])(?<qualified>System\.Text\.Json\.Serialization\.)?JsonIgnore(?:Attribute)?\b(?:\s*\((?<arguments>[^)]*)\))?')
	if (-not $match.Success) {
		return $false
	}
	if (-not $match.Groups['qualified'].Success -and (Get-Usings $File) -cnotcontains 'System.Text.Json.Serialization') {
		return $false
	}
	$arguments = $match.Groups['arguments'].Value
	return [string]::IsNullOrWhiteSpace($arguments) -or $arguments -match '\bJsonIgnoreCondition\.Always\b'
}

# The properties that an object of the class has, its base classes' included, with a property that
# a derived class declares again counted once.
function Get-AllProperties($Info) {

	$seen = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
	$current = $Info
	while ($null -ne $current) {

		foreach ($property in $current.Properties) {
			if ($seen.Add($property.Name)) {
				$property
			}
		}
		$current = ($null -ne $current.Base) ? (Get-ClassInfo $current.Base) : $null
	}
}

# What a property's type holds: a String, Strings in an array, an Object, a Dictionary of strings or
# objects, a Class of Sonarr's to look into, or None of these.
function Get-Shape([string] $Type, $Info) {

	$text = ($Type -replace '\s+', '') -replace '^global::', ''
	if ($text.EndsWith('?')) {
		$text = $text.Substring(0, $text.Length - 1)
	}
	if ($text.EndsWith('[]')) {
		return Get-ElementShape (Get-Shape $text.Substring(0, $text.Length - 2) $Info)
	}

	$angle = $text.IndexOf('<')
	if ($angle -gt 0 -and $text.EndsWith('>')) {

		$outer = $text.Substring(0, $angle)
		$arguments = Split-TypeList $text.Substring($angle + 1, $text.Length - $angle - 2)
		$generic = $outer.Split('.')[-1]
		if ($generic -ceq 'Nullable') {
			return Get-Shape $arguments[0] $Info
		}
		if ($collectionTypes -ccontains $generic) {
			return Get-ElementShape (Get-Shape $arguments[0] $Info)
		}
		if ($dictionaryTypes -ccontains $generic) {

			$value = Get-Shape $arguments[-1] $Info
			return ($value.Kind -in 'String', 'Strings', 'Object') ? @{ Kind = 'Dictionary' } : $value
		}
		# A generic class of Sonarr's, whose properties are what matter.
		$text = $outer
	}

	$simple = $text.Split('.')[-1]
	if ($simple -cin 'string', 'String') {
		return @{ Kind = 'String' }
	}
	if ($simple -cin 'object', 'Object', 'dynamic', 'JsonElement', 'JsonNode') {
		return @{ Kind = 'Object' }
	}
	# A generic parameter, such as T in a provider's List<T> Presets, is the resource that's already
	# being looked into, and a value class goes to Sonarr as one string, such as a URL.
	if ($Info.Generics -ccontains $simple -or $valueClasses -ccontains $simple) {
		return @{ Kind = 'None' }
	}
	$entry = Resolve-Type $text $Info.Entry.File
	if ($null -ne $entry -and $entry.Kind -cin 'class', 'struct', 'record', 'record class', 'record struct') {
		return @{ Kind = 'Class'; Entry = $entry }
	}
	return @{ Kind = 'None' }
}

# The shape of a collection whose elements have the shape given.
function Get-ElementShape($Shape) {
	return ($Shape.Kind -eq 'String') ? @{ Kind = 'Strings' } : $Shape
}

# The name that the module reads a property as: its JSON name capitalized, then renamed by the
# model's own names at the top of the body, or else by the global names.
function Get-ModuleName([string] $JsonName, $ModelNames) {

	$name = $JsonName.Substring(0, 1).ToUpperInvariant() + $JsonName.Substring(1)
	if ($null -ne $ModelNames -and $ModelNames.ContainsKey($name)) {
		return $ModelNames[$name]
	}
	if ($globalNames.ContainsKey($name)) {
		return $globalNames[$name]
	}
	return $name
}

# What claims a property of the shape given under the name given, or why nothing does when a name
# looks like it should.
function Get-Claim($Shape, [string] $Name) {

	if ($Shape.Kind -eq 'Dictionary') {
		return @{ Reason = "a dictionary's keys are data, so no name claims its values" }
	}
	if ($spanConverters.ContainsKey($Name)) {
		return @{ By = 'the span converter list'; How = $spanConverters[$Name] }
	}
	if ($Shape.Kind -ne 'String' -and $convertProperties.ContainsKey($Name)) {

		$type = $convertProperties[$Name]
		if ($type -match '(?i)string') {
			return @{ By = 'EnumerateConverterProperties'; How = $type }
		}
		return @{ Reason = "EnumerateConverterProperties reads it as $type" }
	}
	return @{}
}

# Records a property that can hold a string under each name that the module reads it as, with what
# claims it, where it sits in the body, and the cmdlets that send the body.
function Add-Finding($Property, $Shape, [string] $Path, [int] $Depth, $Body) {

	$id = "$($Property.Class.Name).$($Property.Name)"
	$names = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
	if ($Depth -eq 0) {
		foreach ($model in $Body.Models) {
			$null = $names.Add((Get-ModuleName $Property.JsonName $modelNames[$model]))
		}
	}
	else {
		$null = $names.Add((Get-ModuleName $Property.JsonName $null))
	}

	foreach ($name in $names) {

		$key = "$id|$name"
		if (-not $findings.Contains($key)) {

			$claim = Get-Claim $Shape $name
			$findings[$key] = [pscustomobject]@{
				Id      = $id
				Name    = $name
				Type    = $Property.Type
				By      = $claim.By
				How     = $claim.How
				Reason  = $claim.Reason
				Places  = [System.Collections.Generic.List[string]]::new()
				Cmdlets = [System.Collections.Generic.SortedSet[string]]::new([System.StringComparer]::Ordinal)
			}
		}
		$finding = $findings[$key]
		if (-not $finding.Places.Contains($Path)) {
			$finding.Places.Add($Path)
		}
		foreach ($cmdlet in $Body.Cmdlets) {
			$null = $finding.Cmdlets.Add($cmdlet)
		}
	}
}

# Looks into a class's properties for those that can hold a string, and into each class that one of
# them holds. $OnPath keeps a class that holds itself, such as through a list of presets, from being
# looked into forever.
function Invoke-Walk($Info, [string] $Path, [int] $Depth, $Body, $OnPath) {

	if ($null -eq $Info -or -not $OnPath.Add($Info.Key)) {
		return
	}
	foreach ($property in @(Get-AllProperties $Info)) {

		if ($Depth -eq 0 -and $Body.Properties -and $Body.Properties -cnotcontains $property.Name) {
			continue
		}
		$shape = Get-Shape $property.Type $property.Class
		if ($shape.Kind -eq 'Class') {
			Invoke-Walk (Get-ClassInfo $shape.Entry) "$Path > $($property.Name)" ($Depth + 1) $Body $OnPath
		}
		elseif ($shape.Kind -ne 'None') {
			Add-Finding $property $shape $Path $Depth $Body
		}
	}
	$null = $OnPath.Remove($Info.Key)
}

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$sourceRoot = [System.IO.Path]::GetFullPath($SonarrSource, (Get-Location).ProviderPath)
$sourceDisplay = Get-DisplayPath $sourceRoot
$unreadable = 0
$attention = 0

if (-not (Test-Path -LiteralPath (Join-Path $sourceRoot 'src') -PathType Container)) {

	Note "no src folder in $sourceDisplay - pass the root of a clone of Sonarr's repository" Red
	exit 2
}

# One commit can carry several version tags, so each tag on HEAD counts, not only the one that git
# describe picks.
$pinned = $null
$compose = Join-Path $repoRoot 'sandbox/sonarr/compose.yaml'
if (Test-Path -LiteralPath $compose -PathType Leaf) {

	$version = [regex]::Match([System.IO.File]::ReadAllText($compose), '(?m)^\s*SONARR_VERSION:\s*"?(?<version>[0-9][0-9.]*)"?')
	if ($version.Success) {
		$pinned = $version.Groups['version'].Value
	}
}
$tags = @()
try {
	$tags = @(& git -C $sourceRoot tag --points-at HEAD 2>$null)
	if ($LASTEXITCODE -ne 0) {
		$tags = @()
	}
}
catch [System.Management.Automation.CommandNotFoundException] {
	$tags = @()
}
if ($null -eq $pinned) {
	Note "checking $sourceDisplay, with no SONARR_VERSION in sandbox/sonarr/compose.yaml to compare its tags with" Yellow
}
elseif ($tags -ccontains "v$pinned") {
	Note "checking $sourceDisplay at v$pinned, the version that the sandbox pins"
}
elseif ($tags.Count -gt 0) {
	Note "checking $sourceDisplay at $($tags -join ', '), while the sandbox pins $pinned" Yellow
}
else {
	Note "checking $sourceDisplay, which has no tag on HEAD, while the sandbox pins $pinned" Yellow
}

# What the module claims: the names on the span converter list, the names whose arrays it reads as
# a type of their own, and the names that it gives Sonarr's fields, all compared without case, as
# the converter's dictionaries are.
$optionsPath = Join-Path $repoRoot 'src/MG.Sonarr.Next/Json/SonarrJsonOptions.cs'
if (-not (Test-Path -LiteralPath $optionsPath -PathType Leaf)) {

	Note "no $(Get-DisplayPath $optionsPath)" Red
	exit 2
}
$options = New-SourceFile $optionsPath ([System.IO.File]::ReadAllText($optionsPath))
$optionsCode = Get-Code $options

# The text between the bracket that ends the first match of $Pattern in the options' code and the
# bracket that closes it, comments and literals included, or empty text.
function Get-Section([string] $Pattern) {

	$match = [regex]::Match($optionsCode, $Pattern)
	if (-not $match.Success) {
		return ''
	}
	$open = $match.Index + $match.Length - 1
	$close = Find-Close $optionsCode $open
	return ($close -lt 0) ? '' : $options.Text.Substring($open + 1, $close - $open - 1)
}

$spanConverters = [System.Collections.Generic.Dictionary[string, string]]::new([System.StringComparer]::OrdinalIgnoreCase)
foreach ($match in [regex]::Matches((Get-Section '\.AddSpanConverters\s*\('), 'new\(\s*"(?<name>\w+)"\s*,\s*(?<converter>\w+)\s*\)')) {
	$spanConverters[$match.Groups['name'].Value] = $match.Groups['converter'].Value
}
$convertProperties = [System.Collections.Generic.Dictionary[string, string]]::new([System.StringComparer]::OrdinalIgnoreCase)
foreach ($match in [regex]::Matches((Get-Section '\bEnumerateConverterProperties\s*\(\s*\)\s*\{'), 'new\(\s*"(?<name>\w+)"\s*,\s*typeof\((?<type>[^)]+)\)\s*\)')) {
	$convertProperties[$match.Groups['name'].Value] = $match.Groups['type'].Value
}
$globalNames = [System.Collections.Generic.Dictionary[string, string]]::new([System.StringComparer]::OrdinalIgnoreCase)
foreach ($match in [regex]::Matches((Get-Section '\bEnumerateGlobalReplaceNames\s*\(\s*\)\s*\{'), 'new\(\s*"(?<from>\w+)"\s*,\s*"(?<to>\w+)"\s*\)')) {
	$globalNames[$match.Groups['from'].Value] = $match.Groups['to'].Value
}
if ($spanConverters.Count -eq 0 -or $convertProperties.Count -eq 0 -or $globalNames.Count -eq 0) {

	Note "can't read the span converters, the converted properties, or the global names in $(Get-DisplayPath $optionsPath)" Red
	exit 2
}

# The names that a model gives Sonarr's fields: those that its own GetDeserializedNames returns from
# a JsonNameHolder built from one pair of names or an array of them, or else its base model's, whose
# public static GetDeserializedNames implements the interface for the derived model too.
function Get-ModelNames([string] $Model) {

	if ($modelNames.ContainsKey($Model)) {
		return $modelNames[$Model]
	}
	$file = $modelFiles[$Model]
	$code = Get-Code $file
	$names = [System.Collections.Generic.Dictionary[string, string]]::new([System.StringComparer]::OrdinalIgnoreCase)
	if ($code -match '\bGetDeserializedNames\s*\(') {

		foreach ($factory in [regex]::Matches($code, '\bJsonNameHolder\s*\.\s*From\w+\s*\(')) {

			$open = $factory.Index + $factory.Length - 1
			$close = Find-Close $code $open
			if ($close -gt $open) {
				foreach ($pair in [regex]::Matches($file.Text.Substring($open + 1, $close - $open - 1), '"(?<from>\w+)"\s*,\s*"(?<to>\w+)"')) {
					$names[$pair.Groups['from'].Value] = $pair.Groups['to'].Value
				}
			}
		}
		if ($names.Count -eq 0) {

			Note "can't read the names that $Model gives Sonarr's fields" Red
			$script:unreadable++
		}
	}
	else {

		$base = [regex]::Match($code, '\bclass\s+' + [regex]::Escape($Model) + '\b[^:{]*:\s*(?<base>\w+)').Groups['base'].Value
		if ($base -and $modelFiles.ContainsKey($base)) {
			$names = Get-ModelNames $base
		}
	}
	$modelNames[$Model] = $names
	return $names
}

$modelFiles = @{}
$modelsRoot = Join-Path $repoRoot 'src/MG.Sonarr.Next/Models'
foreach ($path in [System.IO.Directory]::EnumerateFiles($modelsRoot, '*.cs', [System.IO.SearchOption]::AllDirectories)) {

	$text = [System.IO.File]::ReadAllText($path)
	$declarations = $declarationRegex.Matches($text)
	if ($declarations.Count -gt 0) {

		$file = New-SourceFile $path $text
		foreach ($declaration in $declarations) {
			$modelFiles[$declaration.Groups['name'].Value] = $file
		}
	}
}
$modelNames = @{}
foreach ($model in @($bodies | ForEach-Object { $_.Models }) | Sort-Object -Unique) {

	if ($modelFiles.ContainsKey($model)) {
		$null = Get-ModelNames $model
	}
	else {

		Note "the table of bodies names $model, which src/MG.Sonarr.Next/Models doesn't have" Yellow
		$attention++
	}
}
Write-Verbose ('the module: {0} names on the span converter list, {1} arrays read as a type of their own, {2} global names, {3} names of models' -f $spanConverters.Count, $convertProperties.Count, $globalNames.Count, @($modelNames.Values | ForEach-Object { $_.Count } | Measure-Object -Sum).Sum)

# Every type that Sonarr's projects declare, by name, for looking up the type of a property.
$index = [System.Collections.Generic.Dictionary[string, System.Collections.Generic.List[object]]]::new([System.StringComparer]::Ordinal)
foreach ($project in $projects) {

	$projectRoot = Join-Path $sourceRoot 'src' $project
	if (-not (Test-Path -LiteralPath $projectRoot -PathType Container)) {

		Note "no $project project in $sourceDisplay/src" Red
		exit 2
	}
	foreach ($path in [System.IO.Directory]::EnumerateFiles($projectRoot, '*.cs', [System.IO.SearchOption]::AllDirectories)) {

		if ($path -match '[\\/](?:bin|obj)[\\/]') {
			continue
		}
		$text = [System.IO.File]::ReadAllText($path)
		$declarations = $declarationRegex.Matches($text)
		if ($declarations.Count -eq 0) {
			continue
		}
		$file = New-SourceFile $path $text
		foreach ($declaration in $declarations) {

			$name = $declaration.Groups['name'].Value
			if (-not $index.ContainsKey($name)) {
				$index[$name] = [System.Collections.Generic.List[object]]::new()
			}
			$kind = $declaration.Groups['kind'].Value -replace '\s+', ' '
			$index[$name].Add([pscustomobject]@{ Name = $name; Kind = $kind; File = $file })
		}
	}
}

$classes = @{}
$ambiguous = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
$findings = [ordered]@{}

foreach ($body in $bodies) {

	# A resource of Sonarr's API wins over a class of the same name elsewhere, such as the metadata
	# service's EpisodeResource.
	$roots = @(if ($index.ContainsKey($body.Resource)) { $index[$body.Resource] | Where-Object Kind -CEQ 'class' })
	$apiRoots = @($roots | Where-Object { $_.File.Namespace -ceq 'Sonarr.Api.V3' -or $_.File.Namespace.StartsWith('Sonarr.Api.V3.', [System.StringComparison]::Ordinal) })
	if ($roots.Count -gt 1 -and $apiRoots.Count -eq 1) {
		$roots = $apiRoots
	}
	if ($roots.Count -ne 1) {

		$where = ($roots.Count -eq 0) ? 'which the source has no class of' : "of which the source has $($roots.Count) classes"
		Note "the table of bodies names $($body.Resource), $where" Yellow
		$attention++
		continue
	}
	$info = Get-ClassInfo $roots[0]
	if ($null -eq $info) {
		continue
	}
	if ($body.Properties) {

		$declared = @(Get-AllProperties $info | ForEach-Object Name)
		foreach ($name in $body.Properties) {
			if ($declared -cnotcontains $name) {

				Note "the table of bodies sends $($body.Resource).$name, which the source doesn't have" Yellow
				$attention++
			}
		}
	}
	Invoke-Walk $info $body.Resource 0 $body ([System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal))
}

$claimed = 0
$heldCount = 0
$neither = [System.Collections.Generic.List[object]]::new()
$usedHeld = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
foreach ($finding in @($findings.Values | Sort-Object Id, Name)) {

	$renamed = ($finding.Name -ne ($finding.Id -replace '^.*\.', '')) ? " as $($finding.Name)" : ''
	$reason = $held[$finding.Id]
	if ($finding.By) {

		$claimed++
		Write-Verbose "claimed  $($finding.Id)$renamed, by $($finding.By) ($($finding.How))"
		if ($null -ne $reason -and $usedHeld.Add($finding.Id)) {

			Note "$($finding.Id) is held, but $($finding.By) claims it too - drop it from the table of held properties" Yellow
			$attention++
		}
	}
	elseif ($null -ne $reason) {

		$heldCount++
		$null = $usedHeld.Add($finding.Id)
		Write-Verbose "held     $($finding.Id): $reason"
	}
	else {
		$neither.Add($finding)
	}
}

foreach ($finding in $neither) {

	$renamed = ($finding.Name -ne ($finding.Id -replace '^.*\.', '')) ? ", read as $($finding.Name)" : ''
	$why = $finding.Reason ? " - $($finding.Reason)" : ''
	$more = ($finding.Places.Count -gt 1) ? " and $($finding.Places.Count - 1) more" : ''
	Write-Host "$($finding.Id) ($($finding.Type))$renamed$why, in $($finding.Places[0])$more, sent back by $($finding.Cmdlets -join ', ')" -ForegroundColor Yellow
}

foreach ($id in $held.Keys) {
	if (-not $usedHeld.Contains($id)) {

		Note "the table of held properties names $id, which no body has" Yellow
		$attention++
	}
}

$summary = '{0} properties that can hold a string, in {1} bodies - {2} claimed, {3} held' -f $findings.Count, $bodies.Count, $claimed, $heldCount
$status = @()
if ($neither.Count -gt 0) {
	$status += "$($neither.Count) neither"
}
if ($attention -gt 0) {
	$status += "$attention table mismatch(es)"
}
if ($unreadable -gt 0) {
	$status += "$unreadable unreadable"
}
$color = ($unreadable -gt 0) ? 'Red' : ((($neither.Count + $attention) -gt 0) ? 'Yellow' : 'Green')
Note ('{0}{1}' -f $summary, (($status.Count -gt 0) ? " - $($status -join ', ')" : ' - clean')) $color

if ($unreadable -gt 0) {
	exit 2
}
exit ((($neither.Count + $attention) -gt 0) ? 1 : 0)
