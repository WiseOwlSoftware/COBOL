# ACCEPT Input Loop Specification

The ACCEPT loop is a deterministic state machine over a single field. This document
defines key-by-key behavior, AUTO/FULL/REQUIRED enforcement, and field rendering.

---

## 1. Preconditions

- `BoundScreenItem item` is:
  - Elementary (no children).
  - Has a `PicString`.
  - Has either `ToTarget` or `UsingField` (or both, via USING semantics).
- Screen has already been rendered into `TerminalSession.Buffer` (via `DisplayScreen`
  or equivalent).
- `item.Line` and `item.Column` are resolved absolute positions.
- `maxLength` is derived from `PicString` (PIC X(n), 9(n), etc.).

---

## 2. Field State

```csharp
int row = item.Line!.Value;
int col = item.Column!.Value;
int maxLength = ComputeFieldLength(item.PicString);
string currentText = InitializeFieldText(item); // from USING/TO or spaces
int cursorIndex = Math.Clamp(currentText.Length, 0, maxLength); // 0..maxLength
bool secure = item.Secure;
bool fullRequired = item.Full;
bool required = item.Required;
```

- `currentText` is the logical field content.
- `cursorIndex` is the position within the field (0 = before first char).
- `secure` controls echo behavior.

---

## 3. Initial Render

- For `i` in `[0..maxLength-1]`:
  - If `i < currentText.Length`:
    - If `secure`: show masking char (e.g., `'*'`).
    - Else: show `currentText[i]`.
  - Else: show space.
- Attributes from `BoundScreenItem` (highlight, colors, etc.).
- Cursor at `(row, col + cursorIndex)`.

---

## 4. Main Loop

```csharp
while (true)
{
    MoveCursor(row, col + cursorIndex);
    var keyEvent = Device.ReadKey();

    switch (keyEvent.Key)
    {
        case TerminalKey.Character:
            HandleCharacter(keyEvent.Payload!.Value);
            break;

        case TerminalKey.Backspace:
            HandleBackspace();
            break;

        case TerminalKey.Left:
            HandleLeft();
            break;

        case TerminalKey.Right:
            HandleRight();
            break;

        case TerminalKey.Tab:
            if (CanExitField())
                return BuildResult(keyEvent);
            BeepIfBell(item);
            break;

        case TerminalKey.Enter:
        case TerminalKey.Function1:
        case TerminalKey.Function2:
        case TerminalKey.Function3:
        case TerminalKey.Function4:
        case TerminalKey.Function5:
        case TerminalKey.Function6:
        case TerminalKey.Function7:
        case TerminalKey.Function8:
        case TerminalKey.Function9:
        case TerminalKey.Function10:
        case TerminalKey.Function11:
        case TerminalKey.Function12:
        case TerminalKey.Escape:
            if (CanExitField())
                return BuildResult(keyEvent);
            BeepIfBell(item);
            break;

        case TerminalKey.Up:
        case TerminalKey.Down:
        case TerminalKey.None:
        default:
            // No-op for single-field ACCEPT
            break;
    }
}
```

---

## 5. Handlers

### HandleCharacter

```csharp
void HandleCharacter(char ch)
{
    if (cursorIndex >= maxLength)
        return;

    var builder = new StringBuilder(currentText);

    if (cursorIndex < builder.Length)
        builder.Insert(cursorIndex, ch);
    else
        builder.Append(ch);

    if (builder.Length > maxLength)
        builder.Length = maxLength;

    currentText = builder.ToString();
    RedrawField();
    cursorIndex = Math.Min(cursorIndex + 1, maxLength);
}
```

### HandleBackspace

```csharp
void HandleBackspace()
{
    if (cursorIndex == 0)
        return;

    var builder = new StringBuilder(currentText);
    int deleteIndex = cursorIndex - 1;

    if (deleteIndex < builder.Length)
        builder.Remove(deleteIndex, 1);

    currentText = builder.ToString();
    RedrawField();
    cursorIndex = Math.Max(cursorIndex - 1, 0);
}
```

### HandleLeft / HandleRight

```csharp
void HandleLeft()
{
    if (cursorIndex > 0)
        cursorIndex--;
}

void HandleRight()
{
    if (cursorIndex < maxLength)
        cursorIndex++;
}
```

### RedrawField

```csharp
void RedrawField()
{
    var attr = TerminalAttributeMapper.GetAttributes(item);

    for (int i = 0; i < maxLength; i++)
    {
        char chToShow;
        if (i < currentText.Length)
            chToShow = secure ? '*' : currentText[i];
        else
            chToShow = ' ';

        Buffer.Write(row, col + i, chToShow, attr);
    }

    Device.Render(Buffer);
    Device.Flush();
}
```

---

## 6. Exit Conditions

### CanExitField

```csharp
bool CanExitField()
{
    if (required && string.IsNullOrWhiteSpace(currentText))
        return false;

    if (fullRequired && currentText.Length < maxLength)
        return false;

    return true;
}
```

### BeepIfBell

```csharp
void BeepIfBell(BoundScreenItem item)
{
    if (item.Bell)
    {
        // Device-specific beep; Console.Beep or similar.
    }
}
```

### BuildResult

```csharp
TerminalInputResult BuildResult(TerminalKeyEvent keyEvent)
{
    var status = MapStatus(keyEvent); // maps to CrtStatusCode
    return new TerminalInputResult(
        status,
        keyEvent.Key,
        currentText,
        row,
        col + cursorIndex);
}
```
