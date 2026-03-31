# Runtime Entry Points: Bound Statements to TerminalSession

Defines how bound ACCEPT/DISPLAY statements call into TerminalSession, bridging the
compiler's bound tree to the runtime's terminal system.

---

## 1. Bound Statement Shapes

```csharp
public enum BoundAcceptKind
{
    Identifier,
    Screen,
    SystemDate,
    SystemTime,
    // etc.
}

public sealed class BoundAcceptStatement : BoundStatement
{
    public BoundAcceptKind Kind { get; }
    public DataSymbol? Target { get; }              // For identifier forms
    public BoundScreenItem? ScreenItem { get; }     // For screen forms
    public DataSymbol? CrtStatusSymbol { get; }     // From SPECIAL-NAMES, if any
}

public enum BoundDisplayKind
{
    Identifier,
    Literal,
    ScreenSection,
    ScreenItem
}

public sealed class BoundDisplayStatement : BoundStatement
{
    public BoundDisplayKind Kind { get; }
    public BoundScreenSection? ScreenSection { get; }
    public BoundScreenItem? ScreenItem { get; }
}
```

---

## 2. RuntimeContext

```csharp
public sealed class RuntimeContext
{
    public TerminalSession Terminal { get; }
    public ExecutionContext Data { get; }

    public RuntimeContext(TerminalSession terminal, ExecutionContext data)
    {
        Terminal = terminal;
        Data = data;
    }
}
```

---

## 3. DISPLAY screen-name Execution

```csharp
public static class DisplayExecutor
{
    public static void Execute(RuntimeContext context, BoundDisplayStatement stmt)
    {
        switch (stmt.Kind)
        {
            case BoundDisplayKind.ScreenSection:
                context.Terminal.DisplayScreen(stmt.ScreenSection!);
                break;

            case BoundDisplayKind.ScreenItem:
                context.Terminal.DisplayItem(stmt.ScreenItem!);
                break;

            // Other DISPLAY forms (identifiers, literals) handled elsewhere.
        }
    }
}
```

---

## 4. ACCEPT screen-name Execution

```csharp
public static class AcceptExecutor
{
    public static void Execute(RuntimeContext context, BoundAcceptStatement stmt)
    {
        switch (stmt.Kind)
        {
            case BoundAcceptKind.Screen:
                ExecuteScreenAccept(context, stmt);
                break;

            // Other ACCEPT forms handled elsewhere.
        }
    }

    private static void ExecuteScreenAccept(
        RuntimeContext context, BoundAcceptStatement stmt)
    {
        var item = stmt.ScreenItem!;
        var result = context.Terminal.Accept(item);

        // Determine logical target: USING or TO
        var target = item.UsingField ?? item.ToTarget;
        if (target is not null)
        {
            context.Data.StoreAlphanumeric(target, result.Text);
        }

        // Update CRT STATUS if configured
        UpdateCrtStatus(context.Data, stmt.CrtStatusSymbol, result);
    }
}
```

---

## 5. CURSOR Clause Integration

If SPECIAL-NAMES defines a CURSOR data item:

- Binder resolves it to a `DataSymbol`.

**Before DISPLAY/ACCEPT screen-form:**

```csharp
public static void ApplyCursorFromSpecialNames(
    RuntimeContext context,
    DataSymbol? cursorSymbol)
{
    if (cursorSymbol is null)
        return;

    var (row, col) = context.Data.LoadCursorPosition(cursorSymbol);
    context.Terminal.MoveCursor(row, col);
}
```

**After ACCEPT:**

```csharp
public static void UpdateCursorFromResult(
    RuntimeContext context,
    DataSymbol? cursorSymbol,
    TerminalInputResult result)
{
    if (cursorSymbol is null)
        return;

    context.Data.StoreCursorPosition(
        cursorSymbol, result.CursorRow, result.CursorColumn);
}
```

This keeps CURSOR semantics entirely in runtime, with no changes to the compiler
beyond binding the CURSOR data item.
