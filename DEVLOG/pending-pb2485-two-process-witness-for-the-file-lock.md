## PB2485 — a two-process witness for §9.1.15's file lock: refusal, release by CLOSE, by run-unit end, by abnormal end, by a kill, and not kept by a child

**What was missing.** `RunUnitFileLockDriftTests` proves the cross-run-unit file lock (PB833) over 768 sharing × open-mode
pairs by opening one file through two `FileRegistry` instances in ONE test process. That reaches the same host primitive
as two processes, but it cannot see what only a process boundary has: a lock that dies with its HOLDER, a descendant that
inherits what its parent holds (the `O_CLOEXEC` guard in `OfdRegionLocks.Open`), and the path a compiled program really
takes (generated code's `FileConnector.Open`, not the registry API).

**Re-probe.** On a fresh Debug build, two compiled programs in two processes: the holder opens `SHARING WITH READ ONLY` in
EXTEND mode and waits on `ACCEPT`; the second program's `OPEN EXTEND` answers `61` (§9.1.13.9 1) c)), `DELETE FILE` answers
`62` (§9.1.13.9 2)), a refused `OPEN OUTPUT` truncates nothing (§14.9.27.4 GR25), and every one of them answers `00` once
the holder ended by STOP RUN, by a fatal `CALL` of a missing program, or by a kill. No defect: the implementation was right,
and nothing had witnessed it.

**The witness.** `tests/Cobol.Net.Tests.Unit/RunUnitProcessBoundaryTests.cs` (18 cases, `[Trait("Host","ReFS")]` so the gate's
leg 1 holds it) compiles a HOLDER program (any sharing clause and open mode; it prints `HELD <status>` and obeys one stdin
line: `CLOSE`, `ABORT`, or end normally with the file open) and PROBER programs, and runs them as real processes. The new
instrument is `tests/_shared/LiveProcess.cs`, the long-lived twin of `ProcessObserver`: the test and the program speak by
stdin and stdout lines, so every step is ordered by a message, never a delay, and a line that never comes is the harness's
named non-observation (`ProcessObserver.TimeoutMs`), never a value.

Success and abnormal paths (MANDATORY-PRACTICES I11): NO OTHER holder refuses INPUT, I-O, EXTEND and OUTPUT with `61`
(§9.1.13.9 1) a)), the holder's explicit CLOSE gives the lock back while the holder lives (§9.1.15 3)), its run unit's
implicit CLOSE does (§14.6.11 2)), so does an abnormal end (§14.6.12) and so does a kill that runs nothing (the host fact
DOC-A.1-75 documents); READ ONLY holder in EXTEND mode refuses EXTEND, I-O and OUTPUT (§9.1.13.9 1) c), e)); the lock does
not over-refuse (an ALL OTHER holder admits ALL OTHER INPUT, I-O and EXTEND beside it and refuses OUTPUT, READ ONLY and NO OTHER
requests; an ALL OTHER INPUT holder admits READ ONLY INPUT, I-O and EXTEND: the printed Table 19 of §14.9.27.4); the refusal
reaches a FILE STATUS item, a USE declarative with `EC-I-O-FILE-SHARING` checked (`EXCEPTION-STATUS`, `EXCEPTION-FILE` =
`61F`), both, and neither (a `6x` status is not a fatal class, so the program continues, §9.1.13.1); DELETE FILE is `62`
through ON EXCEPTION, FILE STATUS alone, a declarative alone, both, neither (continues) and a RETRY the holder outlasts, leaves the file in place, and deletes it once the holder is
gone; `OPEN ... RETRY 2 TIMES` that the holder outlasts is `61` (§14.7.9.3 4)) and `RETRY FOREVER` succeeds when the holder
CLOSEs. A child started FROM a process that holds the lock (the stand-in for `CALL "SYSTEM"`, which the compiler does not
provide) outlives the holder's CLOSE and does not keep the lock.

**Limits stated.** The `RETRY FOREVER` case can pass for the wrong reason if its first attempt came after the holder's CLOSE;
it can never fail for one (a probe on the build showed the OPEN blocking for seconds while the holder held, then `00`). The
macOS lock-file host is held to the same table by `SidecarColumnLockHostDriftTests` on every machine; this class runs on the
host it is on (Windows and Linux in the gates).

**Docs.** `DESIGN-runtime-library.md` (guards list) and `docs/CONFORMANCE.md` DOC-A.1-75 (witness columns) name the class.
Rows re-verdicted: none (`inventory_rows` was empty; `closes_rows: []` with its reason).

**Branch review round 1 (R1-1, medium).** The reviewer found the DELETE FILE `62` witnessed only through ON EXCEPTION, FILE STATUS
alone and a declarative alone, while the design doc claims both, neither and a RETRY the holder outlasts. The behavior was right and
only the witness was missing: five cases added (`ARefusedDeleteFile_ReachesTheProgramsHandlers_...` over all four handler
combinations, with the file untouched while refused and deleted after the holder's kill; and
`ADeleteFileRetryThatTheHolderOutlasts_IsStatus62_...`). R1-2 (the `Host=ReFS` trait) is kept, with the reason written into the
class's summary: it is the process-boundary member of the family whose siblings carry it.
