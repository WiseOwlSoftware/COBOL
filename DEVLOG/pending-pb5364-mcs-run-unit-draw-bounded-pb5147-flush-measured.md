## PB5364: the MCS run unit draw is bounded, and a session with no liveness byte answers 92; PB5147: every CM102M run records the volume's flush time

**PB5364 (landed).** `McsStore.DrawRunUnit` drew a random 60-bit run unit id and redrew it for as long as the non-waiting hold
of its liveness byte answered `Held`, taking `Held` to mean a 2^-60 collision. The lock-file host (macOS and the other Unixes)
answers `Held` for every byte of a lock directory this user may enter but not list. On that host the `McsStore` constructor
spun forever, where the first statement should have answered I-O status 92. The re-probe on this Windows host forced a region
host that gives those answers. The unfixed draw asked for 10 000 bytes before the witness's counter threw. The draw now tries
at most four ids, since a second `Held` in a row is no collision. It answers 0 when the host answers `Unavailable` or `Held`
every time, and `Statement` then refuses every statement with 92 before it reads anything. A session without its liveness
byte cannot own a partly received message, because every other run unit would read the owner as dead. This is the other half
of the design's rule that an `Unavailable` lock host answers 92 (DESIGN-communication-facility.md §6, updated). The witnesses
are `MessageControlTests.HostThatAnswersEveryLivenessByteHeld_IsStatus92_ForEveryStatement_AndDoesNotHang` (every OS; SEND,
ACCEPT MESSAGE COUNT and RECEIVE answer 92 and the files stay byte for byte unchanged) and
`UnlistableLockDirectory_AnswersALivenessByteHeld_AndTheWaitingMutexUnavailable` (Unix; the real lock-file host over a mode-0300
directory gives the answers the forced host models). The sibling sweep found the other draws on a region-lock byte already
bounded: `KeyedStore.Draw` and `HandleColumnLockHost.Publish`.

**PB5147 (step, still open).** No merge-group red has named CM102M in the 161 runs since PB5154's 210 s budget landed. The
stall is still there, though, absorbed by the budget. CM102M's duration in the trx artifacts of the 40 latest runs is 90.4 s to
91.3 s on Linux (30 runs) and 93.1 s to 121.1 s on Windows (24 runs, 5 of them over 100 s), against a 90 s wait on the
program's own clock. A run beside four `fsync` loads on this host's SSD did not slow down (93 s). The test now writes the run
volume's durable flush time to its output on every run, so the next miner reads flush time next to duration in each merge
group's `Conformance.trx` and can confirm or rule out the flush hypothesis.

Self-review: architecture (one rule: no liveness byte means 92, at the one entry of every statement), full code (the bound's
reasoning is written at the constant; `Unavailable` now refuses through the same check rather than relying on the mutex),
performance (the draw is at most four non-waiting holds, once per session), and duplication (the bounded-draw shape matches
`KeyedStore.Draw`'s; no second copy of the 92 rule).
