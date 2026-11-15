// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;

namespace WiseOwl.Cobol.Frontend.Common;

/// <summary>Where a line of RESULTANT text came from (kb/Work PB82): the file it was read from and its 1-based
/// physical line in that file — the main source file, or a copybook incorporated by COPY (ISO §2023:7.2 text
/// manipulation: the compiler compiles the resultant text; the user edits the source files). A LINE only: the column
/// a resultant position stands at in that line is the <see cref="LineOrigin"/>'s to say.</summary>
public readonly record struct SourceOrigin(string File, int Line)
{
    /// <summary>This origin as a diagnostic <see cref="SourceLocation"/> (whose line is 0-based; the origin's is the
    /// 1-based physical line) at 0-based physical <paramref name="column"/> — the ONE conversion, so no reporter
    /// re-derives the off-by-one.</summary>
    public SourceLocation ToLocation(int column) => new(File, 0, Math.Max(Line - 1, 0), column);
}

/// <summary>One stretch of a resultant line piece (kb/Work PB1801): the characters from 0-based resultant column
/// <see cref="ResultColumn"/> up to the next span's came from <see cref="Origin"/>, the first of them at 0-based
/// PHYSICAL column <see cref="SourceColumn"/> of that line and each next one a column further. A physical column is a
/// CHARACTER POSITION of the line as the file holds it — before the line-entry stage expanded its tabs, so a TAB counts
/// one, as in every editor, and a supplementary-plane character counts one (kb/Work PB2983), whereas a resultant column
/// is a UTF-16 index of the resultant text, which is what the parser reports. The spans carry that conversion: the
/// reference-format stage that bears them splits a span at each tab and at each supplementary-plane character, so within
/// a span one resultant unit is exactly one physical position.
/// <para><see cref="Layout"/> is where the stretch's text was WRITTEN, when that is not where it stands (kb/Work PB5313): the
/// words of a pseudo-text-2 that a COPY or REPLACE replacement places into the resultant text stand at the position of the
/// text they replace — their <see cref="Origin"/>, which every diagnostic and EXCEPTION-LOCATION names (kb/Work PB1801) —
/// but each "begins in the same area of the resultant program as it appears in pseudo-text-2" (§1985:XII.2.4 GR9), so the
/// reference-format area of the text is that of the place it was written. Null when the two are one place.</para></summary>
public readonly record struct OriginSpan(int ResultColumn, SourceOrigin Origin, int SourceColumn, SourcePoint? Layout = null)
{
    /// <summary>The place whose reference-format area the stretch's first character takes: <see cref="Layout"/>, or the
    /// stretch's own origin.</summary>
    public SourcePoint LaidOutAt => Layout ?? new SourcePoint(Origin, SourceColumn);

    /// <summary>This stretch rebased <paramref name="delta"/> resultant units further on (each one physical position, within
    /// a span), at resultant column <paramref name="resultColumn"/>.</summary>
    public OriginSpan Advanced(int delta, int resultColumn) => new(resultColumn, Origin, SourceColumn + delta,
        Layout is { } layout ? layout with { Column = layout.Column + delta } : null);
}

/// <summary>A physical place: 0-based physical <paramref name="Column"/> of the line <paramref name="Origin"/> names.</summary>
public readonly record struct SourcePoint(SourceOrigin Origin, int Column);

/// <summary>
/// THE ORIGIN OF ONE RESULTANT LINE PIECE (kb/Work PB1801): its spans in resultant-column order. The resultant text is
/// not the text the user wrote — the fixed-form logical conversion drops positions 1–7 and joins a continuation line's
/// text onto the latest logical line (§2023:6.5), COPY splices library text into a line, REPLACE substitutes text of another
/// length (§2023:7.2) — so a resultant line maps to its source as a list of stretches, each at its own physical line and
/// column, and the LINE that names the piece (<see cref="Origin"/>) is the origin of its FIRST stretch (kb/Work PB82).
/// The spans are born where the reference format reads the physical lines and carried by <see cref="OriginWriter"/>
/// through every later stage; <see cref="Locate"/> is the one place a resultant column becomes a physical one.
/// </summary>
public sealed class LineOrigin
{
    private readonly OriginSpan[] _spans;

    /// <summary>A piece whose every character comes from <paramref name="origin"/>, the first at 0-based physical
    /// <paramref name="sourceColumn"/>.</summary>
    public LineOrigin(SourceOrigin origin, int sourceColumn = 0) => _spans = [new OriginSpan(0, origin, sourceColumn)];

    /// <param name="spans">At least one span, in strictly increasing <see cref="OriginSpan.ResultColumn"/> order.</param>
    /// <param name="omitted">See <see cref="Omitted"/>.</param>
    public LineOrigin(OriginSpan[] spans, bool omitted = false)
    {
        Omitted = omitted;
        if (spans.Length == 0)
            throw new ArgumentException("a line piece has at least one origin span (kb/Work PB1801)", nameof(spans));
        for (int i = 1; i < spans.Length; i++)
            if (spans[i].ResultColumn <= spans[i - 1].ResultColumn)
                throw new ArgumentException("origin spans are in strictly increasing resultant-column order (kb/Work PB1801)", nameof(spans));
        _spans = spans;
    }

    /// <summary>The file and physical line that name this piece: those of its first stretch (kb/Work PB82).</summary>
    public SourceOrigin Origin => _spans[0].Origin;

    /// <summary>Whether a text stage OMITTED the line this piece stands for (kb/Work PB4412): the stage dropped it from the
    /// resultant text, which holds an empty piece in its place — the lines of a false <c>&gt;&gt;IF</c> or <c>&gt;&gt;WHEN</c>
    /// branch (ISO §2023:7.3.16.4 GR 3). The compiler never read what the line holds, so a question about the line is asked of
    /// nothing: a diagnostic that explains a syntax error by the text of a line (<c>IMarginRCutLocator</c>) skips it. The
    /// stage that omits a line BORN-marks its origin (<see cref="AsOmitted"/>) and every stage after carries it with the origin
    /// (<see cref="From"/>), so a new kind of omission is one call at the stage that drops, and no reader keeps a list.</summary>
    public bool Omitted { get; }

    /// <summary>This piece, marked as a line a text stage omitted (<see cref="Omitted"/>).</summary>
    public LineOrigin AsOmitted() => Omitted ? this : new LineOrigin(_spans, omitted: true);

    /// <summary>The stretches, in resultant-column order.</summary>
    public ReadOnlySpan<OriginSpan> Spans => _spans;

    /// <summary>The physical origin of 0-based resultant <paramref name="column"/>: the span holding it, rebased to
    /// start there. A column before the first span (text a stage wrote ahead of the source's, such as the debugging-line
    /// carrier) stands at the first span's start; a column past the end of the piece continues its last span.</summary>
    public OriginSpan PointAt(int column)
    {
        int k = 0;
        while (k + 1 < _spans.Length && _spans[k + 1].ResultColumn <= column) k++;
        return _spans[k].Advanced(Math.Max(column - _spans[k].ResultColumn, 0), column);
    }

    /// <summary>The diagnostic location of 0-based resultant <paramref name="column"/> of this piece — the file, line
    /// AND column the user edits.</summary>
    public SourceLocation Locate(int column)
    {
        var at = PointAt(column);
        return at.Origin.ToLocation(at.SourceColumn);
    }

    /// <summary>This piece from resultant <paramref name="column"/> on, rebased so that column is column 0 — the origin of
    /// a sub-text that begins inside the piece (<see cref="MappedText.Slice"/>).</summary>
    public LineOrigin From(int column)
    {
        if (column <= 0) return this;
        var spans = new List<OriginSpan> { PointAt(column) with { ResultColumn = 0 } };
        foreach (var span in _spans)
            if (span.ResultColumn > column) spans.Add(span with { ResultColumn = span.ResultColumn - column });
        return new LineOrigin([.. spans], Omitted);
    }
}

/// <summary>
/// The RESULTANT-line → SOURCE-origin map of one compilation unit (kb/Work PB82, PB1801) — the <c>#line</c>-style table
/// the preprocessing chain builds (fixed-form continuation joins, conditional compilation, COPY expansion, REPLACE) and
/// every user-facing position consumes: the parser's diagnostic positions, the binder's diagnostic cursor, and
/// <c>FUNCTION EXCEPTION-LOCATION</c>'s line identifier. Internal anchors that must stay in RESULTANT space (the
/// &gt;&gt;TURN / &gt;&gt;FLAG event lines compared with token lines) never go through it.
/// </summary>
public sealed class SourceLineMap
{
    private readonly LineOrigin[] _origins;

    /// <param name="origins">One origin per resultant line piece (<c>text.Split('\n')</c> order), index 0 = line 1.</param>
    public SourceLineMap(LineOrigin[] origins) => _origins = origins;

    /// <summary>The identity map: resultant line n of <paramref name="file"/> is source line n, column for column.</summary>
    public static SourceLineMap Identity(string file, int lineCount)
    {
        var o = new LineOrigin[Math.Max(lineCount, 1)];
        for (int i = 0; i < o.Length; i++) o[i] = new LineOrigin(new SourceOrigin(file, i + 1));
        return new SourceLineMap(o);
    }

    /// <summary>The file and physical line of RESULTANT line <paramref name="line"/> (1-based, the ANTLR token line), or
    /// null when the line is outside the mapped text (a synthetic position).</summary>
    public SourceOrigin? Origin(int line) => line >= 1 && line <= _origins.Length ? _origins[line - 1].Origin : null;

    /// <summary>The first RESULTANT line (1-based) whose origin is <paramref name="origin"/>, or 0 when no line of the map has it.
    /// What a position known only as a place the user wrote (a COPY statement's file and line) is placed at in the resultant text,
    /// where the lines a statement leaves behind (the framing lines of its library text) keep its origin (kb/Work PB5482).</summary>
    public int ResultantLineOf(SourceOrigin origin)
    {
        if (_firstLineOf is null)
        {
            var first = new Dictionary<SourceOrigin, int>();
            for (int i = 0; i < _origins.Length; i++) first.TryAdd(_origins[i].Origin, i + 1);
            _firstLineOf = first;
        }
        return _firstLineOf.GetValueOrDefault(origin);
    }

    private Dictionary<SourceOrigin, int>? _firstLineOf;

    /// <summary>Whether a text stage omitted RESULTANT line <paramref name="line"/> (1-based; <see cref="LineOrigin.Omitted"/>);
    /// false for a line outside the map.</summary>
    public bool IsOmitted(int line) => line >= 1 && line <= _origins.Length && _origins[line - 1].Omitted;

    /// <summary>THE diagnostic location of RESULTANT line <paramref name="line"/> (1-based) at 0-based resultant
    /// <paramref name="column"/>: the physical file, line and column it came from (kb/Work PB1801), or
    /// <paramref name="fallbackFile"/> at that same line and column when the line is outside the map.</summary>
    public SourceLocation Locate(int line, int column, string fallbackFile) =>
        line >= 1 && line <= _origins.Length
            ? _origins[line - 1].Locate(column)
            : new SourceOrigin(fallbackFile, line).ToLocation(column);

    /// <summary>The stretch that holds 0-based resultant <paramref name="column"/> of RESULTANT line <paramref name="line"/>
    /// (1-based), rebased to start there (<see cref="LineOrigin.PointAt"/>) — its origin AND the place it was laid out at
    /// (<see cref="OriginSpan.LaidOutAt"/>); null when the line is outside the map.</summary>
    public OriginSpan? PointAt(int line, int column) =>
        line >= 1 && line <= _origins.Length ? _origins[line - 1].PointAt(column) : null;
}

/// <summary>Text under transformation together with its per-line origins (kb/Work PB82, PB1801): <see cref="Lines"/>
/// has one entry per piece of <c>Text.Split('\n')</c> — the invariant every mapped stage preserves.</summary>
public sealed class MappedText
{
    public string Text { get; }
    public LineOrigin[] Lines { get; }
    private int[]? _lineStarts;

    public MappedText(string text, LineOrigin[] lines)
    {
        int pieces = 1;
        foreach (char c in text) if (c == '\n') pieces++;
        if (lines.Length != pieces)
            throw new InvalidOperationException(
                $"MappedText: {lines.Length} origin(s) for {pieces} line piece(s) — a preprocessing stage broke the line-origin invariant (kb/Work PB82)");
        Text = text;
        Lines = lines;
    }

    /// <summary>Every line of <paramref name="text"/> originating in <paramref name="file"/>, line n at n (1-based) and
    /// column c at c.</summary>
    public static MappedText Identity(string text, string file)
    {
        int pieces = 1;
        foreach (char c in text) if (c == '\n') pieces++;
        var lines = new LineOrigin[pieces];
        for (int i = 0; i < pieces; i++) lines[i] = new LineOrigin(new SourceOrigin(file, i + 1));
        return new MappedText(text, lines);
    }

    /// <summary>The 0-based line piece containing character position <paramref name="pos"/>.</summary>
    public int LineIndexAt(int pos)
    {
        var starts = LineStarts;
        // binary search for the last start <= pos
        int lo = 0, hi = starts.Length - 1;
        while (lo < hi)
        {
            int mid = (lo + hi + 1) >> 1;
            if (starts[mid] <= pos) lo = mid; else hi = mid - 1;
        }
        return lo;
    }

    /// <summary>The physical origin of character position <paramref name="pos"/>: its file, line and column (kb/Work
    /// PB1801) — what text a stage WRITES in place of the text at <paramref name="pos"/> (a REPLACE replacement, the
    /// text of a COPY statement) takes as its own.</summary>
    public LineOrigin PointAt(int pos)
    {
        int k = LineIndexAt(pos);
        return new LineOrigin([Lines[k].PointAt(pos - LineStarts[k]) with { ResultColumn = 0 }]);
    }

    /// <summary>THE diagnostic location of character position <paramref name="pos"/> — the file, line and column the
    /// user edits (kb/Work PB1801). Every preprocessing stage that reports at a position of its text asks this.</summary>
    public SourceLocation LocateAt(int pos)
    {
        int k = LineIndexAt(pos);
        return Lines[k].Locate(pos - LineStarts[k]);
    }

    /// <summary>A sub-text (<paramref name="start"/>, <paramref name="length"/>) with the origins of its own pieces — the
    /// first rebased to begin where the sub-text does.</summary>
    public MappedText Slice(int start, int length)
    {
        int first = LineIndexAt(start);
        string sub = Text.Substring(start, length);
        int pieces = 1;
        foreach (char c in sub) if (c == '\n') pieces++;
        var lines = new LineOrigin[pieces];
        lines[0] = Lines[first].From(start - LineStarts[first]);
        for (int i = 1; i < pieces; i++) lines[i] = Lines[Math.Min(first + i, Lines.Length - 1)];
        return new MappedText(sub, lines);
    }

    /// <summary>The character position where 0-based line piece <paramref name="piece"/> begins.</summary>
    public int LineStart(int piece) => LineStarts[piece];

    private int[] LineStarts => _lineStarts ??= BuildLineStarts(Text);

    private static int[] BuildLineStarts(string text)
    {
        var starts = new List<int> { 0 };
        for (int i = 0; i < text.Length; i++) if (text[i] == '\n') starts.Add(i + 1);
        return starts.ToArray();
    }
}

/// <summary>Builds a <see cref="MappedText"/> from appended segments, each segment carrying the origins of its own
/// line pieces (kb/Work PB82, PB1801). Every character written keeps the physical position it came from: a slice of a
/// mapped input carries the input's spans, rebased to the column it lands on, and text a stage writes itself (a REPLACE
/// replacement, a COPY statement left in place) takes one stretch at the position it stands for, a character position per character (kb/Work PB3031). An output line piece is
/// named by the origin of the FIRST content written into it, so a COPY statement's leading text, the copybook's lines,
/// and the trailing remainder each keep their own file, line and column — the splice points of
/// <c>ExpandCopiesOneLevel</c> and the replacements of <c>ApplyReplacements</c>.</summary>
public sealed class OriginWriter
{
    private readonly StringBuilder _sb = new();
    private readonly List<LineOrigin> _lines = new();
    private readonly List<OriginSpan> _spans = new();   // the spans of the piece being written
    private int _column;                                  // the resultant column the next character lands on
    private LineOrigin? _open;                            // the input piece an EMPTY run was written for: names the open piece should it end with no content

    /// <summary>The number of line pieces completed so far — the 0-based index of the piece the next content opens
    /// when the current piece is empty (just after a newline).</summary>
    public int LineCount => _lines.Count;

    /// <summary>Append <paramref name="s"/>, text this stage writes itself: every piece of it originates at
    /// <paramref name="at"/> (<see cref="MappedText.PointAt"/>, one stretch), its characters a CHARACTER POSITION further
    /// each (<see cref="CharacterPositions"/>, §2023:6.1 1) c): a supplementary-plane character is two resultant units and one
    /// position, so the span splits at its second unit exactly as the reference-format stage's <c>TextSite.Spans</c> does
    /// for text it reads (kb/Work PB3031) — a column the parser reports inside the written text names the position the
    /// programmer counts, whatever the characters.</summary>
    public void Append(ReadOnlySpan<char> s, LineOrigin at)
    {
        while (true)
        {
            int nl = s.IndexOf('\n');
            if (nl < 0)
            {
                WriteText(s, at, null);
                return;
            }
            WriteText(s[..nl], at, null);
            EndPiece(at, 0);
            s = s[(nl + 1)..];
        }
    }

    /// <summary>Append <paramref name="written"/>, text written ELSEWHERE that this stage places at <paramref name="at"/>
    /// (kb/Work PB5313): every piece originates at <paramref name="at"/>, as <see cref="Append"/>'s text does, and each
    /// character is laid out where <paramref name="written"/> holds it (<see cref="OriginSpan.Layout"/>) — the words of a
    /// pseudo-text-2 a replacement places, which keep the areas they were written in (§1985:XII.2.4 GR9).</summary>
    public void AppendLaidOut(MappedText written, LineOrigin at)
    {
        var s = written.Text.AsSpan();
        for (int piece = 0; ; piece++)
        {
            int nl = s.IndexOf('\n');
            WriteText(nl < 0 ? s : s[..nl], at, written.Lines[piece]);
            if (nl < 0) return;
            EndPiece(at, 0);
            s = s[(nl + 1)..];
        }
    }

    /// <summary>Append a slice of a mapped input — every character keeps its input origin.</summary>
    public void AppendSlice(MappedText input, int start, int length)
    {
        int piece = input.LineIndexAt(start);
        int column = start - input.LineStart(piece);
        if (length <= 0)
        {
            Write([], input.Lines[Math.Min(piece, input.Lines.Length - 1)], column);   // nothing to write, but the piece has an origin
            return;
        }
        var s = input.Text.AsSpan(start, length);
        while (true)
        {
            int nl = s.IndexOf('\n');
            var origin = input.Lines[Math.Min(piece, input.Lines.Length - 1)];
            if (nl < 0)
            {
                Write(s, origin, column);
                return;
            }
            Write(s[..nl], origin, column);
            EndPiece(origin, column);
            s = s[(nl + 1)..];
            piece++;
            column = 0;
        }
    }

    /// <summary>Append a whole mapped text — its characters keep their origins.</summary>
    public void AppendMapped(MappedText input) => AppendSlice(input, 0, input.Text.Length);

    /// <summary>End a line: a newline whose piece, if still empty, originates at <paramref name="origin"/>.</summary>
    public void NewLine(LineOrigin origin) => EndPiece(origin, 0);

    public MappedText Finish()
    {
        // The final piece (possibly empty after a trailing newline): an empty one is named by the empty input piece it was
        // written for. A blank line between two directives is such a piece (kb/Work PB4412): named like the piece before it,
        // it would be a line other than its own, and named by nothing, a line of no file, so a question about its omission
        // or its cut would have no answer. With no such piece it is named like the piece before it.
        _lines.Add(_spans.Count > 0 ? new LineOrigin([.. _spans])
            : _open ?? (_lines.Count > 0 ? _lines[^1] : new LineOrigin(new SourceOrigin("", 0))));
        return new MappedText(_sb.ToString(), _lines.ToArray());
    }

    /// <summary>Write <paramref name="run"/> (no newline), whose first character is at resultant column
    /// <paramref name="column"/> of <paramref name="origin"/>: the origin's spans over the run, rebased to the column the
    /// run lands on, each one that merely continues the span before it folded into it.</summary>
    private void Write(ReadOnlySpan<char> run, LineOrigin origin, int column)
    {
        if (run.IsEmpty)
        {
            if (_spans.Count == 0) _open = origin.From(column);
            return;
        }
        AddSpan(origin.PointAt(column) with { ResultColumn = _column });
        foreach (var span in origin.Spans)
            if (span.ResultColumn > column && span.ResultColumn < column + run.Length)
                AddSpan(span with { ResultColumn = _column + span.ResultColumn - column });
        _sb.Append(run);
        _column += run.Length;
    }

    /// <summary>Write <paramref name="run"/> (no newline), text this stage composes: one stretch from the start of
    /// <paramref name="at"/>, each character a position further than the one before it, and the second unit of a
    /// surrogate pair standing at its first unit's position (kb/Work PB3031). <paramref name="layout"/>, when given, is where
    /// the run was written (<see cref="AppendLaidOut"/>): each character is laid out at the place it holds there.</summary>
    private void WriteText(ReadOnlySpan<char> run, LineOrigin at, LineOrigin? layout)
    {
        if (run.IsEmpty) return;
        var start = at.PointAt(0);
        // The stretch beginning at unit `unit` of the run, `position` character positions past its start.
        OriginSpan Stretch(int unit, int position) => new(_column + unit, start.Origin, start.SourceColumn + position,
            layout?.PointAt(unit).LaidOutAt);
        if (layout is null && !CharacterPositions.HasSurrogate(run))
            AddSpan(Stretch(0, 0));
        else
        {
            // A stretch begins at the run's start, at the second unit of a surrogate pair (two units, one position), and
            // wherever the layout's own stretches do; AddSpan folds a point that merely continues the stretch before it.
            var layoutSpans = layout is null ? default : layout.Spans;
            int next = 1;   // the layout stretch the run has not yet reached
            int position = 0;   // the characters before the one at unit i
            for (int i = 0; i < run.Length; position++)
            {
                bool begins = i == 0;
                for (; next < layoutSpans.Length && layoutSpans[next].ResultColumn <= i; next++)
                    begins |= layoutSpans[next].ResultColumn == i;
                if (begins) AddSpan(Stretch(i, position));
                int units = CharacterPositions.UnitsAt(run, i);
                if (units == 2) AddSpan(Stretch(i + 1, position));
                i += units;
            }
        }
        _sb.Append(run);
        _column += run.Length;
    }

    private void AddSpan(OriginSpan span)
    {
        if (_spans.Count > 0)
        {
            var prev = _spans[^1];
            int delta = span.ResultColumn - prev.ResultColumn;
            if (prev.Origin == span.Origin && prev.SourceColumn + delta == span.SourceColumn
                && (prev.Layout, span.Layout) switch
                {
                    (null, null) => true,
                    ({ } p, { } s) => p.Origin == s.Origin && p.Column + delta == s.Column,
                    _ => false,
                })
                return;   // the same stretch, continued
        }
        _spans.Add(span);
    }

    /// <summary>End the piece being written: named by its first content, or — when it holds none — by
    /// <paramref name="origin"/> at <paramref name="column"/>, where the newline that ends it came from.</summary>
    private void EndPiece(LineOrigin origin, int column)
    {
        _sb.Append('\n');
        _lines.Add(_spans.Count > 0 ? new LineOrigin([.. _spans]) : origin.From(column));
        _spans.Clear();
        _open = null;
        _column = 0;
    }
}
