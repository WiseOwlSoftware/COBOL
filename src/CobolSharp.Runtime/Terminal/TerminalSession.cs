using CobolSharp.Compiler.Semantics.Bound;

namespace CobolSharp.Runtime.Terminal;

public sealed class TerminalSession
{
    public TerminalBuffer Buffer { get; }
    public ITerminalDevice Device { get; }

    public TerminalSession(ITerminalDevice device, int rows = 24, int columns = 80)
    {
        Device = device;
        Buffer = new TerminalBuffer(rows, columns);
    }

    public void DisplayScreen(IReadOnlyList<BoundScreenItem> items)
    {
    }

    public void DisplayItem(BoundScreenItem item)
    {
    }

    public TerminalInputResult Accept(BoundScreenItem item)
    {
        return new TerminalInputResult(CtrStatusCode.None, TerminalKey.None, string.Empty, 1, 1);
    }

    public TerminalInputResult Accept(IReadOnlyList<BoundScreenItem> items, string screenItemName)
    {
        return new TerminalInputResult(CtrStatusCode.None, TerminalKey.None, string.Empty, 1, 1);
    }

    public void MoveCursor(int row, int col)
    {
    }

    public (int Row, int Col) GetCursorPosition()
    {
        return (1, 1);
    }

    public void ClearScreen()
    {
    }

    public void Refresh()
    {
    }

    // Private helpers (empty signatures)
    private void RedrawField(BoundScreenItem item, string currentText)
    {
    }

    private bool CanExitField(BoundScreenItem item, string currentText)
    {
        return true;
    }

    private TerminalInputResult BuildResult(
        BoundScreenItem item,
        TerminalKeyEvent keyEvent,
        string currentText,
        int cursorIndex)
    {
        return new TerminalInputResult(CtrStatusCode.None, keyEvent.Key, currentText, 1, 1);
    }

    private void HandleCharacter(ref string currentText, ref int cursorIndex, char ch, int maxLength)
    {
    }

    private void HandleBackspace(ref string currentText, ref int cursorIndex)
    {
    }

    private void HandleLeft(ref int cursorIndex)
    {
    }

    private void HandleRight(ref int cursorIndex, int maxLength)
    {
    }
}
