**Addendum (2026-11-10): branch review round 1 of `selftest-cost`, findings R1-1 and R1-2.** The reviewer's records are
`E:/COBOL-coord/scratch/review-w20261995-a.jsonl`; its probes are `probe_e2e.py` and `probe_e2e2.py` under
`E:/COBOL-coord/scratch/review-w20261995-a/`.

**R1-1 (medium): a parseable Python file carrying a date was outside the clock key.** `_handles_time` applied DATED_RE
only to Python that does NOT parse, though the note's key list says every file carrying a date is in it. *Scenario*
(`probe_e2e.py`): `consts.py` holds `EXPIRES = '<today+900d>'` and imports nothing; the self-test passes while today is
before it. Edited to today+100d, the `--reuse` run was GREEN, `clock proof reused`, and a fresh run RED. *Decision:* fix.
*Fix:* `dated_literal(tree)`: a string or bytes literal of the module's CODE that DATED_RE matches (an f-string's text
included) puts the file in the key. *Decided beyond the brief:* a comment and a docstring are not code, as
`reads_wall_clock` already treats them. This repository's prose carries provenance dates (`owner 2026-11-10`) in
almost every script, and matching the whole source would have put 142 of the 243 Python files under `scripts/` in the
key, against 55 before. *Cost:* by date literals in code alone, 38 of 320 tracked Python files enter the key, among them
`scripts/inert_lane.py` (its self-test labels and messages quote dates). So an edit to `inert_lane.py` again re-runs the
shifted runs of the self-tests that read it (`gate.py`, `enqueue.py`, `land_lane.py`): that part of the saving measured
above is withdrawn, the price of a key that does not miss a date. *Proof:* `keys_proof` arms for a constants module with
a date literal (True), a date in a bytes literal and an f-string (True), and dates only in a comment and docstrings
(False). A `self_tests.py` arm replays the scenario: moving `helper_plain.EXPIRES` inside the shift re-runs both runs
under `--reuse`, names the file, and is RED. The reviewer's probe now: `2 --reuse, near date: ('RED', 're-run: 1
input(s) changed since the newest PASS — scripts/consts.py')` (was GREEN, `clock proof reused`).

**R1-2 (medium): a helper that compares time values handed to it is outside the key, and the note said otherwise.**
"Why a detector by import, not by call" above argues the import rule catches a helper that receives `now`. A helper
that receives both the reading and the date (`def ok(today, expires): return today < expires`) imports nothing, reads
no clock and holds no date. *Scenario* (`probe_e2e2.py`): editing such a helper from `return True` to the comparison
reuses the proof under `--reuse` (GREEN) while a fresh run is RED. *Decision:* this class cannot be seen without
dataflow analysis, and a list of time-sounding identifiers would be a hand-kept heuristic. The residue is ACCEPTED, and
the import argument above is corrected here: the key is built from syntactic signs (a time import, a clock read, a date
literal, the shift variable). It never finds a module that only computes with time values it is handed.
*The residue, as it now reads:* a module with no time import, no clock read and no date literal that does arithmetic or
comparison on time values handed to it (a plain-number window such as `RETAIN_DAYS = 90` is the same class); a date in a
data file in a form DATED_RE does not match; the names in a directory listing. *What bounds it:* the unshifted run still
runs on every re-run, so today's answer is always tested, and only the 400-day-ahead answer is reused. Every CI run (the
merge group's Windows self-tests, class (d) of CLAUDE.md's Testing rule, and the nightly) runs both runs fresh, so such a
change is caught before main, at the price of a bounce. Stated in `self_test_clock.py`'s KEY RESIDUE, `self_tests.py`'s
module docstring and DESIGN-test-build-ci.md §3.14.10 ("The clock key"). *Proof:* `self_tests.py`'s arm
`KNOWN RESIDUE, PINNED (review R1-2, kb/Work PB5226)` replays the scenario and asserts the reuse (GREEN, shifts `0`,
`clock proof reused`) and the fresh RED (shifts `0 400`): a change that closes the gap must update it. A
`self_test_clock.py` `keys_proof` arm pins the helper as outside the key. The reviewer's probe still reads `2 --reuse,
helper compares: ('GREEN', '... clock proof reused ...')` / `3 fresh, helper compares: ('RED', '')`, as accepted.

**Verdicts at this change:** `python scripts/self_test_clock.py --self-test` → `ALL GREEN`;
`python scripts/self_tests.py --self-test` → `ALL GREEN`.
