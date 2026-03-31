# Runtime Class Layout (Files, Namespaces, Responsibilities)

---

## 1. Namespaces

- `CobolSharp.Runtime.Terminal`
- `CobolSharp.Runtime.Execution`
- `CobolSharp.Runtime.Screen`
- `CobolSharp.Binding.Screen` (already exists)
- `CobolSharp.Runtime.Tests.Terminal`

---

## 2. Files and Classes

### 2.1 CobolSharp.Runtime.Terminal

| File | Type | Responsibility |
|------|------|----------------|
| `TerminalAttributes.cs` | `enum TerminalAttributes` | Bitmask for display attributes + color encoding |
| `TerminalCell.cs` | `struct TerminalCell` | Single cell: char + attributes |
| `TerminalBuffer.cs` | `class TerminalBuffer` | 2D grid, Clear/Write/WriteString/Clone |
| `TerminalKey.cs` | `enum TerminalKey` + `struct TerminalKeyEvent` | Key identity + payload |
| `ITerminalDevice.cs` | `interface ITerminalDevice` | Backend abstraction: Render/MoveCursor/ReadKey/Flush |
| `ConsoleTerminalDevice.cs` | `class ConsoleTerminalDevice : ITerminalDevice` | Real console I/O via System.Console |
| `HeadlessTerminalDevice.cs` | `class HeadlessTerminalDevice : ITerminalDevice` | Test-oriented: scripted input, captured frames |
| `TerminalSession.cs` | `class TerminalSession` | High-level controller: DisplayScreen, DisplayItem, Accept, cursor, clear, refresh |
| `TerminalInputResult.cs` | `enum CrtStatusCode` + `class TerminalInputResult` | ACCEPT result: status, last key, text, cursor position |
| `CrtStatusMapper.cs` | static class | Maps TerminalInputResult to numeric CRT STATUS |
| `CursorCodec.cs` | static class | CURSOR encoding/decoding for PIC 9(4)/9(6) |

### 2.2 CobolSharp.Runtime.Execution

| File | Type | Responsibility |
|------|------|----------------|
| `ExecutionContext.cs` | `class ExecutionContext` | Existing data access layer (MOVE/STORE) |
| `RuntimeContext.cs` | `class RuntimeContext` | Wraps ExecutionContext + TerminalSession |
| `AcceptExecutor.cs` | static class | Executes BoundAcceptStatement: identifier forms, screen forms via TerminalSession.Accept |
| `DisplayExecutor.cs` | static class | Executes BoundDisplayStatement: identifier/literal forms, screen forms via TerminalSession |
| `ScreenRuntimeHelpers.cs` | static class | ApplyCursorFromSpecialNames, UpdateCursorFromResult, UpdateCrtStatus |

### 2.3 CobolSharp.Runtime.Screen

| File | Type | Responsibility |
|------|------|----------------|
| `ScreenAttributeMapper.cs` | static class | Maps BoundScreenItem to TerminalAttributes |
| `ScreenFieldLength.cs` | static class | Computes field length from PicString |

---

## 3. Tests: CobolSharp.Runtime.Tests.Terminal

| File | Covers |
|------|--------|
| `TerminalBufferTests.cs` | Clear/write/clone behavior |
| `HeadlessTerminalDeviceTests.cs` | Frame capture, scripted input |
| `TerminalSessionAcceptTests.cs` | Simple ACCEPT, SECURE, FULL/REQUIRED |
| `CrtStatusMapperTests.cs` | Key to CRT STATUS mapping |
| `CursorCodecTests.cs` | PIC 9(4)/9(6) encode/decode |
| `ScreenIoIntegrationTests.cs` | End-to-end ACCEPT/DISPLAY with runtime context |
