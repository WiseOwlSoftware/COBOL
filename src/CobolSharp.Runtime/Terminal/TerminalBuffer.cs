namespace CobolSharp.Runtime.Terminal;

public sealed class TerminalBuffer
{
    private readonly TerminalCell[,] _cells;

    public int Rows { get; }
    public int Columns { get; }

    public TerminalBuffer(int rows, int columns)
    {
        Rows = rows;
        Columns = columns;
        _cells = new TerminalCell[rows + 1, columns + 1];
    }

    public TerminalCell this[int row, int col]
    {
        get => _cells[row, col];
        set => _cells[row, col] = value;
    }

    public void Clear()
    {
    }

    public void Write(int row, int col, char ch, TerminalAttributes attr)
    {
    }

    public void WriteString(int row, int col, string text, TerminalAttributes attr)
    {
    }

    public TerminalBuffer Clone()
    {
        return new TerminalBuffer(Rows, Columns);
    }
}
