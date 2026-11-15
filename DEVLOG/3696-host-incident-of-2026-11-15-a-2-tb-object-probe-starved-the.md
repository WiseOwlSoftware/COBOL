## Entry 3696 — 2026-11-15 13:03 PST — Host incident of 2026-11-15: a 2 TB object probe starved the host; PB5695 and PB5696 filed

Between 12:38 and 12:43 PST a witness writer of the 2002 golden lane (slug `g2002-15-57-ord`) ran a probe whose class `G2BIG`
holds an OBJECT WORKING-STORAGE table of about 2 TB (`05 E PIC X(1000) OCCURS 2000000000`) and invokes `NEW` on it. It compiled
without a diagnostic. The run committed 79.8 GB, the host fell to 0.5 GB free commit, and a landing of the other session died
("cannot start new thread", exit 6). Only the writer's shell limit of 300 s ended the process: `cobol --run` waits on its child
with no time or memory limit.

Two register notes carry the work, none of it started here. PB5695 (defect, `crashes`) is the compiler side: no diagnostic and
no defined failure for an object or record that size; the choice between a documented compile-time limit and a run-time
exception condition is left open for the fix, with the probe source in the note as text and a capital-letter warning not to
run it. PB5696 (process-only) is the tool side: a probe or test run must be bounded in memory and time by the tool that starts
it, one mechanism for `--run` and the conformance harness.

Interim guard, not the fix: since 12:50 PST the operator session runs a watchdog that kills a `dotnet.exe` or `cobol.exe`
user-program process whose commit passes 12 GB, and every fleet brief carries a HOST SAFETY rule. Gates that ran in the window
may have shown infrastructure reds.

