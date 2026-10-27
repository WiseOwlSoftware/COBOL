## Entry 2653 — 2026-10-26 20:34 PDT — PB3294 — the standard streams stop changing the console's code pages, and an interactive Windows console ACCEPT keeps every character

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

Branch review, round 1 (one blocking finding, R1-1, and one lead, R1-2). R1-1 was right and the first fix had a hole: an
interactive console read through `ReadConsoleW` (what `Console.InputEncoding = Encoding.Unicode` selects) never turns
Ctrl+Z into end-of-input, which the console's own `ReadFile` read does, so `ACCEPT` typed as Ctrl+Z, Enter stored the
character U+001A (ORD 27) and a program reading until end-of-input never saw the end. Re-measured on this build through
the pseudoconsole with a plain C# reader: the `ReadFile` arm reads a lone Ctrl+Z line and a Ctrl+Z-then-text line as
end-of-input and discards the line, and the wide-character arm returns U+001A. Fix at the root: `ConsoleInputReader`
(new, `IO/ConsoleInputReader.cs`), a `TextReader` over the console's reader that overrides only `Read` and `Peek`, so
every other read is built on the one rule: U+001A as the first character of a line is end of input and the rest of the
line is discarded; elsewhere it is an ordinary character; the next read asks the console again. `EnsureUtf8` wraps
`Console.In` in it exactly when it moved an interactive Windows console to `Encoding.Unicode`. Witnesses:
`WindowsConsoleDeviceTests.Accept_from_an_interactive_console_ends_the_input_at_Ctrl_Z` (ORD 33 33 33 33; with the wrap
commented out it fails with the substring missing, run to confirm) and `ConsoleInputReaderTests` (Unit, three facts
over a `StringReader`, so the rule is pinned on every platform). DOC-A.1-1 states the signal.

R1-2 (the lead) sat in the same method and is fixed with it: the old single `try` set the input and output encodings
together and its catch turned every handle into a byte stream, so a refusal of the OUTPUT setter after the input setter
had already moved stdin to code page 1200 installed a UTF-8 reader over a stream that then read UTF-16. The directions
are now tried apart through `TryApply` (a setter that throws has not applied its encoding), and its comment says what
the guard really covers: the Unix output setter's flush of the writers it replaces, and a policy that refuses the console;
a Windows wide-character encoding never asks the console. Also fixed in the helper this branch added: the pseudoconsole
output thread caught `IOException` only, so a run that TIMED OUT (the `finally` disposes the handle under the reading
thread) raised an unhandled `ObjectDisposedException` on a thread and crashed the whole test host instead of reporting the
`TimeoutException` (found while measuring the hung wide-character read); it now catches both.

