# XML Comments
XML comments for C# code follow the style guidance at `%USERPROFILE%/.claude/skills/csharp-xml-docs/references/style-guide.md`.

# Commit messages:

- The subject uses the imperative mood ("Make", not "Makes"), sentence case, and no period at the end, in at most 72 characters. It says what changes in behavior, not which files changed.
- A blank line separates the subject from the body, which is wrapped at 72 characters. The body explains why first, then what changed. A change with several parts gets one bullet per part, with wrapped lines indented two spaces.
- Messages are plain text: type and member names go without backticks, and measurements keep their numbers and units.
- Doc updates that go with a change are in the same commit, and the body names them.
- Cite merged work by its PR number, written as #<number>, not by the hash of one of its branch's commits, which no branch reaches once a squash merge has deleted the branch.