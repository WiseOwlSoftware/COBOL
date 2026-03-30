# Create-AuditArtifacts.ps1
# Run from CobolSharp root

$folders = @(
    "audit",
    "audit\plans"
)

foreach ($f in $folders) {
    if (-not (Test-Path $f)) {
        New-Item -ItemType Directory -Path $f | Out-Null
    }
}

# --- Modernization Ledger Template ---
$modernizationLedger = @"
# CobolSharp Modernization Ledger

> Single source of truth for all remaining work across the CobolSharp pipeline.

## 0. Context

- Primary dialect: COBOL-85 with extensions.
- Planned: IBM/MF extensions and later COBOL versions behind gates.
- Goal: full NIST COBOL-85 suite passing (currently ~75% passing).

## 1. Legend

- **ID:** Stable identifier (e.g., `SEM-QUAL-001`)
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
"@

Set-Content -Path "audit\plans\ModernizationLedgerTemplate.md" -Value $modernizationLedger

# --- NIST Enablement Roadmap ---
$nistRoadmap = @"
# CobolSharp NIST Enablement Roadmap

## 0. Current State

- NIST guard: 95 NC-series tests at 100% (guard.sh).
- NIST available: 459 programs across 16 suites; IC, IF, IX, SQ, ST, RL, SM, CM, DB, SG, RW not yet attempted.

## 1. Goals

- Short-term: Stabilize NC suite as non-regression baseline.
- Medium-term: Bring all COBOL-85 NIST suites online with expected outputs.
- Long-term: Use NIST as primary conformance harness for COBOL-85 core.

## 2. Suite Inventory

| Suite | Status        | Notes                          |
|-------|---------------|--------------------------------|
| NC    | Enabled       | Guarded, expected outputs set  |
| IC    | Not enabled   |                                |
| IF    | Not enabled   |                                |
| IX    | Not enabled   |                                |
| SQ    | Not enabled   |                                |
| ST    | Not enabled   |                                |
| RL    | Not enabled   |                                |
| SM    | Not enabled   |                                |
| CM    | Not enabled   |                                |
| DB    | Not enabled   |                                |
| SG    | Not enabled   |                                |
| RW    | Not enabled   |                                |

## 3. Phased Plan

### Phase 1 — Harden NC

- Lock NC as non-regression:
  - Ensure all NC tests run via a single script.
  - Add CI job to run NC on every change.
- Add diagnostics mapping:
  - Map failing NC cases (if any) to ledger IDs.

### Phase 2 — Enable one suite at a time

For each suite (IC, IF, IX, ...):

1. Inventory programs and expected outputs.
2. Classify failures:
   - Grammar gap
   - Semantic gap
   - Runtime/IO gap
   - Numeric behavior gap
3. Create or link ledger items.
4. Fix in small batches; re-run suite.
5. Mark suite as “Enabled” when:
   - All tests pass or are explicitly gated.
   - Gating is documented.

### Phase 3 — Coverage Reporting

- Add a simple report:
  - Suites enabled vs total.
  - Tests passing vs total.
  - Mapping to ledger items.

## 4. Integration with Modernization Ledger

- Every NIST failure must map to a ledger ID.
- No “mystery failures” allowed.
"@

Set-Content -Path "audit\plans\NistEnablementRoadmap.md" -Value $nistRoadmap

# --- Grammar Gap Closure Plan ---
$grammarPlan = @"
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
"@

Set-Content -Path "audit\plans\GrammarGapClosurePlan.md" -Value $grammarPlan

# --- Semantic Validator Hardening Plan ---
$semanticsPlan = @"
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
- Link to ledger item `SEM-QUAL-001`.

### WS2 — Storage Layout and ODO

- Improve ODO handling:
  - Compute variable-length layout, not just max.
  - Ensure runtime length is tracked where required.
- Add tests for:
  - REDEFINES families with ODO.
  - SYNCHRONIZED and slack bytes.
- Link to ledger item `SEM-ODO-001`.

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
"@

Set-Content -Path "audit\plans\SemanticValidatorHardeningPlan.md" -Value $semanticsPlan

Write-Host "Audit artifacts created in audit\plans."
