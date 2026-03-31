# Batch 5 Implementation Plan

---

## 1. Scope

Batch 5 covers:

- **M429**: Screen I/O runtime (Terminal abstraction, ACCEPT/DISPLAY screen forms).
- **M430**: CRT STATUS runtime wiring.
- **M431**: CURSOR clause runtime wiring.
- **M432**: Multi-char currency strings (runtime formatting).
- **M412-M426**: OVERLENIENT grammar gaps (if any runtime impact).

---

## 2. Phases

### Phase 1 -- Terminal Abstraction (M429 core)

- Introduce core types:
  - `TerminalAttributes`, `TerminalCell`, `TerminalBuffer`.
  - `TerminalKey`, `TerminalKeyEvent`.
  - `ITerminalDevice`.
- Implement devices:
  - `ConsoleTerminalDevice` (minimal, working).
  - `HeadlessTerminalDevice` (for tests).
- Implement `TerminalSession`:
  - Construction, buffer ownership.
  - `DisplayScreen`, `DisplayItem`.
  - `MoveCursor`, `ClearScreen`, `Refresh`.
- Unit tests:
  - Buffer operations.
  - Attribute mapping from `BoundScreenItem`.
  - Headless device frame capture.

### Phase 2 -- ACCEPT Runtime (M429)

- Implement single-field ACCEPT loop in `TerminalSession.Accept(BoundScreenItem)`:
  - Initial field text from USING/TO.
  - Character input, backspace, left/right.
  - SECURE masking.
  - FULL/REQUIRED enforcement.
  - Exit on Enter/Tab/F-keys/Escape.
- Runtime bridge:
  - `AcceptExecutor`:
    - For `BoundAcceptKind.Screen`, call `TerminalSession.Accept`.
    - Write `result.Text` to USING/TO target.
- Tests:
  - Simple ACCEPT.
  - SECURE fields.
  - FULL/REQUIRED behavior.

### Phase 3 -- CRT STATUS (M430)

- Bind CRT STATUS symbol from SPECIAL-NAMES into `BoundAcceptStatement`.
- Implement mapping:
  - `TerminalInputResult` to numeric CRT STATUS (e.g., Enter=0000, F1=1001, etc.).
- Runtime writeback:
  - After ACCEPT, call `UpdateCrtStatus`.
- Tests:
  - F-key ACCEPT sets expected CRT STATUS.
  - Validation error sets special code (e.g., 9001).

### Phase 4 -- CURSOR Clause (M431)

- Bind CURSOR symbol from SPECIAL-NAMES.
- Implement encoding helpers:
  - `LoadCursorPosition` (PIC 9(4)/9(6)).
  - `StoreCursorPosition`.
- Runtime integration:
  - Before DISPLAY/ACCEPT: load CURSOR -> `MoveCursor`.
  - After ACCEPT: store final cursor position.
- Tests:
  - Initial cursor from CURSOR item.
  - Final cursor written back.

### Phase 5 -- Multi-char Currency (M432)

- Design runtime formatting for multi-char currency strings (COBOL-2002+):
  - Extend existing numeric formatting to support multi-char currency symbol.
  - Ensure no impact on COBOL-85 single-char behavior.
- Tests:
  - Unit tests for formatting with multi-char currency.
  - Integration tests where applicable (no NIST coverage).

### Phase 6 -- OVERLENIENT Grammar Gaps (M412-M426)

- Audit which gaps have runtime impact.
- Implement any missing runtime checks (e.g., invalid combinations).
- Add tests to ensure runtime rejects invalid constructs.

### Phase 7 -- Batch 5 Completion Gate

- Run full unit, integration, NIST suites.
- Confirm 0 regressions.
- Update ledger:
  - M429-M432 -> complete.
  - M412-M426 -> updated/closed.
- Update M300 and initialization.json.
