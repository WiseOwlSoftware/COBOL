## PB3294 — the standard streams stop changing the console's code pages, and an interactive Windows console ACCEPT keeps every character

`StandardStreams.EnsureUtf8` reached the Windows console through the code-page setters, and that was two defects with one
root. (1) Its input arm decoded only a REDIRECTED standard input, so an interactive console kept its input code page and
every character outside it was stored as `?`: typing U+20AC and U+4E2D at `ACCEPT X` under code page 437 stored
`64 64 70 234` where DOC-A.1-1 (ISO 14.9.1.4 GR1 leaves the device conversion to the implementor, A.1 item 1 makes it a
documented one) promises the UTF-16 repertoire. (2) Its output arm called the `Console.OutputEncoding` setter, which is
`SetConsoleOutputCP(65001)` on the console the parent shell shares, and never restored it: `chcp 437 & dotnet disp.dll
<nul` left `cmd` on 65001, and so did a run with stdout redirected to a file, because the setter ran whenever ANY handle
was a console. The class's own remarks already refused `Console.InputEncoding` for exactly that reason; the output arm
broke the rule it stated.

Re-probe on this lane's build (the adjudication's programs, `inject.ps1` for console input, `chcp` and a code-page
probe for the output side): both reproduced as the adjudication measured. Fix, at the one place: `EnsureUtf8` decides
each of stdin, stdout and stderr by what it IS. A redirected handle gets an explicit BOM-less UTF-8 reader or writer over
the standard stream, on every platform, with no console API (so it also works with no console at all). A Windows console
handle gets `Encoding.Unicode`, which .NET serves with `ReadConsoleW`/`WriteConsoleW` and whose setter skips
`SetConsoleCP`/`SetConsoleOutputCP` for code page 1200, so the console carries the whole repertoire and keeps the code
pages the shell left. A Unix terminal keeps its locale on input and carries UTF-8 on output (a managed-only setter, as
before). The order matters and is documented: the `OutputEncoding` setter re-creates the console's writers, and a writer
created after it for a redirected handle carried UTF-16 into the pipe (measured in a scratch project), so the console
encodings are set first and the redirected handles' explicit writers after. The method keeps its name: it still makes
every byte stream UTF-8, and its callers and `ProcessStateOwnerDriftTests` stay as they are. DOC-A.1-1 and DOC-A.1-59,
and the runtime design doc's process-state paragraph, now describe the per-handle rule.

Witnesses: `WindowsConsoleDeviceTests` (Conformance) runs real programs on a real console with no window, a Windows
pseudoconsole created by the new `WindowsPseudoConsole` helper (`CreatePseudoConsole`; a pipe cannot reach either
defect). Each case puts the console in code page 437 and asserts a code-page probe reads 437 before the program runs, so
a green cannot mean the defect was never exercised. Cases: ACCEPT into `PIC N(4)` and `PIC X(4)` keeps `8365 20014 70
234` (ORD is the code-unit value plus one, item 188); a DISPLAY program (to standard output and UPON SYSERR) with stdin
redirected, with stdout redirected to a file, with stderr redirected to a file (the bytes are strict UTF-8, and the
other stream shows its characters on the console) and with all three handles on the console leaves the probe at
437/437; the `cobol` command rejecting a program prints its section sign and en dash to the console whole and leaves
437/437. The first six were run against the old tree (the stderr case was added after) and all fail on
the old tree (`Actual: (437, 65001)`; the ORD line missing) and pass on the new one. Two traps in the helper are recorded
in its comments: a child started without `STARTF_USESTDHANDLES` takes the test host's redirected pipes instead of the
pseudoconsole, and closing the pseudoconsole straight after the child exits drops the output it has not rendered yet, so
each script ends with a sentinel line the run waits for. The inventory rows DOC-A.1-1 and DOC-A.1-59 stay CONFORMS and
gain the witnesses (a witness-only batch); the note closes no row.
