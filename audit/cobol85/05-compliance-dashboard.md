# COBOL-85 Compliance Dashboard

**Date:** 2026-03-30 | **Mode:** DRY-RUN (read-only) | **Auditor:** Claude Opus 4.6
**Evidence sources:** `audit/cobol85/unit.trx`, `audit/cobol85/integration.trx`, `audit/cobol85/source-snapshot.txt`, `audit/cobol85/dotnet-build.log`, NIST baselines, `GRAMMAR_AUDIT.md`

---

## Metrics

| Dimension | Metric | Value | Source |
|-----------|--------|-------|--------|
| **Build** | Warnings | 0 | dotnet-build.log |
| **Build** | Errors | 0 | dotnet-build.log |
| **Unit Tests** | Total | 922 pass, 0 fail, 0 skip | unit.trx |
| **Integration Tests** | Total | 287 pass, 0 fail, 1 skip | integration.trx |
| **Integration Tests** | ControlFlow | 73 tests | integration.trx classification |
| **Integration Tests** | Data | 43 tests | integration.trx classification |
| **Integration Tests** | FileIO | 29 tests | integration.trx classification |
| **Integration Tests** | Intrinsic | 29 tests | integration.trx classification |
| **Integration Tests** | Arithmetic | 24 tests | integration.trx classification |
| **Integration Tests** | Condition | 24 tests | integration.trx classification |
| **Integration Tests** | SubscriptRefMod | 22 tests | integration.trx classification |
| **Integration Tests** | Call | 13 tests | integration.trx classification |
| **Integration Tests** | String | 11 tests | integration.trx classification |
| **Integration Tests** | SortMerge | 3 tests | integration.trx classification |
| **Integration Tests** | Skipped | `Subscript_ExpressionSubscript_Multiplication` | integration.trx |
| **Grammar** | Files / lines | 14 files, 3,559 lines | source-snapshot.txt |
| **Grammar** | COBOL-85 gaps closed | ~82 of ~138 (~59%) | GRAMMAR_AUDIT.md |
| **Grammar** | COBOL-85 gaps remaining | ~56 | GRAMMAR_AUDIT.md |
| **Binders** | Classes | 9 | source-snapshot.txt |
| **Binders** | Checklist items evidenced | ~85 of ~100 (~85%) | 02-binder-coverage-checklist.md |
| **Semantic Rules** | Implemented + tested | ~24 of ~37 audited (~65%) | 03-semantic-rules-audit.md |
| **Semantic Rules** | Not implemented | 2 (PERFORM overlap, record locking) | 03-semantic-rules-audit.md |
| **Semantic Rules** | Unclear | 9 | 03-semantic-rules-audit.md |
| **NIST NC** | Programs passing | 95/95 (with FAIL*) | NIST baselines |
| **NIST NC** | Clean tests (0 FAIL*) | 81/95 (85%) | NIST baselines |
| **NIST NC** | FAIL* locked | 77 across 14 tests | NIST baselines |
| **NIST non-NC** | Baselines | 0/364 (0%) | source-snapshot.txt |
| **NIST total** | Coverage | 95/459 (21%) | source-snapshot.txt |
| **Diagnostics** | Registered codes | 188 | source-snapshot.txt |
| **Runtime** | Source files | 18 .cs files | source-snapshot.txt |
| **Runtime** | I/O handlers | Sequential, Indexed, Relative | source-snapshot.txt |

---

## Narrative

### Where Evidence Is Strong

The compiler has solid coverage of the COBOL-85 Nucleus module, confirmed by both the
integration test TRX data (287 pass, 0 fail) and the NIST guard (95 NC programs compile
and run). The build log shows zero warnings and zero errors. The integration.trx reveals
a well-distributed test suite: 73 control flow tests (largest category), 43 data tests,
29 file I/O tests, 29 intrinsic function tests, 24 arithmetic tests, 24 condition tests,
22 subscript/ref-mod tests, and 13 call tests. The binder decomposition (M004) created
9 focused binder classes covering every COBOL-85 statement form. The source-snapshot.txt
confirms the runtime has dedicated file handlers for sequential, indexed, and relative
organizations (SequentialFileHandler.cs, IndexedFileHandler.cs, RelativeFileHandler.cs).
Intrinsic functions are fully dispatched (94/94 per GRAMMAR_AUDIT.md).

### Where Evidence Is Weak or Missing

The 77 FAIL* results locked in NIST baselines are the most concrete conformance gap,
confirmed by direct inspection of the baseline files. The FAIL* distribution (from
04-nist-test-mapping.md) shows ExpressionBinder territory accounts for 30 of 77 FAIL*
(39%) — specifically NC246A (14 FAIL* for qualified names/subscripts), NC247A (7 for
OCCURS), NC208A (3 for qualification), and NC215A (6 for continuation). ControlFlow
accounts for 15 FAIL* (NC225A EVALUATE, NC201A PERFORM VARYING, NC237A SEARCH ALL).

Beyond NC tests, the source-snapshot.txt confirms 364 non-NC NIST programs exist (IC:47,
IF:45, IX:42, SQ:85, ST:40, RL:35, SM:17, DB:15, SG:13, CM:9, OB:9, RW:6) with zero
baselines. The IC suite (inter-program communication) is the highest-value target since
CALL is already implemented with 13 passing integration tests. The SQ suite (85 sequential
I/O programs) would exercise the SequentialFileHandler that exists in source-snapshot.

The grammar still has ~56 known COBOL-85 gaps per GRAMMAR_AUDIT.md. PERFORM range overlap
detection (§14.8.35) is confirmed not implemented. Record locking is confirmed absent.
The integration.trx shows 1 skipped test (`Subscript_ExpressionSubscript_Multiplication`),
suggesting a known subscript edge case.

### Where Evidence Is Ambiguous

Nine semantic rules are marked "Unclear" in 03-semantic-rules-audit.md — meaning the
implementation may exist (source files are present in source-snapshot.txt) but test coverage
cannot be confirmed from TRX test name classification alone. Examples: EVALUATE THRU ranges,
EVALUATE ANY keyword, collating sequence impact on comparisons, intermediate arithmetic
precision, OPTIONAL file behavior, and file access mode enforcement. The condition test
count (24) appears adequate for 20 binder methods but edge case coverage for abbreviated
conditions with nested NOT cannot be confirmed from TRX data.

### Concrete Next Steps (Priority-Ordered)

1. **Fix the 77 FAIL* results** — highest-signal gaps. Start with NC246A (14 — subscripts),
   NC218A (9 — UNSTRING), NC225A (7 — EVALUATE), NC247A (7 — OCCURS).

2. **Attempt non-NC NIST suites** — start with IC (47 programs, CALL implemented) and
   SQ (85 programs, SequentialFileHandler exists).

3. **Close ~56 remaining grammar gaps** — per GRAMMAR_AUDIT.md, categorized and ready.

4. **Add condition edge case tests** — abbreviated NOT, EVALUATE THRU/ANY, collating sequence.

5. **Implement PERFORM range overlap detection** — spec requirement §14.8.35.

6. **Diagnose NC220M/NC237A runtime hangs** — blocking clean NC runs.

7. **Investigate the 9 "Unclear" semantic rules** — deeper code analysis needed to determine
   if implementations exist but lack tests, or are genuinely missing.
