# Semantic Validator Hardening Plan

## 0. Current Snapshot

Semantic Analysis ~85% of COBOL-85:

- SemanticBuilder ~95% (PIC, USAGE, OCCURS, REDEFINES, RENAMES, EXTERNAL/GLOBAL, etc.).
- ReferenceResolver ~50% (PERFORM/GO TO targets, file names; missing general qualified name resolution and subscripted reference validation).
- ParagraphValidator minimal (phantom paragraph detection only).
- StorageLayoutComputer ~90% (ODO uses max length; no true variable-length layout).
- DataItemClassifier ~80% (missing PIC/USAGE compatibility and value range validation).
- SymbolValidator ~90% (gap: nested program GLOBAL propagation).

## 1. Objectives

- Make semantic validators spec-true for COBOL-85 core.
- Eliminate silent misclassification and layout errors.
- Tie validators directly to tests and NIST behavior.

## 2. Workstreams

### WS1 — Qualified Name Resolution

- Implement full OF/IN resolution in ReferenceResolver:
  - Data names with qualification chains.
  - Subscripted references.
- Add negative tests for ambiguous/illegal qualification.
- Link to ledger item SEM-QUAL-001.

### WS2 — Storage Layout and ODO

- Improve ODO handling:
  - Compute variable-length layout, not just max.
  - Ensure runtime length is tracked where required.
- Add tests for:
  - REDEFINES families with ODO.
  - SYNCHRONIZED and slack bytes.
- Link to ledger item SEM-ODO-001.

### WS3 — Data Item Classification

- Extend DataItemClassifier to:
  - Validate PIC/USAGE compatibility.
  - Validate value ranges against PIC.
- Add tests for:
  - Invalid VALUE clauses.
  - BLANK WHEN ZERO and JUSTIFIED interactions.

### WS4 — GLOBAL Propagation and Nested Programs

- Fix nested program GLOBAL propagation in SymbolValidator.
- Add tests for:
  - GLOBAL data visibility across nested programs.
  - Conflicts and shadowing rules.

### WS5 — Paragraph and File State Validators

- ParagraphValidator:
  - Extend beyond phantom paragraphs if needed (e.g., unreachable paragraphs).
- FileStateValidator:
  - Add branch-sensitive analysis for OPEN/CLOSE tracking.
  - Ensure no false positives on legal patterns.

## 3. Milestones

- M1: Qualified name resolution complete and tested.
- M2: ODO and layout rules hardened.
- M3: DataItemClassifier extended and tested.
- M4: GLOBAL propagation fixed and tested.
- M5: FileStateValidator branch sensitivity implemented.
