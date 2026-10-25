## Entry 2524 — 2026-10-24 23:04 PDT — A gate record older than the lander is named as such, and a merge-queue bounce's reds reach tier 0a (PB3660, PB3644)

Two process-tooling defects in the files the gate and the landing share (`scripts/run_gate_legs.py`, `scripts/gate_plan.py`,
`scripts/orchestrator/enqueue.py`, `scripts/orchestrator/queue_facts.py`), no COBOL, grammar or runtime change.

**PB3660, the lander misnamed an older gate's record.** PB3612 added `head_tree` and the Linux half (`linux`) to
`verdict.json` without bumping `RECORD_SCHEMA`. The record is written by the BRANCH's driver and read by main's lander, so
a branch gated before #516 carried a record with neither field; `tested_locally` folded "no `head_tree`" into the dirty-tree
arm, reported "uncommitted changes", and the BLOCKED line told a finisher to gate the tip again, which the branch's own
older driver answers with the same record (PB2974's landing, 2026-10-24). Re-probed with a planted schema-3 record without the
two fields: the reason read "uncommitted changes". The fix is at the one place that owns the record:
`run_gate_legs.RECORD_SCHEMA` is 4, `LANDABLE_SCHEMA` (3) and `LANDING_FIELDS` (`head_tree`, `linux`) say what the lander
requires, and `predates_landing(rec)` is the one judgment. Schema 3 was written both before and after PB3612 (that was the
defect), so for schema 3 the fields themselves decide: a bump to a required 4 would have refused every branch gated since
#516, which I caught before writing it. `tested_locally` now says "written by a gate older than this lander" (`Tested.stale`),
leaves "uncommitted changes" to `tree != head_tree` alone, and the BLOCKED text prescribes merge origin/main first, then gate
the merged head (`REMEDY_OLD_GATE`), when the newest GREEN record is old. The self-tests pin the pairing (RECORD_SCHEMA >=
LANDABLE_SCHEMA, every landing field a key the driver writes, a driver record current for the lander) and cover the old,
the schema-3-with-fields, the schema-2, the dirty and the RED arms plus the two BLOCKED texts. Sweep: no other reader
turned an absent field into a failure of another kind.

**PB3644, a bounce's reds never reached tier 0a.** A branch the merge queue bounced fails in GitHub's CI run, and
`gate_plan.previous_gates` read only local trx files, so the finisher's re-gate planned the failed cases by impact. Probed
on the real repository: a merge-group run is on `gh-readonly-queue/main/pr-<n>-<sha>`, a failed one keeps `test-results-*`
artifacts (run 40161989736: 15 trx, 195 MB), and `gate_plan.read_reds` found its 3 reds in 1.3 s. `queue_facts` (the one
GitHub reader) gained `fetch_bounce_results`: the newest `merge_group` run of the branch's open pull request, and when it
failed (a cancelled group is no bounce) its test artifacts into `TestResults/ci-bounce/` (`gate_plan.CI_BOUNCE_RESULTS`),
once per run, emptied when there is no bounce, left as it was when GitHub cannot be asked. `previous_gates` reads that
directory deep as its third source, and `Host.fetch_bounce_results` fills it before every gate's plan. It orders the gate,
never selects it. Arms in `queue_facts` (9), `gate_plan` (2) and `run_gate_legs` (3).

Docs: `DESIGN-test-build-ci.md` (tier 0a's sources, the record's schema and the lander's reading), the finisher brief.
Self-review: the `queue_facts` fetch has no wall-clock dependence; the 200 MB download happens only for a branch with an
open pull request whose newest merge group failed. No inventory row is claimed or closed.

