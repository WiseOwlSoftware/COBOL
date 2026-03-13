using CobolSharp.Compiler.Common;

namespace CobolSharp.Compiler.Lexing;

/// <summary>
/// A single token produced by the COBOL lexer.
/// </summary>
public sealed class Token
{
    public TokenKind Kind { get; }
    public string Text { get; }
    public TextSpan Span { get; }
    public object? Value { get; }

    public Token(TokenKind kind, string text, TextSpan span, object? value = null)
    {
        Kind = kind;
        Text = text;
        Span = span;
        Value = value;
    }

    public override string ToString() => $"{Kind} '{Text}' {Span}";
}
