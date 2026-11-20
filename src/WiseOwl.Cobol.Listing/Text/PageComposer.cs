// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

namespace WiseOwl.Cobol.Listing.Text;

/// <summary>One printed row of a text listing section.</summary>
/// <param name="Text">The row as printed (trailing spaces are dropped on output).</param>
/// <param name="PageBreakBefore">A page starts before this row (DESIGN-listing.md section 4.5's answer, or a section start).</param>
/// <param name="Program">The name of the program the row belongs to, or null for a row that belongs to none: the page
/// heading names the program of the page's first row that has one (§1985:VI.3.3.4 GR1, §1985:X.3.1.4 GR1).</param>
/// <remarks>A value: a listing of every section prints tens of thousands of rows (the object code), each passing through the
/// paginator once (kb/Work PB5941).</remarks>
public readonly record struct ListingRow(string Text, bool PageBreakBefore, string? Program);

/// <summary>
/// ⛔ THE ONE PAGINATOR of the text listing (DESIGN-listing.md section 7.1): it turns rows into pages. A page starts before a
/// row marked <see cref="ListingRow.PageBreakBefore"/> (a '/' comment line, §1985:IV.7.2.4; a new outermost program) and
/// when the page is full; each page opens with its heading; every page after the first begins with a form feed (U+000C);
/// every line ends in LF. A page length of 0 paginates only at the marked rows.
/// <para>STREAMED (kb/Work PB5941): the rows are read as they are produced and each page is written to the writer when it closes,
/// so the paginator holds one page of rows, never the section or the listing.</para>
/// </summary>
public static class PageComposer
{
    /// <summary>The form feed that begins every page after the first.</summary>
    public const char FormFeed = '\f';

    /// <summary>Compose <paramref name="rows"/> into pages of <paramref name="pageLength"/> lines (heading included; 0 for no length
    /// limit), each page's heading made by <paramref name="heading"/> from the 1-based page number and the program the page names
    /// (null when no row so far belongs to one), and write them to <paramref name="writer"/>. <paramref name="firstPage"/> is the
    /// number of the first page this call writes (a later section continues the numbering), and every page but the listing's first
    /// (<paramref name="firstPage"/> 1) begins with a form feed. Answers how many pages were written.</summary>
    public static int Compose(TextWriter writer, IEnumerable<ListingRow> rows, int pageLength,
        Func<int, string?, IReadOnlyList<string>> heading, int firstPage = 1)
    {
        var page = new List<ListingRow>();
        int number = firstPage;
        string? program = null;   // carried from page to page: a page of rows that belong to no program names the last one
        int capacity = int.MaxValue;

        void Close()
        {
            foreach (var row in page)
                if (row.Program is not null) { program = row.Program; break; }
            if (number > 1) writer.Write(FormFeed);
            foreach (string line in heading(number, program)) WriteLine(writer, line);
            foreach (var row in page) WriteLine(writer, row.Text);
            if (page.Count > 0 && page[^1].Program is { } last) program = last;
            page.Clear();
            number++;
        }

        foreach (var row in rows)
        {
            if (capacity == int.MaxValue && pageLength > 0) capacity = Math.Max(1, pageLength - heading(number, null).Count);
            if (page.Count > 0 && (row.PageBreakBefore || page.Count >= capacity)) Close();
            page.Add(row);
        }
        Close();   // the last page, or the one page of an empty section
        return number - firstPage;
    }

    private static void WriteLine(TextWriter writer, string line)
    {
        writer.Write(line.AsSpan().TrimEnd(' '));
        writer.Write('\n');
    }
}
