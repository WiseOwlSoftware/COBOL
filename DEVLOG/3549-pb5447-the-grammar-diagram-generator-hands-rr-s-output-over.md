## Entry 3549 — 2026-11-12 15:02 PST — PB5447: the grammar-diagram generator hands rr's output over a pipe, not a file in %TEMP%

A full gate of witnesses-85m went RED on one Unit test, `GrammarDiagramGeneratorDriftTests.Generator_RunsClean`.
`gen-grammar-diagrams.ps1` could not read `E:\Temp\ccf-<guid>\CobolWords.md`, "being used by another process". The
lead was that the retry PB44 added could never catch the exception PowerShell throws. A reproduction refuted it:
`catch [System.IO.IOException]` matches the .NET exception inside the `MethodInvocationException`. A second
reproduction showed the red's exact `ParentContainsErrorRecordException` at the `ReadAllText` line is what the 10th
attempt's rethrow prints. So a third process held write access to the fresh file for longer than the whole ~2.25 s
backoff. The path is per-run GUID and java had exited, so no writer shared it. 420 stressed runs with a Restart Manager
probe did not reproduce it, so the holder stays unnamed. A file in %TEMP% is a file any process on the host may open.

The fix removes the handoff file. rr.war reads the EBNF on stdin and writes the Markdown to stdout (`Invoke-Rr`, UTF-8
on all three streams). The temp directory, the intermediates and the retry are deleted, and the 10 generated notes
are byte-identical to the old script's. The drift test now runs the generator with its temp directory pointed at its own
directory (`TMP` and `TEMP` for Windows, `TMPDIR` for Linux, where .NET reads no other) and holds every fresh file
there for writing. Main's script fails it with the gate's message; the fixed
script passes. The sibling sweep found no other instance: `orchestrate.ps1#Take-Lock`'s identical catch clause works,
and the ANTLR `.tokens` copy and the tar self-test hand off files by design.

