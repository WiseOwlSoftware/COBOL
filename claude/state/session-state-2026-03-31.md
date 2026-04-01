# CobolSharp Session State — 2026-03-31 (End of Session)

Paste this document at the start of the next session to restore full context.

---

## 1. Session Summary

**NIST FAIL* sweep: 78→28 (50 eliminated in 7 commits).**

87/95 NIST tests at 100% with clean baselines. 8 tests with 28 FAIL* pending fix.

**Key policy change**: No baseline file in valid/ may contain FAIL*. Guard script
enforces this and reports pending-fix tests as "NO BASELINE (N FAIL* — pending fix)".

**Test counts**: 999 unit + 334 integration + 95 NIST (87 with baselines, 8 pending).

---

## 2. Commits This Session

| Commit | Description | FAIL* Fixed |
|--------|-------------|-------------|
| `31daaf9` | Condition name resolution (qualified/subscripted 88-level) | 17 |
| `7340583` | 7 bug fixes (overflow, ALSO, keyword, RENAMES, PERFORM, collating) | 8 |
| `3a587b1` | UNSTRING MOVE semantics (PIC-aware dispatch) | 6 |
| `1cea680` | EVALUATE per-subject TRUE/FALSE + CORRESPONDING matching/subscripts | 13 |
| `ede1704` | Figurative collating sequence + RENAMES stack | 6 |
| `fd90614` | Baseline cleanup (remove FAIL*-containing baselines) | 0 |
| `a2829bb` | Ledger update (26 items, 16 closed) | 0 |

---

## 3. Remaining 28 FAIL* (8 tests without baselines)

| Test | FAIL* | Root Cause | Complexity |
|------|-------|-----------|------------|
| NC247A | 7 | ODO variable-length groups use compile-time max size | High |
| NC216A | 7 | INSPECT independent-pass model (needs single left-to-right pass) | High |
| NC201A | 5 | PERFORM VARYING: AFTER reset + COMP subscript corruption | Complex |
| NC237A | 3 | SEARCH ALL multi-key binary search direction wrong | Medium-High |
| NC218A | 2 | UNSTRING OR delimiters (pipeline-wide change) | High |
| NC250A | 2 | ALL-literal condition + abbreviated condition | Medium |
| NC225A | 1 | EVALUATE multiple WHEN sharing body (grammar change needed) | Medium |
| NC108M | 1 | Not yet investigated | Unknown |

---

## 4. Bug Catalog Summary

21 distinct bugs cataloged. 15 fixed this session. 6 remaining unfixed bugs are
all High complexity or require grammar changes. Full analysis with file/line
references in DEVLOG entries 178-182.

---

## 5. Key Architectural Decisions (New This Session)

- **Baseline policy**: No FAIL* in valid/ baselines. Guard enforces.
- **ConditionSymbol via Rejections list**: Duplicate-named 88-level items found via
  scope Rejections, disambiguated by qualification chain walking.
- **Per-subject EVALUATE types**: SubjectKinds array replaces global isEvaluateTrue/False.
- **CorrespondingMatcher level-by-level**: Recursive name matching, not flat leaf enum.
- **RENAMES in Children**: Level-66 added to parent, with skips in layout/CORR/INITIALIZE.
- **Figurative remapping**: LOW-VALUE/HIGH-VALUE remapped to min/max weight chars
  when PROGRAM COLLATING SEQUENCE is active.

---

## 6. Session Continuity Rules

- Maintain strict architectural consistency with all prior decisions.
- Baselines must be 100% clean — no FAIL* in valid/.
- Grammar changes require ANTLR + COBOL expert review.
- One test at a time; compile after every change.
- Every commit needs a DEVLOG entry.
- Ledger must stay current with all progress.
