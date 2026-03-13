namespace CobolSharp.Compiler.Common;

/// <summary>
/// Represents the text of a COBOL source file with line/column tracking.
/// </summary>
public sealed class SourceText
{
    private readonly string _text;
    private readonly int[] _lineStarts;

    public string FileName { get; }
    public int Length => _text.Length;

    private SourceText(string text, string fileName)
    {
        _text = text;
        FileName = fileName;
        _lineStarts = ComputeLineStarts(text);
    }

    public static SourceText From(string text, string fileName = "<anonymous>")
    {
        return new SourceText(text, fileName);
    }

    public static SourceText FromFile(string path)
    {
        var text = File.ReadAllText(path);
        return new SourceText(text, path);
    }

    public char this[int index] => _text[index];

    public int LineCount => _lineStarts.Length;

    /// <summary>
    /// Returns a substring of the source text.
    /// </summary>
    public string GetText(int start, int length) => _text.Substring(start, length);

    /// <summary>
    /// Returns the text within a span.
    /// </summary>
    public string GetText(TextSpan span) => _text.Substring(span.Start, span.Length);

    /// <summary>
    /// Gets the zero-based line number for an absolute position.
    /// </summary>
    public int GetLineNumber(int position)
    {
        int lo = 0, hi = _lineStarts.Length - 1;
        while (lo <= hi)
        {
            int mid = lo + (hi - lo) / 2;
            if (_lineStarts[mid] <= position)
                lo = mid + 1;
            else
                hi = mid - 1;
        }
        return hi;
    }

    /// <summary>
    /// Gets the zero-based column for an absolute position.
    /// </summary>
    public int GetColumn(int position)
    {
        int line = GetLineNumber(position);
        return position - _lineStarts[line];
    }

    /// <summary>
    /// Converts an absolute position to a SourceLocation.
    /// </summary>
    public SourceLocation GetLocation(int position)
    {
        int line = GetLineNumber(position);
        int column = position - _lineStarts[line];
        return new SourceLocation(FileName, position, line, column);
    }

    /// <summary>
    /// Returns the text of a given line (zero-based).
    /// </summary>
    public string GetLineText(int lineNumber)
    {
        int start = _lineStarts[lineNumber];
        int end = lineNumber + 1 < _lineStarts.Length
            ? _lineStarts[lineNumber + 1]
            : _text.Length;

        // Trim trailing \r\n
        while (end > start && (_text[end - 1] == '\n' || _text[end - 1] == '\r'))
            end--;

        return _text[start..end];
    }

    public override string ToString() => _text;

    private static int[] ComputeLineStarts(string text)
    {
        var starts = new List<int> { 0 };
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                starts.Add(i + 1);
            }
            else if (text[i] == '\r')
            {
                if (i + 1 < text.Length && text[i + 1] == '\n')
                    i++; // skip \n in \r\n
                starts.Add(i + 1);
            }
        }
        return starts.ToArray();
    }
}
