# Help files

Measured 2026-10-10 on PowerShell 7.6.6, with copies of a binary module's Debug build that hold a test MAML file and with that module's real help file, unless a fact gives another date.

## Where Get-Help looks

- **The file name:** a binary cmdlet's `HelpFile` is its assembly's file name followed by `-Help.xml`, such as `<AssemblyName>.dll-Help.xml`, also when a manifest with another name imports the DLL (measured 2026-10-25 on PowerShell 7.6.8).
- **A manifest whose `RootModule` is the DLL beside it** (measured 2026-10-25 on PowerShell 7.6.8, with a throwaway binary module): the cmdlets' module is the manifest's, with the manifest's name and the type `Binary`, and its `Path` is the DLL. Get-Help finds the file in the `en-US` folder beside the manifest and in the module's folder itself, and when both hold it, the one in `en-US` wins. A module-qualified name has to use the manifest's name: `Get-Help <manifest name>\<cmdlet>` finds the help, and the DLL's name finds nothing.
- **Imported by the DLL's path,** the cmdlets' module is the DLL, named after it, and Get-Help finds the file in a culture folder of the DLL's folder, such as `en-US`, or in the DLL's folder itself.
- **Imported through a manifest whose `.psm1` imports the DLL from a subfolder,** the cmdlets' module is the manifest's module, whose module base is the manifest's folder. Get-Help finds the file in the `en-US` folder beside the manifest, and in the `en-US` folder of the DLL's subfolder. When both hold it, the one beside the manifest wins.
- **Culture:** a folder for the UI culture wins, such as `de-DE` under de-DE. Without one, Get-Help falls back to `en-US`, under de-DE and under fr-FR.
- **An installed copy:** with auto-loading on and an older release of the module installed, Get-Help for one of its cmdlets still returns only the loaded build's help. A module-qualified name, such as `Get-Help <module>\<cmdlet>`, works.

## What Get-Help shows

- **The syntax and the parameter attributes come from the file as it's written.** A syntax block that names a parameter the cmdlet doesn't have shows it, and PARAMETERS lists only the parameters that the file describes, with the file's `required`, `position`, `pipelineInput`, and `aliases`. Get-Help adds `[<CommonParameters>]` to each syntax line, and a `<CommonParameters>` entry after the parameters, by itself.
- **A parameter's aliases:** PowerShell 7 shows an `Aliases` row for each parameter.
- **A command's aliases:** with a help file, Get-Help has no ALIASES section.
- **Without a help file,** Get-Help makes up help from the cmdlet. Its synopsis is the syntax, and its description is empty. Its type names say so, as the help object section describes. That holds through a manifest whose `RootModule` is the DLL too (measured 2026-10-25 on PowerShell 7.6.8).
- **Paragraphs:** Get-Help wraps each `maml:para` at the console's width, with a blank line between paragraphs. A paragraph that starts with `- ` prints as it is, so it reads as a list item. An XML comment before `helpItems` doesn't get in the way. A line break inside a `maml:para` stays, and so does the indentation of the line after it, so a paragraph that the file wraps over two lines prints as two lines, the second with the file's tabs in it (measured 2026-10-24 on PowerShell 7.6.8).
- **A parameter's values:** in a syntax block, a parameter whose entry has a `command:parameterValueGroup` prints its values in braces, in place of its `command:parameterValue`, as in `-<Name> {<value> | <value> | <value>}`. In PARAMETERS and under `Get-Help -Parameter`, the parameter prints as `-<Name> <Type>`, from its `command:parameterValue`, and no list of the values prints, even when its entry there has a `command:parameterValueGroup` too (measured 2026-10-24 on PowerShell 7.6.8).
- **Examples:** each example's title, code, and remarks print as they're written, so a long title wraps too. When an example has no remarks, Get-Help indents the next example's title by a space. When the remarks don't end with an empty `maml:para`, the next title follows them without a blank line. The line breaks in an example's code stay, such as those between a command and its output, and Get-Help indents every line of the code by 4 spaces. A code line longer than the width wraps at a space, like a paragraph, so the rest of the command prints as a line of its own (measured 2026-10-24 on PowerShell 7.6.8).

## The help object

- In `Get-Help -Full` output, the entries of `parameters.parameter` and of `syntax.syntaxItem.parameter` hold `name`, `required`, `position`, `pipelineInput`, and `aliases` as the strings that the file writes. A parameter's type is `type.name`. In a syntax block, `parameterValue` is the text of the `command:parameterValue` element, or `$null` when the parameter has none. A `command:parameterValueGroup` beside it doesn't change it, and its values are in `parameterValueGroup.parameterValue` (measured 2026-10-24 on PowerShell 7.6.8).
- `[System.Management.Automation.Cmdlet]::CommonParameters` and `OptionalCommonParameters` list the common parameters. PowerShell 7's list includes `ProgressAction`.
- **The type names** tell help from a file apart from help made up from the cmdlet. Help from a file has `MamlCommandHelpInfo#<module>#<cmdlet>`, `MamlCommandHelpInfo#<module>`, `MamlCommandHelpInfo`, and `HelpInfo`, where `<module>` is the DLL's module when the DLL is imported by its path and the manifest's module through a manifest. Help made up from the cmdlet has `ExtendedCmdletHelpInfo`, `CmdletHelpInfo`, and `HelpInfo`. `-Full` puts each name with `#FullView` added in front of the rest, so the first type name differs with and without `-Full`. Measured 2026-10-24 on PowerShell 7.6.8, with and without a binary module's help file, through a manifest whose `.psm1` imports the DLL and with the DLL imported by its path, and 2026-10-25 on PowerShell 7.6.8 through a manifest whose `RootModule` is the DLL.

## Update-Help

Measured 2026-10-10 on PowerShell 7.6.6.

- With a `HelpInfoURI` that isn't a folder of updatable help, such as a GitHub issues page, `Update-Help -Module <module>` fails: "The HelpInfoUri '...' does not resolve to a container."
- Without a `HelpInfoURI`, `Update-Help -Module <module>` fails, and so does `-Module <module>*`: "The Update-Help command failed because the specified module does not support updatable help."

## What it means for PoshSonarr

- PoshSonarr's help goes in `MG.Sonarr.Next.Shell.dll-Help.xml`, named after the assembly that holds the cmdlets, in the `en-US` folder beside the manifest, whose root module is that DLL. Renaming the assembly renames the file it needs.
- Read the help through the manifest as `Get-Help PoshSonarr\<cmdlet>`. The DLL's name finds nothing then, and a build imported by its path is a module named after the DLL, `MG.Sonarr.Next.Shell`.
- Because Get-Help trusts the file, a test that compares the help's syntax blocks and parameters with the cmdlet's parameter sets and parameters catches a stale file. Have it read the help with a module-qualified name and check that its type names include `MamlCommandHelpInfo`, so that it also fails when the build doesn't ship the file. Don't compare the first type name, which changes with `-Full`.
- List a cmdlet's aliases in the help's notes, because Get-Help shows no ALIASES section.
- Give each syntax entry of an enum parameter a `command:parameterValueGroup` with the enum's values, so the syntax shows them, and list the values in the parameter's description too, the only place where PARAMETERS shows them.
- Give every example remarks that end with an empty paragraph, because of the spacing that Get-Help gives examples.
- Write each `maml:para` on one line of the file, however long. Keep each line of an example's code within 76 characters, so that with Get-Help's indent it fits a console 80 characters wide, and a wrapped command doesn't read as two.
- Give the manifest a `HelpInfoURI` only when it points at a folder of updatable help. A web page, such as an issues page, makes `Update-Help` fail, and without one `Update-Help` says the module has no updatable help.
