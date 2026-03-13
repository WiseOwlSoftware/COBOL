namespace CobolSharp.Compiler.Common;

/// <summary>
/// A contiguous region in source text, defined by start position and length.
/// </summary>
public readonly struct TextSpan : IEquatable<TextSpan>
{
    public int Start { get; }
    public int Length { get; }
    public int End => Start + Length;

    public TextSpan(int start, int length)
    {
        Start = start;
        Length = length;
    }

    public static TextSpan FromBounds(int start, int end) => new(start, end - start);

    public bool Contains(int position) => position >= Start && position < End;
    public bool OverlapsWith(TextSpan other) => Start < other.End && other.Start < End;

    public override string ToString() => $"[{Start}..{End})";

    public bool Equals(TextSpan other) => Start == other.Start && Length == other.Length;
    public override bool Equals(object? obj) => obj is TextSpan other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Start, Length);

    public static bool operator ==(TextSpan left, TextSpan right) => left.Equals(right);
    public static bool operator !=(TextSpan left, TextSpan right) => !left.Equals(right);
}
