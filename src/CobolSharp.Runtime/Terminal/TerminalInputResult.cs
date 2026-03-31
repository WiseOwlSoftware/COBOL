namespace CobolSharp.Runtime.Terminal;

public enum CtrStatusCode
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
    public CtrStatusCode Status { get; }
    public TerminalKey LastKey { get; }
    public string Text { get; }
    public int CursorRow { get; }
    public int CursorColumn { get; }

    public TerminalInputResult(
        CtrStatusCode status,
        TerminalKey lastKey,
        string text,
        int cursorRow,
        int cursorColumn)
    {
        Status = status;
        LastKey = lastKey;
        Text = text;
        CursorRow = cursorRow;
        CursorColumn = cursorColumn;
    }
}
