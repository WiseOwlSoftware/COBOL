// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using System.Text;

namespace WiseOwl.Cobol.Listing.Text;

/// <summary>
/// The paginated text listing (DESIGN-listing.md sections 6.1, 6.2 and 7.1): UTF-8, LF line ends, a form feed before every
/// page after the first. It reads ONLY the model: what is listed and where a page starts were decided by the builder
/// (<see cref="ListingLine.Listed"/>, <see cref="ListingLine.PageBreakBefore"/>), and the compiled text of a replaced line
/// is the model's (<see cref="ListingLine.Replaced"/>).
/// <para>A source row, at the default width 132: columns 1-6 the listing line number, column 8 the inclusion depth (blank
/// at 0, 1-9, '+' beyond), column 9 the marker (<see cref="MarkerOf"/>), columns 11-16 the physical line number in its
/// file, columns 18-97 the line as written (a fixed-form line's 80 positions; anything longer wraps onto rows marked '+').
/// Columns 100-132 are the embedded margin of later steps.</para>
/// <para>Under a printed line, each diagnostic placed on it (DESIGN-listing.md section 6.3): a caret row under its column when the
/// line is printed whole as written (<see cref="CaretRow"/>), then <c>==&gt; severity CODE (col n): message</c> from the source
/// text's column, wrapped at the page width (<see cref="DiagnosticRows"/>). After the source section, on a page of its own, the
/// summary: the counts by severity, then every diagnostic in listing order with its listing line, file, line and column
/// (<see cref="SummaryRows"/>).</para>
/// </summary>
public static class TextListingRenderer
{
    /// <summary>The first column (1-based) of the source text on a row.</summary>
    public const int TextColumn = 18;

    /// <summary>The source text's positions on a row: columns 18 through 97.</summary>
    public const int TextWidth = 80;

    /// <summary>The ruler under each source page heading: it marks the source text's 80 positions.</summary>
    public static readonly string Ruler = new string(' ', TextColumn - 1)
        + string.Concat(Enumerable.Range(1, TextWidth / 10).Select(n => "----+----" + (n % 10).ToString(CultureInfo.InvariantCulture)));

    /// <summary>Render <paramref name="document"/> as the paginated text listing <paramref name="request"/> asks for.</summary>
    public static string Render(ListingDocument document, ListingRequest request)
    {
        var programs = document.Programs.ToDictionary(p => p.Id, p => p.Name);
        string? ProgramOf(ListingLine line) => line.Program is { } id ? programs[id] : null;
        var firstLineOf = new Dictionary<int, int>();   // inclusion id -> its first listing line
        foreach (var line in document.Lines)
            if (line.Inclusion is { } inclusion) firstLineOf.TryAdd(inclusion, line.Number);
        var banners = document.Inclusions.Where(i => firstLineOf.ContainsKey(i.Id)).ToDictionary(i => firstLineOf[i.Id]);
        var files = document.Files.ToDictionary(f => f.Id, f => f.Path);
        var diagnostics = document.Diagnostics.ToDictionary(d => d.Id);

        var rows = new List<ListingRow>();
        foreach (var line in document.Lines)
        {
            if (!line.Listed || line.Replacement == ListingReplacement.DateCompiledEntry) continue;
            bool breakBefore = line.PageBreakBefore;
            if (banners.TryGetValue(line.Number, out var inclusion))
            {
                // The banner opens the inclusion (DESIGN-listing.md section 6.2): a page the first line starts begins at the banner.
                // It is text in the source columns like any line, so a long path wraps as a long line does.
                bool firstPiece = true;
                foreach (string piece in Wrap(Banner(inclusion, files)))
                {
                    rows.Add(new ListingRow(firstPiece ? new string(' ', TextColumn - 1) + piece : ContinuationRow(piece),
                        firstPiece && breakBefore, ProgramOf(line)));
                    firstPiece = false;
                }
                breakBefore = false;
            }
            string text = line.Replacement == ListingReplacement.DateCompiled ? line.Replaced ?? line.Text : line.Text;
            int pieces = 0;
            foreach (string piece in Wrap(text))
            {
                rows.Add(new ListingRow(pieces == 0 ? SourceRow(line, piece) : ContinuationRow(piece), pieces == 0 && breakBefore, ProgramOf(line)));
                pieces++;
            }
            // The caret stands under the column only where the row shows the line whole and as written.
            bool whole = pieces == 1 && line.Replacement == ListingReplacement.None;
            foreach (int id in line.Diagnostics)
                foreach (string row in DiagnosticRows(diagnostics[id], whole ? line.Text : null, request.PageWidth))
                    rows.Add(new ListingRow(row, false, ProgramOf(line)));
        }
        var header = document.Header;
        string source = PageComposer.Compose(rows, request.PageLength, (page, program) =>
        [
            IdentificationLine(header, program, page, request.PageWidth),
            KeepEnd("SOURCE  ", header.MainSource, request.PageWidth),
            "",
            Ruler,
        ], firstPage: 1, out int sourcePages);
        // The summary starts a page (DESIGN-listing.md section 7.1); outside the source section a heading names the first outermost
        // program (section 6.1).
        string? firstProgram = document.Programs.FirstOrDefault(p => p.Depth == 0)?.Name;
        string summary = PageComposer.Compose(
            [.. SummaryRows(document, request.PageWidth).Select(row => new ListingRow(row, false, firstProgram))], request.PageLength,
            (page, program) => [IdentificationLine(header, program, page, request.PageWidth), "DIAGNOSTICS", ""],
            firstPage: sourcePages + 1);
        return source + PageComposer.FormFeed + summary;
    }

    /// <summary>The rows of one diagnostic under its line (DESIGN-listing.md section 6.3): a caret under its column when
    /// <paramref name="wholeText"/> (the line's text, printed whole as written) is given, then <c>==&gt; severity CODE (col n):
    /// message</c> from the source text's column, its further rows indented under the message, none wider than
    /// <paramref name="width"/>.</summary>
    public static IEnumerable<string> DiagnosticRows(ListingDiagnostic diagnostic, string? wholeText, int width)
    {
        if (wholeText is not null && diagnostic.Column is { } column && CaretRow(wholeText, column) is { } caret) yield return caret;
        string place = diagnostic.Column is { } col ? string.Create(CultureInfo.InvariantCulture, $" (col {col})") : "";
        string text = $"{Arrow}{SeverityWord(diagnostic.Severity)} {diagnostic.Code}{place}: {diagnostic.Message}";
        foreach (string row in Indented(text, TextColumn - 1, TextColumn - 1 + Arrow.Length, width)) yield return row;
    }

    /// <summary>The caret row of 1-based <paramref name="column"/> of <paramref name="text"/> (a character position of the line's
    /// text, tabs expanded): a '^' under that position of the source columns, a supplementary-plane character being one position;
    /// null when the column lies past the text's end plus one.</summary>
    public static string? CaretRow(string text, int column)
    {
        if (column < 1 || column - 1 > text.Length) return null;
        return new string(' ', TextColumn - 1 + Positions(text.AsSpan(0, column - 1))) + "^";
    }

    /// <summary>The marker that opens a diagnostic row.</summary>
    public const string Arrow = "==> ";

    /// <summary>The summary of every diagnostic (DESIGN-listing.md section 6.3): the counts by severity, then each diagnostic in listing
    /// order, its listing line (or '-') and its file, line and column ahead of its words.</summary>
    public static IEnumerable<string> SummaryRows(ListingDocument document, int width)
    {
        int Count(ListingSeverity severity) => document.Diagnostics.Count(d => d.Severity == severity);
        yield return string.Create(CultureInfo.InvariantCulture,
            $"Errors: {Count(ListingSeverity.Error)}   Warnings: {Count(ListingSeverity.Warning)}   Information: {Count(ListingSeverity.Info)}");
        yield return "";
        var files = document.Files.ToDictionary(f => f.Id, f => f.Path);
        foreach (var d in document.Diagnostics)
        {
            string place = d is { File: { } file, FileLine: { } fileLine, Column: { } column }
                ? string.Create(CultureInfo.InvariantCulture, $" {files[file]}({fileLine},{column})") : "";
            string lead = d.Line is { } line ? line.ToString(CultureInfo.InvariantCulture).PadLeft(6) : "     -";
            foreach (string row in Indented($"{lead}  {SeverityWord(d.Severity)} {d.Code}{place}: {d.Message}", 0, 8, width))
                yield return row;
        }
    }

    /// <summary>The word a severity is printed as, the word every diagnostic line of the compiler uses.</summary>
    public static string SeverityWord(ListingSeverity severity) => severity switch
    {
        ListingSeverity.Error => "error",
        ListingSeverity.Warning => "warning",
        ListingSeverity.Info => "info",
        _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, "a severity with no word"),
    };

    /// <summary><paramref name="text"/> as rows of at most <paramref name="width"/> positions: the first indented by
    /// <paramref name="first"/> spaces, the rest by <paramref name="rest"/>, broken at a space where one is, else cut.</summary>
    public static IEnumerable<string> Indented(string text, int first, int rest, int width)
    {
        int indent = first;
        int at = 0;
        do
        {
            int room = Math.Max(width - indent, 1);
            int end = IndexAfterPositions(text, at, room);
            if (end < text.Length)
            {
                int space = text.LastIndexOf(' ', end, end - at);
                if (space > at) end = space;
            }
            yield return new string(' ', indent) + text[at..end].TrimEnd(' ');
            at = end;
            while (at < text.Length && text[at] == ' ') at++;
            indent = rest;
        }
        while (at < text.Length);
    }

    /// <summary>The index in <paramref name="text"/> after <paramref name="positions"/> positions from <paramref name="start"/>
    /// (a supplementary-plane character is one position), or its length.</summary>
    private static int IndexAfterPositions(string text, int start, int positions)
    {
        int at = start;
        for (int n = 0; n < positions && at < text.Length; n++)
            at += char.IsHighSurrogate(text[at]) && at + 1 < text.Length && char.IsLowSurrogate(text[at + 1]) ? 2 : 1;
        return at;
    }

    /// <summary>The positions <paramref name="text"/> takes: a supplementary-plane character is one.</summary>
    private static int Positions(ReadOnlySpan<char> text)
    {
        int positions = 0;
        for (int at = 0; at < text.Length; positions++)
            at += char.IsHighSurrogate(text[at]) && at + 1 < text.Length && char.IsLowSurrogate(text[at + 1]) ? 2 : 1;
        return positions;
    }

    /// <summary>The first heading line (DESIGN-listing.md section 6.1), <paramref name="width"/> columns at most: the compiler,
    /// the edition, the program the page names (§1985:VI.3.3.4 GR1: the program-name "identifies the source program, the
    /// object program, and all listings pertaining to a particular program"), the compilation instant (§1985:VI.3.4.1, the
    /// date "in the Identification Division source program listing"; §1989:A.2.46.3 RV3) and the page number, which ends at
    /// column <paramref name="width"/>.
    /// <para>⛔ WHAT YIELDS WHEN THE LINE DOES NOT FIT, in this order: the compiler's version, cut at its end (a release build's
    /// is <c>30.0.0+</c> and a 40-character commit, so its build metadata goes first), then the edition, cut at its end, then
    /// the program-name, cut at its end. The instant and the page number never yield: the instant and its separator take 28
    /// columns and the page label at most 15, and the narrowest page has <see cref="ListingRequest.MinimumPageWidth"/>. The
    /// program-name yields only when it is longer than the page leaves it (width - 40 columns below page 100000, so at least
    /// 60: never at <c>--std 85</c>, whose names have at most 30 characters, nor at the default width for a name of 63).</para></summary>
    public static string IdentificationLine(ListingHeader header, string? program, int page, int width)
    {
        // Each field and the order it yields in; the instant and the page label never yield.
        string[] fields =
        [
            "WiseOwl COBOL " + header.CompilerVersion,
            string.Create(CultureInfo.InvariantCulture, $"{header.EditionName} (--std {header.Edition})"),
            program ?? "",
            ListingTimestamp.Format(header.CompiledAt),
        ];
        string pageLabel = string.Create(CultureInfo.InvariantCulture, $"PAGE {page}");
        int Length() => fields.Where(f => f.Length > 0).Sum(f => f.Length + Separator.Length) + pageLabel.Length;
        for (int yielding = 0; yielding < fields.Length - 1 && Length() > width; yielding++)
            fields[yielding] = fields[yielding][..Math.Max(fields[yielding].Length - (Length() - width), 0)];
        if (Length() > width)
            throw new InvalidOperationException($"a page {width} columns wide cannot hold the instant and the page number");
        return string.Join(Separator, fields.Where(f => f.Length > 0)).PadRight(width - pageLabel.Length) + pageLabel;
    }

    /// <summary>The separator between the fields of a heading line.</summary>
    private const string Separator = "  ";

    /// <summary>The marker column of a row (DESIGN-listing.md section 6.2): 'X' omitted by conditional compilation, 'E' the
    /// DATE-COMPILED replacement, '/' a page-eject comment line, '*' a comment line, 'D' a debugging line, '&gt;' a compiler
    /// directive line, '-' a continuation line; a space otherwise.</summary>
    public static char MarkerOf(ListingLine line) => line switch
    {
        { Omitted: true } => 'X',
        { Replacement: ListingReplacement.DateCompiled } => 'E',
        { Kind: ListingLineKind.PageEjectComment } => '/',
        { Kind: ListingLineKind.Comment } => '*',
        { Kind: ListingLineKind.DebuggingLine } => 'D',
        { Kind: ListingLineKind.Directive } => '>',
        { Kind: ListingLineKind.Continuation } => '-',
        _ => ' ',
    };

    /// <summary>The depth column: blank for the source, 1-9, '+' beyond.</summary>
    public static char DepthMark(int depth) => depth switch { 0 => ' ', <= 9 => (char)('0' + depth), _ => '+' };

    private static string SourceRow(ListingLine line, string text) =>
        string.Create(CultureInfo.InvariantCulture,
            $"{line.Number,6} {DepthMark(line.Depth)}{MarkerOf(line)} {line.FileLine,6} {text}");

    private static string ContinuationRow(string text) => new string(' ', 8) + "+" + new string(' ', TextColumn - 10) + text;

    private static string Banner(ListingInclusion inclusion, IReadOnlyDictionary<int, string> files) =>
        string.Create(CultureInfo.InvariantCulture,
            $"COPY {inclusion.TextName}{(inclusion.LibraryName is { } library ? " OF " + library : "")} -> "
            + $"{(inclusion.File is { } file ? files[file] : "(no library text)")} (depth {inclusion.Depth})");

    /// <summary>The text cut into pieces of at most <see cref="TextWidth"/> character positions (a character outside the
    /// Basic Multilingual Plane is one position); an empty text is one empty piece.</summary>
    public static IEnumerable<string> Wrap(string text)
    {
        // Nearly every line fits: a text of at most 80 UTF-16 units holds at most 80 positions, and is its own one piece.
        if (text.Length <= TextWidth)
        {
            yield return text;
            yield break;
        }
        int start = 0, at = 0, positions = 0;
        while (at < text.Length)
        {
            if (positions == TextWidth)
            {
                yield return text[start..at];
                start = at;
                positions = 0;
            }
            at += char.IsHighSurrogate(text[at]) && at + 1 < text.Length && char.IsLowSurrogate(text[at + 1]) ? 2 : 1;
            positions++;
        }
        yield return text[start..];
    }

    /// <summary><paramref name="label"/> and <paramref name="path"/>, <paramref name="width"/> columns at most: a path that does
    /// not fit keeps its END, which names the file, behind "...".</summary>
    private static string KeepEnd(string label, string path, int width)
    {
        const string Elided = "...";
        if (label.Length + path.Length <= width) return label + path;
        return label + Elided + path[^Math.Max(width - label.Length - Elided.Length, 0)..];
    }
}
