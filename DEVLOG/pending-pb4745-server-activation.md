## Messaging slice S5 (PB4745): a SEND starts the server mcs.json defines, and a test can start servers beside itself

The last slice of `DESIGN-message-control.md` (§5.4.5 and Q9). Before it, `MessagingSettings.Definitions` was a `static _ => null` that only a unit test replaced, the peer answered
every `Activate` with "a server cannot be started from this run unit yet", and `mcs.json`'s `servers` member was read by nobody, so neither `start` nor `maxLength` could ever
reach a production SEND. The re-probe on this tree (a requestor, `mcs.json` naming a server with `start`, no server running) could only end in EC-MCS-NO-SERVER.

**The fix.** `ServerDefinitions` reads `servers` (`name`; `start` as a command line, double quote grouping, or an argument array; `maxLength` a positive whole number) when the peer
starts, so `maxLength` is live as well; an absent file defines nothing and an unusable one (not JSON, a member of the wrong shape, a name defined twice once trailing spaces and
case are ignored) is an `IOException`, which is EC-MCS-IMP at the first statement (Q8). `ServerActivation.Run` is the requestor's §5.4.5: it opens `start/<N>.lock`, holds
`HostRegionLocks.McsStartByte` exclusively and waiting (a host that cannot carry the lock is EC-MCS-IMP), enumerates again, and `ServerActivation.Launch`es the command in the MCS
directory with `WISEOWL_COBOL_MCS`, `_SERVER`, `_NOTIFY` and `_LOG`, waiting for the server's connection to `n/<r>`. A process that ends first is EC-MCS-IMP with the exit code and the log's
path; a server that publishes and ends at once still counts as started (its connection waits in the backlog). The launched process has no standard input and its pipes are drained
into `servers/<N>.<r>.log` while the start is awaited and for as long as they last. The log is really the started run unit's: `StandardStreams.EnsureUtf8`, which only the two process
entries call, sends the console to `WISEOWL_COBOL_MCS_LOG` (appended), so a server never writes to a pipe its requestor has closed. `MessagingPeer.Abort` cancels a start in progress
and `Launch` kills what it launched.

**Q9.** The conformance suite's wiseowl-only header field `servers: {BALANCE: unit.cbl}` (generate.py: refused in `cases/`, an empty map, a blank or doubly named server, an
undeclared file, an assembly that is the test's, a build step's or another server's; a manifest `servers` entry; `wiseowl/options/<id>.<file>.json` accepted for a server file) makes
the in-process runner compile each file and start it with `ServerActivation.Launch`, the same launch a requestor uses (`RunningServers`), before the test, ending it with the test.
`pb4745-send-served-by-a-concurrent-run-unit` is the first such case.

**Tests.** `MessagingProcessTests` (activation and its log; two requestors racing, one launch, one log; a start that ends first; a start that cannot be started; four unusable
`servers` definitions; `maxLength`) and `ServerDefinitionsTests`. Writing the race test found nothing in the code: its first server received `RT` as a non-NULL tag on the second RECEIVE
(a RECEIVE FROM the first requestor), so the second requestor correctly got EC-MCS-NO-SERVER; the server now sets RT to NULL.

Rows: none claimed or closed (the design lists none for S5). Docs: DESIGN-message-control (status IMPLEMENTED, §5.4.5, §5.5, §7.2, Q9), DESIGN-conformance-suite §3.1, CONFORMANCE D-MCS-SERVER,
the user guide's messaging section, the conformance-suite README. Gate 000002 was RED on seven Unit drift/config tests (the log open sat in `IO/StandardStreams.cs`, which the file-share and RuntimeConfig sweeps own; the new unit test made its own scratch directory), fixed by opening the log in `ServerActivation` and using `ScratchDirectory`. Final gate: `=== BUILD-LOCAL GATE: GREEN — Conformance 22,679/22,679 · Unit 40,544/40,544 · Characterization 37/37 cases ran (skipped 0) (run 000003-20261108T210835Z; Conformance.trx UnitTestResult ConformanceSuiteTests_P13.SuiteTest_IsNoWorseThanItsVerdictAllows(edition: "2023", id: "pb4745-send-served-by-a-concurrent-run-unit") Passed) ===`.

### Review round 1 (finisher)

R1-1 (blocking): FileMode.Append seeks once at open and then writes at the stream own offset, so the two pipe drains and the started server console overwrote each other in servers/N.r.log. New ServerLog (write-only, seeks to the end before every write under one lock) is opened once per launch and shared by both drains; the started process console uses it too. Test: TwoWritersOfOneServerLog_KeepEachOthersLines. R1-2: AProcessStartedWithAServerLog_WritesItsConsoleToTheLog_NotToItsOwnStandardOutput starts a server with the log variable and no requestor. R1-3: Launch returns the drain task; Run disposes the process, and the log disposes, when the pipes end.
