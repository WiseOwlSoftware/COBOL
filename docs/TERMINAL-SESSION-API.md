# TerminalSession API Specification

## Purpose

TerminalSession is the high-level controller for COBOL screen I/O. It owns a
TerminalBuffer, talks to an ITerminalDevice, and implements the semantics of:

- DISPLAY screen-name (Format 2)
- ACCEPT screen-name (Format 3)
- Cursor movement, attributes, and CRT STATUS updates

The compiler/runtime should only interact with this type -- never directly with the device.

---

## 1. Construction and Invariants

```csharp
public sealed class TerminalSession
{
    public TerminalBuffer Buffer { get; }
    public ITerminalDevice Device { get; }

    public int Rows => Buffer.Rows;
    public int Columns => Buffer.Columns;

    public TerminalSession(ITerminalDevice device, int rows = 24, int columns = 80)
    {
        Device = device ?? throw new ArgumentNullException(nameof(device));
        Buffer = new TerminalBuffer(rows, columns);
        _cursorRow = 1;
        _cursorCol = 1;
    }
}
```

**Invariants:**

- `Rows` and `Columns` are fixed for the lifetime of the session.
- Cursor position is always within `[1..Rows] x [1..Columns]`.
- `Buffer` is the single source of truth for what's on screen.

---

## 2. Rendering Semantics

### 2.1 DisplayScreen

```csharp
public void DisplayScreen(BoundScreenSection section)
```

**Detailed behavior:**

- **Blanking:**
  - If any top-level item has `BlankScreen == true`, call `Buffer.Clear()` before rendering.
  - If an item has `BlankLine == true`, clear that line before writing the item.
- **Rendering order:**
  - Traverse `section.Items` in level-number order (already bound).
  - For each `BoundScreenItem`:
    - Compute absolute LINE/COL (binder should have resolved relative positions).
    - If `ValueLiteral` is present: write literal.
    - If `FromSource` is present: format data item and write.
    - If `PicString` only: treat as input field (may render spaces or existing value).
- **Attributes:**
  - Compute `TerminalAttributes` from:
    - Highlight / Lowlight
    - ReverseVideo
    - Underline
    - Blink
    - ForegroundColor / BackgroundColor
- **Erase clauses:**
  - `EraseEol`: clear from item's COL to end of line.
  - `EraseEos`: clear from item's COL to end of screen.
- **Flush:**
  - Call `Device.Render(Buffer)` then `Device.Flush()`.

**Guarantee:** After `DisplayScreen`, the device's visible state matches `Buffer`.

### 2.2 DisplayItem

```csharp
public void DisplayItem(BoundScreenItem item)
```

**Behavior:**

- Does not clear the whole screen.
- Clears line if `BlankLine`.
- Writes only this item's content and attributes.
- Calls `Device.Render(Buffer)` and `Device.Flush()`.

**Use case:** `DISPLAY screen-name` where the screen is small or incremental.

---

## 3. Cursor Management

```csharp
private int _cursorRow;
private int _cursorCol;

public void MoveCursor(int row, int col)
{
    _cursorRow = Math.Clamp(row, 1, Rows);
    _cursorCol = Math.Clamp(col, 1, Columns);
    Device.MoveCursor(_cursorRow, _cursorCol);
}

public (int Row, int Col) GetCursorPosition() => (_cursorRow, _cursorCol);
```

**Rules:**

- Cursor is always clamped to buffer bounds.
- `DisplayScreen` may optionally reset cursor to (1,1) or leave it unchanged --
  pick a policy and document it.
- `ACCEPT` always sets cursor to the start of the input field before reading.

---

## 4. ACCEPT Input Loop (High-Resolution Behavior)

```csharp
public TerminalInputResult Accept(BoundScreenItem item)
```

**Preconditions:**

- `item` is an elementary screen item with a PIC and either TO/USING.
- Screen has been rendered (either by caller or by a prior `DisplayScreen`).

**Internal state:**

```csharp
var bufferRow = item.Line!.Value;
var bufferCol = item.Column!.Value;
var maxLength = ComputeFieldLength(item.PicString);
var currentText = InitializeFieldText(item); // from USING/TO or spaces
var cursorIndex = currentText.Length;         // 0..maxLength
```

**Loop:**

1. **Position cursor:**
   ```csharp
   MoveCursor(bufferRow, bufferCol + cursorIndex);
   ```

2. **Read key:**
   ```csharp
   var keyEvent = Device.ReadKey();
   ```

3. **Handle key:**

   - **Character:**
     - If `Secure`: update `currentText`, but write masking char (e.g., `'*'`) or spaces.
     - Else: write actual char to `Buffer`.
     - Advance `cursorIndex` (up to `maxLength`).

   - **Backspace:**
     - If `cursorIndex > 0`: delete previous char, shift left, write spaces at end.
     - Move cursor left.

   - **Left/Right:**
     - Move `cursorIndex` within `[0..maxLength]`.
     - Move cursor accordingly.

   - **Enter / Function keys / Escape:**
     - Candidate exit keys.
     - If `Required` and `currentText` is empty: reject, maybe beep (BELL), continue loop.
     - If `Full` and `currentText.Length < maxLength`: reject, continue loop.
     - Otherwise: exit loop with status.

4. **On exit:**
   ```csharp
   return new TerminalInputResult(
       status: MapStatus(keyEvent),
       lastKey: keyEvent.Key,
       text: currentText,
       cursorRow: _cursorRow,
       cursorColumn: _cursorCol);
   ```

**Postconditions:**

- `Buffer` reflects final visible state (masked or unmasked).
- `TerminalInputResult.Text` is the logical value to write back to COBOL data item.
- Caller is responsible for:
  - Moving `Text` into `ToTarget`/`UsingField`.
  - Updating CRT STATUS data item from `Status`/`LastKey`.

---

## 5. ACCEPT by Name

```csharp
public TerminalInputResult Accept(BoundScreenSection section, string screenItemName)
{
    var item = ResolveScreenItem(section, screenItemName);
    return Accept(item);
}
```

**Resolution rules:**

- Match by `Name` (case-insensitive, COBOL rules).
- If multiple matches: binder should have prevented this; treat as error.
- If not found: runtime error (or pre-validated by binder).

---

## 6. CRT Status and ACCEPT Result

### 6.1 CrtStatusCode

```csharp
public enum CrtStatusCode
{
    None,
    Enter,
    Escape,
    FunctionKey,
    ValidationError,
    Timeout
}
```

### 6.2 TerminalInputResult

```csharp
public sealed class TerminalInputResult
{
    public CrtStatusCode Status { get; }
    public TerminalKey LastKey { get; }
    public string Text { get; }              // Final field contents
    public int CursorRow { get; }
    public int CursorColumn { get; }

    public TerminalInputResult(
        CrtStatusCode status,
        TerminalKey lastKey,
        string text,
        int cursorRow,
        int cursorColumn);
}
```

---

## 7. Clear and Refresh

```csharp
public void ClearScreen()
{
    Buffer.Clear();
    Device.Render(Buffer);
    Device.Flush();
    MoveCursor(1, 1);
}

public void Refresh()
{
    Device.Render(Buffer);
    Device.Flush();
}
```

---

## 8. Screen Model Integration

Assumes existing bound types from the SCREEN SECTION grammar (M411):

```csharp
public sealed class BoundScreenSection
{
    public IReadOnlyList<BoundScreenItem> Items { get; }
}

public sealed class BoundScreenItem
{
    public string? Name { get; }
    public int Level { get; }
    public bool IsGroup { get; }

    public int? Line { get; }          // Absolute or relative, resolved beforehand
    public int? Column { get; }

    public bool BlankScreen { get; }
    public bool BlankLine { get; }
    public bool EraseEol { get; }
    public bool EraseEos { get; }

    public bool Bell { get; }
    public bool Blink { get; }
    public bool Highlight { get; }
    public bool Lowlight { get; }
    public bool ReverseVideo { get; }
    public bool Underline { get; }
    public int? ForegroundColor { get; }
    public int? BackgroundColor { get; }

    public bool Secure { get; }
    public bool Auto { get; }
    public bool Full { get; }
    public bool Required { get; }

    public string? PicString { get; }
    public DataSymbol? FromSource { get; }
    public DataSymbol? ToTarget { get; }
    public DataSymbol? UsingField { get; }
    public string? ValueLiteral { get; }

    public IReadOnlyList<BoundScreenItem> Children { get; }
}
```

---

## 9. Integration Points

- **Runtime calls:**
  - `session.DisplayScreen(screenSection)` for `DISPLAY screen-name`.
  - `session.Accept(screenItem)` for `ACCEPT screen-name`.
- **CRT STATUS** is updated by mapping `TerminalInputResult.Status` and `LastKey` to
  the CRT STATUS data item.
- **CURSOR clause** influences initial cursor position and is honored by `MoveCursor`
  before ACCEPT/DISPLAY.

---

## 10. Next Design Steps

- Fully specify the ACCEPT input loop (key-by-key behavior, AUTO/FULL/REQUIRED).
- Define the CRT STATUS mapping table from `TerminalInputResult`.
- Sketch the runtime entry points that bridge from bound statements to `TerminalSession`.
