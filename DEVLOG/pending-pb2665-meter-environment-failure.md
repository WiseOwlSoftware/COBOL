## kb/Work PB2665: an unreadable meter is an environment failure, not a failure of the work

Since 2026-10-12, 39 of 53 meter units failed (36 on "Browser extension is not connected"). Each failure
cost more than its own $0.36: the supervisor synthesized a `resume` handoff for the dead meter (a $0.56 to $1.58 unit
that re-derived branch heads nobody had touched), overwrote the work chain's last handoff with it, and counted the
failure as one of the breaker's three, which the next `done` unit reset to zero. A meter that failed on every cycle
printed `FAILURE 1 of 3` for ever, ran every few minutes (the reading stayed stale), and asked the owner for nothing; the
one time it did trip the breaker, the loop and its landing lane stopped for about 352 minutes.

**Re-probe.** Defect 1 of the note, `next_unit.py` deferring the `land` its handoff named behind a dead lander's lease,
no longer exists: the landing lease and the unit `land` were deleted by PB3024's closing change (24af80121), and a stale
meter with a handoff naming `enqueue` chooses `enqueue`. Defects 2 and 3 reproduced on this tree (`checkpoint.py
synthesize --unit meter` names `resume`; the supervisor copied it over `handoff.last.json`).

**The fix is one classification.** `orchestrate.ps1` declares `$ReadOnlyUnit = @('meter')`: a unit that reads the
environment and touches no branch is invisible to the work chain. Its handoff never replaces `handoff.last.json`
(`Clear-NamedUnit` only spends the `next_unit` the last handoff named it by, so a named meter does not repeat for
ever), nothing is synthesized for it (a leftover checkpoint at start included), and it moves none of `$failures`,
`$lastFailed` and `$lastStarted`. Ending without a `done` handoff, valid or not, is `environment_failure` on its
`units.jsonl` line while `failed` stays false. `next_unit.py` rule 3 counts those lines per unit type and per account
(`meter_failure_run`): three in a row, whatever ran between them, close the meter until a maximum reading age after
the newest attempt (`meter_unreadable`); the choice then carries `meter_unreadable {failures, since, retry_at}` and
`Report-MeterUnreadable` asks the owner once per streak (the streak's first attempt is the marker inside the question,
so a supervisor restart asks nothing more). A name from a handoff or the operator's steer still runs the meter, and the
loop meanwhile goes on from `budget.py`'s estimate. This is the same pattern as PB3673 and PB3674, an environment cause
read as a work failure, applied to the loop's unit accounting.

**Found in the same file (I9).** The supervisor never removed `handoff.json` after a unit, so the next unit's start
archived every finished handoff as a hand-written orphan: 31 false "no unit of this run wrote" lines in one log. It is
now archived beside the log and removed. The design's file table also still named the landing lease as a user of
`coord.locked`; it is the landing lane's records.

**Tests.** `test_next_unit.py` (new, 16 checks: the gate on planted `units.jsonl` lines, interleaved units, a read that
breaks the run, another account's attempts, the reopening and the same-`since` re-closing, explicit names outranking
the gate) and `test_orchestrate_meter.ps1` (new, 20 checks, with a `meterfail` mode in the fake CLI: three failed meters
then the next rule's unit with one owner question, no synthesized resume, three instant crashes, a carried breaker count
of two neither reset nor raised by three meters and tripped by the work chain's own crash, a meter that read leaves the
wave's handoff and spends its name, no false orphan). The other fourteen `test_orchestrate_*.ps1` parts stay green. The
design (`DESIGN-orchestrator-loop.md` sections 3.2, 4, 4.5, 5.1, 12) describes the new rules.

The note flips to landed with `closes_rows: []` (orchestrator tooling; no ISO rule, no inventory row).
