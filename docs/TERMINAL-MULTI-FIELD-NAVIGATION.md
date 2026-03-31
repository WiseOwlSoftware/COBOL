# Multi-Field Navigation Semantics

## 1. Purpose

COBOL ACCEPT can operate on:

- A single screen item
- A form consisting of multiple screen items

We define deterministic navigation rules for:

- Enter
- Tab
- Shift-Tab (future)
- Function keys
- Escape

---

## 2. Field Ordering

Fields are ordered by:

- LINE ascending
- COLUMN ascending
- Stable order for ties

Binder guarantees each field has resolved LINE/COL.

```csharp
IReadOnlyList<BoundScreenItem> BuildFieldList(BoundScreenSection section)
{
    return section.Items
        .SelectMany(Flatten)
        .Where(i => i.PicString != null
            && (i.UsingField != null || i.ToTarget != null))
        .OrderBy(i => i.Line)
        .ThenBy(i => i.Column)
        .ToList();
}
```

---

## 3. Navigation Rules

### Enter

- If FULL/REQUIRED satisfied:
  - If not last field: move to next field
  - If last field: complete form
- If not satisfied: reject, beep if BELL

### Tab

- Always attempts to move to next field
- FULL/REQUIRED still enforced
- If last field: completes form

### Shift-Tab (future)

- Moves to previous field
- FULL/REQUIRED enforced on current field before leaving

### Function Keys / Escape

- Immediately complete form
- FULL/REQUIRED enforced
- CRT STATUS reflects key

### Arrow Keys

- For now: ignored
- Future: Up/Down move between fields in same column region

---

## 4. Form-Level ACCEPT Loop

```csharp
public TerminalFormResult AcceptForm(IReadOnlyList<BoundScreenItem> fields)
{
    int index = 0;
    TerminalInputResult? last = null;

    while (true)
    {
        var field = fields[index];
        last = Accept(field); // single-field loop

        if (IsFormCompletionKey(last.LastKey))
            return new TerminalFormResult(fields, last);

        if (last.LastKey == TerminalKey.Enter
            || last.LastKey == TerminalKey.Tab)
        {
            if (index < fields.Count - 1)
            {
                index++;
                continue;
            }
            return new TerminalFormResult(fields, last);
        }

        // Future: Shift-Tab, Up/Down
    }
}
```

---

## 5. Writeback Semantics

Each `Accept(field)` writes:

- `result.Text` to USING/TO target
- CRT STATUS updated once at end of form
