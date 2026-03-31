namespace CobolSharp.Runtime.Terminal;

public interface ITerminalDevice
{
    void Render(TerminalBuffer buffer);
    void MoveCursor(int row, int col);
    TerminalKeyEvent ReadKey();
    void Flush();
}
