// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Frontend.Common;

namespace WiseOwl.Cobol.CodeGen;

/// <summary>
/// A minimal indentation-aware writer for emitting readable C# source text. WiseOwl COBOL emits C# as text and
/// hands it to Roslyn to parse + compile, so the generated <c>.cs</c> is directly inspectable — a deliberate
/// design choice (the output is meant to read like hand-written .NET).
/// </summary>
public sealed class CodeWriter
{
    private readonly StringBuilder _sb = new();
    private int _indent;
    private bool _atLineStart = true;

    // DESIGN-listing.md section 6.9: the statements being emitted, innermost last, each with whether its sequence point is written.
    private readonly List<StatementMapping> _mappings = [];
    private bool _mapsStatements;   // HideUnmappedLines was called: every line is hidden but a statement's sequence point
    private string? _documentRoot;   // the directory a directive names its file relative to; null: the full path
    private bool _hidden;      // the last line directive written is `#line hidden`
    private int _capturing;    // inside CaptureText: the text is spliced into one line, where no directive may stand

    /// <summary>Increase the indentation level by one (4 spaces).</summary>
    public void Indent() => _indent++;

    /// <summary>Decrease the indentation level by one.</summary>
    public void Outdent() => _indent = Math.Max(0, _indent - 1);

    /// <summary>Write a line at the current indentation. An empty argument writes a blank line.</summary>
    public void Line(string text = "")
    {
        if (text.Length == 0)
        {
            _sb.Append('\n');
            _atLineStart = true;
            return;
        }
        if (_atLineStart && _capturing == 0 && _mapsStatements && MapLine(text, ClosingPoint(text)) is { } rest)
        {
            // The line holds code after the statement's sequence point: that code is hidden, on a line of its own.
            WriteIndentIfNeeded();
            _sb.Append(text, 0, rest).Append('\n').Append("#line hidden\n");
            _hidden = true;
            _atLineStart = true;   // the rest keeps the line's indentation
            text = text[rest..].TrimStart();
        }
        WriteIndentIfNeeded();
        _sb.Append(text).Append('\n');
        _atLineStart = true;
    }

    /// <summary>
    /// Write an open-brace line and increase indentation; pair with <see cref="CloseBrace"/>. Returns a
    /// disposable so callers can use a <c>using</c> block to scope a brace pair.
    /// </summary>
    public BlockScope Block(string header)
    {
        Line(header);
        Line("{");
        Indent();
        return new BlockScope(this);
    }

    /// <summary>Outdent and write a closing brace.</summary>
    public void CloseBrace()
    {
        Outdent();
        Line("}");
    }

    /// <summary>⛔ THE ROSLYN BACKEND'S SEQUENCE POINTS (DESIGN-listing.md section 6.9; behind the backend seam): from here on, every line
    /// written outside a statement is hidden from a debugger (<c>#line hidden</c>), so the only sequence points of the program are the
    /// statements' own. The program emitter calls it once, after the file's header. Each directive names its file relative to
    /// <paramref name="documentRoot"/> (<see cref="BackendOptions.DocumentRoot"/>), or by its full path when it is null.</summary>
    public void HideUnmappedLines(string? documentRoot)
    {
        _documentRoot = documentRoot;
        _sb.Append("#line hidden\n");
        _hidden = true;
        _mapsStatements = true;
    }

    /// <summary>Map the C# a statement emits to where the statement is written (<paramref name="span"/>; null: a statement with no
    /// position of its own, whose code belongs to the statement enclosing it), until the scope is disposed. The statement's first line
    /// that certainly carries a sequence point (<see cref="FirstSequencePoint(string)"/>) is mapped to the span exactly, and so is every line
    /// of code before it whose code an optimized build may drop (a local declared with a constant, <see cref="PointOf(string)"/>), so no
    /// code of the statement falls into the range of the statement before it; every line after it is hidden, so a debugger stops once
    /// per COBOL statement and the PDB holds one sequence point at its position, spanning the statement (two, when the statement's code
    /// begins with a constant store the build kept and the writer cannot show it kept, <see cref="Settle"/>). A statement nested in it
    /// maps its own first line; the enclosing statement's next line of code after it (a PERFORM's step and test after its body) takes one
    /// more sequence point at the same position, spanning only the verb, so that code is the enclosing statement's and not the nested
    /// one's. A nested node at the same span (a wrapper's inner node) is the same statement and maps nothing of its own.</summary>
    public StatementScope MapStatement(StatementSpan? span)
    {
        if (span is null || (_mappings.Count > 0 && _mappings[^1].Span == span)) return default;
        _mappings.Add(new StatementMapping(span));
        return new StatementScope(this);
    }

    /// <summary>End the innermost statement's mapping, settling its droppable points (<see cref="Settle"/>). The statement enclosing it, if
    /// any, maps the code it writes next (a PERFORM's step and test after its body) to its verb, once more.</summary>
    private void EndStatement()
    {
        var ended = _mappings[^1];
        _mappings.RemoveAt(_mappings.Count - 1);
        // The later stretches first: settling one rewrites text after the earlier ones' positions, never before.
        for (int i = (ended.Droppable?.Count ?? 0) - 1; i >= 0; i--) Settle(ended.Droppable![i]);
        if (_mappings.Count > 0 && _mappings[^1].Written) _mappings[^1].Resumed = true;
    }

    /// <summary>Write the line directive the line <paramref name="text"/> needs: while the innermost statement's sequence point is not
    /// written and the line begins code that carries one (<see cref="PointOf(string)"/>), the statement's span, at that code's column; else
    /// <c>#line hidden</c> (once). A line whose code an optimized build may drop (a local declared with a constant, which the build drops
    /// when the code never reads the local) takes the span and leaves the point unwritten: Roslyn keeps one sequence point per IL offset,
    /// the last, so the next line that certainly carries one takes the span again, and the statement keeps its point whether the build
    /// dropped that code or not (DESIGN-listing.md section 6.9). Returns the index in <paramref name="text"/> after that first code when
    /// more code follows it on the line, which <see cref="Line"/> then writes hidden on a line of its own (a debugger stops once per
    /// statement, and the PDB holds no sequence point at a column the statement does not begin at); null otherwise.</summary>
    private int? MapLine(string text, (int Start, int End)? closing)
    {
        if (text[0] == '#') return null;   // a directive of the emitter's own carries no code
        if (_mappings.Count > 0 && _mappings[^1] is { } mapping && (!mapping.Written || mapping.Resumed))
        {
            // `#line (line, column) - (endLine, endColumn) offset "file"` maps the code at 1-based character `offset` of the next line to
            // the span, whose end is inclusive (C# 10's span form). The statement's first sequence point spans the statement; a later
            // one (resumed after a nested statement) spans its verb.
            var span = mapping.Span;
            var (endLine, endColumn) = mapping.Written ? (span.Line, span.VerbEndColumn) : (Math.Max(span.EndLine, span.Line), span.EndColumn);
            endColumn = endLine == span.Line ? Math.Max(endColumn - 1, span.Column) : Math.Max(endColumn - 1, 1);
            if ((closing is { } brace ? new LinePoint(brace.Start, brace.End, null) : PointOf(text)) is { } at)
            {
                int directive = _sb.Length;
                _sb.Append(CultureInfo.InvariantCulture,
                    $"#line ({span.Line}, {span.Column}) - ({endLine}, {endColumn}) {_indent * 4 + at.Start + 1} \"{DirectiveFile(span.File)}\"\n");
                _hidden = false;
                var stretch = mapping.Droppable is [.., { Confirmation: null } open] ? open : null;
                if (at.Droppable is { } locals)
                {
                    if (stretch is null) (mapping.Droppable ??= []).Add(stretch = new DroppableStretch(directive));
                    stretch.Locals.AddRange(locals);
                }
                else
                {
                    if (stretch is not null) stretch.Confirmation = (directive, _sb.Length - directive);
                    mapping.Resumed = false;
                    mapping.Written = true;
                }
                return text.AsSpan(at.End).IsWhiteSpace() ? null : at.End;
            }
        }
        if (_hidden) return null;
        _sb.Append("#line hidden\n");
        _hidden = true;
        return null;
    }

    /// <summary>The brace structure of the lines written (DESIGN-listing.md section 6.9): the reading of a line alone cannot tell what a
    /// closing brace closes, and an optimized build gives the brace that ends a try, catch, finally, lock or using block a sequence point
    /// of its own (the leave from the block, the release of the lock, the dispose), which is code of the statement that wrote the block.
    /// Each block a line opens at its end is pushed (whether its end is such a point), each brace a line begins with pops one, and the
    /// first popped brace that ends such a block is the line's first point. A line that opens and closes its braces itself (<c>try { x; }
    /// catch { }</c>) changes nothing. Returns that brace's place in <paramref name="text"/>, or null.</summary>
    private (int Start, int End)? ClosingPoint(string text)
    {
        if (text[0] == '#') return null;   // a directive of the emitter's own
        int at = 0;
        (int, int)? point = null;
        while (at < text.Length && (text[at] == ' ' || text[at] == '}'))
        {
            if (text[at] == '}' && _blocks.TryPop(out bool ends) && ends && point is null) point = (at, at + 1);
            at++;
        }
        var clause = text.AsSpan(at).TrimEnd();
        // A trailing comment is not code: the clause ends before it (a line with a string before its `//` is taken whole).
        if (clause.IndexOf("//", StringComparison.Ordinal) is var comment and >= 0 && clause[..comment].IndexOfAny('"', '\'') < 0)
            clause = clause[..comment].TrimEnd();
        if (clause.Length == 0) return point;   // a comment or the braces alone: the header before them still heads the next block
        bool pointed = PointedBlock(clause);
        if (clause[^1] == '{') _blocks.Push(clause.Length == 1 ? _pointedHeader : pointed);
        _pointedHeader = pointed;
        return point;

        // A try, catch, finally, lock or using clause: the block it heads ends at a sequence point.
        static bool PointedBlock(ReadOnlySpan<char> clause) =>
            Keyword(clause, "try") || Keyword(clause, "catch") || Keyword(clause, "finally") || Keyword(clause, "lock")
            || (Keyword(clause, "using") && clause.TrimEnd('{').TrimEnd() is [.., ')']);
    }

    private readonly Stack<bool> _blocks = new();   // each open block a line opened, innermost on top: whether its closing brace is a point
    private bool _pointedHeader;   // the last line written heads a block whose closing brace is a point (its brace on the next line)

    /// <summary>Whether <paramref name="s"/> begins with the keyword <paramref name="word"/>.</summary>
    private static bool Keyword(ReadOnlySpan<char> s, string word) =>
        s.StartsWith(word, StringComparison.Ordinal) && (s.Length == word.Length || !SyntaxFacts.IsIdentifierPartCharacter(s[word.Length]));

    /// <summary>The file a line directive names: the one decider's name of it (<see cref="SourceDocuments.NameOf"/>, relative to the
    /// document root), computed once per file.</summary>
    private string DirectiveFile(string file)
    {
        if (_directiveFiles.TryGetValue(file, out string? named)) return named;
        return _directiveFiles[file] = SourceDocuments.NameOf(file, _documentRoot);
    }

    private readonly Dictionary<string, string> _directiveFiles = new(StringComparer.Ordinal);   // each file's name in a directive, once

    /// <summary>Where in <paramref name="text"/> the first code Roslyn gives a sequence point in an optimized build begins and ends, read
    /// with Roslyn's own parser so a line is classified as Roslyn compiles it; null when the line begins no such code (a brace, a label, a
    /// declaration with no initializer, a <c>try</c> or <c>do</c> header, a fragment of an expression). The sequence point of an expression
    /// statement, a jump (return, throw, goto, break, continue, yield) and a local declaration is the statement (the declaration up to its
    /// first initialized variable; a declaration whose initializer is a constant is none here, since an optimized build drops its code when
    /// nothing reads the local, and <see cref="PointOf(string)"/> reads it as droppable); of an if, while (not on the constant
    /// <c>true</c>), switch, lock or using statement its header, up to the closing parenthesis; of a for statement its declaration or first initializer, else its condition. A block, a labeled, checked,
    /// unchecked, try, do or for statement with neither has none of its own: its first statement's is the line's. A line that continues a
    /// statement an earlier line began (<c>else if (...)</c>, <c>else x = y;</c>, <c>catch (...) when (...)</c>, <c>} while (...);</c>,
    /// <c>case 1: x = y; break;</c>) has its clause's point (<see cref="Continuation"/>): code the emitter writes in an else, a filter or a
    /// switch section is a line of code like any other.
    /// <para><c>End</c> is where the line stops being that sequence point's: its end, or, sooner, the first place inside it that takes a
    /// sequence point of its own in another method (the first statement of a lambda's or an anonymous method's block, or its expression
    /// body; an arm of a switch expression), whose code must not be mapped to the statement's column.</para></summary>
    internal static (int Start, int End)? FirstSequencePoint(string text) =>
        PointOf(text) is { Droppable: null } point ? (point.Start, point.End) : null;

    /// <summary><see cref="FirstSequencePoint(string)"/>, and besides it the point of a line whose code an optimized build may drop: a local
    /// declaration whose first initialized variable is a constant (<see cref="LinePoint.Droppable"/>, the locals it declares).</summary>
    internal static LinePoint? PointOf(string text) =>
        SimpleStatement(text) is { } simple ? new LinePoint(simple.Start, simple.End, null) : ParsedPoint(text);

    /// <summary><see cref="FirstSequencePoint(string)"/> read by Roslyn's parser alone.</summary>
    internal static (int Start, int End)? ParsedSequencePoint(string text) =>
        ParsedPoint(text) is { Droppable: null } point ? (point.Start, point.End) : null;

    /// <summary><see cref="PointOf(string)"/> read by Roslyn's parser alone.</summary>
    private static LinePoint? ParsedPoint(string text)
    {
        // A line that continues a statement an earlier line began (an else, a catch or finally clause, a do statement's while, a switch
        // label; each perhaps after the braces that close the earlier part) is parsed inside the head that makes it a statement, and its
        // sequence point is that clause's: an `else if (...)` is the nested if's header, which an EVALUATE's second WHEN compiles to.
        var (head, clause) = Continuation(text);
        var statement = SyntaxFactory.ParseStatement(head + text);
        if (clause(statement, head.Length, head.Length + text.Length) is not { } point) return null;
        int end = point.End;
        foreach (var node in statement.DescendantNodes(n => n.Span.Start < end))
        {
            int nested = node switch
            {
                AnonymousFunctionExpressionSyntax { Body: BlockSyntax block } => block.Statements.FirstOrDefault()?.SpanStart ?? int.MaxValue,
                AnonymousFunctionExpressionSyntax function => function.Body.SpanStart,
                SwitchExpressionArmSyntax arm => arm.SpanStart,
                _ => int.MaxValue,
            };
            if (nested > point.Start && nested < end) end = nested;
        }
        return point with { Start = point.Start - head.Length, End = end - head.Length };
    }

    /// <summary>The head that makes <paramref name="text"/> a statement, and the reading of that statement's sequence point inside the line
    /// (its text from <c>from</c> to <c>to</c>): no head and the statement's own point for a line that begins a statement; for a line that
    /// begins with closing braces, an <c>else</c>, a <c>catch</c>, a <c>finally</c>, a <c>while</c> after a brace, or a switch label, a head
    /// that opens what the line closes and the clause's point. An optimized build gives an else clause its statement's point, a catch its
    /// filter's (<c>when (...)</c>) or else its header's (<c>catch (...)</c>, where the caught exception is taken), the brace that closes a
    /// try block before a catch or finally on its line a point of its own (the leave from the block), a finally its block's first statement's, a do statement's
    /// condition the span from <c>while</c> to its semicolon, and a switch section its first statement's.</summary>
    private static (string Head, Func<StatementSyntax, int, int, LinePoint?> Clause) Continuation(string text)
    {
        int at = 0, braces = 0;
        while (at < text.Length && (text[at] == ' ' || text[at] == '}'))
            if (text[at++] == '}') braces++;
        var rest = text.AsSpan(at);
        // What the head opens: the braces the line closes, else an empty block for the clause to follow.
        string open = braces == 0 ? "{} " : new string('{', braces);
        if (Keyword(rest, "else"))
            return ("if (true) " + open, static (s, from, to) => s is IfStatementSyntax { Else.Statement: { } e } ? PointOf(e, from, to) : null);
        // The brace that closes a try block on the line is a point of its own (the leave from the block), before the clause's.
        if (braces > 0 && (Keyword(rest, "catch") || Keyword(rest, "finally")))
            return ("try " + open, static (s, from, to) => s is TryStatementSyntax t
                ? Within((t.Block.CloseBraceToken.SpanStart, t.Block.CloseBraceToken.Span.End), from, to) : null);
        if (Keyword(rest, "catch"))
            return ("try " + open, static (s, from, to) => s is TryStatementSyntax { Catches: [var c, ..] }
                ? c.Filter is { } filter
                    ? filter.CloseParenToken.IsMissing ? null : Within((filter.SpanStart, filter.CloseParenToken.Span.End), from, to)
                    : Within((c.CatchKeyword.SpanStart, c.Declaration is { CloseParenToken.IsMissing: false } d
                        ? d.CloseParenToken.Span.End : c.CatchKeyword.Span.End), from, to)
                : null);
        if (Keyword(rest, "finally"))
            return ("try " + open, static (s, from, to) => s is TryStatementSyntax { Finally.Block: { } block }
                ? PointOf(block.Statements.FirstOrDefault(), from, to) : null);
        if (braces > 0 && Keyword(rest, "while"))
            return ("do " + open, static (s, from, to) => s is DoStatementSyntax d && !d.SemicolonToken.IsMissing
                ? Within((d.WhileKeyword.SpanStart, d.SemicolonToken.Span.End), from, to) : null);
        if (braces == 0 && (Keyword(rest, "case") || Keyword(rest, "default")))
            return ("switch (0) { ", static (s, from, to) => s is SwitchStatementSyntax { Sections: [var section, ..] }
                ? PointOf(section.Statements.FirstOrDefault(), from, to) : null);
        return ("", static (s, from, to) => PointOf(s, from, to));
    }

    /// <summary>The sequence point of a line that is one simple statement, read without a parse (most of a program's lines are, and a
    /// parse per statement cost every compilation several megabytes): an assignment or a call ending the line with its only semicolon,
    /// holding no string, character, brace, lambda or declaration, whose point is the whole line. Null when the line is anything else,
    /// and <see cref="FirstSequencePoint(string)"/> parses it; <c>CodeWriterTests</c> holds the two readings equal on every shape.</summary>
    internal static (int Start, int End)? SimpleStatement(string text)
    {
        int end = text.Length;
        while (end > 0 && text[end - 1] == ' ') end--;
        if (end < 2 || text[end - 1] != ';') return null;
        int start = 0;
        while (start < end && text[start] == ' ') start++;
        if (start == end || !(char.IsAsciiLetter(text[start]) || text[start] == '_')) return null;
        var body = text.AsSpan(start, end - 1 - start);
        if (body.IndexOfAny("\"'{};") >= 0 || body.Contains("=>", StringComparison.Ordinal)) return null;
        // An assignment's target is one name, member or element: a space before its '=' ends that, and a declaration (a type, a space, a
        // name) has one, as has a keyword statement (return, throw, goto, new ...), each left to the parse.
        int assign = body.IndexOf(" = ", StringComparison.Ordinal);
        int open = body.IndexOf('(');
        int head = assign >= 0 && (open < 0 || assign < open) ? assign : open;
        if (head <= 0 || body[..head].Contains(' ')) return null;
        return (start, end);
    }

    /// <summary>The point <paramref name="point"/> when it lies on the line, the text from <paramref name="from"/> to <paramref name="to"/>,
    /// and holds a character of it; null otherwise.</summary>
    private static LinePoint? Within((int From, int To) point, int from, int to) =>
        point.From >= from && point.From < to && point.To <= to && point.To - point.From > 0 ? new LinePoint(point.From, point.To, null) : null;

    private static LinePoint? PointOf(StatementSyntax? statement, int from, int to)
    {
        switch (statement)
        {
            case null:
                return null;
            case ExpressionStatementSyntax e:
                return e.Expression.IsMissing || e.SemicolonToken.IsMissing ? null : Within((e.SpanStart, e.Span.End), from, to);
            case ReturnStatementSyntax or ThrowStatementSyntax or GotoStatementSyntax or BreakStatementSyntax or ContinueStatementSyntax
                or YieldStatementSyntax:
                return statement.GetLastToken().IsMissing ? null : Within((statement.SpanStart, statement.Span.End), from, to);
            case LocalDeclarationStatementSyntax d when d.Declaration.Variables.FirstOrDefault(v => v.Initializer is not null) is { } first:
            {
                var point = first == d.Declaration.Variables[^1]
                    ? d.SemicolonToken.IsMissing ? null : Within((d.SpanStart, d.Span.End), from, to)
                    : Within((d.SpanStart, first.Span.End), from, to);
                // A constant's store is dropped by an optimized build when nothing reads the local: the point is droppable.
                return first.Initializer!.Value is LiteralExpressionSyntax or DefaultExpressionSyntax && point is { } droppable
                    ? droppable with { Droppable = [.. d.Declaration.Variables.Select(v => v.Identifier.ValueText)] }
                    : point;
            }
            case IfStatementSyntax i:
                return i.CloseParenToken.IsMissing ? null : Within((i.SpanStart, i.CloseParenToken.Span.End), from, to);
            case WhileStatementSyntax w when !Microsoft.CodeAnalysis.CSharpExtensions.IsKind(w.Condition, SyntaxKind.TrueLiteralExpression):
                return w.CloseParenToken.IsMissing ? null : Within((w.SpanStart, w.CloseParenToken.Span.End), from, to);
            case SwitchStatementSyntax sw:
                return sw.CloseParenToken.IsMissing ? null : Within((sw.SpanStart, sw.CloseParenToken.Span.End), from, to);
            case LockStatementSyntax l:
                return l.CloseParenToken.IsMissing ? null : Within((l.SpanStart, l.CloseParenToken.Span.End), from, to);
            case UsingStatementSyntax u when u.Expression is not null || u.Declaration is not null:
                return u.CloseParenToken.IsMissing ? null : Within((u.SpanStart, u.CloseParenToken.Span.End), from, to);
            case ForStatementSyntax f when f.Declaration is { } declaration:
                return Within((declaration.SpanStart, declaration.Span.End), from, to);
            case ForStatementSyntax f when f.Initializers.Count > 0:
                return Within((f.Initializers[0].SpanStart, f.Initializers[0].Span.End), from, to);
            case ForStatementSyntax f when f.Condition is { } condition:
                return Within((condition.SpanStart, condition.Span.End), from, to);
            case ForStatementSyntax f:
                return PointOf(f.Statement, from, to);
            case BlockSyntax b:
                return PointOf(b.Statements.FirstOrDefault(), from, to);
            case LabeledStatementSyntax labeled:
                return PointOf(labeled.Statement, from, to);
            case CheckedStatementSyntax c:
                return PointOf(c.Block, from, to);
            case TryStatementSyntax t:
                return PointOf(t.Block, from, to);
            case DoStatementSyntax d:
                return PointOf(d.Statement, from, to);
            default:
                return null;
        }
    }

    private void WriteIndentIfNeeded()
    {
        if (!_atLineStart) return;
        _sb.Append(' ', _indent * 4);
        _atLineStart = false;
    }

    /// <summary>
    /// Run <paramref name="emit"/> and return everything it wrote, INSTEAD of appending it to the source — the
    /// statement-emitter's output captured as a string.
    /// <para>It exists for EXPRESSION-POSITION statement text: a <c>BoundUdfEvaluated</c> per-evaluation window
    /// (ISO §2023:8.8.4.13 r2) renders its pre-ops inside an immediately-invoked <c>Func&lt;bool&gt;</c>, whose body is
    /// C# statement text the condition renderer must have as a string. Capturing lets that body come from the ONE
    /// statement emitter — so a hoisted store gets the same scale alignment, truncation and wide-tier handling as
    /// it would at statement position — instead of a second hand-written store renderer.</para>
    /// <para>Indentation is reset to zero for the duration (the captured text is spliced into a single line) and
    /// restored afterwards, so a capture cannot perturb the surrounding layout. Re-entrant: the nested capture's
    /// span is its own.</para>
    /// </summary>
    public string CaptureText(Action emit)
    {
        int start = _sb.Length, savedIndent = _indent;
        bool savedAtLineStart = _atLineStart;
        _indent = 0;
        _atLineStart = true;
        _capturing++;
        try
        {
            emit();
            return _sb.ToString(start, _sb.Length - start);
        }
        finally
        {
            _capturing--;
            _sb.Length = start;
            _indent = savedIndent;
            _atLineStart = savedAtLineStart;
        }
    }

    /// <summary>The accumulated C# source.</summary>
    public override string ToString() => _sb.ToString();

    /// <summary>Where a line's first sequence point lies in its text (<see cref="PointOf(string)"/>).</summary>
    /// <param name="Start">The index in the line where the point's code begins.</param>
    /// <param name="End">The index where the line stops being that point's.</param>
    /// <param name="Droppable">Null when an optimized build certainly gives that code its point; else the locals the line declares with a
    /// constant, whose store the build drops when nothing reads them, the point with it.</param>
    internal readonly record struct LinePoint(int Start, int End, IReadOnlyList<string>? Droppable);

    /// <summary>A statement's lines from its first droppable point (at <see cref="At"/> in the text) to the line that certainly carries the
    /// point, every one mapped to the same span.</summary>
    private sealed class DroppableStretch(int at)
    {
        /// <summary>Where the stretch's first directive begins in the text.</summary>
        public int At { get; } = at;

        /// <summary>The locals its droppable lines declare with a constant.</summary>
        public List<string> Locals { get; } = [];

        /// <summary>Where the directive of the line that certainly carries the point lies in the text, once it is written.</summary>
        public (int At, int Length)? Confirmation { get; set; }
    }

    /// <summary>Settle a statement's droppable stretch at its end (DESIGN-listing.md section 6.9). When the build certainly keeps the store
    /// of one of its locals (<see cref="Kept"/>), the stretch's first point stands and the confirming directive becomes <c>#line hidden</c>,
    /// so a debugger stops once; else both stay, and the PDB holds the point the build kept, or both (two stops on the statement, at its
    /// span, rather than the statement's first code in the range of the statement before it).</summary>
    private void Settle(DroppableStretch stretch)
    {
        if (stretch.Confirmation is not { } confirmation || !Kept(_sb.ToString(stretch.At, _sb.Length - stretch.At), stretch.Locals)) return;
        _sb.Remove(confirmation.At, confirmation.Length).Insert(confirmation.At, "#line hidden\n");
    }

    /// <summary>Whether an optimized build certainly keeps the constant store that declares one of <paramref name="locals"/> in
    /// <paramref name="text"/> (a statement's C# from that declaration to its end). Roslyn drops a local's stores only when nothing reads
    /// the local, or when it keeps the local on the evaluation stack (a store overwritten before any read, in straight-line code); a local
    /// used inside a try, catch or finally block, a lambda or a local function, the body of an if or a switch section, or a loop, or passed
    /// by reference, is never kept on the stack. So the store is kept when the local is read and used in one of those places; otherwise
    /// the writer cannot tell, and says no.
    /// <c>CodeWriterTests</c> holds this to Roslyn's own PDB.</summary>
    private static bool Kept(string text, List<string> locals)
    {
        var block = SyntaxFactory.ParseStatement("{\n" + text + "\n}");
        foreach (string local in locals)
        {
            bool read = false, unstacked = false;
            foreach (var name in block.DescendantNodes().OfType<IdentifierNameSyntax>().Where(n => n.Identifier.ValueText == local))
            {
                read |= name.Parent is not AssignmentExpressionSyntax { RawKind: (int)SyntaxKind.SimpleAssignmentExpression } assignment
                    || assignment.Left != name;
                unstacked |= name.Parent is ArgumentSyntax { RefKindKeyword.RawKind: not (int)SyntaxKind.None }
                    || name.AncestorsAndSelf().Zip(name.Ancestors()).Any(pair => Unstacking(pair.Second, pair.First));
            }
            if (read && unstacked) return true;
        }
        return false;

        // Whether a use inside `child` of `node` is one the evaluation stack cannot carry from the declaration: in an exception region, a
        // lambda or a local function, in the code an if or a switch runs on one path only, or in a loop (its test, step or body, which run
        // after a branch back).
        static bool Unstacking(Microsoft.CodeAnalysis.SyntaxNode node, Microsoft.CodeAnalysis.SyntaxNode child) => node switch
        {
            TryStatementSyntax t => child == t.Block,
            CatchClauseSyntax or FinallyClauseSyntax or AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax => true,
            IfStatementSyntax i => child == i.Statement || child == i.Else,
            SwitchSectionSyntax or WhileStatementSyntax or DoStatementSyntax or ForEachStatementSyntax => true,
            ForStatementSyntax f => child == f.Condition || child == f.Statement || (child is ExpressionSyntax e && f.Incrementors.Contains(e)),
            _ => false,
        };
    }

    /// <summary>One statement whose code is being mapped to <see cref="Span"/>.</summary>
    private sealed class StatementMapping(StatementSpan span)
    {
        public StatementSpan Span { get; } = span;

        /// <summary>Its droppable stretches (<see cref="MapLine"/>), in the order written: its first point's, and each resumed one's; null
        /// while it has none (most statements).</summary>
        public List<DroppableStretch>? Droppable { get; set; }

        /// <summary>The statement's sequence point is written: its later lines are hidden.</summary>
        public bool Written { get; set; }

        /// <summary>A statement nested in it ended: its next line of code takes a sequence point of its own, at its verb.</summary>
        public bool Resumed { get; set; }
    }

    /// <summary>A <c>using</c>-scoped statement mapping (<see cref="MapStatement"/>); the default value maps nothing.</summary>
    public readonly struct StatementScope(CodeWriter? writer) : IDisposable
    {
        /// <summary>End the statement's mapping.</summary>
        public void Dispose() => writer?.EndStatement();
    }

    /// <summary>A <c>using</c>-scoped brace pair; closing it emits the matching <c>}</c>.</summary>
    public readonly struct BlockScope(CodeWriter writer) : IDisposable
    {
        /// <summary>Emit the closing brace for the block.</summary>
        public void Dispose() => writer.CloseBrace();
    }
}
