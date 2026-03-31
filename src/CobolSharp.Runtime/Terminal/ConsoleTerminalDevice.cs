namespace CobolSharp.Runtime.Terminal;

public sealed class ConsoleTerminalDevice : ITerminalDevice
{
    public ConsoleTerminalDevice(int rows = 24, int columns = 80)
    {
    }

    public void Render(TerminalBuffer buffer)
    {
    }

    public void MoveCursor(int row, int col)
    {
    }

    public TerminalKeyEvent ReadKey()
    {
        return new TerminalKeyEvent(TerminalKey.None);
    }

    public void Flush()
    {
    }
}
