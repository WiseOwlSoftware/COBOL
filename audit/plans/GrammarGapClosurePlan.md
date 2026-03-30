# Grammar Gap Closure Plan (COBOL-85 Core)

## 0. Current Snapshot

- Parsing/Grammar ~75% of COBOL-85.
- 92 COBOL-85 items deferred (Data/Proc/Env Div), documented in GRAMMAR_AUDIT.md.
- 45 reserved words missing (mostly Report Writer + Environment Div).
- Report Writer: stub grammar only; no semantics/codegen.

## 1. Objectives

- Reach full COBOL-85 grammar coverage for:
  - Identification, Environment, Data, Procedure divisions.
- Keep:
  - Report Writer and COBOL-2002+ features gated and documented.

## 2. Workstreams

### WS1 — Lexer Completion

- Add missing 45 reserved words.
- Ensure tokens are gated where they belong to non-core features (Report Writer, OO, etc.).
- Add unit tests for tokenization of edge cases.

### WS2 — Statement Coverage

- Current: 48 of ~60 statements parsed.
- Missing examples:
  - WRITE/REWRITE FILE form
  - USE GLOBAL/EXCEPTION modes
  - SORT Format 2 (table sort)
  - START WITH LENGTH
  - Some SET forms
- For each missing statement:
  - Implement grammar.
  - Add parse-only tests.
  - Add negative tests for invalid forms.

### WS3 — Division/Clause Gaps

- Close the 92 documented COBOL-85 items:
  - 38 Data Div
  - 27 Proc Div
  - 27 Env Div
- For each item:
  - Implement grammar or gate it.
  - Update GRAMMAR_AUDIT.md.
  - Add tests.

### WS4 — Report Writer and Extensions

- Keep Report Writer grammar as stub until core COBOL-85 is complete.
- Ensure all non-core features are:
  - Gated by dialect flags.
  - Documented in a dialect matrix.

## 3. Milestones

- M1: All missing tokens added and tested.
- M2: All core statements implemented and tested.
- M3: 92-item gap list closed or gated.
- M4: Grammar audit doc updated and stable.
