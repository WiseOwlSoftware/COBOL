# CRT STATUS Mapping Specification

Defines the mapping from `TerminalInputResult` to the COBOL CRT STATUS data item.

---

## 1. Internal Status Model

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

public sealed class TerminalInputResult
{
    public CrtStatusCode Status { get; }
    public TerminalKey LastKey { get; }
    public string Text { get; }
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

## 2. CRT STATUS Data Item

Typically:

```cobol
SPECIAL-NAMES.
    CRT STATUS IS CRT-STATUS.

WORKING-STORAGE SECTION.
01 CRT-STATUS PIC 9(4).
```

---

## 3. Mapping Table (Default Scheme)

| Condition | CRT STATUS Value |
|-----------|-----------------|
| Enter | 0000 |
| Escape | 0001 |
| Function 1 | 1001 |
| Function 2 | 1002 |
| Function 3 | 1003 |
| Function 4 | 1004 |
| Function 5 | 1005 |
| Function 6 | 1006 |
| Function 7 | 1007 |
| Function 8 | 1008 |
| Function 9 | 1009 |
| Function 10 | 1010 |
| Function 11 | 1011 |
| Function 12 | 1012 |
| Validation error | 9001 |
| Timeout | 9002 |

---

## 4. Mapping Function

```csharp
public static int MapToCrtStatus(TerminalInputResult result)
{
    if (result.Status == CrtStatusCode.ValidationError)
        return 9001;

    if (result.Status == CrtStatusCode.Timeout)
        return 9002;

    switch (result.LastKey)
    {
        case TerminalKey.Enter:      return 0000;
        case TerminalKey.Escape:     return 0001;
        case TerminalKey.Function1:  return 1001;
        case TerminalKey.Function2:  return 1002;
        case TerminalKey.Function3:  return 1003;
        case TerminalKey.Function4:  return 1004;
        case TerminalKey.Function5:  return 1005;
        case TerminalKey.Function6:  return 1006;
        case TerminalKey.Function7:  return 1007;
        case TerminalKey.Function8:  return 1008;
        case TerminalKey.Function9:  return 1009;
        case TerminalKey.Function10: return 1010;
        case TerminalKey.Function11: return 1011;
        case TerminalKey.Function12: return 1012;
        default:                     return 0000;
    }
}
```

---

## 5. Runtime Writeback

```csharp
public static void UpdateCrtStatus(
    ExecutionContext context,
    DataSymbol? crtStatusSymbol,
    TerminalInputResult result)
{
    if (crtStatusSymbol is null)
        return;

    int value = MapToCrtStatus(result);
    context.StoreNumeric(crtStatusSymbol, value);
}
```

- `ExecutionContext` is the runtime data access layer.
- `StoreNumeric` writes an integer into a PIC 9(n) item with proper formatting.
