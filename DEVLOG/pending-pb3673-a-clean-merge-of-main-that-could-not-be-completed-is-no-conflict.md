## PB3673 — a clean merge of main that could not be completed is no conflict: the pin settle's failure carries its class, the landing ends it exit 2 or exit 5 instead of sending a finisher to resolve nothing; PB3748 discharged and pinned

**PB3673 re-probed.** `merge_drivers.merge` still returned `[abi_pin.PIN_PATH]` for a settle that failed, and
`enqueue.land` turned every non-empty answer into `CONFLICT in …RuntimeAbi.pin`, exit 4, a `BLOCKED` report line and a
finisher (about 100 k tokens and a re-gate) for a merge with no conflict. Two witnesses in the note (a locked
`SourceGen.dll`, PB3630 and PB3664) were environment causes; the same collapse also hid three sibling arms of `merge`:
the refusal of the fast-forward, of the merge with no file in conflict, and of the merge commit all answered `["?"]`, a
conflict in a file named `?`.

**Fix shape (one classification, extended).** The gate's `run_gate_legs.NOT_RUN_CAUSES` already classifies a cause by who
can fix it (ENVIRONMENT: retried once, then the operator's; BRANCH: a finisher's). It gains two rows, `merge-blocked` and
`merge-failed`. `abi_pin.settle` and `run_measurement` return `Failure(cause, why)`; the class is read from the WHOLE
output of the failed build (MSB3027, MSB3021, a held file, a full disk, a command that cannot start), because the message
keeps only the last 40 lines and the lock error can sit above them. `merge_drivers.merge` returns a `Merged(conflicts,
incomplete, in_progress)`; `enqueue.land` ends an ENVIRONMENT cause exit 2 (`MERGE NOT COMPLETED`, no report line) and a
BRANCH cause exit 5 (one `BLOCKED` line with the cause), at both merges of main (step 2 and the catch-up). The `merge`
command ends exit 3 with `NOT COMPLETED [<cause>]`, and `--keep` leaves a merge stopped at the pin in progress for
`abi_pin.py settle --stage`. The finisher brief, DESIGN-test-build-ci.md (3.14.3, 3.17.2) and DESIGN-orchestrator-loop.md
(the lane's exit table) say so. The three lazy `import abi_pin` in `merge_drivers.py` are one top-level import and
`abi_pin_path()` is gone.

**Goldens.** Self-test arms in `abi_pin.py` (the class of each cause, the lock error above the tail, a malformed owned file
never reaches the measurement), `merge_drivers.py` (the settle failure is a `Failure`, not a conflict, branch unchanged;
`--keep` leaves it in progress) and `enqueue.py` (exit 2 with no report line, exit 5 with one line, the tags held to
rows, the catch-up merge); `LandingPipelineDriftTests` and `MergeDriverDriftTests` declare them.

**PB3748 discharged.** `covered_by_driver` matched from the right only until PB3727 (`1cc737801`, the same day) replaced it
with `pattern_matches`, which anchors a pattern with a `/` at the root; the note was written against the older tree. The
`patterns:` arm now asserts the nested paths (`x/kb/Work/PB1.md` is not covered), the pin the note asked for.

**Not done here.** PB2665 (the loop's meter unit and `orchestrate.ps1`'s breaker read an environment cause as a unit
failure) is the third member of the family the note names and its own note.
