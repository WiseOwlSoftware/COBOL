# Terminal Abstraction Design Document

Top-level architecture document for the CobolSharp terminal subsystem. Provides a
stable, vendor-style terminal abstraction for COBOL SCREEN I/O. This document is the
single entry point; each subsystem has its own detailed spec document.

---

## 1. Goals and Constraints

**Goals:**

- Provide a stable, vendor-style terminal abstraction for COBOL SCREEN I/O.
- Support ACCEPT/DISPLAY screen forms, CRT STATUS, CURSOR, SECURE, FULL, REQUIRED.
- Be testable in headless mode and swappable across backends (console, GUI, remote).

**Constraints:**

- No direct `Console.WriteLine` from compiler/runtime.
- No ANSI escape codes in semantic/runtime layers.
- All terminal behavior goes through a single abstraction: `TerminalSession`.

---

## 2. Architecture Overview

```
+---------------------+
|   RuntimeContext     |  Bridges bound statements to TerminalSession
|   ExecutionContext   |  and data items
+---------+-----------+
          |
          v
+---------------------+
|  TerminalSession     |  High-level controller:
|                      |  - Renders SCREEN SECTION / screen items
|  +----------------+  |  - Runs ACCEPT loops
|  | TerminalBuffer |  |  - Manages cursor
|  | (2D grid of    |  |  - Integrates CRT STATUS and CURSOR
|  |  TerminalCell) |  |
|  +----------------+  |
+---------+-----------+
          |
          v
+---------------------+
|  ITerminalDevice     |  Backend interface for rendering,
|                      |  cursor movement, key input, flush
+---------------------+
   |            |
   v            v
Console     Headless      (Future: GUI, Remote)
```

| Component | Role | Detailed Spec |
|-----------|------|---------------|
| **TerminalBuffer** | In-memory 2D grid of TerminalCell (char + attributes) | [TERMINAL-BUFFER-ATTRIBUTE-MODEL.md](TERMINAL-BUFFER-ATTRIBUTE-MODEL.md) |
| **TerminalAttributes** | Bitmask for highlight, reverse, underline, blink, secure, colors | [TERMINAL-BUFFER-ATTRIBUTE-MODEL.md](TERMINAL-BUFFER-ATTRIBUTE-MODEL.md) |
| **ITerminalDevice** | Backend interface for rendering, cursor movement, key input, flush | [TERMINAL-DEVICE-BACKEND.md](TERMINAL-DEVICE-BACKEND.md) |
| **TerminalSession** | High-level controller: renders, accepts, manages cursor | [TERMINAL-SESSION-API.md](TERMINAL-SESSION-API.md) |
| **ACCEPT Loop** | Single-field state machine with key handling | [TERMINAL-ACCEPT-INPUT-LOOP.md](TERMINAL-ACCEPT-INPUT-LOOP.md) |
| **CRT STATUS** | Maps TerminalInputResult to PIC 9(4) numeric | [TERMINAL-CRT-STATUS-MAPPING.md](TERMINAL-CRT-STATUS-MAPPING.md) |
| **CURSOR** | Encodes/decodes row/col in PIC 9(4) or 9(6) | [TERMINAL-CURSOR-ENCODING.md](TERMINAL-CURSOR-ENCODING.md) |
| **Multi-field** | Tab/Enter navigation across screen forms | [TERMINAL-MULTI-FIELD-NAVIGATION.md](TERMINAL-MULTI-FIELD-NAVIGATION.md) |
| **Runtime bridge** | Bound statements to TerminalSession | [TERMINAL-RUNTIME-ENTRY-POINTS.md](TERMINAL-RUNTIME-ENTRY-POINTS.md) |
| **Test harness** | HeadlessTerminalDevice + scripted tests | [TERMINAL-TEST-HARNESS.md](TERMINAL-TEST-HARNESS.md) |

---

## 3. Core Types

### 3.1 TerminalAttributes

```csharp
[Flags]
public enum TerminalAttributes
{
    None        = 0,
    Highlight   = 1 << 0,
    Lowlight    = 1 << 1,
    Reverse     = 1 << 2,
    Underline   = 1 << 3,
    Blink       = 1 << 4,
    Secure      = 1 << 5,
    ForegroundMask = 0x0F00,
    BackgroundMask = 0xF000
}
```

### 3.2 TerminalCell / TerminalBuffer

```csharp
public struct TerminalCell
{
    public char Char;
    public TerminalAttributes Attributes;
}

public sealed class TerminalBuffer
{
    private readonly TerminalCell[,] _cells;
    public int Rows { get; }
    public int Columns { get; }

    public TerminalBuffer(int rows, int columns);
    public TerminalCell this[int row, int col] { get; set; }

    public void Clear();
    public void Write(int row, int col, char ch, TerminalAttributes attr);
    public void WriteString(int row, int col, string text, TerminalAttributes attr);
    public TerminalBuffer Clone();
}
```

---

## 4. Device Abstraction

### 4.1 ITerminalDevice

```csharp
public interface ITerminalDevice
{
    void Render(TerminalBuffer buffer);
    void MoveCursor(int row, int col);
    TerminalKeyEvent ReadKey();
    void Flush();
}
```

### 4.2 TerminalKey / TerminalKeyEvent

```csharp
public enum TerminalKey
{
    None,
    Character,
    Enter,
    Escape,
    Tab,
    Backspace,
    Up,
    Down,
    Left,
    Right,
    Function1, Function2, Function3, Function4,
    Function5, Function6, Function7, Function8,
    Function9, Function10, Function11, Function12
}

public readonly struct TerminalKeyEvent
{
    public TerminalKey Key { get; }
    public char? Payload { get; }
    public TerminalKeyEvent(TerminalKey key, char? payload = null);
}
```

### 4.3 Implementations

- **ConsoleTerminalDevice** -- uses `System.Console` to render buffer, move cursor,
  read keys.
- **HeadlessTerminalDevice** -- scripted input, captured frames, no real I/O.
- **Future**: GUI/remote devices.

---

## 5. TerminalSession

### 5.1 API

```csharp
public sealed class TerminalSession
{
    public TerminalBuffer Buffer { get; }
    public ITerminalDevice Device { get; }

    public TerminalSession(ITerminalDevice device, int rows = 24, int columns = 80);

    void DisplayScreen(BoundScreenSection section);
    void DisplayItem(BoundScreenItem item);

    TerminalInputResult Accept(BoundScreenItem item);
    TerminalInputResult Accept(BoundScreenSection section, string screenItemName);

    void MoveCursor(int row, int col);
    (int Row, int Col) GetCursorPosition();

    void ClearScreen();
    void Refresh();
}
```

### 5.2 Responsibilities

- **DisplayScreen**
  - Honor BLANK SCREEN/LINE, ERASE EOL/EOS.
  - Render VALUE/FROM, attributes, PIC fields.
  - Flush via device.
- **Accept**
  - Single-field ACCEPT loop:
    - Character input, backspace, left/right.
    - FULL/REQUIRED enforcement.
    - SECURE masking.
    - Exit on Enter/Tab/F-keys/Escape.
  - Returns `TerminalInputResult`.

---

## 6. CRT STATUS and CURSOR

### TerminalInputResult

```csharp
public enum CrtStatusCode
{
    None, Enter, Escape, FunctionKey, ValidationError, Timeout
}

public sealed class TerminalInputResult
{
    public CrtStatusCode Status { get; }
    public TerminalKey LastKey { get; }
    public string Text { get; }
    public int CursorRow { get; }
    public int CursorColumn { get; }
}
```

### CRT STATUS Mapping

`TerminalInputResult` maps to PIC 9(4) numeric:

| Key | Value |
|-----|-------|
| Enter | 0000 |
| Escape | 0001 |
| F1-F12 | 1001-1012 |
| Validation error | 9001 |
| Timeout | 9002 |

### CURSOR Encoding

- **PIC 9(4)**: RRCC (`row * 100 + col`)
- **PIC 9(6)**: RRRCCC (`row * 1000 + col`)

Runtime helpers load/store these values and call `TerminalSession.MoveCursor`.

---

## 7. Testing Strategy

- Use `HeadlessTerminalDevice` with scripted `TerminalKeyEvent` sequences.
- Assert:
  - `TerminalInputResult.Text`, `Status`, `LastKey`.
  - Final `TerminalBuffer` contents.
  - CRT STATUS data item value.
  - CURSOR data item value.
