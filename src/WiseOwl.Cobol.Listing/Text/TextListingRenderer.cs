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

        var rows = new List<ListingRow>();
        foreach (var line in document.Lines)
        {
            if (!line.Listed || line.Replacement == ListingReplacement.DateCompiledEntry) continue;
            bool breakBefore = line.PageBreakBefore;
            if (banners.TryGetValue(line.Number, out var inclusion))
            {
                // The banner opens the inclusion (DESIGN-listing.md section 6.2): a page the first line starts begins at the banner.
                rows.Add(new ListingRow(new string(' ', TextColumn - 1) + Banner(inclusion, files), breakBefore, ProgramOf(line)));
                breakBefore = false;
            }
            string text = line.Replacement == ListingReplacement.DateCompiled ? line.Replaced ?? line.Text : line.Text;
            bool first = true;
            foreach (string piece in Wrap(text))
            {
                rows.Add(new ListingRow(first ? SourceRow(line, piece) : ContinuationRow(piece), first && breakBefore, ProgramOf(line)));
                first = false;
            }
        }
        var header = document.Header;
        return PageComposer.Compose(rows, request.PageLength, (page, program) =>
        [
            Fit(IdentificationLine(header, program), PageLabel(page), request.PageWidth),
            Fit("SOURCE  " + header.MainSource, "", request.PageWidth),
            "",
            Ruler,
        ]);
    }

    /// <summary>The first heading line (DESIGN-listing.md section 6.1), without the page number: the compiler, the edition, the
    /// program the page names (§1985:VI.3.3.4 GR1: the program-name "identifies the source program, the object program, and
    /// all listings pertaining to a particular program") and the compilation instant.</summary>
    public static string IdentificationLine(ListingHeader header, string? program) =>
        string.Create(CultureInfo.InvariantCulture,
            $"WiseOwl COBOL {header.CompilerVersion}  {header.EditionName} (--std {header.Edition})  {program ?? ""}  {ListingTimestamp.Format(header.CompiledAt)}");

    /// <summary>The page number as the heading prints it.</summary>
    public static string PageLabel(int page) => string.Create(CultureInfo.InvariantCulture, $"PAGE {page}");

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
        var piece = new StringBuilder();
        int positions = 0;
        foreach (var rune in text.EnumerateRunes())
        {
            if (positions == TextWidth)
            {
                yield return piece.ToString();
                piece.Clear();
                positions = 0;
            }
            piece.Append(rune.ToString());
            positions++;
        }
        yield return piece.ToString();
    }

    /// <summary><paramref name="left"/>, then <paramref name="right"/> ending at column <paramref name="width"/>; the left
    /// part is cut when both do not fit.</summary>
    private static string Fit(string left, string right, int width)
    {
        int room = width - right.Length - (right.Length > 0 ? 2 : 0);
        if (left.Length > room) left = left[..Math.Max(room, 0)];
        return right.Length == 0 ? left : left.PadRight(width - right.Length) + right;
    }
}
