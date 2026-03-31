using System.Collections.Generic;

namespace CobolSharp.Runtime.Terminal;

public sealed class HeadlessTerminalDevice : ITerminalDevice
{
    private readonly Queue<TerminalKeyEvent> _input = new();
    private readonly List<TerminalBuffer> _frames = new();
    private readonly List<(int Row, int Col)> _cursorMoves = new();

    public IReadOnlyList<TerminalBuffer> Frames => _frames;
    public IReadOnlyList<(int Row, int Col)> CursorMoves => _cursorMoves;

    public HeadlessTerminalDevice(IEnumerable<TerminalKeyEvent> scriptedInput)
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
