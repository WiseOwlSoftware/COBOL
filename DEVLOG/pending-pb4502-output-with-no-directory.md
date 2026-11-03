## PB4502: an output path with no directory is refused (exit 73) instead of crashing the compiler

`cobol prog.cob -o NUL`, and `-o /dev/null` from Git Bash (which passes it as `NUL`), crashed every compile with an
unhandled `ArgumentNullException`. `Path.GetDirectoryName` of the NUL device path (or of a volume root) is null, and
`CompilerDriver.Compile` built the default external-repository directory from it with a null-forgiving `!`;
`ExpansionAssemblies.Write` wrote the same default a second time.

`CompilerDriver.Options` now states the output once (`OutputDll`, `OutputDirectory`, `RepositoryDirectories`, the one
place the repository's default directory is written), and `Compile` refuses an output with no directory before the
front end, as the new outcome `OutputNotWritable`, exit status 73 (sysexits EX_CANTCREAT), with a message that names
the path. The manual's exit-status table has the row, and `compiler-facts.json` is regenerated. The sibling sweep of
every `Path.GetDirectoryName(...)!` in `src/` is in the note: the compiler's remaining three now hold by construction,
and the runtime's six take a user file's path, where a probe of `ASSIGN TO "NUL"` through OPEN, WRITE, CLOSE and EXTEND
ran clean.

Found while writing the minimal repros for the upstream records PB4499-PB4501.

The branch review (round 1) found the refusal too narrow: `-o /dev/null` on Linux, or a directory the user may not write, still threw from the `.g.cs` write. The write phase is now one method, and an `IOException` or `UnauthorizedAccessException` from it is `OutputNotWritable` with the host's message, exit 73.
