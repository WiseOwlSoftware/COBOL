## Entry 3527 — 2026-11-12 10:41 PST — PB5371: the fuzz ledger's date of a night is a recorded fact, not a slice of its run id

The published fuzz ledger said "The totals below run from 2026-10-20 to batc-h--30" and listed Night 10 as started on
`batc-h--30`. `fuzz_history.night_date` cut characters 5 to 12 out of the run id and formatted them as a date. A run id is
the run directory's name; the harness names it `fuzz-<yyyymmdd>-<hhmmss>` by default, but Night 10's directory was named by
hand, `fuzz-batch-3000-20261103`, and its slice is `batch-30`. Ledger versions 24c8d12f80a5 and 1485221e1e53 both carried
it. The Mythos session found it on 2026-11-12 while reading the page before publishing it; the owner asked for the fix.

The fix makes the date a field of the record. `run_stats` writes `date`, the local date of run.json's `started` on the host
that records the night (`local_date`; run.json writes UTC, whose date is the next day for eight of the ten nights).
`night_date` returns it and stops the render on a record without one. `unrecorded_runs`, which finds a run that has no
record by the harness-given name, takes the date from that pattern's own date group. The ten records of
`conformance-suite/fuzz-history.json` carry `date`; each equals the date in its run's name. `FUZZ-LEDGER.md` is
regenerated (version 5c32a0aba63c): two lines and the stamp change.

`test_fuzz_history.py` gained the DATE arms: the date follows the zone and not the name, a hand-named run shows its
recorded date, a start time with no zone and a record with no date are refused, and every record of the tracked history
carries a date within a day of its start. The tracked-history arm was run before the backfill and stopped on night 1.

The sibling sweep found one more assumption of the default name: a finding from an unrecorded run with a hand-given name
is left off the page. It needs the run stated in the note's frontmatter, a design of its own, filed as PB5372 (open).
`docs/rearchitecture/DESIGN-differential-fuzz.md` states both where it describes the history.

