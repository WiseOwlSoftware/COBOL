# CobolSharp Modernization Ledger

> Single source of truth for all remaining work across the CobolSharp pipeline.

## 0. Context

- Primary dialect: COBOL-85 with extensions.
- Planned: IBM/MF extensions and later COBOL versions behind gates.
- Goal: full NIST COBOL-85 suite passing (currently ~75% passing).

## 1. Legend

- **ID:** Stable identifier (e.g., SEM-QUAL-001)
- **Area:** Preprocessor | Grammar | Semantics | Flow | IR | CIL | Runtime | Tests
- **Phase:** 0–21 (from pipeline doc)
- **Priority:** P0 (blocker), P1 (release), P2 (nice-to-have)
- **Status:** Open | In Progress | Done | Deferred
- **Risk:** Low | Medium | High

## 2. Ledger Table

| ID             | Area       | Phase | Title                                      | Priority | Status      | Risk  | Owner  |
|----------------|-----------|-------|--------------------------------------------|----------|------------|-------|--------|
| SEM-QUAL-001   | Semantics | 8     | Qualified name resolution (OF/IN)          | P0       | Open       | High  |        |
| GRAM-085-001   | Grammar   | 3–5   | Close 92 COBOL-85 grammar gaps             | P0       | Open       | High  |        |
| FLOW-FS-001    | Flow      | 18    | Branch-sensitive FileStateValidator        | P1       | Open       | Med   |        |
| PRE-REP-001    | Preproc   | 0–2   | REPLACE ON reactivation + edge pseudo-text | P1       | Open       | Med   |        |
| NIST-SUITE-001 | Tests     | N/A   | Enable IC/IF/IX/SQ/... NIST suites         | P0       | Open       | High  |        |

(Add rows as needed.)

## 3. Detailed Items

### SEM-QUAL-001 — Qualified name resolution (OF/IN)

- **Area:** Semantics
- **Phase(s):** 8 (ReferenceResolver)
- **Current state:** Qualified name resolution is missing for general OF/IN in expressions; only RENAMES handled.
- **Definition of done:**
  - All qualified data references (OF/IN) resolved correctly in expressions and subscripts.
  - Negative tests for ambiguous/illegal qualification.
  - NIST and custom tests added.
- **Notes:**
  - Coordinate with SymbolValidator and StorageLayoutComputer.

### GRAM-085-001 — Close 92 COBOL-85 grammar gaps

- **Area:** Grammar
- **Phase(s):** 3–5
- **Current state:** 92 COBOL-85 items deferred (Data/Proc/Env Div).
- **Definition of done:**
  - All 92 items implemented or explicitly gated.
  - GRAMMAR_AUDIT.md updated.
  - New tests added for each gap.

(Repeat this pattern for each ledger item.)
