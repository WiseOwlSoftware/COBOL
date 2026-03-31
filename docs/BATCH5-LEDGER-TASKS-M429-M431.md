# Batch 5 Ledger Tasks: M429-M431

Canonical, ledger-ready task list for the Batch 5 runtime work. Fully enumerated,
implementation-ready, with test names and method signatures. Zero ambiguity.

---

## M429 -- Screen I/O Runtime (Terminal Abstraction + ACCEPT/DISPLAY)

**Subsystem:** Runtime -> Terminal
**Priority:** P1
**Status:** open

### Deliverables

**1. Terminal Abstraction Core**

- `TerminalAttributes.cs`
- `TerminalCell.cs`
- `TerminalBuffer.cs`
- `TerminalKey.cs`
- `TerminalKeyEvent.cs`
- `ITerminalDevice.cs`
- `ConsoleTerminalDevice.cs`
- `HeadlessTerminalDevice.cs`
- `TerminalSession.cs`
- `TerminalInputResult.cs`

**2. Runtime Integration**

- `DisplayExecutor.Execute()` -> call `TerminalSession.DisplayScreen` / `DisplayItem`
- `AcceptExecutor.Execute()` -> call `TerminalSession.Accept`
- `ScreenAttributeMapper`
- `ScreenFieldLength`

**3. ACCEPT Loop Implementation**

Methods to implement inside `TerminalSession`:

```csharp
private void RedrawField(BoundScreenItem item, string currentText);
private bool CanExitField(BoundScreenItem item, string currentText);
private TerminalInputResult BuildResult(BoundScreenItem item,
    TerminalKeyEvent keyEvent, string currentText, int cursorIndex);
private void HandleCharacter(ref string currentText, ref int cursorIndex,
    char ch, int maxLength);
private void HandleBackspace(ref string currentText, ref int cursorIndex);
private void HandleLeft(ref int cursorIndex);
private void HandleRight(ref int cursorIndex, int maxLength);
```

**4. DISPLAY Implementation**

Inside `TerminalSession`:

```csharp
public void DisplayScreen(BoundScreenSection section);
public void DisplayItem(BoundScreenItem item);
```

### Tests

**TerminalBuffer**

- `TerminalBufferTests.Clear_ResetsAllCells()`
- `TerminalBufferTests.Write_WritesCharacterAndAttributes()`
- `TerminalBufferTests.Clone_ProducesDeepCopy()`

**HeadlessTerminalDevice**

- `HeadlessTerminalDeviceTests.CapturesFrames()`
- `HeadlessTerminalDeviceTests.ScriptedInput_DequeuesInOrder()`

**TerminalSession.Display**

- `TerminalSessionDisplayTests.RendersValueLiteral()`
- `TerminalSessionDisplayTests.RendersFromSource()`
- `TerminalSessionDisplayTests.AppliesAttributes()`
- `TerminalSessionDisplayTests.BlankLine_ClearsLine()`
- `TerminalSessionDisplayTests.BlankScreen_ClearsScreen()`

**TerminalSession.Accept**

- `TerminalSessionAcceptTests.SimpleInput_ReturnsText()`
- `TerminalSessionAcceptTests.SecureField_MasksCharacters()`
- `TerminalSessionAcceptTests.FullField_RejectsShortInput()`
- `TerminalSessionAcceptTests.RequiredField_RejectsEmpty()`
- `TerminalSessionAcceptTests.Backspace_DeletesCharacter()`
- `TerminalSessionAcceptTests.LeftRight_MoveCursor()`
- `TerminalSessionAcceptTests.Enter_CompletesField()`
- `TerminalSessionAcceptTests.FunctionKey_CompletesField()`

---

## M430 -- CRT STATUS Runtime Wiring

**Subsystem:** Runtime -> Terminal
**Priority:** P2
**Status:** open

### Deliverables

**1. CRT STATUS Mapping**

File: `CrtStatusMapper.cs`

```csharp
public static int MapToCrtStatus(TerminalInputResult result);
```

**2. Runtime Writeback**

File: `ScreenRuntimeHelpers.cs`

```csharp
public static void UpdateCrtStatus(
    ExecutionContext context,
    DataSymbol? crtStatusSymbol,
    TerminalInputResult result);
```

**3. Binder Integration**

- Bind CRT STATUS symbol from SPECIAL-NAMES into `BoundAcceptStatement.CrtStatusSymbol`.

**4. AcceptExecutor Integration**

Inside `AcceptExecutor.ExecuteScreenAccept`:

```csharp
UpdateCrtStatus(context.Data, stmt.CrtStatusSymbol, result);
```

### Tests

**CrtStatusMapperTests**

- `Enter_Returns0000()`
- `Escape_Returns0001()`
- `Function3_Returns1003()`
- `ValidationError_Returns9001()`

**AcceptExecutorTests**

- `Accept_SetsCrtStatus_OnEnter()`
- `Accept_SetsCrtStatus_OnFunctionKey()`
- `Accept_SetsCrtStatus_OnValidationError()`

---

## M431 -- CURSOR Clause Runtime Wiring

**Subsystem:** Runtime -> Terminal
**Priority:** P2
**Status:** open

### Deliverables

**1. CURSOR Encoding/Decoding**

File: `CursorCodec.cs`

```csharp
public static (int Row, int Col) LoadCursorPosition(
    ExecutionContext data, DataSymbol cursorSymbol);
public static void StoreCursorPosition(
    ExecutionContext data, DataSymbol cursorSymbol, int row, int col);
```

**2. Runtime Integration**

Before DISPLAY/ACCEPT:

```csharp
ApplyCursorFromSpecialNames(context, cursorSymbol);
```

After ACCEPT:

```csharp
UpdateCursorFromResult(context, cursorSymbol, result);
```

**3. Binder Integration**

- Bind CURSOR symbol from SPECIAL-NAMES into runtime metadata.

### Tests

**CursorCodecTests**

- `Decode_9_4_ReturnsRowCol()`
- `Decode_9_6_ReturnsRowCol()`
- `Encode_9_4_ProducesRRCC()`
- `Encode_9_6_ProducesRRRCCC()`
- `Clamp_OutOfRangeValues()`

**CursorIntegrationTests**

- `Display_UsesInitialCursorPosition()`
- `Accept_WritesFinalCursorPosition()`

---

## Paste-Ready Ledger Summary

```
M429 — Screen I/O Runtime (Terminal Abstraction + ACCEPT/DISPLAY)
- Implement TerminalAttributes, TerminalCell, TerminalBuffer
- Implement TerminalKey, TerminalKeyEvent
- Implement ITerminalDevice, ConsoleTerminalDevice, HeadlessTerminalDevice
- Implement TerminalSession (DisplayScreen, DisplayItem, Accept)
- Implement ACCEPT loop handlers (HandleCharacter, HandleBackspace, HandleLeft, HandleRight)
- Implement RedrawField, CanExitField, BuildResult
- Integrate with DisplayExecutor and AcceptExecutor
- Add ScreenAttributeMapper and ScreenFieldLength
- Tests:
  - TerminalBufferTests.Clear_ResetsAllCells
  - TerminalBufferTests.Write_WritesCharacterAndAttributes
  - TerminalBufferTests.Clone_ProducesDeepCopy
  - HeadlessTerminalDeviceTests.CapturesFrames
  - HeadlessTerminalDeviceTests.ScriptedInput_DequeuesInOrder
  - TerminalSessionDisplayTests.*
  - TerminalSessionAcceptTests.*

M430 — CRT STATUS Runtime Wiring
- Implement CrtStatusMapper.MapToCrtStatus
- Implement UpdateCrtStatus in ScreenRuntimeHelpers
- Bind CRT STATUS symbol in BoundAcceptStatement
- Integrate into AcceptExecutor
- Tests:
  - CrtStatusMapperTests.*
  - AcceptExecutorTests.*

M431 — CURSOR Clause Runtime Wiring
- Implement CursorCodec.LoadCursorPosition / StoreCursorPosition
- Integrate ApplyCursorFromSpecialNames and UpdateCursorFromResult
- Bind CURSOR symbol in SPECIAL-NAMES
- Tests:
  - CursorCodecTests.*
  - CursorIntegrationTests.*
```
