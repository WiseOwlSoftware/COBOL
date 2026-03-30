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
