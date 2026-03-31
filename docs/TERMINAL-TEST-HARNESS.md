# Test Harness Patterns for Scripted Screen I/O

## 1. Purpose

We need deterministic, headless tests for:

- ACCEPT
- DISPLAY
- FULL/REQUIRED
- SECURE
- CRT STATUS
- CURSOR

The test harness must:

- Simulate keystrokes
- Capture all rendered frames
- Capture cursor movements
- Assert final buffer state

---

## 2. HeadlessTerminalDevice

```csharp
public sealed class HeadlessTerminalDevice : ITerminalDevice
{
    private readonly Queue<TerminalKeyEvent> _input;
    private readonly List<TerminalBuffer> _frames = new();
    private readonly List<(int Row, int Col)> _cursorMoves = new();

    public IReadOnlyList<TerminalBuffer> Frames => _frames;
    public IReadOnlyList<(int Row, int Col)> CursorMoves => _cursorMoves;

    public HeadlessTerminalDevice(IEnumerable<TerminalKeyEvent> scriptedInput)
    {
        _input = new Queue<TerminalKeyEvent>(scriptedInput);
    }

    public void Render(TerminalBuffer buffer)
    {
        _frames.Add(buffer.Clone());
    }

    public void MoveCursor(int row, int col)
    {
        _cursorMoves.Add((row, col));
    }

    public TerminalKeyEvent ReadKey()
    {
        return _input.Count == 0
            ? new TerminalKeyEvent(TerminalKey.None)
            : _input.Dequeue();
    }

    public void Flush() { }
}
```

---

## 3. Script Helpers

```csharp
public static class TerminalScript
{
    public static IEnumerable<TerminalKeyEvent> FromString(string text)
    {
        foreach (char ch in text)
            yield return new TerminalKeyEvent(TerminalKey.Character, ch);
    }

    public static TerminalKeyEvent Enter() => new(TerminalKey.Enter);
    public static TerminalKeyEvent Tab() => new(TerminalKey.Tab);
    public static TerminalKeyEvent Backspace() => new(TerminalKey.Backspace);
}
```

---

## 4. Test Pattern: Simple ACCEPT

```csharp
[Fact]
public void Accept_SimpleField()
{
    var script = TerminalScript.FromString("HELLO")
        .Append(TerminalScript.Enter());

    var device = new HeadlessTerminalDevice(script);
    var session = new TerminalSession(device, 5, 20);

    var item = MakeItem("FIELD", 2, 5, "X(10)");

    session.DisplayItem(item);
    var result = session.Accept(item);

    Assert.Equal("HELLO", result.Text);

    var frame = device.Frames.Last();
    Assert.Equal('H', frame[2, 5].Char);
    Assert.Equal('O', frame[2, 9].Char);
}
```

---

## 5. Test Pattern: SECURE

```csharp
[Fact]
public void Accept_SecureField()
{
    var script = TerminalScript.FromString("SECRET")
        .Append(TerminalScript.Enter());

    var device = new HeadlessTerminalDevice(script);
    var session = new TerminalSession(device, 5, 20);

    var item = MakeItem("PWD", 3, 10, "X(10)", secure: true);

    session.DisplayItem(item);
    var result = session.Accept(item);

    Assert.Equal("SECRET", result.Text);

    var frame = device.Frames.Last();
    for (int i = 0; i < 6; i++)
        Assert.Equal('*', frame[3, 10 + i].Char);
}
```

---

## 6. Test Pattern: FULL/REQUIRED

```csharp
[Fact]
public void Accept_RequiredField_RejectsEmpty()
{
    var script = new[]
    {
        TerminalScript.Enter(), // rejected
        new TerminalKeyEvent(TerminalKey.Character, 'A'),
        TerminalScript.Enter()  // accepted
    };

    var device = new HeadlessTerminalDevice(script);
    var session = new TerminalSession(device, 5, 20);

    var item = MakeItem("REQ", 2, 5, "X(5)", required: true);

    session.DisplayItem(item);
    var result = session.Accept(item);

    Assert.Equal("A", result.Text);
}
```

---

## 7. Test Pattern: CRT STATUS

```csharp
[Fact]
public void Accept_UpdatesCrtStatus()
{
    var script = TerminalScript.FromString("X")
        .Append(TerminalScript.Function3());

    var device = new HeadlessTerminalDevice(script);
    var session = new TerminalSession(device, 5, 20);

    var item = MakeItem("F", 2, 5, "X(5)");
    var crt = MakeCrtStatusSymbol("CRT-STATUS", pic: "9(4)");

    session.DisplayItem(item);
    var result = session.Accept(item);

    UpdateCrtStatus(context.Data, crt, result);

    Assert.Equal(1003, context.Data.LoadNumeric(crt));
}
```
