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
/// summary: the counts by severity, then every diagnostic in listing order with its listing line, file, line and column, then the
/// table of flagged constructs by edition (<see cref="SummaryRows"/>).</para>
/// <para>Under a line a replacing action changed (marked 'R'), its compiled text in rows marked 'R' (<see cref="ReplacedRows"/>);
/// after a DISPLAY directive's line, whether or not that line is printed, each transfer to the listing as
/// <c>DISPLAY: text</c> (<see cref="DisplayRows"/>; DESIGN-listing.md section 6.10).</para>
/// <para>The sections are printed in <see cref="ListingSections.Order"/>, each starting a page (section 7.1), each visited through
/// <see cref="IListingSectionVisitor{T}"/>. This class composes the pages and prints the source section and the diagnostics
/// summary; each other section's rows are its own class's
/// (<see cref="OptionsText"/>, <see cref="ProgramMapText"/>, <see cref="StatisticsText"/>, <see cref="DataMapText"/>,
/// <see cref="ObjectCodeText"/>), so a section added is a class added.</para>
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

    /// <summary>Render <paramref name="document"/> as the paginated text listing <paramref name="request"/> asks for: each section
    /// the document holds (<see cref="ListingDocument.Sections"/>), in <see cref="ListingSections.Order"/>, each starting a page; a
    /// section that could not be produced prints why. Written to <paramref name="writer"/> page by page as the rows are produced
    /// (kb/Work PB5941): neither a section nor the listing is ever held whole.</summary>
    public static void Write(ListingDocument document, ListingRequest request, TextWriter writer)
    {
        var sections = new Sections(document, request);
        var header = document.Header;
        int pages = 0;
        foreach (var section in ListingSections.Order)
        {
            if (document.Sections.FirstOrDefault(s => s.Section == section) is not { } status) continue;
            string title = ListingSections.Title(section);
            IReadOnlyList<string> Heading(int page, string? program) => section == ListingSection.Source
                ? [IdentificationLine(header, program, page, request.PageWidth),
                   KeepEnd(title + "  ", header.MainSource, request.PageWidth), "", Ruler]
                : [IdentificationLine(header, program, page, request.PageWidth), title, ""];
            IEnumerable<ListingRow> rows = status.Produced ? sections.Visit(section)
                : sections.Rows(Indented(ListingSections.NotProduced(status.Reason), 0, 2, request.PageWidth));
            pages += PageComposer.Compose(writer, rows, request.PageLength, Heading, firstPage: pages + 1);
        }
    }

    /// <summary>The text listing's rows of each produced section (DESIGN-listing.md section 7): the source section's here, the
    /// diagnostic summary's here, each other section's its own class's. A page outside the source section names the first
    /// outermost program (section 6.1).</summary>
    private sealed class Sections(ListingDocument document, ListingRequest request) : IListingSectionVisitor<IEnumerable<ListingRow>>
    {
        private readonly string? _firstProgram = document.Programs.FirstOrDefault(p => p.Depth == 0)?.Name;
        private int Width => request.PageWidth;

        public IEnumerable<ListingRow> Rows(IEnumerable<string> rows) => rows.Select(r => new ListingRow(r, false, _firstProgram));

        public IEnumerable<ListingRow> Source() => SourceRows(document, request);

        public IEnumerable<ListingRow> Programs() => Rows(ProgramMapText.Rows(document.Programs, document.Header.Parse, Width));

        public IEnumerable<ListingRow> Diagnostics() => Rows(SummaryRows(document, Width));

        public IEnumerable<ListingRow> Options() => Rows(OptionsText.Rows(document.Options, Width));

        public IEnumerable<ListingRow> Statistics() => Rows(document.Statistics is { } statistics
            ? StatisticsText.Rows(statistics, document.Files, document.Header.Parse, Width)
            : throw new InvalidOperationException("the statistics section is produced but the document holds no statistics"));

        public IEnumerable<ListingRow> DataMap() => Rows(document.DataMap is { } map ? DataMapText.Rows(map, Width)
            : throw new InvalidOperationException("the data map section is produced but the document holds no data map"));

        public IEnumerable<ListingRow> CrossReference() => Rows(document.CrossReference is { } xref ? CrossReferenceText.Rows(xref, Width)
            : throw new InvalidOperationException("the cross-reference section is produced but the document holds no cross-reference"));

        public IEnumerable<ListingRow> ObjectMap() => Rows(document.ObjectCode is { } map ? ObjectCodeText.MapRows(map, Width)
            : throw new InvalidOperationException("the object-code map section is produced but the document holds no object-code map"));

        public IEnumerable<ListingRow> ObjectCode() => Rows(document.ObjectCode is { } map ? ObjectCodeText.CodeRows(map, document.Lines, Width)
            : throw new InvalidOperationException("the object-code section is produced but the document holds no object-code map"));
    }

    /// <summary>The source section's rows (DESIGN-listing.md section 6.2): each listed line, the banner of each inclusion, the
    /// compiled text under a replaced line, each diagnostic under its line and each DISPLAY transfer at its directive.</summary>
    private static IEnumerable<ListingRow> SourceRows(ListingDocument document, ListingRequest request)
    {
        var programs = document.Programs.ToDictionary(p => p.Id, p => p.Name);
        string? ProgramOf(ListingLine line) => line.Program is { } id ? programs[id] : null;
        var firstLineOf = new Dictionary<int, int>();   // inclusion id -> its first listing line
        foreach (var line in document.Lines)
            if (line.Inclusion is { } inclusion) firstLineOf.TryAdd(inclusion, line.Number);
        var banners = document.Inclusions.Where(i => firstLineOf.ContainsKey(i.Id)).ToDictionary(i => firstLineOf[i.Id]);
        var files = document.Files.ToDictionary(f => f.Id, f => f.Path);
        var diagnostics = document.Diagnostics.ToDictionary(d => d.Id);
        var displays = document.Displays.ToLookup(d => d.Line);

        foreach (var line in document.Lines)
        {
            // §2023:7.3.12.4 GR2: the transfer takes place "irrespective of whether it is suppressed by the LISTING directive".
            if (!line.Listed || line.Replacement == ListingReplacement.DateCompiledEntry)
            {
                foreach (var display in displays[line.Number])
                    foreach (string row in DisplayRows(display)) yield return new ListingRow(row, false, ProgramOf(line));
                continue;
            }
            bool breakBefore = line.PageBreakBefore;
            if (banners.TryGetValue(line.Number, out var inclusion))
            {
                // The banner opens the inclusion (DESIGN-listing.md section 6.2): a page the first line starts begins at the banner.
                // It is text in the source columns like any line, so a long path wraps as a long line does.
                bool firstPiece = true;
                foreach (string piece in Wrap(Banner(inclusion, files)))
                {
                    yield return new ListingRow(firstPiece ? new string(' ', TextColumn - 1) + piece : ContinuationRow(piece),
                        firstPiece && breakBefore, ProgramOf(line));
                    firstPiece = false;
                }
                breakBefore = false;
            }
            string text = line.Replacement == ListingReplacement.DateCompiled ? line.Replaced ?? line.Text : line.Text;
            int pieces = 0;
            foreach (string piece in Wrap(text))
            {
                yield return new ListingRow(pieces == 0 ? WithMargin(SourceRow(line, piece), line.Margin, request.PageWidth) : ContinuationRow(piece),
                    pieces == 0 && breakBefore, ProgramOf(line));
                pieces++;
            }
            if (line.Replacement == ListingReplacement.Replacing)
                foreach (string row in ReplacedRows(line.Replaced ?? "")) yield return new ListingRow(row, false, ProgramOf(line));
            // The caret stands under the column only where the row shows the line whole and as written.
            bool whole = pieces == 1 && line.Replacement is ListingReplacement.None or ListingReplacement.Replacing;
            foreach (int id in line.Diagnostics)
                foreach (string row in DiagnosticRows(diagnostics[id], whole ? line.Text : null, request.PageWidth))
                    yield return new ListingRow(row, false, ProgramOf(line));
            foreach (var display in displays[line.Number])
                foreach (string row in DisplayRows(display)) yield return new ListingRow(row, false, ProgramOf(line));
        }
    }

    /// <summary>The 1-based column the embedded margin starts in (DESIGN-listing.md section 6.2: columns 100 to 132).</summary>
    public const int MarginColumn = 100;

    /// <summary>The narrowest page that prints the margin (DESIGN-listing.md section 7.1: below 132 the margin is not printed).</summary>
    public const int MarginPageWidth = 132;

    /// <summary>⛔ THE MARGIN OF A SOURCE ROW (DESIGN-listing.md section 6.2): <paramref name="row"/> padded to column
    /// <see cref="MarginColumn"/> and the line's margin as the builder computed it (<see cref="ListingLine.Margin"/>: a data
    /// definition line's offset, length and usage), on a page at least <see cref="MarginPageWidth"/> wide; the row as it is
    /// otherwise. Never wider than the page.</summary>
    public static string WithMargin(string row, ListingMargin? margin, int width)
    {
        if (margin is null || width < MarginPageWidth) return row;
        string text = MarginText(margin);
        string marked = row.PadRight(MarginColumn - 1) + text;
        return marked.Length <= width ? marked : marked[..width];
    }

    /// <summary>The margin's text: on a definition line <c>OFF=n LEN=n usage</c>, an offset or length the data map does not hold printed
    /// as <c>-</c>, a usage it does not hold (a group that states none) not printed; on a line of references <c>DEF n n ...</c>, the
    /// listing lines that define the names it uses (the page cuts what does not fit, <see cref="WithMargin"/>).</summary>
    public static string MarginText(ListingMargin margin) => margin.Item is null
        ? DefinitionsMark + string.Join(" ", margin.Definitions.Select(static d => d.ToString(CultureInfo.InvariantCulture)))
        : string.Create(CultureInfo.InvariantCulture,
            $"OFF={margin.Offset?.ToString(CultureInfo.InvariantCulture) ?? "-"} LEN={margin.Length?.ToString(CultureInfo.InvariantCulture) ?? "-"} {margin.Usage ?? ""}").TrimEnd();

    /// <summary>The word a statement line's margin starts with: the listing lines after it define the names the line uses.</summary>
    public const string DefinitionsMark = "DEF ";

    /// <summary>Each text padded to its width; a text as wide as its column, or wider, is followed by one space.</summary>
    internal static string Columns(params ReadOnlySpan<(string Text, int Width)> columns)
    {
        var row = new StringBuilder();
        foreach (var (text, columnWidth) in columns) row.Append(text.Length < columnWidth ? text.PadRight(columnWidth) : text + " ");
        return row.ToString();
    }

    /// <summary>The rows under a line a replacing action changed (DESIGN-listing.md section 6.2): each line of its compiled text
    /// <paramref name="replaced"/> in the source columns, marked 'R' in the marker column, a long one wrapped onto '+' rows; an
    /// empty compiled text (the replacement removed what the line held) is one empty 'R' row.</summary>
    public static IEnumerable<string> ReplacedRows(string replaced)
    {
        foreach (string resultant in replaced.Split('\n'))
        {
            bool first = true;
            foreach (string piece in Wrap(resultant))
            {
                yield return first ? new string(' ', 8) + ReplacedMarker + new string(' ', TextColumn - 10) + piece : ContinuationRow(piece);
                first = false;
            }
        }
    }

    /// <summary>The rows of one DISPLAY directive transfer (DESIGN-listing.md section 6.10): <c>DISPLAY: text</c> in the source
    /// columns, a long one wrapped onto '+' rows.</summary>
    public static IEnumerable<string> DisplayRows(ListingDisplay display)
    {
        bool first = true;
        foreach (string piece in Wrap(DisplayText(display)))
        {
            yield return first ? new string(' ', TextColumn - 1) + piece : ContinuationRow(piece);
            first = false;
        }
    }

    /// <summary>One DISPLAY directive transfer as every renderer prints it: <c>DISPLAY: text</c>.</summary>
    public static string DisplayText(ListingDisplay display) => "DISPLAY: " + display.Text;

    /// <summary>The rows of one diagnostic under its line (DESIGN-listing.md section 6.3): a caret under its column when
    /// <paramref name="wholeText"/> (the line's text, printed whole as written) is given, then <c>==&gt; severity CODE (col n):
    /// message</c> from the source text's column, its further rows indented under the message, none wider than
    /// <paramref name="width"/>.</summary>
    public static IEnumerable<string> DiagnosticRows(ListingDiagnostic diagnostic, string? wholeText, int width)
    {
        if (wholeText is not null && diagnostic.Column is { } column && CaretRow(wholeText, column) is { } caret) yield return caret;
        foreach (string row in Indented(DiagnosticText(diagnostic), TextColumn - 1, TextColumn - 1 + Arrow.Length, width)) yield return row;
    }

    /// <summary>A diagnostic under its line as every renderer prints it: <c>==&gt; severity CODE (col n): message</c>, the column
    /// when it has one.</summary>
    public static string DiagnosticText(ListingDiagnostic diagnostic)
    {
        string place = diagnostic.Column is { } col ? string.Create(CultureInfo.InvariantCulture, $" (col {col})") : "";
        return $"{Arrow}{SeverityWord(diagnostic.Severity)} {diagnostic.Code}{place}: {diagnostic.Message}";
    }

    /// <summary>The caret row of 1-based <paramref name="column"/> of <paramref name="text"/> (a character position of the line's
    /// text, tabs expanded, a supplementary-plane character being one position, as every listing record's column counts:
    /// <c>ReferenceFormatMap.CharacterPositionAt</c>): a '^' under that position of the source columns; null when the column lies past
    /// the text's end plus one.</summary>
    public static string? CaretRow(string text, int column)
    {
        if (column < 1 || column - 1 > Positions(text)) return null;
        return new string(' ', TextColumn - 1 + column - 1) + "^";
    }

    /// <summary>The marker that opens a diagnostic row.</summary>
    public const string Arrow = "==> ";

    /// <summary>The summary of every diagnostic (DESIGN-listing.md section 6.3): the counts by severity, then each diagnostic in listing
    /// order, its listing line (or '-') and its file, line and column ahead of its words, then the table of flagged constructs by
    /// edition (section 6.11, <see cref="FlaggedConstructText.Rows"/>).</summary>
    public static IEnumerable<string> SummaryRows(ListingDocument document, int width)
    {
        yield return CountsText(document.Diagnostics);
        yield return "";
        var files = document.Files.ToDictionary(f => f.Id, f => f.Path);
        foreach (var d in document.Diagnostics)
        {
            string place = PlaceText(d, files) is { } at ? " " + at : "";
            string lead = d.Line is { } line ? line.ToString(CultureInfo.InvariantCulture).PadLeft(6) : "     -";
            foreach (string row in Indented($"{lead}  {SeverityWord(d.Severity)} {d.Code}{place}: {d.Message}", 0, 8, width))
                yield return row;
        }
        foreach (string row in FlaggedConstructText.Rows(document, width)) yield return row;
    }

    /// <summary>The summary's first row as every renderer prints it: the count of <paramref name="diagnostics"/> of each severity.</summary>
    public static string CountsText(IReadOnlyList<ListingDiagnostic> diagnostics)
    {
        int Count(ListingSeverity severity) => diagnostics.Count(d => d.Severity == severity);
        return string.Create(CultureInfo.InvariantCulture,
            $"Errors: {Count(ListingSeverity.Error)}   Warnings: {Count(ListingSeverity.Warning)}   Information: {Count(ListingSeverity.Info)}");
    }

    /// <summary>Where a diagnostic stands as the summary prints it, <c>file(line,column)</c>, the file named by its path in
    /// <paramref name="files"/>; null for a diagnostic with no file, line or column.</summary>
    public static string? PlaceText(ListingDiagnostic diagnostic, IReadOnlyDictionary<int, string> files) =>
        diagnostic is { File: { } file, FileLine: { } fileLine, Column: { } column }
            ? string.Create(CultureInfo.InvariantCulture, $"{files[file]}({fileLine},{column})") : null;

    /// <summary>The word a severity is printed as: the word every diagnostic line of the compiler uses (<c>Diagnostic.SeverityWord</c>,
    /// which this assembly cannot reference; <c>DiagnosticRecordsTests.TheSeverityWords_AreTheCompilersWords</c> holds the two equal).</summary>
    public static string SeverityWord(ListingSeverity severity) => severity switch
    {
        ListingSeverity.Error => "error",
        ListingSeverity.Warning => "warning",
        ListingSeverity.Info => "info",
        _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, "a severity with no word"),
    };

    /// <summary>The note each part read off a parse that recovered from a syntax error prints (DESIGN-listing.md sections 6.5
    /// and 6.8; <see cref="ListingParse.Recovered"/>): <paramref name="what"/> holds only what the recovery kept.</summary>
    public static string RecoveredNote(string what) =>
        $"the parse recovered from a syntax error: {what} are only those it kept, none from source it skipped";

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
            yield return Row(indent, text, at, end);
            at = end;
            while (at < text.Length && text[at] == ' ') at++;
            indent = rest;
        }
        while (at < text.Length);
    }

    /// <summary>One row of <see cref="Indented"/>: <paramref name="indent"/> spaces and <paramref name="text"/> from <paramref name="start"/>
    /// up to <paramref name="end"/> without its trailing spaces, made in one allocation, or none when the row is the whole text as it
    /// stands (kb/Work PB5941: the object code prints a row per IL instruction).</summary>
    private static string Row(int indent, string text, int start, int end)
    {
        var piece = text.AsSpan(start, end - start).TrimEnd(' ');
        if (indent == 0 && piece.Length == text.Length) return text;
        return indent <= Blanks.Length ? string.Concat(Blanks.AsSpan(0, indent), piece) : new string(' ', indent) + piece.ToString();
    }

    /// <summary>The spaces a row is indented by, as wide as the widest page.</summary>
    private static readonly string Blanks = new(' ', ListingRequest.MaximumPageWidth);

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
            CompilerText(header),
            EditionText(header),
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

    /// <summary>The compiler as every renderer names it: <c>WiseOwl COBOL</c> and its version.</summary>
    public static string CompilerText(ListingHeader header) => "WiseOwl COBOL " + header.CompilerVersion;

    /// <summary>The edition as every renderer names it: its name and its <c>--std</c> number.</summary>
    public static string EditionText(ListingHeader header) =>
        string.Create(CultureInfo.InvariantCulture, $"{header.EditionName} (--std {header.Edition})");

    /// <summary>The separator between the fields of a heading line.</summary>
    private const string Separator = "  ";

    /// <summary>The marker column of a row (DESIGN-listing.md section 6.2): 'X' omitted by conditional compilation, 'E' the
    /// DATE-COMPILED replacement, 'R' changed by REPLACE or COPY REPLACING, '/' a page-eject comment line, '*' a comment line,
    /// 'D' a debugging line, '&gt;' a compiler directive line, '-' a continuation line; a space otherwise.</summary>
    public static char MarkerOf(ListingLine line) => line switch
    {
        { Omitted: true } => 'X',
        { Replacement: ListingReplacement.DateCompiled } => 'E',
        { Replacement: ListingReplacement.Replacing } => ReplacedMarker,
        { Kind: ListingLineKind.PageEjectComment } => '/',
        { Kind: ListingLineKind.Comment } => '*',
        { Kind: ListingLineKind.DebuggingLine } => 'D',
        { Kind: ListingLineKind.Directive } => '>',
        { Kind: ListingLineKind.Continuation } => '-',
        _ => ' ',
    };

    /// <summary>The marker of a line a replacing action changed and of each row of its compiled text under it.</summary>
    public const char ReplacedMarker = 'R';

    /// <summary>The depth column: blank for the source, 1-9, '+' beyond.</summary>
    public static char DepthMark(int depth) => depth switch { 0 => ' ', <= 9 => (char)('0' + depth), _ => '+' };

    /// <summary>The file-line column of a source row: the line's physical line in its file, printed only where it says something the
    /// listing number does not. Until a program's first inclusion the two are the same number, and printing it twice on every line
    /// read as a duplicate (kb/Work PB5950); after an inclusion, and on every line of one, they differ and the column names the line
    /// in the file the programmer edits.</summary>
    public static string FileLineColumn(ListingLine line) =>
        line.FileLine == line.Number ? "" : line.FileLine.ToString(CultureInfo.InvariantCulture);

    private static string SourceRow(ListingLine line, string text) =>
        string.Create(CultureInfo.InvariantCulture,
            $"{line.Number,6} {DepthMark(line.Depth)}{MarkerOf(line)} {FileLineColumn(line),6} {text}");

    private static string ContinuationRow(string text) => new string(' ', 8) + "+" + new string(' ', TextColumn - 10) + text;

    /// <summary>The banner that opens an inclusion's lines (DESIGN-listing.md section 6.2): the COPY statement's names, the file read
    /// and the depth.</summary>
    public static string Banner(ListingInclusion inclusion, IReadOnlyDictionary<int, string> files) =>
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
