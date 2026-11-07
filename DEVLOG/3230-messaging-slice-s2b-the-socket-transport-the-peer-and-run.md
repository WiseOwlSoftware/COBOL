## Entry 3230 — 2026-11-06 18:46 PST — Messaging slice S2b: the socket transport, the peer, and run-unit start and end (PB4742)

DESIGN-message-control.md section 9 slice S2b puts the 2023 messaging protocol (S2a, which ran only over `InMemoryTransport`) on real AF_UNIX
sockets and joins it to the run unit. Nothing in the compiler or the emitted code reaches it yet (S3 and S4 do), so every existing program is
unchanged: the new `RunUnit.Messaging` is a shell that allocates its queues, wake and transport only at a run unit's first messaging
statement or, for a run unit that serves a name, when it begins.

**What the re-probe showed.** The note was a slice entry with no repro: the probe was the build (0 warnings, 0 errors on the base) and the S2a
tests, which pass on the base. The platform facts the slice stands on were already witnessed by S0 on Windows (a killed child's sockets end for
its peer as `ConnectionReset`, a renamed listening file is reachable at its new name, a connect between `Bind` and `Listen` is refused). The
Linux half of that witness is CI's.

**The fix shape.** `SocketTransport` is `IPeerTransport` over the socket directory. A listener is bound at `t/<r>`, listens, then `File.Move`s to
`s/<N>/<r>` or `p/<N>/<r>`, so a file under `s/` or `p/` always belonged to a listening socket; a refused connect deletes the file (the refusal
rule), a file already gone is skipped. `PeerLink` is one connected stream with an awaited read pump that posts a `FrameReceived` per frame and one
`LinkLost`, and nothing for an end this side asked for. To keep the pump from holding a thread while its peer is silent, `McsFrame` gained
`ReadAsync`; both reads now share one `ParseHeader`, so the frame rules (the 2^31 guard, the unknown kind, the Ring-with-payload rule) are written
once, and a theory pins the two reads to the same answers on eight inputs. A bell's accept loop reads one `Ring` per connection on its own task with
a five-second limit. `MessagingPeer` is the one executor of the protocol's actions: it runs each statement on the run unit's thread, taking the
answers owed to its own actions, then transport events (`MessagingPeer.Decode`, the one mapping, which `MessagingProtocolTests` now uses too), then
the deadlines that have passed, until the protocol completes the statement. A deadline arms a timer on `MessagingSettings.Time`, never the run unit's
clock. A failed start (directory refused, path too long, notify socket refusing) makes every statement EC-MCS-IMP with the reason and throws nothing.
`MessagingSettings` is host configuration beside `Clock` and `DebugMode` (so `RunUnitStateDriftTests` holds that it carries over, by reflection) and
reads `WISEOWL_COBOL_MCS_SERVER` and `WISEOWL_COBOL_MCS_NOTIFY` only when a member is first asked; both are registered in `RuntimeConfig`.
`RunUnit.Terminate` ends the peer last, in its own `finally` (ISO 14.6.11 item 7, 14.6.12), and `RunUnit.StartAfter` starts a serving run unit.
`HostRegionLocks.McsStartByte` takes the next byte of the region map for slice S5's activation lock. `Activate` is not executed yet (S5, PB4745): the
peer answers it `ActionFailed`, which no definition can ask for because none has a `start`.

**Goldens and tests.** There is no COBOL-visible behavior to witness: S3 binds SEND and RECEIVE and S4 emits them, with section 7.1's cases. The
witnesses are `unit:SocketTransportTests` (publish, the window before the rename, a kill leaving its file and the next refused connect deleting it,
graceful close, a malformed frame, bells, notify, many links) and `unit:MessagingPeerTests` (request and reply, two requestors, no server, a server
killed before a SEND, a deadline on a manual `TimeProvider`, a run unit ending with an active message normally and abnormally, a server killed
with one, the start failure, notify, and `RunUnit` start and end). Rows re-verdicted: none (the design names none for this slice); `closes_rows: []`
with its reason.

**Doc changes.** `docs/CONFORMANCE.md` D-MCS-TRANSPORT; DESIGN-message-control.md 5.5 now describes the wake and the shell as built;
DESIGN-runtime-library.md names `McsStartByte`.

**Self-review.** Architecture: one executor, one decode, one frame-rule statement; the pumps touch no protocol state. Full code: the cited rules are
implemented, not paraphrased; sibling arms (Server and Bell publication, the ring and the connect) share `ConnectTo`. Performance: nothing on a
program's hot path; a run unit that never messages pays one object and one delegate. Duplication: the test scheduler's own action switch stays,
because it scripts what the production one runs.

**Review round 1.** R1-1: `RunUnit.StartAfter` takes `serve`; the ambient replacement `EndForHost` creates serves nothing (as the first lazy ambient run unit), so a served name is not republished with no program behind it (`TheAmbientReplacementOfAHostEndedRunUnitPublishesNothing`). R1-2: `PeerLink.PumpAsync` posts its one `LinkLost` for every fault, including the OutOfMemoryException of a legal-but-unallocatable length (`ALinkWhoseAnnouncedLengthCannotBeAllocatedIsLostNotLeftWaiting`). Leads R1-3, R1-6 and R1-7 are left for the clerk.

**The merge-queue bounce, twice on one case.** `MessagingPeerTests.ANotifySocketThatRefusesIsAStartFailureAndEveryStatementIsImpWithTheReason` was red on Windows shard 2/7 in both merge groups (runs 42331066362 and 42332311910), with `ArgumentOutOfRangeException` out of `UnixDomainSocketEndPoint`. The lane classed it RUNNER, and it was a real path-length dependency with a product defect behind it. The test named its notify socket under its scratch directory, 113 bytes on a runner (`C:\Users\runneradmin\...`) against the limit of 107, and shorter on the gating host. Behind that, a notify path comes from `WISEOWL_COBOL_MCS_NOTIFY` with no budget, so one over the limit escaped `ConnectTo` and `MessagingPeer.Start` as a raw exception, where design 5.5 says a start failure is never one (review lead R1-4). `McsSocketDirectory.EndPointOf` is now the one place the runtime forms an endpoint (`AfUnixEndPointDriftTests`): it answers an `IOException` naming the path and the limit, before a socket exists, so nothing is left undisposed. `SocketTransport.Publish` also asks it of the final name, which it renames to after binding at a shorter one. `Start` also catches a `SocketException`, and no longer connects to the notify socket after a failed `Publish` (lead R1-5). Every test socket now lives under `<S>`, the directory the budget covers: `PlatformAssumptionTests` too, whose names had eight bytes to spare on a runner, and `AServerStartedByARequestor...`, which had two. The witnesses do not depend on this host's path: `APathNoAfUnixSocketCanHoldIsAnIoFailureOnEveryPathAndLeavesNothingBehind`, `ATransportUnderALongTempRootRefusesEveryPathAndCreatesNothing`, `ANotifyPathOverTheAfUnixLimitIsAStartFailureWithItsReasonNeverAnException`, `ASocketDirectoryWithNoRoomOnALongTempRootIsAStartFailureAndCreatesNothing` (a seam in `MessagingPeer`'s constructor stands it on a long `<S>`), `AnEndPointIsFormedUpToTheLimitInUtf8BytesAndAnythingElseIsAnIoFailure` and `APublishThatFailsTellsTheNotifySocketNothingAndTheReceiveThatNeedsARequestorIsImp`. The classes also passed under a 45-byte temp root, five longer than the runner's and the longest the budget admits. Review leads R2-1 and R2-2 (the docs and the note's sites) are fixed. The re-gate met two more of the same family, both fixed here. `MessagingPeerTests` ran first in its host, beside a Linux gate on the same machine, and two cases went red at 61 s and 91 s against a fixed 30-second hang limit. The pumps need the thread pool, and a held-back pool is the likely cause, inferred from those durations and from every later case passing in under a second; it was not measured. A limit a correct run can miss on a busy host is a defect either way: the limit is now one harness unit (`ObservationBudget.UnitMs`, kb/Work PB3108) in both classes, and a blocking statement takes its own thread. The Linux unit leg's three `PlatformAssumptionTests` kill witnesses read a line of the SDK's as the child's first (`/home/brent/.dotnet/sdk/10.0.403/Sdks/Mic...`, cut short by the assertion), because `dotnet run` writes its build's warnings to the same stdout, as a hand run under WSL showed with NU1903; `StartChild` now reads past them to `LISTENING`.

