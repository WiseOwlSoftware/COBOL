## A lock-file host's exhausted publication no longer hides a conflicting holder (PB3632)

PB3518 fixed the Windows handle host: a publication whose every try collides is a failure to publish, not a finding about
the host, so the request is still refused ('61') beside a column Table 19 refuses (§14.9.27.4, §9.1.13.9 1)). It left the
lock-file host's question open. `SidecarLockFiles.Publish` answered a bare null for two different causes: the host takes no
lock here (the probe is not refused, or the physical file's directory is not there), and every one of the twelve tries lost
its race. Its two callers read null as Unavailable, so `SidecarColumnLockHost.Establish` granted an OPEN a held READ ONLY
INPUT connector refuses, and `SidecarRegionLocks.Hold` (the sibling arm, found on the way) answered Unavailable for a record
lock or store mutex another run unit held, which its callers read as a host with no such lock.

**The fix is the result type, not a flag at each caller.** `Publish` now returns the `RegionLockResult` vocabulary the
handle host's publication loop already uses (`Ok` with the holder in an out parameter, `Unavailable`: no lock here, `Held`:
every try lost its race). `SidecarColumnLockHost.Establish` takes the exhausted case down the arm a lock directory this
user may not write already took (PB2917), now one method, `Unpublishable`: test the refused columns, the asker's own
superseded file left out, `Refused` when another holder is seen, `Unavailable` otherwise. `SidecarRegionLocks.Hold` looks at
the holders after an exhausted publication: a live conflicting one is `Held` (and a waiting hold goes on waiting for it, as
it does after a collision that followed a successful publication), an unobservable one is as everywhere in that method, and
with nothing in the way the hold is Unavailable. The journal's `PublishHolder` seam keeps its null-or-holder shape: a
transaction's holder that cannot be published fails the statement ('30') for either cause.

**Re-probe.** The note's claim reproduced by test, not by hand: the new witnesses were red on the old arms (Expected
Refused, Actual Unavailable; the region one answered Unavailable at the first exhausted round) and green on the fix. The
lost races are driven through a new test seam, `SidecarLockFiles.RaceInjector`, beside `PauseObserver` (the same
`AsyncLocal` shape), because a directory mid-`rmdir` cannot be held open on demand. No COBOL program reaches this host on
Windows or Linux (the lock-file host is the one macOS and the other Unixes use), so there is no conformance golden; the
unit witnesses run on every host and the Linux half of the gate runs them in WSL.

**Witnesses.** `SidecarColumnLockHostDriftTests.AnExhaustedPublicationBesideAHolderTable19RefusesIsRefused_AndOneNothingRefusesIsUnavailable`
(a first request, a superseding request, nothing refusing, no pending file left behind) and
`RegionLocksDriftTests.AHoldWhoseEveryPublicationTryLostItsRace_IsHeldByAConflictingHolder_AndWaitsForIt` (no-wait Held,
shared beside shared Unavailable, a waiting hold granted once the holder lets go). The existing publication witnesses now
assert the result (`Held` for a race that never ends). `DESIGN-runtime-library.md` 2.2.1 carries the rule beside PB3518's.

**Self-review.** Architecture: one mechanism per job (the result type tells the causes apart; each caller decides). Sibling
sweep over every `Publish` caller (column host, region locks, journal medium) and the third column host (`OfdColumnLockHost`
has no publication loop: its shared hold is one try whose Held is already Refused). Silent failure: no catch widened;
`Unpublishable` inherits the host's existing exception-to-Unavailable mapping. Row closure: none (the note claims no inventory row).

**Review round 1 (R1-1, a flaky witness).** The region witness let the holder go from a second thread as soon as the first round began, so a release that landed before the look after the lost round made a WAITING hold answer Unavailable (measured 3 reds in 128 parallel runs): the same interleaving a real holder makes when its rmdir of the last file loses the races. Fix at the root in `SidecarRegionLocks.Hold`: a waiting hold that finds nothing in the way after a lost round publishes one more round before it answers Unavailable (bounded: a second lost round with nothing in the way is Unavailable). New deterministic witness `AWaitingHoldWhoseHolderLetGoDuringTheLostRound_IsGrantedOnTheNextRound` (the injector releases the holder at the round's first attempt, one thread); the thread-driven witness stays and now passes either way (64 parallel runs, 0 reds). R1-2 (the journal's "host cannot hold" text for both causes) sits in a file this change does not touch and is left as a lead.
