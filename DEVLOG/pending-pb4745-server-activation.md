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
the user guide's messaging section, the conformance-suite README. Gate: see the verdict line below.
