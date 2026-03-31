# TerminalBuffer and Attribute Model Specification

## Purpose

TerminalBuffer is the in-memory representation of the screen:

- 2D grid of TerminalCell
- Each cell has a character + attributes
- Used by both TerminalSession and ITerminalDevice

This is the canonical model of the COBOL "screen." This is the data plane of the
terminal system. Everything else is control.

---

## 1. Coordinate System

Convention: **1-based** inside TerminalBuffer, matching COBOL LINE/COL semantics.
`TerminalSession` hides any internal conversion from the compiler.

- `Row` range: `1` to `Rows` (default 24).
- `Column` range: `1` to `Columns` (default 80).
- Out-of-range writes are silently clipped (no wrap, no scroll).

---

## 2. TerminalBuffer

```csharp
public sealed class TerminalBuffer
{
    private readonly TerminalCell[,] _cells;

    public int Rows { get; }
    public int Columns { get; }

    public TerminalBuffer(int rows, int columns)
    {
        Rows = rows;
        Columns = columns;
        _cells = new TerminalCell[rows + 1, columns + 1]; // ignore index 0
        Clear();
    }

    public TerminalCell this[int row, int col]
    {
        get => _cells[row, col];
        set => _cells[row, col] = value;
    }
}
```

---

## 3. Clear and Write Operations

```csharp
public void Clear()
{
    for (int r = 1; r <= Rows; r++)
    {
        for (int c = 1; c <= Columns; c++)
        {
            _cells[r, c].Char = ' ';
            _cells[r, c].Attributes = TerminalAttributes.None;
        }
    }
}

public void Write(int row, int col, char ch, TerminalAttributes attr)
{
    if (row < 1 || row > Rows || col < 1 || col > Columns)
        return; // clipping is friendlier for COBOL

    _cells[row, col].Char = ch;
    _cells[row, col].Attributes = attr;
}

public void WriteString(int row, int col, string text, TerminalAttributes attr)
{
    if (text is null) return;

    for (int i = 0; i < text.Length; i++)
    {
        int c = col + i;
        if (c > Columns) break;
        Write(row, c, text[i], attr);
    }
}
```

---

## 4. TerminalCell

```csharp
public struct TerminalCell
{
    public char Char;
    public TerminalAttributes Attributes;
}
```

Each cell in the buffer holds exactly one character and its display attributes. The
default state is `Char = ' '`, `Attributes = None`.

---

## 5. TerminalAttributes

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

    // Color encoding (implementation-defined mapping)
    ForegroundMask = 0x0F00,
    BackgroundMask = 0xF000
}
```

| Flag | COBOL Clause | Effect |
|------|-------------|--------|
| `Highlight` | `HIGHLIGHT` | High-intensity / bold foreground |
| `Lowlight` | `LOWLIGHT` | Low-intensity / dim foreground |
| `Reverse` | `REVERSE-VIDEO` | Swap foreground and background colors |
| `Underline` | `UNDERLINE` | Underline text |
| `Blink` | `BLINK` | Blinking text |
| `Secure` | `SECURE` | Input should not be echoed (for ACCEPT) |
| Foreground | `FOREGROUND-COLOR` | Color indices 0-7 (or 0-15) |
| Background | `BACKGROUND-COLOR` | Color indices 0-7 (or 0-15) |

---

## 6. Attribute Mapping from BoundScreenItem

Given a `BoundScreenItem`, compute attributes:

```csharp
public static TerminalAttributes GetAttributes(BoundScreenItem item)
{
    var attr = TerminalAttributes.None;

    if (item.Highlight)    attr |= TerminalAttributes.Highlight;
    if (item.Lowlight)     attr |= TerminalAttributes.Lowlight;
    if (item.ReverseVideo) attr |= TerminalAttributes.Reverse;
    if (item.Underline)    attr |= TerminalAttributes.Underline;
    if (item.Blink)        attr |= TerminalAttributes.Blink;
    if (item.Secure)       attr |= TerminalAttributes.Secure;

    if (item.ForegroundColor is int fg)
        attr |= EncodeForeground(fg);

    if (item.BackgroundColor is int bg)
        attr |= EncodeBackground(bg);

    return attr;
}
```

Where `EncodeForeground`/`EncodeBackground` map 0-7 to bitfields under
`ForegroundMask`/`BackgroundMask`.

---

## 7. Cloning for Tests

For headless testing, a deep copy is needed:

```csharp
public TerminalBuffer Clone()
{
    var clone = new TerminalBuffer(Rows, Columns);
    for (int r = 1; r <= Rows; r++)
    {
        for (int c = 1; c <= Columns; c++)
        {
            clone[r, c] = this[r, c];
        }
    }
    return clone;
}
```

---

## 8. Typical Usage Flow

### DISPLAY screen-name

```
session.DisplayScreen(section)
  -> Buffer.Clear() if needed.
  -> For each item:
       var attr = GetAttributes(item);
       Buffer.WriteString(line, col, text, attr);
  -> Device.Render(Buffer);
  -> Device.Flush();
```

### ACCEPT screen-name

```
Screen already rendered.
session.Accept(item):
  -> Uses Buffer to display typed characters.
  -> Uses Secure attribute to decide whether to echo.
  -> On exit, Buffer holds final visible state.
```

---

## 9. Next Design Steps

- Fully specify the ACCEPT input loop (key-by-key behavior, AUTO/FULL/REQUIRED).
- Define the CRT STATUS mapping table from `TerminalInputResult`.
- Sketch the runtime entry points that bridge from bound statements to `TerminalSession`.
