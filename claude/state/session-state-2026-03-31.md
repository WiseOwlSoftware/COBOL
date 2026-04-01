# CobolSharp Session State — 2026-03-31 (Final)

Paste this document at the start of the next session to restore full context.

---

## 1. Session Summary

**NIST FAIL* sweep: 78→21 (57 eliminated in 9 commits).**

89/95 NIST tests at 100% with clean baselines. 6 tests with 21 FAIL* pending fix
(no baselines — policy: baselines must be 100% clean, no FAIL* allowed).

**Test counts:** 999 unit + 334 integration (1 skip) + 95 NIST (89 with baselines).

**Ledger version:** 4. **Branch:** main.

---

## 2. Commits This Session

| Commit | Description | FAIL* Fixed |
|--------|-------------|-------------|
| `31daaf9` | Condition name resolution (qualified/subscripted 88-level) | 17 |
| `7340583` | 7 bug fixes (overflow, ALSO, keyword, RENAMES, PERFORM, collating) | 8 |
| `3a587b1` | UNSTRING MOVE semantics (PIC-aware dispatch) | 6 |
| `1cea680` | EVALUATE per-subject TRUE/FALSE + CORRESPONDING matching/subscripts | 13 |
| `ede1704` | Figurative collating sequence + RENAMES stack | 6 |
| `fd90614` | Baseline cleanup (remove FAIL*-containing baselines, guard enforcement) | 0 |
| `a2829bb` | Ledger update (26 items, 16 closed) | 0 |
| `c3fe2a6` | ALL-literal conditions + UNSTRING OR delimiters + PERFORM VARYING AFTER | 6 |
| `999a33f` | NC108M baseline (resolved by earlier fixes) | 1 |

---

## 3. Remaining 21 FAIL* (6 tests without baselines)

| Test | FAIL* | Pass/Total | Failing Tests | Root Cause | Complexity |
|------|-------|------------|---------------|-----------|------------|
| NC247A | 7 | 12/21 | IF-TEST-GF-2, INS-TEST-F1-2, MOV-TEST-F1-2, STR-TEST-GF-2/3, UST-TEST-GF-2/3 | ODO variable-length groups use compile-time max size instead of runtime DEPENDING ON value. Fix requires new IrOdoGroupLocation + runtime length computation in CilLocationEmitter. | High |
| NC216A | 7 | 50/57 | INS-TEST-F3-19.01/.03/.04/.05, INS-TEST-F1-23.02, INS-TEST-F1-27, INS-TEST-F3-38 | INSPECT uses independent passes instead of spec-required single left-to-right pass with character consumption. 6 of 7 from this. 1 from signed DISPLAY overpunch. | High |
| NC237A | 3 | 10/13 | IDX-TEST-F2-9, F2-12, F2-13 | SEARCH ALL binary search uses single direction comparison. Multi-key tables (ASCENDING + DESCENDING keys) need per-key direction logic. | Medium-High |
| NC201A | 2 | 57/59 | PFM-TEST-F4-24 (x2) | INITIALIZE fails to zero COMP OCCURS arrays: `EncodeCompBinary` handles lengths 2/4/8 only, not full array (20 bytes). Space-filled (0x2020=8224). Fix: per-element INITIALIZE in `DataMovementLowerer.InitializeDataItem` when `Occurs.MaxOccurs > 1`. | Medium |
| NC250A | 1 | 114/115 | IF--TEST-26 | `B OF IF-D33 AND NOT B OF IF-D32` — abbreviated condition expansion with NOT on qualified condition name. Likely an issue in `RewriteAbbreviatedRelations` or condition parsing. | Medium |
| NC225A | 1 | 62/63 | EVA-TEST-GF-35-1.01 | Multiple WHEN phrases sharing a statement body parsed as separate clauses. Grammar change needed: `evaluateWhenClause` needs `(WHEN ...)+` instead of single WHEN. | Medium (grammar) |

---

## 4. Detailed Bug Analysis for Remaining Items

### NC247A — ODO Variable-Length Groups (7 FAIL*)
- `StorageLayoutComputer.cs:249`: `totalSize = elementSize * MaxOccurs` — compile-time constant
- `CilLocationEmitter.cs:52`: `Ldc_I4(s.Location.Length)` — fixed constant
- Fix: Tag ODO-containing groups with (fixedPartSize, elementSize, dependingOnSymbol). New `IrOdoGroupLocation` variant. CilLocationEmitter emits `length = fixedPart + elementSize * dependingOnValue`.

### NC216A — INSPECT Single-Pass (7 FAIL*)
- `InspectRuntime.cs`: Each tallying/replacing item is a separate call, operating independently
- `StringLowerer.cs` + `CilStringEmitter.cs`: Emit separate calls per item
- Fix: New `InspectSinglePass(target, tallyingOps[], replacingOps[])` runtime method. Left-to-right scan, character consumption, operations tried in source order.
- Also: grammar ambiguity in multi-counter TALLYING (counter data-name consumed as pattern), signed DISPLAY overpunch not de-signed.

### NC237A — SEARCH ALL Multi-Key (3 FAIL*)
- `ControlFlowLowerer.cs:975`: `ExtractFirstRelationalComparison` gets only the first relational from AND-ed conditions
- For multi-key (ASCENDING GRP-1 + DESCENDING SEC), needs per-key direction in the binary search tree
- Fix: Extract all relational comparisons, match each to its table key's sort direction.

### NC201A — COMP Subscript Corruption (2 FAIL*)
- `VARYING PFM-F4-24-A (S1) FROM 10 BY PFM-F4-24-C (S2) UNTIL ...` where body modifies S1/S2
- PIC S9(3) COMP = 2 bytes per element, but PicDescriptor may report 3 bytes
- Likely IrElementRef multiplier mismatch for COMP OCCURS arrays
- Needs runtime debugging with actual execution trace

### NC250A — Abbreviated Condition (1 FAIL*)
- `B OF IF-D33 AND NOT B OF IF-D32` — `NOT B OF IF-D32` may be parsed as NOT applied to the wrong scope
- May be in `ConditionBinder.RewriteAbbreviatedRelations` or the grammar's condition precedence

### NC225A — EVALUATE Multiple WHEN Body (1 FAIL*)
- Grammar: `evaluateWhenClause: WHEN ... statementBlock*` — each WHEN gets its own clause
- Spec: consecutive WHENs before a body should be ORed
- Fix requires grammar change: `evaluateWhenClause: (WHEN evaluateWhenGroup (ALSO ...)*)+  statementBlock*`
- **Requires ANTLR + COBOL expert review before implementation.**

---

## 5. What Was Fixed This Session (21 bugs, by category)

### Binding/Resolution (22 FAIL* fixed)
- Qualified/subscripted condition names (88-level) — ExpressionBinder + SemanticModel
- Level-66 RENAMES not in parent Children — SemanticBuilder
- RENAMES _dataStack.Clear() destroying stack — SemanticBuilder
- Qualified paragraph name OF/IN ignored — ProcedureNameResolver + ControlFlowBinder + Binder
- ALL-literal condition value expansion — ProgramSymbol + SemanticBuilder + ConditionLowerer

### EVALUATE (13 FAIL* fixed)
- Multi-subject TRUE/FALSE collapsed to single subject — ControlFlowBinder + ControlFlowLowerer
- Latent ANY jump bug in lowerer — ControlFlowLowerer

### CORRESPONDING (7 FAIL* fixed)
- Elementary↔group mismatch — CorrespondingMatcher (level-by-level recursive)
- Target subscripts lost — BoundNodes + ArithmeticStatementBinder + DataMovementLowerer

### UNSTRING (9 FAIL* fixed)
- Raw byte copy skips MOVE semantics — StorageArea + CilStringEmitter
- Spurious overflow on source exhaustion — StorageArea + CilStringEmitter
- OR delimiters discarded — full pipeline (BoundNodes, StringStatementBinder, IrInstruction, StringLowerer, CilStringEmitter, StorageArea)

### Collating Sequence (6 FAIL* fixed)
- ALPHABET ALSO clause ignored — SemanticBuilder
- eitherNumeric bypass of collating sequence — ConditionLowerer
- Figurative comparisons ignore collating sequence — ConditionLowerer
- LOW-VALUE/HIGH-VALUE not remapped — ConditionLowerer + SemanticBuilder

### INSPECT (1 FAIL* fixed)
- Keyword inheritance (bare patterns default to ALL) — StringStatementBinder

### PERFORM (3 FAIL* fixed)
- VARYING AFTER not reset on outer increment — ControlFlowLowerer

---

## 6. Key Architectural Decisions (This Session)

- **Baseline policy**: No FAIL* in valid/ baselines. Guard enforces. Tests with failures
  have no baseline and are reported as "NO BASELINE (N FAIL* — pending fix)".
- **ConditionSymbol via Rejections list**: Duplicate-named 88-level items found via scope
  Rejections, disambiguated by qualification chain walking (SemanticModel).
- **Per-subject EVALUATE types**: `EvaluateSubjectKind` enum (Value/True/False). SubjectKinds
  array on BoundEvaluateStatement replaces global isEvaluateTrue/isEvaluateFalse.
- **CorrespondingMatcher level-by-level**: `MatchCorrespondingLevel` — recursive name
  matching at each level. If both groups: recurse. If either elementary: yield pair.
- **RENAMES in Children**: Level-66 added to parent 01-record Children, with level-66
  skips in CorrespondingMatcher, StorageLayoutComputer, RecordLayoutBuilder, INITIALIZE.
- **Figurative remapping**: LOW-VALUE/HIGH-VALUE remapped to min/max weight characters
  when PROGRAM COLLATING SEQUENCE is active.
- **ALL-literal ConditionValue**: `IsAllLiteral` flag on ConditionValue. Lowerer repeats
  pattern string to fill parent's StorageLength before comparison.
- **UNSTRING OR delimiters**: BoundUnstringDelimiter record, IrUnstringDelimiter. Runtime
  scans all delimiters at each position, picks earliest match (first-listed wins on tie).
- **PERFORM VARYING AFTER reset**: `ResetInnerVaryingFromValues` walks Next chain emitting
  MOVE instructions in the outer increment block (GR10(d) step 8).

---

## 7. Batch 5 Status (from prior session, unchanged)

Design phase complete. All skeletons and test scaffolds created. Implementation not started.

### Open Items
| Item | Status | What |
|------|--------|------|
| M429 | open | Screen I/O runtime (terminal abstraction + ACCEPT/DISPLAY) |
| M430 | open | CRT STATUS runtime wiring |
| M431 | open | CURSOR clause runtime wiring |
| M432 | open | Multi-char currency strings (COBOL-2002+) |
| M412-M426 | open | OVERLENIENT grammar gaps (15 items, P3) |

Runtime skeletons (TerminalBuffer, HeadlessTerminalDevice, TerminalSession, etc.) are
stubbed with empty bodies. Build errors from invalid cross-project references fixed
(BoundScreenItem/DataSymbol refs replaced with placeholders).

---

## 8. Session Continuity Rules

- Maintain strict architectural consistency with all prior decisions.
- Baselines must be 100% clean — no FAIL* in valid/.
- Grammar changes require ANTLR + COBOL expert review.
- One test at a time; compile after every change.
- Every commit needs a DEVLOG entry.
- Ledger must stay current with all progress.
- Run `bash scripts/guard.sh` after every meaningful change.
