# TerminalDevice Backend Specification

## Purpose

ITerminalDevice abstracts the actual I/O mechanism (console, GUI, headless). It knows
how to:

- Render a TerminalBuffer
- Move the cursor
- Read keystrokes
- Flush output

TerminalSession depends on this interface; implementations can vary. ITerminalDevice is
a pure side-effect interface: no state beyond what it needs to talk to the OS or test
harness.

---

## 1. Interface

```csharp
public interface ITerminalDevice
{
    void Render(TerminalBuffer buffer);
    void MoveCursor(int row, int col);
    TerminalKeyEvent ReadKey();
    void Flush();
}
```

---

## 2. TerminalKey and TerminalKeyEvent

```csharp
public enum TerminalKey
{
    None,
    Character,      // Printable char in Payload
    Enter,
    Escape,
    Tab,
    Backspace,
    Up,
    Down,
    Left,
    Right,
    Function1,
    Function2,
    Function3,
    Function4,
    Function5,
    Function6,
    Function7,
    Function8,
    Function9,
    Function10,
    Function11,
    Function12
}

public readonly struct TerminalKeyEvent
{
    public TerminalKey Key { get; }
    public char? Payload { get; }   // For Key == Character

    public TerminalKeyEvent(TerminalKey key, char? payload = null);
}
```

---

## 3. Responsibilities

### Render

- Translate TerminalBuffer cells + attributes into device-specific operations.
- May optimize by tracking dirty regions.

### MoveCursor

- Position the cursor at a 1-based row/column (COBOL semantics).

### ReadKey

- Block until a key is available.
- Map platform key codes to `TerminalKeyEvent`.

### Flush

- Ensure all pending output is visible.

---

## 4. ConsoleTerminalDevice (Real Console)

**Responsibilities:**

- Map `TerminalBuffer` to console output.
- Map `MoveCursor` to console cursor APIs.
- Map keypresses to `TerminalKeyEvent`.

**Implementation sketch:**

```csharp
public sealed class ConsoleTerminalDevice : ITerminalDevice
{
    private readonly int _rows;
    private readonly int _cols;

    public ConsoleTerminalDevice(int rows = 24, int cols = 80)
    {
        _rows = rows;
        _cols = cols;
        Console.CursorVisible = true;
    }

    public void Render(TerminalBuffer buffer)
    {
        for (int r = 1; r <= buffer.Rows; r++)
        {
            Console.SetCursorPosition(0, r - 1);
            for (int c = 1; c <= buffer.Columns; c++)
            {
                var cell = buffer[r, c];
                ApplyAttributes(cell.Attributes);
                Console.Write(cell.Char);
            }
        }
        ResetAttributes();
    }

    public void MoveCursor(int row, int col)
    {
        Console.SetCursorPosition(col - 1, row - 1);
    }

    public TerminalKeyEvent ReadKey()
    {
        var keyInfo = Console.ReadKey(intercept: true);
        return MapConsoleKey(keyInfo);
    }

    public void Flush()
    {
        // Console is immediate; no-op or future hook.
    }

    // Attribute mapping helpers omitted for brevity.
}
```

**Key mapping:**

- `ConsoleKey.Enter` -> `TerminalKey.Enter`
- `ConsoleKey.Backspace` -> `TerminalKey.Backspace`
- `ConsoleKey.F1..F12` -> `TerminalKey.Function1..Function12`
- Printable chars -> `TerminalKey.Character` with `Payload`

---

## 5. HeadlessTerminalDevice (Tests)

**Purpose:** Deterministic, scriptable, no real I/O.

```csharp
public sealed class HeadlessTerminalDevice : ITerminalDevice
{
    private readonly Queue<TerminalKeyEvent> _input;
    private readonly List<TerminalBuffer> _frames = new();

    public IReadOnlyList<TerminalBuffer> Frames => _frames;

    public HeadlessTerminalDevice(IEnumerable<TerminalKeyEvent> scriptedInput)
    {
        _input = new Queue<TerminalKeyEvent>(scriptedInput);
    }

    public void Render(TerminalBuffer buffer)
    {
        // Snapshot buffer (deep copy) for later assertions
        _frames.Add(buffer.Clone());
    }

    public void MoveCursor(int row, int col)
    {
        // Optionally record cursor moves for assertions
    }

    public TerminalKeyEvent ReadKey()
    {
        if (_input.Count == 0)
            return new TerminalKeyEvent(TerminalKey.None);

        return _input.Dequeue();
    }

    public void Flush()
    {
        // No-op
    }
}
```

**Usage in tests:**

- Provide a scripted sequence: e.g., "HELLO" + Enter.
- Run `DisplayScreen` + `Accept`.
- Assert:
  - Final `TerminalInputResult.Text`.
  - Final `Frames.Last()` content.
  - No unexpected keys consumed.

---

## 6. Future Devices

Because the interface is minimal, you can later add:

- **GuiTerminalDevice** -- wraps a window with a grid control.
- **RemoteTerminalDevice** -- sends buffer diffs over a socket.

No changes to `TerminalSession` or compiler required.
