# CobolSharp Session State — 2026-03-31

Paste this document at the start of the next session to restore full context.

---

## 1. Session Summary

**Batches 1-4 complete.** All 12 REJECT grammar gaps closed. 3 semantic/runtime items
closed. Grammar architecture fully modernized. LPAREN subscript trigger refactored to
production-quality `_dataNameTokens` HashSet whitelist.

**Batch 5 in progress.** Design phase complete. All skeletons and test scaffolds created.
Implementation not yet started.

**Commits this session:**
- `05043a9` — Batch 2: 3 REJECT gaps + ANTLR review + dialect wiring
- `f81aeeb` — Batch 3 design: M407 + M411 design docs
- `122cc72` — Batch 3 implementation: M407 CURRENCY + M411 SCREEN + LPAREN refactor
- `2813a0c` — Batch 3 state save
- `f8a7fad` — Batch 4: M427 table sort + M428 symbolic chars + M433 shadowing

**Test counts:** 922 unit + 334 integration + 95 NIST = 0 failures.

**Ledger version:** 11. **Next item:** Batch 5.

---

## 2. Terminal Subsystem (M429-M431)

### Architecture

TerminalSession is the single entry point for all COBOL screen I/O. The runtime never
calls Console directly.

```
RuntimeContext → TerminalSession → ITerminalDevice
                     ↓
               TerminalBuffer (2D grid of TerminalCell)
```

### TerminalBuffer Model

- 1-based coordinates (COBOL LINE/COL semantics)
- `TerminalCell[,]` array sized `[rows+1, columns+1]` (index 0 unused)
- `Clear()`, `Write(row, col, char, attr)`, `WriteString(row, col, text, attr)`
- `Clone()` for headless test frame capture
- Default: 24 rows x 80 columns

### TerminalAttributes Bitmask

```csharp
[Flags]
public enum TerminalAttributes
{
    None = 0,
    Highlight = 1 << 0, Lowlight = 1 << 1, Reverse = 1 << 2,
    Underline = 1 << 3, Blink = 1 << 4, Secure = 1 << 5,
    ForegroundMask = 0x0F00, BackgroundMask = 0xF000
}
```

Colors 0-7 encoded in upper bits. Highlight/Lowlight mutually exclusive (validated
in SemanticBuilder).

### ITerminalDevice

```csharp
public interface ITerminalDevice
{
    void Render(TerminalBuffer buffer);
    void MoveCursor(int row, int col);
    TerminalKeyEvent ReadKey();
    void Flush();
}
```

- **ConsoleTerminalDevice**: real terminal via System.Console
- **HeadlessTerminalDevice**: scripted input queue, captured frame list, cursor move
  history. For deterministic tests.

### ACCEPT Loop Semantics

Single-field state machine:

1. Initialize: `currentText` from USING/TO, `cursorIndex` at end, `maxLength` from PIC
2. Loop: `MoveCursor` → `ReadKey` → dispatch
3. **Character**: insert at cursorIndex, shift right, advance cursor. If Secure: write `*`.
4. **Backspace**: delete char before cursor, shift left
5. **Left/Right**: move cursorIndex within `[0..maxLength]`
6. **Enter/Tab/F-keys/Escape**: candidate exit keys
   - If Required and empty: reject, beep
   - If Full and short: reject, beep
   - Otherwise: exit, return `TerminalInputResult`
7. **Up/Down/None**: no-op

### CRT STATUS Mapping

```
Enter → 0000    Escape → 0001
F1 → 1001       F2 → 1002       ...    F12 → 1012
ValidationError → 9001           Timeout → 9002
```

Written via `CrtStatusMapper.MapToCrtStatus(result)` → `StoreNumeric(crtStatusSymbol, value)`.

### CURSOR Encoding

- **PIC 9(4)**: `value = row * 100 + col` (RRCC), range 1-24/1-80
- **PIC 9(6)**: `value = row * 1000 + col` (RRRCCC), range 1-999/1-999
- Out-of-range clamped. Before ACCEPT: load → MoveCursor. After ACCEPT: store from result.

### Runtime Integration

- `AcceptExecutor.Execute(context, stmt)` → resolves screen item → `session.Accept(item)` → writes `result.Text` to USING/TO → `UpdateCrtStatus`
- `DisplayExecutor.Execute(context, stmt)` → `session.DisplayScreen(items)` or `session.DisplayItem(item)`
- `ScreenRuntimeHelpers`: `ApplyCursorFromSpecialNames`, `UpdateCursorFromResult`, `UpdateCrtStatus`
- `ScreenAttributeMapper.GetAttributes(item)` → composes `TerminalAttributes` from BoundScreenItem
- `ScreenFieldLength.ComputeFieldLength(picString)` → PIC length computation

---

## 3. Grammar Strictness (M412-M426)

All 15 OVERLENIENT gaps have:
- Problem statement and ISO rationale
- Unified diff grammar delta (pseudocode, needs mapping to actual ANTLR rules)
- LL(*) safety notes
- Test file mapping

### Summary of Deltas

| Item | Statement | Change |
|------|-----------|--------|
| M412 | ACCEPT/DISPLAY | Remove permissive tail; restrict to ISO clauses |
| M413 | IF/ELSE | Mandatory condition, single ELSE |
| M414 | EVALUATE | WHEN requires condition+; WHEN OTHER once, last |
| M415 | PERFORM | UNTIL requires condition; VARYING requires FROM/BY/UNTIL |
| M416 | STRING | INTO required; DELIMITED BY required |
| M417 | UNSTRING | INTO required; DELIMITED BY required |
| M418 | INSPECT | TALLYING and REPLACING as separate alternatives |
| M419 | CALL | Single USING; valid RETURNING |
| M420 | SET | Three distinct forms: TO/UP BY/DOWN BY |
| M421 | SEARCH | WHEN required; AT END unique |
| M422 | SORT | ASCENDING/DESCENDING required; KEY required |
| M423 | MERGE | Same as SORT |
| M424 | WRITE | Single FROM; valid ADVANCING |
| M425 | READ | Single INTO; single AT END |
| M426 | OPEN/CLOSE | Single mode; file list only |

**Review gate:** All deltas must be validated by ANTLR + COBOL expert agents against
actual grammar rules before implementation. The pseudocode diffs do NOT map 1:1 to
current ANTLR rules.

Full details: `docs/BATCH5-OVERLENIENT-GRAMMAR-APPROVAL.md`

---

## 4. Test Skeletons Created

### Terminal Tests (`tests/CobolSharp.Tests.Unit/Terminal/`)

| File | Tests |
|------|-------|
| `TerminalBufferTests.cs` | Clear_ResetsAllCells, Write_WritesCharacterAndAttributes, Clone_ProducesDeepCopy |
| `HeadlessTerminalDeviceTests.cs` | CapturesFrames, ScriptedInput_DequeuesInOrder |
| `TerminalSessionDisplayTests.cs` | RendersValueLiteral, RendersFromSource, AppliesAttributes, BlankLine_ClearsLine, BlankScreen_ClearsScreen |
| `TerminalSessionAcceptTests.cs` | SimpleInput_ReturnsText, SecureField_MasksCharacters, FullField_RejectsShortInput, RequiredField_RejectsEmpty, Backspace_DeletesCharacter, LeftRight_MoveCursor, Enter_CompletesField, FunctionKey_CompletesField |
| `CrtStatusMapperTests.cs` | Enter_Returns0000, Escape_Returns0001, Function3_Returns1003, ValidationError_Returns9001 |
| `AcceptExecutorTests.cs` | Accept_SetsCrtStatus_OnEnter, Accept_SetsCrtStatus_OnFunctionKey, Accept_SetsCrtStatus_OnValidationError |
| `CursorCodecTests.cs` | Decode_9_4_ReturnsRowCol, Decode_9_6_ReturnsRowCol, Encode_9_4_ProducesRRCC, Encode_9_6_ProducesRRRCCC, Clamp_OutOfRangeValues |
| `CursorIntegrationTests.cs` | Display_UsesInitialCursorPosition, Accept_WritesFinalCursorPosition |

### Overlenient Tests (`tests/CobolSharp.Tests.Unit/Overlenient/`)

15 files, M412-M426, 3 tests each = 45 test methods. All empty bodies.

---

## 5. Runtime File Skeletons Created

### `src/CobolSharp.Runtime/Terminal/`

| File | Type |
|------|------|
| `TerminalAttributes.cs` | `enum TerminalAttributes` |
| `TerminalCell.cs` | `struct TerminalCell` |
| `TerminalBuffer.cs` | `class TerminalBuffer` (empty method bodies) |
| `TerminalKey.cs` | `enum TerminalKey` + `struct TerminalKeyEvent` |
| `ITerminalDevice.cs` | `interface ITerminalDevice` |
| `ConsoleTerminalDevice.cs` | `class ConsoleTerminalDevice` (empty bodies) |
| `HeadlessTerminalDevice.cs` | `class HeadlessTerminalDevice` (empty bodies) |
| `TerminalSession.cs` | `class TerminalSession` (empty bodies, all method signatures) |
| `TerminalInputResult.cs` | `enum CtrStatusCode` + `class TerminalInputResult` |
| `CrtStatusMapper.cs` | `static class CrtStatusMapper` (empty body) |
| `CursorCodec.cs` | `static class CursorCodec` (empty body) |

### `src/CobolSharp.Runtime/Screen/`

| File | Type |
|------|------|
| `ScreenAttributeMapper.cs` | `static class ScreenAttributeMapper` (empty body) |
| `ScreenFieldLength.cs` | `static class ScreenFieldLength` (empty body) |

### `src/CobolSharp.Runtime/Execution/`

Directory created but no files yet (RuntimeContext, AcceptExecutor, DisplayExecutor,
ScreenRuntimeHelpers are documented in design but skeleton files were in the user's
spec using namespaces that don't exist yet — to be created during implementation).

---

## 6. Ledger Context

### Completed Items (this session)

| Item | Status | What |
|------|--------|------|
| M407 | complete | CURRENCY SIGN WITH PICTURE SYMBOL |
| M411 | complete | SCREEN SECTION grammar island |
| M427 | complete | SORT Format 2 table sort runtime |
| M428 | complete | SYMBOLIC CHARACTERS N:N validation |
| M433 | complete | Lexer keyword shadowing mitigation |

### Open Items (Batch 5)

| Item | Status | What |
|------|--------|------|
| M429 | open | Screen I/O runtime (terminal abstraction + ACCEPT/DISPLAY) |
| M430 | open | CRT STATUS runtime wiring |
| M431 | open | CURSOR clause runtime wiring |
| M432 | open | Multi-char currency strings (COBOL-2002+) |
| M412-M426 | open | OVERLENIENT grammar gaps (15 items, P3) |

### Batch 5 Phases

1. Terminal abstraction core (TerminalBuffer, ITerminalDevice, TerminalSession)
2. ACCEPT runtime (single-field loop)
3. CRT STATUS (M430)
4. CURSOR clause (M431)
5. Multi-char currency (M432)
6. OVERLENIENT grammar gaps (M412-M426)
7. Completion gate

### Next Concrete Step

Implement `TerminalBuffer` (fill in empty method bodies) and its unit tests. Then
`HeadlessTerminalDevice`, then `TerminalSession.DisplayItem`/`DisplayScreen`, then
the ACCEPT loop.

---

## 7. Design Documents Created This Session

| Document | Content |
|----------|---------|
| `docs/M407-currency-sign-picmode-refactor.md` | PICMODE exploit design |
| `docs/M411-screen-section-grammar-island.md` | SCREEN SECTION grammar island design |
| `docs/batch3-architectural-summary.md` | Batch 3 risks/dependencies/touchpoints |
| `docs/TERMINAL-ABSTRACTION-DESIGN.md` | Top-level terminal architecture |
| `docs/TERMINAL-SESSION-API.md` | TerminalSession controller spec |
| `docs/TERMINAL-DEVICE-BACKEND.md` | ITerminalDevice + implementations |
| `docs/TERMINAL-BUFFER-ATTRIBUTE-MODEL.md` | TerminalBuffer + attributes |
| `docs/TERMINAL-ACCEPT-INPUT-LOOP.md` | ACCEPT field state machine |
| `docs/TERMINAL-CRT-STATUS-MAPPING.md` | CRT STATUS encoding |
| `docs/TERMINAL-CURSOR-ENCODING.md` | CURSOR PIC 9(4)/9(6) encoding |
| `docs/TERMINAL-MULTI-FIELD-NAVIGATION.md` | Tab/Enter field navigation |
| `docs/TERMINAL-RUNTIME-ENTRY-POINTS.md` | Bound statement bridge |
| `docs/TERMINAL-TEST-HARNESS.md` | HeadlessTerminalDevice + test patterns |
| `docs/TERMINAL-RUNTIME-CLASS-LAYOUT.md` | File/namespace/class map |
| `docs/BATCH5-IMPLEMENTATION-PLAN.md` | Batch 5 phased plan |
| `docs/BATCH5-LEDGER-TASKS-M429-M431.md` | Canonical ledger tasks |
| `docs/BATCH5-OVERLENIENT-GRAMMAR-DELTAS.md` | M412-M426 grammar deltas |
| `docs/BATCH5-OVERLENIENT-GRAMMAR-APPROVAL.md` | Approved grammar-change proposals |

---

## 8. Session Continuity Rules

- Maintain strict architectural consistency with all prior decisions.
- Never contradict prior grammar, binder, or runtime decisions.
- Never re-derive solved problems (PICMODE exploit, LPAREN whitelist, etc.).
- Always resume from this saved state.
- Always update the modernization ledger at the end of each session.
- Grammar changes require ANTLR + COBOL expert agent review.
- DRY-RUN mode for design; IMPLEMENT NOW for code changes.
- One test at a time; compile after every change.
- Every commit needs a DEVLOG entry.

---

## 9. Key Architectural Decisions (Do Not Revisit)

- **PICMODE exploit**: `WITH PICTURE SYMBOL` uses existing PICMODE to capture "SYMBOL"
  as PIC_STRING. Zero lexer changes.
- **LPAREN whitelist**: `_dataNameTokens` HashSet in lexer. Whitelist (not blacklist).
  Mirrors `cobolWord` + `functionName`. Safe failure mode.
- **SCREEN SECTION**: Always enabled (no dialect gate). Grammar island in CobolScreen.g4.
  All screen tokens in `cobolWord` for shadowing mitigation.
- **PicEnvironment.CurrencyOutputChar**: Decouples PIC symbol from output character.
  3-param constructor. PicRuntime uses CurrencyOutputChar for output, CurrencySign for
  pattern scanning.
- **BoundTableSortStatement**: Separate from BoundSortStatement. IrTableSort instruction.
  SortRuntime.SortTable with StableSort helper.
- **TerminalSession**: Owns TerminalBuffer + ITerminalDevice. Single entry point for
  all screen I/O. Runtime never calls Console directly.
