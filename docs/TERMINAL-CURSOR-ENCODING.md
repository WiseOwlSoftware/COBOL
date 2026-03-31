# CURSOR Data Encoding Specification

## 1. Purpose

The CURSOR clause in SPECIAL-NAMES binds a COBOL data item to the terminal cursor
position. The runtime must:

- Read the initial cursor position from the data item before DISPLAY/ACCEPT
- Move the terminal cursor accordingly
- Write the final cursor position back after ACCEPT

This is entirely a runtime concern.

---

## 2. Supported PIC Formats

We support the two formats used by IBM, Micro Focus, ACUCOBOL, RM/COBOL:

**PIC 9(4) -> RRCC**

- Row: 2 digits
- Column: 2 digits
- Range: row 1-24, col 1-80
- Example: `0510` -> row 5, col 10

**PIC 9(6) -> RRRCCC**

- Row: 3 digits
- Column: 3 digits
- Range: row 1-999, col 1-999
- Example: `012034` -> row 12, col 34

If the PIC length is neither 4 nor 6, treat as 4.

---

## 3. Encoding Rules

### Decode

```csharp
public static (int Row, int Col) LoadCursorPosition(
    ExecutionContext data,
    DataSymbol cursorSymbol)
{
    int raw = data.LoadNumeric(cursorSymbol);

    if (cursorSymbol.PicLength == 6)
        return (Clamp(raw / 1000, 1, 999), Clamp(raw % 1000, 1, 999));

    // Default: 9(4)
    return (Clamp(raw / 100, 1, 24), Clamp(raw % 100, 1, 80));
}
```

### Encode

```csharp
public static void StoreCursorPosition(
    ExecutionContext data,
    DataSymbol cursorSymbol,
    int row,
    int col)
{
    if (cursorSymbol.PicLength == 6)
    {
        int value = Clamp(row, 1, 999) * 1000 + Clamp(col, 1, 999);
        data.StoreNumeric(cursorSymbol, value);
        return;
    }

    // Default: 9(4)
    int v = Clamp(row, 1, 24) * 100 + Clamp(col, 1, 80);
    data.StoreNumeric(cursorSymbol, v);
}
```

---

## 4. Runtime Integration

**Before DISPLAY/ACCEPT:**

```csharp
var (row, col) = LoadCursorPosition(context.Data, cursorSymbol);
context.Terminal.MoveCursor(row, col);
```

**After ACCEPT:**

```csharp
StoreCursorPosition(context.Data, cursorSymbol,
    result.CursorRow, result.CursorColumn);
```
