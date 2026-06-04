# CobolSharp — Plan to 100% COBOL-85 (ISO 1989:1985) Compliance

Status: 2026-06-04. Guard GREEN: 1040 unit / 347 integration / **360 NIST baselines** of 459 programs.

## 1. What "100% COBOL-85" means here

COBOL-85 is defined as a set of functional **modules**. Compliance = every module implemented + validated.
The **NIST CCVS85** suite (459 programs in `tests/nist/programs/`) is the official validation vehicle; passing
every in-scope program at 0 `FAIL*` (plus correct *flagging* of non-conforming constructs in the `…M` modules)
is our primary compliance signal. Where the spec defines behaviour the NIST suite under-exercises, we author
additional test cases (`tests/nist/extra/` + integration tests) so each spec feature has a passing test.

## 2. Module status (the real gap)

| COBOL-85 module | NIST prefix | Present | Baselined | Status |
|---|---|--:|--:|---|
| Nucleus | NC | 95 | 93 | ✅ COMPLETE (NC214M non-det, NC303M flag — excluded) |
| Sequential I-O | SQ + OBSQ | 89 | 86 | ✅ COMPLETE (SQ303M/401M flag) |
| Relative I-O | RL | 35 | 32 | ✅ COMPLETE (RL301M/401M flag; RL212A producer) |
| Indexed I-O | IX | 42 | 40 | ✅ COMPLETE (IX301M/401M flag) |
| Inter-Program | IC | 47 | 23 | ◐ all standalone callers done; ~21 callee-halves + IC116M/117M/118M/401M flag — **verify tail** |
| Sort-Merge | ST | 40 | 29 (+10 prod) | ✅ COMPLETE (ST301M flag) |
| Source Text (COPY/REPLACE) | SM | 17 | 15 | ✅ COMPLETE (SM301M/401M flag) |
| Intrinsic Functions | IF | 45 | 42 | ✅ COMPLETE (IF401M/402M/403M flag) |
| **Report Writer** | RW | 6 | **0** | ✗ UNIMPLEMENTED (all COMPILE_FAIL) |
| **Communication** | CM | 9 | **0** | ✗ UNIMPLEMENTED — needs a Message Control System (obsolete in COBOL-2002) |
| **Debug** | DB | 15 | **0** | ✗ UNIMPLEMENTED (all COMPILE_FAIL) |
| **Segmentation** | SG | 13 | **0** | ✗ UNIMPLEMENTED (all COMPILE_FAIL) |
| Obsolete-feature variants | OBNC/OBIC | 5 | **0** | ✗ UNIMPLEMENTED |
| EXEC85 driver | EXEC | 1 | **0** | ✗ non-standard test driver |

**Conclusion:** the 8 "core" modules are complete; *everything remaining is the four unimplemented modules
(RW, CM, DB, SG) + obsolete-feature variants + the IC callee-half verification.* The non-baselined programs in
the ✅ suites are all excluded-by-design (flagging `…M` modules that emit no CCVS report, `NO_OUTPUT` producers,
non-deterministic ACCEPT FROM DATE/TIME, and `PROCEDURE DIVISION USING` callee halves).

## 3. Workstreams

Each workstream follows the proven model: **parallel design → worktree-isolated parallel implementation →
sequential guard-gated integration onto `main`**. NIST programs validate; additional tests fill spec gaps.

- **WS-DB — Debug module** (DB, 15). `USE FOR DEBUGGING`, the `DEBUG-ITEM` special register, debugging lines
  (`D` in col 7), `WITH DEBUGGING MODE`, `>>DEBUG`/COMPILE-time vs runtime. Medium. Self-contained tests.
- **WS-RW — Report Writer** (RW, 6). REPORT SECTION / `RD`, report groups (TYPE, LINE, COLUMN, SOURCE, SUM,
  VALUE), `INITIATE`/`GENERATE`/`TERMINATE`, control breaks, `PAGE`/`LINE-COUNTER`/`PAGE-COUNTER`, `GROUP
  INDICATE`. Large but well-defined; a dedicated `ReportWriterRuntime`. Self-contained tests.
- **WS-SG — Segmentation** (SG, 13). Section segment-numbers (priority 0–99), `SEGMENT-LIMIT`; on a no-overlay
  target the key semantics are independent-segment (≥50) re-initialization on each entry and the ALTER/PERFORM
  interactions. Small-medium. Self-contained tests.
- **WS-CM — Communication** (CM, 9) *[scope-gated, see §4]*. `CD` (communication description), `SEND`/`RECEIVE`/
  `ENABLE`/`DISABLE`/`PURGE`, message control. Requires a synthetic in-process Message Control System (the spec
  makes the MCS implementor-defined); obsolete in COBOL-2002. Large + infrastructure.
- **WS-OBS — Obsolete-feature variants** (OBNC/OBIC, 5). Obsolete-but-COBOL-85 elements (ALTER, GO TO without a
  procedure-name, paragraph-name segmentation, etc.) exercised in the OB* tests. Small.
- **WS-IC — Inter-Program tail** (≤24). Verify each non-baselined IC program: confirm callee-halves are genuine
  `NO_OUTPUT` callees (excluded) vs any standalone caller that should baseline; fix + baseline the genuine ones.
- **WS-SPEC — Spec-feature audit + extra tests.** Walk the ISO COBOL-85 spec module-by-module; for each feature/
  option NIST does not exercise on a *passing* path, author a focused test (`tests/nist/extra/` or an integration
  test) and make it pass. Closes the gap between "NIST-validated" and "spec-implemented."
- **WS-FLAG — Flagging conformance.** The `…M` modules verify the compiler correctly *flags* obsolete/
  non-conforming constructs. Audit each against `DialectStrictnessChecks`; add the missing diagnostics so the
  flag tests are satisfied under the strict dialect (they emit no CCVS report, so success = correct diagnostics).
- **WS-DASH — Compliance dashboard.** A `scripts/compliance.sh` that reports per-module NIST pass% + a
  spec-feature checklist (from §2 + WS-SPEC), so "% to 100%" is always measurable and regressions visible.

## 4. Scope decision (the one branch point — see the question posed alongside this plan)

COBOL-2002 marked **Communication, Segmentation, Debug, and Report Writer as obsolete**; the owner's earlier
"operational" decision (DEVLOG 301) excluded CM/SG and deferred DB/RW. A literal "100% COBOL-85" re-opens them.
The branch is mainly **Communication** (needs a synthetic MCS to run SEND/RECEIVE meaningfully) and, to a lesser
degree, Segmentation. DB and RW are full, testable modules and are planned in regardless.

- **Tier 1 (planned in):** RW, DB, IC-tail, OBNC/OBIC, WS-SPEC, WS-FLAG, WS-DASH.
- **Tier 2 (recommended):** SG (segment-priority semantics — tractable + testable).
- **Tier 3 (literal-100% only):** CM (synthetic MCS), EXEC85.

## 5. Execution & measurement

- **Parallelism:** within a module, independent features are separate worktree agents; across modules, separate
  workstreams. Integration onto `main` stays sequential + guard-gated (the proven pattern).
- **Definition of done per module:** every in-scope NIST program baselined at 0 `FAIL*` and non-vacuous; each
  spec feature has a passing test (WS-SPEC); the guard stays ALL GREEN.
- **100% = ** every Tier-1/2 (and Tier-3 if elected) NIST program baselined + WS-SPEC checklist fully green +
  WS-FLAG diagnostics correct, with `scripts/compliance.sh` reporting 100%.
