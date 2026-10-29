## Entry 2735 — 2026-10-29 13:36 PDT — PB3811: the build guard reads an output path given on the command line

`fleet_active_build.py` (the PreToolUse guard that refuses a `dotnet` build while a live subagent works in the tree the
build writes) treated an output path moved in a project file as UNKNOWN but ignored the same redirection on the command
line. A scratch build such as `cd <scratch probe> && dotnet build -o <wave worktree>/src/App/bin/Debug/net10.0` (likewise
`-p:OutDir=` and `dotnet publish --output`) was ALLOWED while it overwrote the binaries the live implementer in that
worktree executes; the same build run from inside the worktree was denied.

Re-probe on the branch head, before the fix: those three commands printed ALLOW, `cd <worktree> && dotnet build` DENY.
After: all four DENY.

Fix, one mechanism. `build_sites` returns `Site(target, outputs)` instead of a bare string. `dotnet_arguments` reads the
words after `dotnet` once: the projects named, the output redirections (`-o`, `--output`, `--artifacts-path` with the value
in the next word or after `:` / `=`; `-p:`, `-property:`, `--property:`, `/p:`, `/property:` naming one of
`OUTPUT_PATH_PROPERTIES`) and what it cannot read. `output_directories` resolves a redirection against the directory the
command runs in (`-o`, `--artifacts-path`) or the project directory (a property, as MSBuild does), and `build_places` adds
each directory's working tree, or the plain directory, to the places the project graph gave; the directory need not exist.
UNKNOWN, as before: a variable in the path, a response file (`@x.rsp`), `-p:UseArtifactsOutput=`, a property value that
climbs out of the project directory (the graph's other projects are the base it is relative to), and an environment variable
MSBuild reads as a property (`OutDir=x dotnet build`, `export OutDir=x`, `$env:OutputPath = 'x'`). `Directory.Build.rsp` and
`MSBuild.rsp` join the implicit imports whose presence above a scratch project makes its graph UNKNOWN (a response file carries
the same switches; checked in every ancestor, a superset of where MSBuild looks).

Found on the way and fixed in the same change: words after a bare `--` were read as the build's own (`dotnet run -- other.cs`
named a project `other.cs` that does not exist, so it was UNKNOWN and denied; `dotnet run -- hello.cob -o $OUT` would now
have been read as an output option), and the `#:property` check of a file-based app compared names case-sensitively where
MSBuild does not.

Self-test: 39 new `output:` cases, over real temporary worktrees, scratch projects and agent transcripts: every spelling of
the option and of each property family aimed into a live agent's worktree, the main tree, a plain directory and a scratch
agent's directory, relative values, the UNKNOWN shapes, the `--` boundary, `-os linux`, both response files. With
`dotnet_arguments` made blind to redirections 28 of them fail. `DESIGN-test-build-ci.md` 1.7 states the rule.
No ISO rule governs it; the note claims no inventory row.

