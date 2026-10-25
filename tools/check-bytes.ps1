#Requires -Version 7.6

<#
.SYNOPSIS
	Check files for line-ending and encoding problems at the byte level, and optionally fix the two
	that have a single right answer.

.DESCRIPTION
	The repo keeps every text file as UTF-8 without a BOM, with LF line endings (.gitattributes,
	.editorconfig). This script reads each file's exact bytes - no decoding, no newline translation -
	and reports what breaks that:

		UTF-8 BOM                                  -Fix strips it
		CRLF line endings                          -Fix turns them into LF
		stray CR (one not followed by LF)          reported only
		CR CR LF (a stray CR before a CRLF)        reported only, CRLF included
		UTF-16 BOM                                 reported only
		bytes that are not valid UTF-8             reported only
		U+FFFD replacement character               reported only
		final newline added or removed vs -Base    reported only

	-Fix rewrites a file only for the first two, and leaves every other byte as it was. The rest have
	no single right fix: a stray CR may be a line break or junk, the real encoding behind invalid
	UTF-8 is unknown, the character a U+FFFD stands in for is already gone, and .editorconfig sets
	no insert_final_newline rule to restore. A CRLF right after a stray CR stays as well: without its
	CR, the stray CR would sit in front of the LF, and the next -Fix would delete it as the CR of a
	CRLF, a decision nobody made.

	U+FFFD is valid UTF-8, so the UTF-8 check passes it, but a literal one in a text file almost
	always marks a character that an earlier decode could not map. A file that needs the character
	itself should spell it as an escape, such as [char]0xFFFD in PowerShell.

	Git covers less of this than it seems to. text=auto eol=lf turns CRLF into LF when a file is
	staged, unless the copy already in the index has CRLF: text=auto keeps that file's line endings
	as they are until git add --renormalize. Git never strips a BOM either, and under text=auto one
	stray CR makes Git treat the whole file as binary and skip normalizing it, CRLFs included.
	Trailing whitespace is out of scope: git diff --check already flags it on changed lines, and two
	trailing spaces are a deliberate line break in Markdown.

	Which files get checked: with no -Path, every file changed since -Base (staged or not) plus
	untracked files that are not ignored. -Base counts from the newest commit HEAD shares with it
	(git merge-base), so when it names a branch that has moved on since HEAD left it, that branch's
	later commits don't count as changes here. A file whose only change is CRLF is not in that list -
	git diff normalizes it before comparing and sees nothing - so pass it, or a directory holding
	it, to check it anyway. A directory inside the repo expands to its tracked and untracked files,
	skipping ignored ones; an ignored directory, or one outside the repo, expands to every file
	under it. A file with a NUL byte in its first 8000 bytes is treated as binary and skipped (Git's
	own test), unless it starts with a UTF-16 BOM, which is checked first.

	Exit codes: 0 when nothing needs attention, including when -Fix fixed everything it found;
	1 when something still does; 2 when a path was missing or unreadable, -Base did not resolve or
	shares no history with HEAD, or no -Path was given outside a git repository.

.EXAMPLE
	.\tools\check-bytes.ps1
	# every file changed since HEAD, plus untracked files
.EXAMPLE
	.\tools\check-bytes.ps1 CLAUDE.md README.md
.EXAMPLE
	.\tools\check-bytes.ps1 . -Fix
	# the whole repo except ignored files, fixing what -Fix can
.EXAMPLE
	.\tools\check-bytes.ps1 -Base dev
	# everything this branch changed, compared with dev
.EXAMPLE
	pwsh -NoProfile -File tools/check-bytes.ps1 CLAUDE.md
	# from Bash
#>
[CmdletBinding()]
param (
	# Files or directories to check; relative paths resolve against the current directory. Omit to
	# check every file changed since -Base plus untracked files.
	[Parameter(Mandatory = $false, Position = 0, ValueFromRemainingArguments = $true)]
	[string[]] $Path,

	# Revision to compare against: the default file list starts there, and the final-newline check
	# compares each file with its copy there. Anything git rev-parse accepts. When HEAD doesn't
	# descend from it, the newest commit HEAD shares with it stands in (git merge-base).
	[Parameter(Mandatory = $false)]
	[string] $Base = 'HEAD',

	# Strip a UTF-8 BOM and turn CRLF into LF. Everything else is still only reported.
	[Parameter(Mandatory = $false)]
	[switch] $Fix
)

$ErrorActionPreference = 'Stop'

function Note([string] $Message, [ConsoleColor] $Color = [ConsoleColor]::Cyan) {
	Write-Host "[check-bytes] $Message" -ForegroundColor $Color
}

# Runs git and returns its exit code, raw stdout bytes and stderr text. This goes through Process
# rather than a plain native call because PowerShell decodes native output with the console code
# page and splits it into lines - which would mangle non-ASCII paths and lose the final newline the
# blob comparison needs to see.
function Invoke-Git([string] $WorkingDirectory, [string[]] $Arguments) {

	$info = [System.Diagnostics.ProcessStartInfo]::new('git')
	$info.ArgumentList.Add('-C')
	$info.ArgumentList.Add($WorkingDirectory)
	foreach ($argument in $Arguments) {
		$info.ArgumentList.Add($argument)
	}
	$info.RedirectStandardOutput = $true
	$info.RedirectStandardError = $true
	$info.UseShellExecute = $false

	$process = [System.Diagnostics.Process]::Start($info)
	try {
		# stderr drains on its own task, so a chatty git cannot fill that pipe and stall stdout.
		$stderr = $process.StandardError.ReadToEndAsync()
		$stdout = [System.IO.MemoryStream]::new()
		$process.StandardOutput.BaseStream.CopyTo($stdout)
		$process.WaitForExit()
		return [pscustomobject]@{
			ExitCode = $process.ExitCode
			Output   = $stdout.ToArray()
			Error    = $stderr.GetAwaiter().GetResult().Trim()
		}
	}
	finally {
		$process.Dispose()
	}
}

# Paths from a git command run with -z: NUL-separated, never quoted, UTF-8, relative to the repo root.
function Get-GitPathList([string[]] $Arguments) {

	$result = Invoke-Git $repoRoot $Arguments
	if ($result.ExitCode -ne 0) {

		Note "git $($Arguments -join ' ') failed: $($result.Error)" Red
		exit 2
	}
	return [System.Text.Encoding]::UTF8.GetString($result.Output).Split([char]0, [System.StringSplitOptions]::RemoveEmptyEntries)
}

function Test-InRepo([string] $FullPath) {

	if ($null -eq $repoRoot) {
		return $false
	}
	return ($FullPath.Equals($repoRoot, $pathComparison) -or
		$FullPath.StartsWith($repoRoot.TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar, $pathComparison))
}

# Repo-relative with forward slashes, the way git prints paths, so output pastes straight into git.
function Get-RepoRelativePath([string] $FullPath) {
	return [System.IO.Path]::GetRelativePath($repoRoot, $FullPath).Replace('\', '/')
}

# 1-based line number of the byte at $Offset: one more than the LFs before it.
function Get-LineNumber([byte[]] $Bytes, [int] $Offset) {

	$line = 1
	$lf = [Array]::IndexOf($Bytes, [byte]10, 0, $Offset)
	while ($lf -ge 0) {
		$line++
		$lf = [Array]::IndexOf($Bytes, [byte]10, $lf + 1, $Offset - $lf - 1)
	}
	return $line
}

# Where a finding sits, for its message: the distinct lines of the first five offsets, with ', ...'
# when there are more.
function Format-LineList([byte[]] $Bytes, [System.Collections.Generic.List[int]] $Offsets) {

	$lines = @($Offsets | Select-Object -First 5 | ForEach-Object { Get-LineNumber $Bytes $_ } | Select-Object -Unique)
	$where = ($lines.Count -eq 1) ? "line $($lines[0])" : "lines $($lines -join ', ')"
	return ($Offsets.Count -gt 5) ? "$where, ..." : $where
}

function Add-File([string] $FullPath) {

	if ($seen.Add($FullPath)) {
		$files.Add($FullPath)
	}
}

# Windows paths compare case-insensitively; elsewhere they do not.
$pathComparer = $IsWindows ? [System.StringComparer]::OrdinalIgnoreCase : [System.StringComparer]::Ordinal
$pathComparison = $IsWindows ? [System.StringComparison]::OrdinalIgnoreCase : [System.StringComparison]::Ordinal

$here = (Get-Location).ProviderPath
$topLevel = Invoke-Git $here @('rev-parse', '--show-toplevel')
$repoRoot = ($topLevel.ExitCode -eq 0) ? [System.IO.Path]::GetFullPath([System.Text.Encoding]::UTF8.GetString($topLevel.Output).Trim()) : $null

# Files that differ from -Base: the only ones whose final newline can have changed, and the first
# half of the default file list. Both compare with the newest commit HEAD shares with -Base, which is
# -Base itself unless it has moved on since HEAD left it. Comparing with the tip of a branch that has
# moved on would count its later commits as changes made here, final newlines included.
$changedSet = [System.Collections.Generic.HashSet[string]]::new($pathComparer)
$changed = @()
$baseLabel = $Base
if ($null -ne $repoRoot) {

	$resolved = Invoke-Git $repoRoot @('rev-parse', '--verify', '--quiet', "${Base}^{commit}")
	if ($resolved.ExitCode -ne 0) {

		Note "-Base '$Base' does not name a commit in $repoRoot" Red
		exit 2
	}
	$baseTip = [System.Text.Encoding]::UTF8.GetString($resolved.Output).Trim()
	$mergeBase = Invoke-Git $repoRoot @('merge-base', $baseTip, 'HEAD')
	if ($mergeBase.ExitCode -ne 0) {

		Note "-Base '$Base' shares no history with HEAD" Red
		exit 2
	}
	$baseCommit = [System.Text.Encoding]::UTF8.GetString($mergeBase.Output).Trim()
	if ($baseCommit -ne $baseTip) {

		$baseLabel = [System.Text.Encoding]::UTF8.GetString((Invoke-Git $repoRoot @('rev-parse', '--short', $baseCommit)).Output).Trim()
		Note "comparing with $baseLabel, the newest commit HEAD shares with $Base"
	}
	$changed = @(Get-GitPathList @('diff', '--name-only', '-z', '--diff-filter=d', $baseCommit, '--'))
	foreach ($relative in $changed) {
		$null = $changedSet.Add([System.IO.Path]::GetFullPath($relative, $repoRoot))
	}
}

$files = [System.Collections.Generic.List[string]]::new()
$seen = [System.Collections.Generic.HashSet[string]]::new($pathComparer)
$errors = 0

if (-not $Path) {

	if ($null -eq $repoRoot) {

		Note 'not inside a git repository - pass the files or directories to check' Red
		exit 2
	}
	$untracked = @(Get-GitPathList @('ls-files', '-z', '--others', '--exclude-standard'))
	foreach ($relative in $changed + $untracked) {
		Add-File ([System.IO.Path]::GetFullPath($relative, $repoRoot))
	}
	Note "$($files.Count) file(s) changed since $baseLabel or untracked"
}
else {

	foreach ($item in $Path) {

		$full = [System.IO.Path]::GetFullPath($item, $here)
		if (Test-Path -LiteralPath $full -PathType Leaf) {
			Add-File $full
		}
		elseif (Test-Path -LiteralPath $full -PathType Container) {

			# Inside the repo, git decides what a directory holds, so build output and other ignored
			# files stay out. An ignored directory was asked for by name, so it is walked in full.
			$useGit = ((Test-InRepo $full) -and
				(Invoke-Git $repoRoot @('check-ignore', '--quiet', '--', (Get-RepoRelativePath $full))).ExitCode -ne 0)
			if ($useGit) {

				foreach ($relative in @(Get-GitPathList @('ls-files', '-z', '--cached', '--others', '--exclude-standard', '--', (Get-RepoRelativePath $full)))) {

					# --cached also lists tracked files deleted from the working tree.
					$entry = [System.IO.Path]::GetFullPath($relative, $repoRoot)
					if (Test-Path -LiteralPath $entry -PathType Leaf) {
						Add-File $entry
					}
				}
			}
			else {

				Get-ChildItem -LiteralPath $full -Recurse -File -Force |
					Where-Object { $_.FullName -notmatch '[\\/]\.git[\\/]' } |
					ForEach-Object { Add-File $_.FullName }
			}
		}
		else {

			Note "not found: $item" Red
			$errors++
		}
	}
}

# git ls-files puts untracked files before tracked ones; sorted output reads like a directory listing.
$files.Sort($pathComparer)

if ($files.Count -eq 0) {

	Note 'nothing to check' ($errors -gt 0 ? 'Red' : 'Green')
	exit (($errors -gt 0) ? 2 : 0)
}

$strictUtf8 = [System.Text.UTF8Encoding]::new($false, $true)
$checked = 0
$binary = 0
$fixedFiles = 0
$needAttention = 0

foreach ($file in $files) {

	$display = (Test-InRepo $file) ? (Get-RepoRelativePath $file) : $file
	try {
		$bytes = [System.IO.File]::ReadAllBytes($file)
	}
	catch {
		Note "${display}: could not read - $($_.Exception.GetBaseException().Message)" Red
		$errors++
		continue
	}
	$length = $bytes.Length

	# The UTF-16 test runs before the binary one, since UTF-16 text is full of NULs.
	if ($length -ge 2 -and (($bytes[0] -eq 0xFF -and $bytes[1] -eq 0xFE) -or ($bytes[0] -eq 0xFE -and $bytes[1] -eq 0xFF))) {

		$checked++
		$needAttention++
		Write-Host "${display}: UTF-16 BOM - the file is not UTF-8" -ForegroundColor Yellow
		continue
	}

	# Git's own binary test: a NUL anywhere in the first 8000 bytes.
	if ([Array]::IndexOf($bytes, [byte]0, 0, [Math]::Min($length, 8000)) -ge 0) {

		$binary++
		Write-Verbose "${display}: binary, skipped"
		continue
	}
	$checked++

	$hasBom = $length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF

	# Visit only the CRs. [Array]::IndexOf with a [byte] needle is a vectorized search, so a file
	# without any costs one call. The cast matters: an [int] needle never equals a byte element.
	$crlfAt = [System.Collections.Generic.List[int]]::new()
	$strayAt = [System.Collections.Generic.List[int]]::new()
	$crCrLfAt = [System.Collections.Generic.List[int]]::new()
	$at = [Array]::IndexOf($bytes, [byte]13)
	while ($at -ge 0) {

		if ($at + 1 -lt $length -and $bytes[$at + 1] -eq 10) {

			# A stray CR right before this CRLF was the last one recorded. The two are reported as a
			# pair and neither is fixed: without this CR, the stray one would pass for a CRLF's.
			if ($at -gt 0 -and $bytes[$at - 1] -eq 13) {
				$strayAt.RemoveAt($strayAt.Count - 1)
				$crCrLfAt.Add($at - 1)
			}
			else {
				$crlfAt.Add($at)
			}
		}
		else {
			$strayAt.Add($at)
		}
		$at = ($at + 1 -lt $length) ? [Array]::IndexOf($bytes, [byte]13, $at + 1) : -1
	}

	$reportOnly = [System.Collections.Generic.List[string]]::new()
	if ($strayAt.Count -gt 0) {

		# Lines are counted by LF, so a file using bare CRs as line breaks puts them all on line 1.
		$reportOnly.Add("$($strayAt.Count) stray CR(s), not part of a CRLF, on $(Format-LineList $bytes $strayAt)")
	}
	if ($crCrLfAt.Count -gt 0) {
		$reportOnly.Add("$($crCrLfAt.Count) CR CR LF sequence(s) on $(Format-LineList $bytes $crCrLfAt), which -Fix leaves alone: the extra CR may be a line break or junk")
	}

	# U+FFFD is EF BF BD. Only characters from U+F000 up start with 0xEF, so in most text the search
	# stops at nothing but a BOM or a real U+FFFD.
	$replacementAt = [System.Collections.Generic.List[int]]::new()
	$at = [Array]::IndexOf($bytes, [byte]0xEF)
	while ($at -ge 0) {

		if ($at + 2 -lt $length -and $bytes[$at + 1] -eq 0xBF -and $bytes[$at + 2] -eq 0xBD) {
			$replacementAt.Add($at)
		}
		$at = ($at + 1 -lt $length) ? [Array]::IndexOf($bytes, [byte]0xEF, $at + 1) : -1
	}
	if ($replacementAt.Count -gt 0) {
		$reportOnly.Add("$($replacementAt.Count) U+FFFD replacement character(s) on $(Format-LineList $bytes $replacementAt) - an earlier decode lost the original")
	}

	try {
		$null = $strictUtf8.GetCharCount($bytes)
	}
	catch [System.Text.DecoderFallbackException] {

		$offset = $_.Exception.GetBaseException().Index
		$reportOnly.Add(('not valid UTF-8: byte 0x{0:X2} on line {1}' -f $bytes[$offset], (Get-LineNumber $bytes $offset)))
	}

	# Only a file that differs from -Base can have gained or lost its final newline. One that is new
	# since -Base has no blob to compare with.
	if ($changedSet.Contains($file)) {

		$blob = Invoke-Git $repoRoot @('cat-file', 'blob', "${baseCommit}:$(Get-RepoRelativePath $file)")
		if ($blob.ExitCode -eq 0) {

			$hadNewline = $blob.Output.Length -gt 0 -and $blob.Output[-1] -eq 10
			$hasNewline = $length -gt 0 -and $bytes[-1] -eq 10
			if ($hadNewline -and -not $hasNewline) {
				$reportOnly.Add("final newline removed ($baseLabel has one)")
			}
			elseif ($hasNewline -and -not $hadNewline) {
				$reportOnly.Add("final newline added ($baseLabel has none)")
			}
		}
	}

	$fixable = [System.Collections.Generic.List[string]]::new()
	if ($hasBom) {
		$fixable.Add('UTF-8 BOM')
	}
	if ($crlfAt.Count -gt 0) {
		$fixable.Add("CRLF line endings on $($crlfAt.Count) line(s)")
	}

	$fixed = $false
	if ($Fix -and $fixable.Count -gt 0) {

		# Copy everything except the BOM and the CR of each CRLF. WriteAllBytes truncates and rewrites
		# the existing file rather than replacing it, so a symlink or hard link still points at it.
		$start = $hasBom ? 3 : 0
		$rewritten = [System.IO.MemoryStream]::new($length)
		foreach ($cr in $crlfAt) {
			$rewritten.Write($bytes, $start, $cr - $start)
			$start = $cr + 1
		}
		$rewritten.Write($bytes, $start, $length - $start)
		try {
			[System.IO.File]::WriteAllBytes($file, $rewritten.ToArray())
			$fixed = $true
			$fixedFiles++
		}
		catch {
			Note "${display}: could not write - $($_.Exception.GetBaseException().Message)" Red
			$errors++
		}
	}

	foreach ($finding in $fixable) {

		if ($fixed) {
			Write-Host "${display}: $finding - fixed" -ForegroundColor Green
		}
		elseif ($Fix) {
			Write-Host "${display}: $finding" -ForegroundColor Yellow
		}
		else {
			Write-Host "${display}: $finding - fixable with -Fix" -ForegroundColor Yellow
		}
	}
	foreach ($finding in $reportOnly) {
		Write-Host "${display}: $finding" -ForegroundColor Yellow
	}

	if ($reportOnly.Count -gt 0 -or ($fixable.Count -gt 0 -and -not $fixed)) {
		$needAttention++
	}
	elseif ($fixable.Count -eq 0) {
		Write-Verbose "${display}: ok"
	}
}

$parts = @("$checked checked")
if ($binary -gt 0) {
	$parts += "$binary binary skipped"
}
if ($fixedFiles -gt 0) {
	$parts += "$fixedFiles fixed"
}
$status = @()
if ($needAttention -gt 0) {
	$status += "$needAttention need attention"
}
if ($errors -gt 0) {
	$status += "$errors error(s)"
}
$color = ($errors -gt 0) ? 'Red' : (($needAttention -gt 0) ? 'Yellow' : 'Green')
Note ('{0} - {1}' -f ($parts -join ', '), (($status.Count -gt 0) ? ($status -join ', ') : 'clean')) $color

if ($errors -gt 0) {
	exit 2
}
exit (($needAttention -gt 0) ? 1 : 0)
