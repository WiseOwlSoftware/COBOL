# registrar-1 — landing hand-off for the owner's LOCAL orchestrator (kb/Work PB1522 step 2)

Branch `claude/adj-registrar-1` (cut from `0caa7d5`). Land it through `bash scripts/push-main.sh`; this cloud session
wrote NO DEVLOG entry and did NOT touch plan §0 (PB1522: the local orchestrator owns both). Re-apply verdict batches with
`record_verdicts.py` on the merged tree if main moved (practice L6) — the inventory hunk is two batch files' worth:
`batch-nondoc.json` then `batch-r42.json`.

## Owner decision R42 (verbatim, given to the orchestrator 2026-09-24; `kb/Work/R42.md` lands on main separately)

> "every A.1 item that belongs to a DECLINED module (screen handling A.4.2, commit/rollback A.4.3 - docs/CONFORMANCE.md
> section 5 Not claimed) closes as DOCUMENTED-NON-SUPPORT, the same withdrawal ground as DOC-A.1-84/85/86/173 (A.1
> preamble: the item is not required if the optional feature is not implemented). In the registrar step, record such rows
> as DOCUMENTED-NON-SUPPORT with notes citing R42 + the section 5 row and a witness test that diagnoses the declined
> construct (e.g. conformance:negative/a42-options-initialize-screen, conformance:negative/apply-commit-lock-mode).
> Wave-1 rows affected: DOC-A.1-3, 4, 11, 27, 28, 41, 45, 83, 91. Apply the same to any wave-2 row of that shape instead
> of NEEDS-OWNER-DECISION. DOC-A.1-47 (FLOAT-DECIMAL) is NOT covered - it stays with PB579."

## Drafted DEVLOG paragraph content

**Title:** Cloud registrar-1 — both adjudication waves registered: 22 rows recorded (GAP 1664 → 1652), R42 implemented as
selector data, 126 A.1 rows held on an owner decision, 14 new kb/Work notes.

- **Inputs.** 11 `claude/adj-*` branches, 208 distinct rule-ids = exactly the 208 verdict-less inventory rows (wave 1: 90,
  wave 2: 118; pinned `0caa7d5`). Adjudicated histogram: DIVERGES 79 · NEEDS-OWNER-DECISION 71 · PARTIAL 31 · CONFORMS 15
  · NOT-IMPLEMENTED 12. Refuters: wave 1 applied its overturns on the branches; wave 2 attacked 9 CONFORMS rows, 8 upheld,
  1 overturned (SR-5.5-2 CONFORMS → PARTIAL, the boolean shift count, PB1413); `adj-w2-14-s1` ran no refuter (all 22 rows
  NEEDS-OWNER-DECISION).
- **Shape.** `normalize.py` fixed wave 1's shape violations at source — editions lists/prose → comma strings (prose to
  notes, 14 records), code-location commentary → notes (61 records), unresolvable `Class.Member` symbols re-sited to the
  resolving member, the computed DOC anchor added. No verdict changed, no evidence dropped.
- **Recorded.** `batch-nondoc.json` 11 rows — PARTIAL 5, DIVERGES 3, NOT-IMPLEMENTED 2, CONFORMS 1 (closed) — GAP
  1664 → 1663. `batch-r42.json` 11 DOC rows → DOCUMENTED-NON-SUPPORT (3, 4, 11, 27, 28, 41, 45, 83, 91, 172, 199), each on
  the negative golden that diagnoses its declined construct — GAP 1663 → 1652. **Total +12 closed.** No record dropped
  (rows changed = records).
- **R42 made structural (rule 4/5).** Stamping the eleven rows by hand turned `AnnexA1RegisterDriftTests` red (verdicted
  items with no §7 row, outside the audit's derived `unreachable`): the screen selector held every A.1 row out with the
  boilerplate pattern "This item is (required|optional)." and the commit selector listed DOC-A.1-28 among its excluded
  antecedents — both pre-R42 scope calls. `screen-handling-only` gained two kind-DOC arms (xref into a declined A.4.2
  clause; the SCREEN FORMAT of ACCEPT/DISPLAY/SET keyed on the item's own words, so device items 1, 2, 5, 111 stay out)
  and its text arm now excludes DOC by kind; `commit-and-rollback-only` gained a kind-DOC xref arm. They select exactly
  the eleven R42 items; the audit derives 15 withdrawn / 184 in scope with no edit; `DerivedVerdictDriftTests` pins the
  set and the four device items that must stay out; CONFORMANCE.md §7 denominator paragraph and §5 row counts updated.
- **Held (not recorded).** `batch-doc.json`, 126 DOC rows (DIVERGES 76 · PARTIAL 26 · CONFORMS 14 · NOT-IMPLEMENTED 10).
  Trial-applied and measured (`doc-trial-gate.txt`): filtered gate Failed 4 / Passed 46 — 122 computed anchors
  `docs/CONFORMANCE.md#DOC-A.1-N` resolve nowhere, 122 items verdicted without a §7 row, 14 closing on tests without a §7
  row; inventory restored. Owner question **PB1535**.
- **Owner questions.** PB1535 (how to record an A.1 determination fixed by code but absent from §7; options a/b/c,
  recommendation (c) now + (a) item by item; plus the "need not be documented but CONFORMS" sub-case: 35, 69, 104, 118,
  119, 125, 138, 169, 170, 194, 201, 220). PB1536 (Q1 conditionally-required A.1 items whose condition is absent — 149,
  152, 200 + 30, 34, 37, 142, 143, 88, 96, 98; Q2 retire FMT-5.2). Extended: PB1198 Q1 + DOC-A.1-212 (does R42's ground
  reach the declined A.3 4) MCS facility?), PB1099 Q3 + DOC-A.1-161/-162, PB468 Q9 + GR-4.4-1/-2, PB579 + DOC-A.1-47;
  PB1151, PB1255, PB1517, PB1518, PB1519 now claim their re-adjudicated rows.
- **New notes (one per mechanism).** PB1523 GLOBAL bridge duplicate/phantom → CS0102/CS1061 backend crash (MAJOR,
  crashes; contradicts the recorded GR-13.18.27.4-3 CONFORMS) · PB1524 EC-OO-RESOURCE never raised (MAJOR, crashes) ·
  PB1525 no §4.2.10 extension register / warning (MAJOR) · PB1526 integer intrinsic argument fraction truncated under
  checking, FACTORIAL(X/2+1) = 6 (MAJOR, wrong answer) · PB1527 object reference in a STRONG group: MOVE image 8 spaces
  vs §7 DOC-A.1-214, STRING aborts (MAJOR, wrong answer) · PB1528 unpositioned screen ACCEPT/DISPLAY ON EXCEPTION →
  COBOL0307 (MINOR) · PB1529 CONTINUE AFTER wraps before clamp (MAJOR, wrong answer) · PB1530 EXTERNAL ASSIGN mismatch
  compares the resolved target (MINOR) · PB1531 open EC-IMP-suffix family (MINOR, under-rejects) · PB1532 finalizer
  instance-file close on an orphan RunUnit (MAJOR, wrong answer) · PB1533 DEFINE PARAMETER env-name case (MINOR) ·
  PB1534 analysis, 41 A.1 determinations with no item note · PB1535 / PB1536 decisions. 44 owning notes extended
  (`owners_map.py`). Wave-2 leads re-probed on the Debug build before filing (PB1523, PB1525, PB1526, PB1527, PB1528;
  PB1524 by code reading); PB1529 and PB1531 re-probed too.
- **CONFORMS test-needed** (golden lane): recorded — none (SR-13.18.27.3-1 closed on its witnesses). Held in
  `batch-doc.json`: DOC-A.1-69, DOC-A.1-169, DOC-A.1-220.
- **Gate.** `work.py check` clean; `dotnet build CobolSharp.sln -c Debug` green; filtered gate
  `SpecTraceabilityInventory|DefectiveRowCoverage|DerivedVerdict|AnnexA1|WorkRegister` **50/50**; Unit
  `~Drift|~Inventory|~Schema|~Derivation` **26586/26586**; `audit_annex_a1 --self-test` all green;
  `gen_conformance_notes --check` 15 notes match; `work.py parity` no findings. The full Conformance assembly was NOT run
  (verdict/doc/schema-only change set + one test file).

## Plan §0 refresh

GAP **1652** of 4348 (was 1664). Verdict-less rows: 208 → **186** (126 DOC held on PB1535 + 60 NEEDS-OWNER-DECISION).
A.1 audit: 15 items withdrawn, 184 in scope (`python scripts/spec/audit_annex_a1.py`).

## Adjudicator / refuter output found false

- The adjudicators' brief called a MISSING §7 determination "DIVERGES"; the schema defines DOC DIVERGES as "§7 documents
  one thing and the compiler does another". 112 of the 116 CONFORMS/PARTIAL/DIVERGES DOC rows have no §7 row at all —
  the verdicts are held (PB1535) rather than recorded under a meaning the schema does not allow.
- The orchestrator's expectation that the R42 rows would leave the gate green was false as measured (above); fixed
  structurally, not by exempting the rows.
- DOC-A.1-28's adjudicated code-location named `src/Cobol.Net.Compiler/Binding/StatementBinder.cs`; the file is
  `Binding/Bound/StatementBinder.cs`. Many wave-1 code-locations carried commentary or non-resolving `Class.Member`
  symbols (normalized, originals kept in notes).
- The prompt's "GR-13.18.27.4-3 recorded CONFORMS" is right but that CONFORMS is contradicted by PB1523's `rr.cob`
  (the program in which B resolves does not compile); the row is claimed by PB1523 for re-verdict.
