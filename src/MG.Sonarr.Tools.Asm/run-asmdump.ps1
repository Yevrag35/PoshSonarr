#Requires -Version 7.6

<#
.SYNOPSIS
	Dump RyuJIT FullOpts disassembly for a PoshSonarr method (or a whole class) to a text file.

.DESCRIPTION
	Builds the solution in Release, which includes the MG.Sonarr.Tools.Asm harness this script sits
	beside, then runs the harness with DOTNET_TieredCompilation=0 and DOTNET_JitDisasm set so the JIT
	prints disassembly for exactly the methods the harness force-compiles via
	RuntimeHelpers.PrepareMethod. Disassembly lands in the output file; harness diagnostics stay on
	stderr.

	The build targets the solution, not the harness project: the MG.Sonarr.Next.Shell project's
	Release post-build step, which packages the module, finds its script through $(SolutionDir), and
	only a solution build defines that property. So every build here also packages the module. The
	build shows its output only when it fails, and then only its errors.

	Layout: this script lives inside the MG.Sonarr.Tools.Asm project folder under src\, but its
	output does not - dumps and their prev\ baselines land in <repo>\.temp, which is gitignored, so a
	dump is never staged for commit.

	Current/previous rotation: a successful dump moves the existing output file to <outdir>\prev\<same
	name> before the new one lands, so consecutive runs of the same command can be compared. A failed
	or 0-byte run touches neither file, so the current dump stays the last good one and prev\ the one
	before it; a failed run's partial output, if any, is left in <dump name>.new beside the dump.
	Rotation is unconditional - it happens even under -DontOpen, which only suppresses the viewer.

	Viewer (skipped by -DontOpen): with a previous present the run opens WinMerge on prev-vs-current;
	with no previous it opens Notepad++ on the current dump. Both default to their usual locations
	under $env:PROGRAMFILES - set POSHSONARR_ASM_DIFF / POSHSONARR_ASM_VIEWER to point elsewhere on a
	given machine.

	The harness searches MG.Sonarr* assemblies only. Its ProjectReference to MG.Sonarr.Next.Shell
	transitively covers the module's projects, and -Assembly narrows the search to one of them.

	Newslot virtual methods on a class (every interface implementation - C# emits an implicit one as
	`virtual final newslot` - and every virtual member that is not an override):
	RuntimeHelpers.PrepareMethod can silently skip these on net10, so the harness compiles them by
	entering the body instead, in two tiers. "invoked" reflection-invokes it on an uninitialized
	instance - the resulting exception is expected and harmless, since compilation finishes before
	the body runs. "thunked" covers a byref-like return or parameter type (ReadOnlySpan<T>, Span<T>),
	which reflection refuses to box: an emitted thunk passes default arguments, calls the target and
	pops the result. Overrides and every virtual on a struct or ref struct compile through
	PrepareMethod like any other method. A newslot virtual neither tier can enter (one declared on an
	abstract class) falls back to PrepareMethod tagged "virtual?", and its listing is not guaranteed -
	that is the case where "prepared N" can exceed the dumped count.

	Async/iterator methods: the harness also prepares the compiler-generated state machine's
	MoveNext - that listing is where the real code lives.

	Inline trace (-Inlines): the harness also records the JIT's inlining decisions for the dumped
	methods - every call it inlined and every call it left as a call, with the JIT's reason - in
	<dump name>.inlines.txt beside the dump, rotated into prev\ along with the dump. The calls left as
	calls also print to the console. The release JIT has no inline dump of its own, so the decisions
	come from the runtime's JIT-tracing events through an in-process listener; waiting for that
	listener to start and drain adds a moment to the run.

	Examples below are written from the repo root; running it from anywhere else works the same,
	since every path the script uses is derived from its own location, not the caller's.

.EXAMPLE
	.\src\MG.Sonarr.Tools.Asm\run-asmdump.ps1 Wildcard IsMatch
.EXAMPLE
	.\src\MG.Sonarr.Tools.Asm\run-asmdump.ps1 WildcardSet
	# every declared method + constructor of the class
.EXAMPLE
	.\src\MG.Sonarr.Tools.Asm\run-asmdump.ps1 RentedBuffer AsSpan -TypeArgs char
.EXAMPLE
	.\src\MG.Sonarr.Tools.Asm\run-asmdump.ps1 PooledBuffer+Enumerator MoveNext -TypeArgs int
	# a nested type inside a generic one; -TypeArgs closes the outer type
.EXAMPLE
	.\src\MG.Sonarr.Tools.Asm\run-asmdump.ps1 SonarrCmdletBase -Assembly Next.Shell.Bases
.EXAMPLE
	.\src\MG.Sonarr.Tools.Asm\run-asmdump.ps1 SpanStringBuilder Append -Pattern '*SpanStringBuilder:Append*' -Summary
.EXAMPLE
	.\src\MG.Sonarr.Tools.Asm\run-asmdump.ps1 Wildcard IsMatch -Inlines
	# also writes asm_Wildcard_IsMatch.inlines.txt - why each call did or did not inline
.EXAMPLE
	.\src\MG.Sonarr.Tools.Asm\run-asmdump.ps1 Wildcard IsMatch   # first run: Notepad++
	# ...edit the source...
	.\src\MG.Sonarr.Tools.Asm\run-asmdump.ps1 Wildcard IsMatch   # second run: WinMerge, old vs new
#>
[CmdletBinding()]
param (
	# Type name: simple ("Wildcard"), namespace-qualified, or nested ("Outer+Inner"). Generic
	# types may omit the arity suffix ("RentedBuffer" finds RentedBuffer`1) but then need -TypeArgs.
	[Parameter(Mandatory = $true, Position = 0)]
	[string] $Class,

	# Omit (or pass *) for every declared method + constructor; ".ctor" for constructors only;
	# a property name resolves to its get_/set_ accessors.
	[Parameter(Mandatory = $false, Position = 1)]
	[SupportsWildcards()]
	[string] $Method,

	# Assembly to search, named in full (MG.Sonarr.Next.Shell) or without its MG.Sonarr. prefix
	# (Next.Shell). Narrows the type search when a type name exists in more than one assembly.
	[Parameter(Mandatory = $false)]
	[string] $Assembly,

	# Generic type arguments closing the declaring type (int, string, BCL full names, or any
	# MG.Sonarr type name; "int?" for a nullable).
	[Parameter(Mandatory = $false)]
	[string[]] $TypeArgs,

	# Generic type arguments closing a generic method's own type parameters - same resolution rules
	# as -TypeArgs.
	[Parameter(Mandatory = $false)]
	[Alias("MethodTypeArgs")]
	[string[]] $MethodArgs,

	# Output file; relative paths resolve under <repo>\.temp (NOT next to this script), and the
	# directory is created if missing. Default: asm_<Class>[_<Method>].txt, where a nested type's
	# <Class> keeps its outer type's name (asm_PooledBuffer_Enumerator_MoveNext.txt).
	[Parameter(Mandatory = $false)]
	[string] $OutFile,

	# Raw DOTNET_JitDisasm override. Default '*<Class>*:*' - wide on purpose: with tiering off,
	# only methods the harness prepares get compiled, so the class-wide pattern IS the prepared set,
	# and it still catches async/iterator state machines and other compiler-generated nested types
	# an exact '*Class:Method' pattern would miss. <Class> drops the namespace and arity suffixes, and
	# a nested type keeps its outer type's name: '*PooledBuffer*+Enumerator*:*'.
	[Parameter(Mandatory = $false)]
	[string] $Pattern,

	# Also set DOTNET_JitDisasmSummary=1: one line per JIT-compiled method lands in the dump -
	# handy for diagnosing a pattern that matches nothing.
	[Parameter(Mandatory = $false)]
	[switch] $Summary,

	# Also record the JIT's inlining decisions for the dumped methods in <dump name>.inlines.txt beside
	# the dump (rotated into prev\ like it), and print each call it did not inline with the JIT's reason.
	[Parameter(Mandatory = $false)]
	[switch] $Inlines,

	# Skip the build (the dump reflects the last Release build, not current sources).
	[Parameter(Mandatory = $false)]
	[switch] $NoBuild,

	# Suppress the post-run viewer (WinMerge or Notepad++). Rotation into prev\ still happens.
	[Parameter(Mandatory = $false)]
	[switch] $DontOpen
)

$ErrorActionPreference = 'Stop'

function Note([string] $Message, [ConsoleColor] $Color = [ConsoleColor]::Cyan) {
	Write-Host "[run-asmdump] $Message" -ForegroundColor $Color
}

# Moves a non-empty current file into prev\ under the same name, creating prev\ if needed. Returns
# whether it moved one.
function Move-ToPrevious([string] $Current, [string] $Previous) {

	if (-not (Test-Path -LiteralPath $Current) -or (Get-Item -LiteralPath $Current).Length -eq 0) {
		return $false
	}
	$previousDir = Split-Path $Previous -Parent
	if (-not (Test-Path -LiteralPath $previousDir)) {

		New-Item -ItemType Directory -Path $previousDir | Out-Null
	}
	Move-Item -LiteralPath $Current -Destination $Previous -Force
	return $true
}

# Project/assembly name of the harness this script drives: the .csproj beside it and the exe it
# builds are both named after it, so a project rename only needs this one line changed.
$harnessName = 'MG.Sonarr.Tools.Asm'
$harnessProj = Join-Path $PSScriptRoot "$harnessName.csproj"
$exe = Join-Path $PSScriptRoot "bin\Release\net10.0\$harnessName.exe"

# The script sits in a project folder under src\, two levels below the repo root. Everything it
# reaches outside that folder - the solution it builds and the .temp output dir - hangs off $repoRoot,
# so assert the layout rather than let a future move silently build and dump against the wrong place.
$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$tempDir = Join-Path $repoRoot '.temp'

# Viewer tools default to their usual install locations. This script is committed and shared across
# machines, so both are overridable without editing it.
$winMerge = $env:POSHSONARR_ASM_DIFF ? $env:POSHSONARR_ASM_DIFF : (Join-Path $env:PROGRAMFILES 'WinMerge\WinMergeU.exe')
$notepadPlusPlus = $env:POSHSONARR_ASM_VIEWER ? $env:POSHSONARR_ASM_VIEWER : (Join-Path $env:PROGRAMFILES 'Notepad++\notepad++.exe')

$slnxPath = Join-Path $repoRoot 'PoshSonarr.slnx'
if (-not (Test-Path -LiteralPath $slnxPath)) {

	Note "expected PoshSonarr.slnx in $repoRoot - this script must sit in a project folder under src\ at the repo root" Red
	exit 1
}
if (-not (Test-Path -LiteralPath $harnessProj)) {

	Note "harness project not found: $harnessProj - this script must sit inside the $harnessName project folder" Red
	exit 1
}

if (-not $NoBuild) {

	# The build's warnings belong to the code under src\, not to the dump, and would bury this run's own
	# output, so the build keeps only its errors, and shows them only when it fails.
	Note 'building PoshSonarr.slnx (Release)...'
	$buildLog = dotnet build $slnxPath -c Release --nologo -v q -tl:off '-clp:ErrorsOnly'
	if ($LASTEXITCODE -ne 0) {

		$buildLog | ForEach-Object { Write-Host $_ }
		Note "build failed ($LASTEXITCODE)" Red
		exit $LASTEXITCODE
	}
}
if (-not (Test-Path -LiteralPath $exe)) {

	Note "harness exe missing: $exe (run without -NoBuild)" Red
	exit 1
}

# -Class without its namespace or arity suffixes: "Wildcard", or "PooledBuffer+Enumerator" for a nested
# type, which keeps its outer type's name so that two types' nested Enumerators get patterns and dump
# names of their own.
$typePath = ($Class -split '\.')[-1] -replace '`\d+', ''
if (-not $Pattern) {

	$Pattern = "*$($typePath -replace '\+', '*+')*:*"
}

if (-not $OutFile) {

	$safe = $typePath -replace '[^\w]+', '_'
	if ($Method -and $Method -ne '*') {
		$OutFile = "asm_${safe}_$($Method -replace '[^\w]+', '_').txt"
	}
	else {
		$OutFile = "asm_$safe.txt"
	}
}
if (-not [IO.Path]::IsPathRooted($OutFile)) {

	$OutFile = Join-Path $tempDir $OutFile
}

# .temp is gitignored, so a fresh clone does not have it - and the redirect below cannot create the
# directory itself. Covers a relative -OutFile naming a subfolder too.
$outDir = Split-Path $OutFile -Parent
if (-not (Test-Path -LiteralPath $outDir)) {

	New-Item -ItemType Directory -Path $outDir | Out-Null
}

# The prior run of this same OutFile is kept under prev\ with the same file name, so WinMerge shows a
# matching name on both sides. The run writes to a staging file first: nothing is rotated or promoted
# until the exit code and byte count say the dump is real.
$prevDir = Join-Path $outDir 'prev'
$prevFile = Join-Path $prevDir (Split-Path $OutFile -Leaf)
$staging = "$OutFile.new"

# -Inlines: the harness writes its trace to a staging file of its own beside the dump's, which moves
# with the dump below.
$inlinesFile = Join-Path $outDir ([IO.Path]::GetFileNameWithoutExtension($OutFile) + '.inlines.txt')
$inlinesPrev = Join-Path $prevDir (Split-Path $inlinesFile -Leaf)
$inlinesStaging = "$inlinesFile.new"

$exeArgs = @($Class)
if ($Method) { $exeArgs += $Method }
if ($Assembly) { $exeArgs += '--assembly', $Assembly }
if ($TypeArgs) { $exeArgs += '--targs', ($TypeArgs -join ',') }
if ($MethodArgs) { $exeArgs += '--margs', ($MethodArgs -join ',') }
if ($Inlines) { $exeArgs += '--inlines', $inlinesStaging }

Note "pattern : $Pattern"
Note "output  : $OutFile"
if ($Inlines) {

	Note "inlines : $inlinesFile"
	# A staging file left by an interrupted run must not pass for this run's trace.
	Remove-Item -LiteralPath $inlinesStaging -ErrorAction Ignore
}

# Scope the JIT env vars to this run; restore whatever the session had.
$envNames = 'DOTNET_TieredCompilation', 'DOTNET_JitDisasm', 'DOTNET_JitDisasmSummary'
$saved = @{}
foreach ($n in $envNames) {
	$saved[$n] = [Environment]::GetEnvironmentVariable($n)
}
try {
	$env:DOTNET_TieredCompilation = '0'
	$env:DOTNET_JitDisasm = $Pattern
	$env:DOTNET_JitDisasmSummary = if ($Summary) { '1' } else { $null }
	& $exe @exeArgs > $staging
	$rc = $LASTEXITCODE
}
finally {

	foreach ($n in $envNames) {
		[Environment]::SetEnvironmentVariable($n, $saved[$n])
	}
}

$size = (Test-Path -LiteralPath $staging) ? (Get-Item -LiteralPath $staging).Length : 0
$inlinesSize = ($Inlines -and (Test-Path -LiteralPath $inlinesStaging)) ? (Get-Item -LiteralPath $inlinesStaging).Length : 0

# Only a successful, non-empty run replaces the current dump, rotating a non-empty current into prev\
# first - so a failed run neither pushes a useless file into prev\ nor overwrites the last good dump.
# Independent of -DontOpen: the viewer is a separate decision below.
$promoted = $rc -eq 0 -and $size -gt 0
$rotated = $false
$inlinesRotated = $false
if ($promoted) {

	$rotated = Move-ToPrevious $OutFile $prevFile
	Move-Item -LiteralPath $staging -Destination $OutFile -Force

	# The inline trace moves with its dump, so prev\ keeps each trace beside the dump it came from.
	if ($Inlines) {

		$inlinesRotated = Move-ToPrevious $inlinesFile $inlinesPrev
		if ($inlinesSize -gt 0) {

			Move-Item -LiteralPath $inlinesStaging -Destination $inlinesFile -Force
		}
	}
}
else {

	# A failed run's partial output stays in the staging file for a look; an empty one is clutter, and
	# so is a trace whose dump never landed.
	if ($size -eq 0) {
		Remove-Item -LiteralPath $staging -ErrorAction Ignore
	}
	if ($Inlines) {
		Remove-Item -LiteralPath $inlinesStaging -ErrorAction Ignore
	}
}

if ($rc -ne 0) {

	Note "harness exited $rc - see its stderr above; the current dump was left as it was" Red
	if ($size -gt 0) {
		Note "partial output: $staging" Yellow
	}
}
elseif ($size -eq 0) {

	Note '0-byte dump: DOTNET_JitDisasm matched nothing the harness compiled. Loosen -Pattern, or re-run with -Summary to list everything that JIT-compiled.' Yellow
}
else {

	$listings = Select-String -LiteralPath $OutFile -Pattern '^; Assembly listing for method (.+?)( \(.*\))?$'
	$sizes = Select-String -LiteralPath $OutFile -Pattern '^; Total bytes of code (\d+)'
	Note ('dumped {0} method(s), {1:N0} bytes:' -f $listings.Count, $size) Green
	for ($i = 0; $i -lt $listings.Count; $i++) {
		$bytes = ($i -lt $sizes.Count) ? [int]$sizes[$i].Matches[0].Groups[1].Value : $null
		Write-Host ('    {0,6} bytes  {1}' -f $bytes, $listings[$i].Matches[0].Groups[1].Value)
	}

	$prevStamp = (Test-Path -LiteralPath $prevFile) ? (Get-Item -LiteralPath $prevFile).LastWriteTime : $null
	if ($null -eq $prevStamp) {

		Note 'previous: none - this dump becomes the baseline the next run diffs against'
	}
	else {

		Note ('previous: {0} ({1:yyyy-MM-dd HH:mm:ss}{2})' -f $prevFile, $prevStamp, ($rotated ? ', rotated this run' : ''))
	}

	if ($Inlines) {

		$inlinesPrevStamp = (Test-Path -LiteralPath $inlinesPrev) ? (Get-Item -LiteralPath $inlinesPrev).LastWriteTime : $null
		if ($inlinesSize -eq 0) {

			Note 'inlines : no trace written this run - see the harness warning above' Yellow
		}
		elseif ($null -eq $inlinesPrevStamp) {

			Note 'inlines : previous none - this trace becomes the baseline the next run diffs against'
		}
		else {

			Note ('inlines : previous {0} ({1:yyyy-MM-dd HH:mm:ss}{2})' -f $inlinesPrev, $inlinesPrevStamp, ($inlinesRotated ? ', rotated this run' : ''))
		}
	}

	if (-not $DontOpen) {

		if ($null -ne $prevStamp -and (Test-Path -LiteralPath $winMerge)) {

			# Left is history and stays read-only; /s reuses one window across repeated dumps instead of
			# stacking them up; /fileext asm mirrors the -lasm lexer Notepad++ gets.
			$mergeArgs = @(
				'/e', '/s', '/u', '/wl', '/ignorecomments', '/ignorews', '/ignoreblanklines', '/ignorecase', '/ignoreeol', '/fileext', 'cpp',
				'/dl', ('previous ({0:yyyy-MM-dd HH:mm:ss})' -f $prevStamp),
				'/dr', ('current ({0:yyyy-MM-dd HH:mm:ss})' -f (Get-Item -LiteralPath $OutFile).LastWriteTime),
				$prevFile, $OutFile
			)
			& $winMerge @mergeArgs
		}
		elseif (Test-Path -LiteralPath $notepadPlusPlus) {

			if ($null -ne $prevStamp) { Note "WinMerge not found at $winMerge - opening current only" Yellow }
			& $notepadPlusPlus $OutFile -lasm
		}
		else {

			Note "no viewer found (WinMerge, Notepad++) - dump left at $OutFile" Yellow
		}
	}
}

exit $rc
