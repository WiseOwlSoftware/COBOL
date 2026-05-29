# CobolSharp Session State — 2026-05-28

Paste this at the start of the next session to restore full context.

---

## 1. Session Summary

**All 95 NC-series NIST nucleus tests now pass at 100% with clean baselines (0 FAIL*).**
Started at 89/95 with 21 FAIL* across 6 tests; closed all six this session.

Build/test: full guard (`bash scripts/guard.sh`) ALL GREEN — unit + integration + 95 NIST,
95 clean baselines in `tests/nist/valid/`, zero FAIL* anywhere. Branch: main.

---

## 2. Commits This Session (oldest → newest)

| Commit | Description |
|--------|-------------|
| `27cb56c` | Build infra: repo-local `nuget.config` pins nuget.org (fixes NU1507 from a global `demeanor` source) |
| `cc93c7a` | NC201A + NC250A 100% — INITIALIZE OCCURS per-occurrence + figurative 88-VALUE fill |
| `f0c5163` | NC237A 100% — SEARCH ALL multi-key binary search (per-key ASC/DESC) |
| `e705d5c` | INSPECT single comparison cycle (TALLYING/REPLACING grouped) — NC216A 7→5 |
| `c50d76f` | NC247A 100% — ODO variable-length group sizing (IrOdoGroupLocation; receiving=max) |
| `f77b27a` | NC216A 100% — multi-counter TALLYING grammar predicate + signed-numeric de-sign |
| `225c7d2` | NC225A 100% — EVALUATE consecutive WHENs share one imperative |

DEVLOG entries 184–190 cover these in detail.

---

## 3. Key Fixes (root causes)

- **NC201A** — `INITIALIZE` of an OCCURS table only initialized the whole array as one field
  (COMP arrays left as 0x20 spaces). Now OCCURS-aware: per-occurrence init via constant
  subscripts (`DataMovementLowerer`).
- **NC250A** — figurative 88-level VALUEs (QUOTE/SPACE/HIGH/LOW-VALUE) stored as a single
  char; now `FromAllString` so they fill the parent field (`SemanticBuilder`).
- **NC237A** — SEARCH ALL used one direction; now extracts all WHEN keys with per-key
  ASC/DESC direction and branches at the first differing key (`ControlFlowLowerer`,
  `ExtractSearchKeys`/`EmitSearchKeyDirection`).
- **NC247A** — ODO group sized at compile-time max; new `IrOdoGroupLocation` computes
  runtime length = max − (maxOccurs − DOI)*elementSize. Receiving group with internal
  DEPENDING ON uses max (ISO OCCURS GR 7) — `receiving` flag on resolver, MOVE target sets it.
- **NC216A** — INSPECT was per-operand independent passes; rewrote to a single comparison
  cycle (`InspectRuntime.TallyingPass`/`ReplacingPass`, grouped `IrInspectTallying`/
  `IrInspectReplacing`). Multi-counter TALLYING grammar ambiguity fixed with predicate
  `{IsBareInspectOperand()}?`. Signed-numeric de-sign for INSPECT (GR 4d) via
  `ReadInspectTarget`.
- **NC225A** — EVALUATE gave each WHEN its own body; grammar now groups consecutive WHEN
  phrases before a shared body; binder emits one arm per phrase (OR semantics).

## 4. Grammar changes (both user-approved this session)

- `CobolIO.g4` `inspectCountPhrase`: added `{IsBareInspectOperand()}?` predicate (helper in
  `CobolParserCoreBase`) so a data-name followed by FOR is the next counter, not a transitive
  pattern. (Do NOT make the adjective mandatory — that breaks valid `LEADING X Y Z` transitivity.)
- `CobolControlFlow.g4` `evaluateWhenClause`: `evaluateWhenPhrase+ statementBlock*` so
  consecutive WHENs share an imperative.

Parser auto-regenerates on build (GenerateIfNewer.ps1, needs Java).

## 5. Known gaps / next frontier

- **INSPECT REPLACING on a signed numeric — RESOLVED (DEVLOG 191).** `ReplacingPass` now
  de-signs per GR 4d (runs the cycle over the absolute digits, re-encodes with the original
  sign retained). Test: `StringTests.Inspect_Replacing_OnSignedNumeric_DeSignsAndRetainsSign`.
  Only edge left: a non-numeric replacement (e.g. REPLACING a digit BY a letter) leaves the
  field unchanged — undefined in the spec, no NIST coverage.
- **"Qualified-cond combined-condition parse" — NOT A BUG (DEVLOG 191).** The earlier note
  was a false alarm: the repro was a 97-char fixed-form line truncated correctly at column 72.
  The compiler is correct (free-form + short fixed-form both work). Regression test:
  `ConditionTests.Combined_QualifiedConditionNames_WithInlineStatement`. NOTE: keep generated
  fixed-form repros within columns 8–72.
- Non-NC NIST suites (IC, IF, IX, SQ, ST) not yet attempted — the next major target.

## 6. Continuity rules (unchanged)

- Baselines must be 100% clean — no FAIL* in `tests/nist/valid/`. Guard enforces.
- Grammar changes require explaining problem + solution and getting user approval.
- One test at a time; build + run guard after each; every commit gets a DEVLOG entry.
