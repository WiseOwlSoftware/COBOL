namespace CobolSharp.Runtime.Terminal;

// Placeholder — will be implemented when Screen I/O runtime (M429) is built.
// BoundScreenItem references will be replaced with a runtime-layer screen model.
public sealed class TerminalSession
{
    public TerminalBuffer Buffer { get; }
    public ITerminalDevice Device { get; }

    public TerminalSession(ITerminalDevice device, int rows = 24, int columns = 80)
    {
        Device = device;
        Buffer = new TerminalBuffer(rows, columns);
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
}
