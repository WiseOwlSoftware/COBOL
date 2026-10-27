## PB3838 tier 1: an `upstream` note kind records another project's defects to a checked bar

**Why.** GnuCOBOL's departures from ISO and its defects, which our two differentials find every night, lived as counted prose in
evidence documents; the witness programs sat in git-ignored run directories on one machine. The owner asked (2026-10-26) for
records detailed enough to file an issue or a patch later: a minimal repro, the exact `cobc` command, the observed result, a
checked ISO citation. Tier 1 of the PB3838 plan is the register that holds them.

**What landed.**
- `scripts/spec/work.py`: `kind: upstream` joins `KINDS`. `upstream_problems` (called for every note by `register_problems`, so
  by `work.py check`, the gate's audit phase and CI's `audits` job) holds a record to the eight-item bar as far as a note alone
  can show it: `upstream_project`, `upstream_version`, `upstream_class` (six classes), `upstream_edition` (an edition-2023-only
  behaviour must be `feature-missing`, never a bug against 3.2), `upstream_status` with `upstream_ref` required exactly when the
  state is not `unreported`, a non-empty `spec_refs`, a terminal `status` exactly when the matter ended upstream, the eight body
  sections non-empty, and no fenced block over 60 lines (the GPL guard: a longer block is how testsuite text gets committed).
  An `upstream_*` key on any other kind is refused.
- The record is kept out of every list of OUR work by one rule rather than by teaching each reader the kind: it must be
  `process_only: true` and carry no `cluster`, `tier`, `blocked_by`, `inventory_rows` or `closes_rows`. `actionable`, `next`,
  the planner's lanes, the ledger's compiler-work counts and the fuzz cadence's open-note rule (`open_fuzz_notes`) already read
  those fields and none reads `kind`. The harm flags of a record describe the GnuCOBOL user and may be set freely.
- `kb/Work.base`: an `Upstream records` view and the upstream properties; `Open but no harm flag set` excludes the kind;
  `check_base_agrees` holds all three (and derives the property list from `UPSTREAM_KEYS`, so a new field is automatic).
- Drift test: `work.py --self-test` now runs `upstream_findings` (a well-formed record passes; 14 ways of failing the bar and a
  stray `upstream_*` key on a defect each name the rule broken; a record is not actionable and not in a cluster). Falsified by
  replacing `upstream_problems` with a stub: the self-test reports the cases it then fails. `differential_fuzz.py --self-test`
  gained an open, fuzz-tagged upstream record in the cadence fixture: it does not hold the next run.
- `CLAUDE.md`, `docs/DOC_INDEX.md` and `README.md` list the fifth kind; `kb/Work/PB3838.md` carries the record format and its
  Sites for this change.

**What the re-probe showed.** Re-probe: at the base `KINDS` had four members (`git show HEAD:scripts/spec/work.py`), so an upstream-shaped note failed
`work.py check` ("kind not in KINDS"). No record is filed by this change; the harvest (tier 2) comes first, and a
finding that cannot meet the bar stays in the harvest.

**Self-review.** Architecture: one rule (`process_only` plus no campaign fields) in `upstream_problems`, no second reading of
the kind in the planner. Duplication: the bar's section and class vocabularies are module constants read by the check, the
fixture and the Bases check. Gap noted for the next tier: `check` cannot run `cite.py --check` on `spec_refs` (it validates the
spelling only); the harvest's refuter does that before a record is filed.
