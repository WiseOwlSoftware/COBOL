// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using System.Text;
using WiseOwl.Cobol.Listing.Text;

namespace WiseOwl.Cobol.Listing.Html;

/// <summary>
/// The hyperlinked HTML listing (DESIGN-listing.md section 7.2): ONE self-contained page, its style and its small script inline and
/// no external resource. It reads ONLY the model, as the text listing does, and prints every word the text listing prints through
/// the text listing's own helpers (the markers, the banners, the margin, the diagnostic and DISPLAY rows, the severity, kind and
/// origin words, the section titles and notes, each table's headings, the data map's facts, the file map's organization, access
/// and flags, the object code's token, range and stretch, the statistics' counts and notes), so the two cannot disagree; the Unit
/// assembly's <c>HtmlListingRendererTests.EveryWordThePageSharesWithTheText_IsTheTextListings</c> reads both renderings of one
/// document and holds it so. What differs is the form:
/// <list type="bullet">
/// <item>the page has no pages: a line the visibility pass starts a page before (<see cref="ListingLine.PageBreakBefore"/>) carries
/// a rule on screen and a page break when printed;</item>
/// <item>every line is present: a line the text listing does not print (<see cref="ListingLine.Listed"/> false, omitted by a false
/// conditional-compilation branch, or a comment-entry line replaced with DATE-COMPILED) stands greyed in a collapsed block that
/// says why, and an inclusion is a collapsible block opened by its banner, collapsed when COPY SUPPRESS wrote it;</item>
/// <item>each diagnostic is anchored under its line, its column marked in the line's text, and listed in the diagnostic summary,
/// which links to it; each statement's object code (its method, IL range and, when asked for, its instructions) opens under its
/// line;</item>
/// <item>a reference between the listing's own records is a link: a data description line's margin to its data map entry, an
/// entry's facts to the entries they name, the program map, the object-code map and the verbs to their lines.</item>
/// </list>
/// The markup is well-formed XML as well as HTML (every element closed, the five XML characters escaped, the script in a CDATA
/// section), so a test or a tool can read the page with an XML reader.
/// </summary>
public static class HtmlListingRenderer
{
    /// <summary>Render <paramref name="document"/> as the HTML listing: the identification, a bar of links to each section the
    /// document holds with the filter, then each section in <see cref="ListingSections.Order"/>, a section that could not be
    /// produced saying why. <paramref name="request"/>'s page length and width shape only the text listing. Written to
    /// <paramref name="page"/> as it is rendered (kb/Work PB5941): the page is never held whole.</summary>
    public static void Write(ListingDocument document, ListingRequest request, TextWriter page)
    {
        ArgumentNullException.ThrowIfNull(request);
        var header = document.Header;
        page.Write("<!DOCTYPE html>\n<html lang=\"en\">\n<head>\n<meta charset=\"utf-8\"/>\n");
        page.Write("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"/>\n<title>");
        Escape(page, header.MainSource + " - WiseOwl COBOL listing");
        page.Write("</title>\n<style>");
        page.Write(Style);
        page.Write("</style>\n</head>\n<body>\n");
        Identification(page, document);
        var statuses = ListingSections.Order.Select(s => document.Sections.FirstOrDefault(status => status.Section == s))
            .OfType<ListingSectionStatus>().ToList();
        Navigation(page, statuses);
        page.Write("<main>\n");
        var sections = new Sections(document, page);
        foreach (var status in statuses)
        {
            page.Write("<section class=\"section\" id=\"");
            page.Write(SectionId(status.Section));
            page.Write("\">\n<h2>");
            Escape(page, ListingSections.Title(status.Section));
            page.Write("</h2>\n");
            if (status.Produced) sections.Visit(status.Section);
            else Note(page, ListingSections.NotProduced(status.Reason));
            page.Write("</section>\n");
        }
        page.Write("</main>\n<script>//<![CDATA[\n");
        page.Write(Script);
        page.Write("\n//]]></script>\n</body>\n</html>\n");
    }

    /// <summary>The element id of a section's block: <c>S-</c> and its <c>--listing-sections</c> word.</summary>
    public static string SectionId(ListingSection section) => "S-" + ListingSections.Spelling(section);

    /// <summary>The element id of a listing line's row: <c>L</c> and its number.</summary>
    public static string LineId(int line) => LinePrefix + line.ToString(CultureInfo.InvariantCulture);

    /// <summary>The element id of the block under a statement's line that holds one stretch of its object code (and its instructions):
    /// <c>C</c> and the stretch's 1-based place in <see cref="ListingObjectCodeMap.Ranges"/>.</summary>
    public static string RangeId(int range) => RangePrefix + range.ToString(CultureInfo.InvariantCulture);

    private const string LinePrefix = "L";
    private const string RangePrefix = "C";

    /// <summary>The element id of a diagnostic under its line: <c>D</c> and its id.</summary>
    public static string DiagnosticId(int diagnostic) => "D" + diagnostic.ToString(CultureInfo.InvariantCulture);

    /// <summary>The element id of an inclusion's block: <c>I</c> and its id.</summary>
    public static string InclusionId(int inclusion) => "I" + inclusion.ToString(CultureInfo.InvariantCulture);

    /// <summary>The element id of a data map entry's row: <c>M</c> and its id.</summary>
    public static string DataItemId(int item) => "M" + item.ToString(CultureInfo.InvariantCulture);

    /// <summary>The element id of a file map entry's row: <c>F</c> and its id.</summary>
    public static string FileEntryId(int file) => "F" + file.ToString(CultureInfo.InvariantCulture);

    /// <summary>The element id of a program map row: <c>P</c> and the unit's id.</summary>
    public static string ProgramId(int program) => "P" + program.ToString(CultureInfo.InvariantCulture);

    /// <summary>The element id of a method of the object-code map: <c>X</c> and its id.</summary>
    public static string MethodId(int method) => "X" + method.ToString(CultureInfo.InvariantCulture);

    /// <summary>Why a line stands in a collapsed block, or null for a line the text listing prints: not listed (a LISTING
    /// directive or COPY SUPPRESS), replaced with the DATE-COMPILED paragraph, or omitted by conditional compilation.</summary>
    public static string? HiddenReason(ListingLine line) => line switch
    {
        { Listed: false } => "not listed",
        { Replacement: ListingReplacement.DateCompiledEntry } => "replaced with the DATE-COMPILED paragraph",
        { Omitted: true } => "omitted by conditional compilation",
        _ => null,
    };

    private static void Identification(TextWriter page, ListingDocument document)
    {
        var header = document.Header;
        page.Write("<header>\n<h1>");
        Escape(page, TextListingRenderer.CompilerText(header));
        page.Write("</h1>\n<dl class=\"id\">");
        Term(page, "Source", header.MainSource);
        Term(page, "Edition", TextListingRenderer.EditionText(header));
        Term(page, "Compiled", ListingTimestamp.Format(header.CompiledAt));
        Term(page, "Outcome", header.Outcome.ToString());
        Term(page, "Parse", header.Parse.ToString());
        page.Write("</dl>\n<table class=\"files\">\n<thead><tr><th>FILE</th><th>ROLE</th><th>PATH</th><th>SHA-256</th></tr></thead>\n<tbody>\n");
        foreach (var file in document.Files)
        {
            page.Write("<tr>");
            Cell(page, file.Id.ToString(CultureInfo.InvariantCulture), "n");
            Cell(page, file.Role.ToString());
            Cell(page, file.Path);
            Cell(page, file.Sha256, "h");
            page.Write("</tr>\n");
        }
        page.Write("</tbody>\n</table>\n</header>\n");
    }

    private static void Navigation(TextWriter page, IReadOnlyList<ListingSectionStatus> statuses)
    {
        page.Write("<nav>\n");
        foreach (var status in statuses)
        {
            page.Write("<a href=\"#");
            page.Write(SectionId(status.Section));
            page.Write("\">");
            Escape(page, ListingSections.Title(status.Section));
            page.Write("</a>\n");
        }
        page.Write("<label>Filter <input type=\"search\" id=\"filter\" placeholder=\"text on a source line\"/></label>\n");
        page.Write("<label><input type=\"checkbox\" id=\"dgonly\"/> lines with diagnostics</label>\n");
        page.Write("<button type=\"button\" id=\"expand\">Expand all</button>\n");
        page.Write("<button type=\"button\" id=\"collapse\">Collapse all</button>\n</nav>\n");
    }

    /// <summary>Each produced section's markup, appended to the page (DESIGN-listing.md section 7): each method returns the page.</summary>
    private sealed class Sections(ListingDocument document, TextWriter page) : IListingSectionVisitor<TextWriter>
    {
        private readonly HashSet<int> _lines = [.. document.Lines.Select(l => l.Number)];
        private readonly HashSet<int> _items = [.. document.DataMap?.Items.Select(i => i.Id) ?? []];
        private readonly IReadOnlyDictionary<int, ListingMethod> _methods =
            document.ObjectCode?.Methods.ToDictionary(m => m.Id) ?? new Dictionary<int, ListingMethod>();

        /// <summary>A link to a listing line when the page holds it, else the number.</summary>
        private void LineLink(int line)
        {
            string number = line.ToString(CultureInfo.InvariantCulture);
            if (_lines.Contains(line)) Link(page, "#" + LineId(line), number);
            else page.Write(number);
        }

        public TextWriter Source()
        {
            new SourceWriter(document, page, _items, _methods).Write();
            return page;
        }

        public TextWriter Programs()
        {
            Head(page, [.. ProgramMapText.Headings, "ATTRIBUTES"]);
            foreach (var program in document.Programs)
            {
                page.Write("<tr id=\"");
                page.Write(ProgramId(program.Id));
                page.Write("\">");
                Cell(page, program.Id.ToString(CultureInfo.InvariantCulture), "n");
                Cell(page, ProgramMapText.KindWord(program.Kind));
                page.Write("<td class=\"n\">");
                LineLink(program.FirstLine);
                page.Write("</td><td class=\"n\">");
                LineLink(program.LastLine);
                page.Write("</td>");
                Indented(page, program.Name, program.Depth);
                Cell(page, program.Attributes is { } attributes ? ProgramMapText.AttributesText(attributes) : "");
                page.Write("</tr>\n");
            }
            page.Write("</tbody>\n</table>\n");
            if (document.Header.Parse is ListingParse.Recovered) Note(page, ProgramMapText.RecoveredNote);
            return page;
        }

        public TextWriter Diagnostics()
        {
            page.Write("<p class=\"counts\">");
            Escape(page, TextListingRenderer.CountsText(document.Diagnostics));
            page.Write("</p>\n");
            Head(page, ["LINE", "SEVERITY", "CODE", "PLACE", "MESSAGE"]);
            var files = document.Files.ToDictionary(f => f.Id, f => f.Path);
            foreach (var diagnostic in document.Diagnostics)
            {
                page.Write("<tr class=\"");
                page.Write(SeverityClass(diagnostic.Severity));
                page.Write("\"><td class=\"n\">");
                if (diagnostic.Line is { } line && _lines.Contains(line))
                    Link(page, "#" + DiagnosticId(diagnostic.Id), line.ToString(CultureInfo.InvariantCulture));
                else page.Write(diagnostic.Line?.ToString(CultureInfo.InvariantCulture) ?? "-");
                page.Write("</td>");
                Cell(page, TextListingRenderer.SeverityWord(diagnostic.Severity));
                Cell(page, diagnostic.Code);
                Cell(page, TextListingRenderer.PlaceText(diagnostic, files) ?? "");
                Cell(page, diagnostic.Message, "msg");
                page.Write("</tr>\n");
            }
            page.Write("</tbody>\n</table>\n");
            FlaggedConstructs();
            return page;
        }

        /// <summary>The table of flagged constructs by edition (DESIGN-listing.md section 6.11) in the text listing's words
        /// (<see cref="FlaggedConstructText"/>), each listing line a link to its diagnostic under its line where the page holds that
        /// line; nothing when no diagnostic is an edition flag.</summary>
        private void FlaggedConstructs()
        {
            if (document.FlaggedConstructs.Count == 0) return;
            page.Write("<h3>");
            Escape(page, FlaggedConstructText.Title);
            page.Write("</h3>\n");
            Head(page, FlaggedConstructText.Headings);
            var diagnostics = document.Diagnostics.ToDictionary(d => d.Id);
            foreach (var construct in document.FlaggedConstructs)
            {
                page.Write("<tr>");
                Cell(page, FlaggedConstructText.EditionText(construct.Edition), "n");
                Cell(page, FlaggedConstructText.KindWord(construct.Kind));
                Cell(page, FlaggedConstructText.CountText(construct), "n");
                Cell(page, FlaggedConstructText.ConstructText(construct));
                page.Write("<td>");
                bool first = true;
                foreach (int id in construct.Diagnostics)
                {
                    if (!first) page.Write(' ');
                    first = false;
                    if (diagnostics[id].Line is { } line && _lines.Contains(line))
                        Link(page, "#" + DiagnosticId(id), line.ToString(CultureInfo.InvariantCulture));
                    else page.Write(diagnostics[id].Line?.ToString(CultureInfo.InvariantCulture) ?? "-");
                }
                page.Write("</td></tr>\n");
            }
            page.Write("</tbody>\n</table>\n");
        }

        public TextWriter Options()
        {
            Head(page, OptionsText.Headings);
            foreach (var option in document.Options)
            {
                page.Write("<tr>");
                Cell(page, option.Name);
                Cell(page, OptionsText.OriginWord(option.Origin));
                Cell(page, option.Value);
                page.Write("</tr>\n");
            }
            page.Write("</tbody>\n</table>\n");
            return page;
        }

        public TextWriter Statistics()
        {
            var statistics = document.Statistics
                ?? throw new InvalidOperationException("the statistics section is produced but the document holds no statistics");
            page.Write("<table>\n<tbody>\n");
            Count(StatisticsText.LinesLabel, statistics.Lines.Lines);
            foreach (var (name, count) in StatisticsText.CountsOf(statistics.Lines)) Count(name, count, "sub");
            page.Write("</tbody>\n</table>\n<h3>");
            Escape(page, StatisticsText.FilesTitle);
            page.Write("</h3>\n<table>\n<tbody>\n");
            var paths = document.Files.ToDictionary(f => f.Id, f => f.Path);
            foreach (var file in statistics.Files)
            {
                page.Write("<tr>");
                Cell(page, file.File.ToString(CultureInfo.InvariantCulture), "n");
                Cell(page, file.Lines.Lines.ToString(CultureInfo.InvariantCulture), "n");
                Cell(page, paths[file.File]);
                Cell(page, StatisticsText.CountsText(file.Lines));
                page.Write("</tr>\n");
            }
            page.Write("</tbody>\n</table>\n");
            if (statistics.Parse is { } parse)
            {
                page.Write("<table>\n<tbody>\n");
                Count(StatisticsText.ProgramsLabel, parse.Programs);
                Count(StatisticsText.StatementsLabel, parse.Statements);
                page.Write("</tbody>\n</table>\n");
                if (document.Header.Parse is ListingParse.Recovered) Note(page, StatisticsText.RecoveredNote);
            }
            else Note(page, StatisticsText.NotParsedNote);
            page.Write("<table>\n<tbody>\n<tr>");
            Cell(page, StatisticsText.DiagnosticsLabel);
            Cell(page, StatisticsText.DiagnosticCountsText(statistics));
            page.Write("</tr>\n</tbody>\n</table>\n");
            if (statistics.Parse is not { } verbs) return page;
            Head(page, StatisticsText.VerbHeadings);
            foreach (var verb in verbs.Verbs)
            {
                page.Write("<tr>");
                Cell(page, verb.Verb);
                Cell(page, verb.Count.ToString(CultureInfo.InvariantCulture), "n");
                page.Write("<td>");
                bool first = true;
                foreach (int line in verb.Lines)
                {
                    if (!first) page.Write(' ');
                    LineLink(line);
                    first = false;
                }
                page.Write("</td></tr>\n");
            }
            page.Write("</tbody>\n</table>\n");
            return page;
        }

        /// <summary>A row of the statistics: a count's label and the count.</summary>
        private void Count(string label, int count, string? cssClass = null)
        {
            page.Write(cssClass is null ? "<tr>" : "<tr class=\"" + cssClass + "\">");
            Cell(page, label);
            Cell(page, count.ToString(CultureInfo.InvariantCulture), "n");
            page.Write("</tr>\n");
        }

        public TextWriter DataMap()
        {
            var map = document.DataMap ?? throw new InvalidOperationException("the data map section is produced but the document holds no data map");
            Head(page, [.. DataMapText.ItemHeadings, "DETAILS"], "map");
            var nesting = DataMapText.NestingOf(map.Items);
            foreach (var item in map.Items)
            {
                page.Write("<tr id=\"");
                page.Write(DataItemId(item.Id));
                page.Write("\">");
                Cell(page, item.Id.ToString(CultureInfo.InvariantCulture), "n");
                page.Write("<td class=\"n\">");
                if (item.Line is { } line) LineLink(line); else Escape(page, DataMapText.NumberText(null));
                page.Write("</td>");
                Cell(page, DataMapText.LevelText(item.Level));
                Cell(page, DataMapText.KindWord(item.Kind));
                Indented(page, item.Name, nesting[item.Id]);
                Cell(page, DataMapText.NumberText(item.Offset), "n");
                Cell(page, DataMapText.NumberText(item.Length), "n");
                Cell(page, DataMapText.NumberText(item.Bytes), "n");
                Cell(page, item.Usage ?? "");
                Cell(page, item.Picture ?? "");
                Parts(DataMapText.DetailParts(item));
                page.Write("</tr>\n");
            }
            page.Write("</tbody>\n</table>\n<h3>");
            Escape(page, DataMapText.FilesTitle);
            page.Write("</h3>\n");
            Head(page, [.. DataMapText.FileHeadings, "DETAILS"], "map");
            foreach (var file in map.Files)
            {
                page.Write("<tr id=\"");
                page.Write(FileEntryId(file.Id));
                page.Write("\">");
                Cell(page, file.Id.ToString(CultureInfo.InvariantCulture), "n");
                page.Write("<td class=\"n\">");
                if (file.Line is { } line) LineLink(line); else Escape(page, DataMapText.NumberText(null));
                page.Write("</td>");
                Cell(page, file.Name);
                Cell(page, DataMapText.OrganizationText(file));
                Cell(page, DataMapText.AccessText(file));
                page.Write("<td>");
                bool first = true;
                foreach (int record in file.Records)
                {
                    if (!first) page.Write(',');
                    ItemLink(record, record.ToString(CultureInfo.InvariantCulture));
                    first = false;
                }
                // The text listing prints the EXTERNAL and GLOBAL words after the records.
                foreach (string flag in DataMapText.FlagWords(file)) Escape(page, "  " + flag);
                page.Write("</td>");
                Parts(DataMapText.FileDetailParts(file));
                page.Write("</tr>\n");
            }
            page.Write("</tbody>\n</table>\n");
            return page;
        }

        private void Parts(IReadOnlyList<DataMapPart> parts)
        {
            page.Write("<td>");
            foreach (var part in parts)
                if (part.Item is { } item) ItemLink(item, part.Text);
                else Escape(page, part.Text);
            page.Write("</td>");
        }

        private void ItemLink(int item, string text)
        {
            if (_items.Contains(item)) Link(page, "#" + DataItemId(item), text);
            else Escape(page, text);
        }

        public TextWriter ObjectMap()
        {
            var map = document.ObjectCode ?? throw new InvalidOperationException("the object-code map section is produced but the document holds no object-code map");
            page.Write("<p>");
            Escape(page, ObjectCodeText.AssemblyLabel + "  " + map.Assembly);
            page.Write("</p>\n");
            Head(page, ObjectCodeText.MethodHeadings);
            foreach (var method in map.Methods)
            {
                page.Write("<tr id=\"");
                page.Write(MethodId(method.Id));
                page.Write("\">");
                Cell(page, method.Id.ToString(CultureInfo.InvariantCulture), "n");
                Cell(page, ObjectCodeText.Token(method.Token));
                Cell(page, method.CodeSize.ToString(CultureInfo.InvariantCulture), "n");
                Cell(page, ObjectCodeText.MethodName(method));
                page.Write("</tr>\n");
            }
            page.Write("</tbody>\n</table>\n");
            Head(page, ObjectCodeText.RangeHeadings);
            foreach (var range in map.Ranges)
            {
                page.Write("<tr><td class=\"n\">");
                LineLink(range.Line);
                page.Write("</td>");
                Cell(page, range.Column.ToString(CultureInfo.InvariantCulture), "n");
                page.Write("<td class=\"n\">");
                Link(page, "#" + MethodId(range.Method), range.Method.ToString(CultureInfo.InvariantCulture));
                page.Write("</td>");
                Cell(page, ObjectCodeText.RangeText(range));
                page.Write("</tr>\n");
            }
            page.Write("</tbody>\n</table>\n");
            return page;
        }

        public TextWriter ObjectCode()
        {
            var map = document.ObjectCode ?? throw new InvalidOperationException("the object-code section is produced but the document holds no object-code map");
            if (map.Ranges.Count == 0) Note(page, ObjectCodeText.NoCodeNote);
            var text = document.Lines.ToDictionary(l => l.Number, l => l.Text);
            for (int index = 1; index <= map.Ranges.Count; index++)
            {
                var range = map.Ranges[index - 1];
                page.Write("<div class=\"code\"><p>");
                LineLink(range.Line);
                page.Write(' ');
                // ONE COPY OF THE INSTRUCTIONS (DESIGN-listing.md section 7.2; kb/Work PB5941): a stretch whose statement's line the page
                // holds shows its instructions under that line, and this entry links to them; one whose line it does not hold shows them here.
                bool underItsLine = text.TryGetValue(range.Line, out string? source);
                if (underItsLine) Link(page, "#" + RangeId(index), StretchHeading(range, _methods));
                else Escape(page, StretchHeading(range, _methods));
                page.Write("</p>\n");
                if (source is not null)
                {
                    page.Write("<pre>");
                    Escape(page, source.TrimEnd());
                    page.Write("</pre>\n");
                }
                if (!underItsLine) Instructions(page, range.Instructions);
                page.Write("</div>\n");
            }
            return page;
        }
    }

    /// <summary>The source section's markup (DESIGN-listing.md sections 6.2 and 7.2): every line in listing order, each inclusion a
    /// collapsible block, each run of lines the text listing does not print a collapsed block, and under a line its compiled text
    /// when a replacing action changed it, its diagnostics, its DISPLAY transfers and its object code.</summary>
    private sealed class SourceWriter(ListingDocument document, TextWriter page, HashSet<int> items,
        IReadOnlyDictionary<int, ListingMethod> methods)
    {
        private readonly Dictionary<int, ListingInclusion> _inclusions = document.Inclusions.ToDictionary(i => i.Id);
        private readonly Dictionary<int, string> _files = document.Files.ToDictionary(f => f.Id, f => f.Path);
        private readonly Dictionary<int, ListingDiagnostic> _diagnostics = document.Diagnostics.ToDictionary(d => d.Id);
        private readonly ILookup<int, ListingDisplay> _displays = document.Displays.ToLookup(d => d.Line);
        private readonly ILookup<int, (ListingObjectRange Range, int Index)> _ranges =
            (document.ObjectCode?.Ranges ?? []).Select((r, index) => (r, index + 1)).ToLookup(r => r.r.Line);
        private readonly List<int> _open = [];   // the inclusions whose blocks are open, outermost first
        private readonly HashSet<int> _opened = [];   // the inclusions whose blocks have been opened once

        public void Write()
        {
            page.Write("<div class=\"src\">\n");
            var lines = document.Lines;
            for (int at = 0; at < lines.Count; at++)
            {
                var line = lines[at];
                Enter(line.Inclusion);
                if (HiddenReason(line) is { } reason && !_open.Any(i => _inclusions[i].Suppress))
                {
                    int last = at;
                    while (last + 1 < lines.Count && !_displays.Contains(lines[last].Number)
                           && lines[last + 1].Inclusion == line.Inclusion && HiddenReason(lines[last + 1]) == reason)
                        last++;
                    page.Write("<details class=\"hid\"><summary>");
                    int count = last - at + 1;
                    Escape(page, string.Create(CultureInfo.InvariantCulture,
                        $"{count} {(count == 1 ? "line" : "lines")} {reason} ({lines[at].Number}{(count > 1 ? "-" + lines[last].Number.ToString(CultureInfo.InvariantCulture) : "")})"));
                    page.Write("</summary>\n");
                    for (int each = at; each <= last; each++) Line(lines[each]);
                    page.Write("</details>\n");
                    Displays(lines[last]);
                    at = last;
                    continue;
                }
                Line(line);
                Displays(line);
            }
            Enter(null);
            page.Write("</div>\n");
        }

        /// <summary>Close the open inclusion blocks <paramref name="inclusion"/> is not inside, then open each of its own not open yet,
        /// outermost first, each under its banner.</summary>
        private void Enter(int? inclusion)
        {
            var chain = new List<int>();
            for (int? at = inclusion; at is { } id; at = _inclusions[id].Parent) chain.Insert(0, id);
            int keep = 0;
            while (keep < _open.Count && keep < chain.Count && _open[keep] == chain[keep]) keep++;
            for (int close = _open.Count - 1; close >= keep; close--) page.Write("</details>\n");
            _open.RemoveRange(keep, _open.Count - keep);
            foreach (int id in chain.Skip(keep))
            {
                Open(id);
                _open.Add(id);
            }
        }

        /// <summary>Open inclusion <paramref name="id"/>'s block under its banner: collapsed when its COPY statement wrote SUPPRESS,
        /// open otherwise. The block's first opening carries its id; a later one (the block reopened for the lines after a DISPLAY
        /// transfer closed it, <see cref="Displays"/>) says it is continued and carries none.</summary>
        private void Open(int id)
        {
            var inclusion = _inclusions[id];
            bool continued = !_opened.Add(id);
            page.Write("<details class=\"inc");
            page.Write(inclusion.Suppress ? " sup\"" : "\" open=\"\"");
            if (!continued)
            {
                page.Write(" id=\"");
                page.Write(InclusionId(id));
                page.Write('"');
            }
            page.Write("><summary>");
            Escape(page, TextListingRenderer.Banner(inclusion, _files) + (inclusion.Suppress ? " SUPPRESS" : "") + (continued ? " (continued)" : ""));
            page.Write("</summary>\n");
        }

        private void Line(ListingLine line)
        {
            page.Write("<div class=\"ln k-");
            page.Write(KindClass(line.Kind));
            if (line.PageBreakBefore) page.Write(" pb");
            if (HiddenReason(line) is not null) page.Write(" off");
            if (line.Diagnostics.Count > 0) page.Write(" hasdg");
            page.Write("\" id=\"");
            page.Write(LinePrefix);
            Number(page, line.Number);
            page.Write("\"><a class=\"no\" href=\"#");
            page.Write(LinePrefix);
            Number(page, line.Number);
            page.Write("\">");
            Number(page, line.Number);
            page.Write("</a><span class=\"dp\">");
            page.Write(TextListingRenderer.DepthMark(line.Depth));
            page.Write("</span><span class=\"mk\">");
            char marker = TextListingRenderer.MarkerOf(line);
            Escape(page, new ReadOnlySpan<char>(in marker));
            page.Write("</span><span class=\"fl\">");
            Number(page, line.FileLine);
            page.Write("</span><span class=\"tx\">");
            bool asWritten = line.Replacement is ListingReplacement.None or ListingReplacement.Replacing;
            if (line.Replacement == ListingReplacement.DateCompiled) Escape(page, line.Replaced ?? line.Text);
            else if (asWritten && line.Diagnostics.Count > 0)
                Marked(line.Text, line.Diagnostics.Select(id => _diagnostics[id].Column).OfType<int>().ToHashSet());
            else Escape(page, line.Text);
            page.Write("</span>");
            if (line.Margin is { } margin)
            {
                if (items.Contains(margin.Item)) Link(page, "#" + DataItemId(margin.Item), TextListingRenderer.MarginText(margin), "mg");
                else
                {
                    page.Write("<span class=\"mg\">");
                    Escape(page, TextListingRenderer.MarginText(margin));
                    page.Write("</span>");
                }
            }
            page.Write("</div>\n");
            if (line.Replacement == ListingReplacement.Replacing)
                foreach (string resultant in (line.Replaced ?? "").Split('\n'))
                {
                    page.Write("<div class=\"rp\"><span class=\"mk\">");
                    page.Write(TextListingRenderer.ReplacedMarker);
                    page.Write("</span><span class=\"tx\">");
                    Escape(page, resultant);
                    page.Write("</span></div>\n");
                }
            foreach (int id in line.Diagnostics)
            {
                var diagnostic = _diagnostics[id];
                page.Write("<div class=\"dg ");
                page.Write(SeverityClass(diagnostic.Severity));
                page.Write("\" id=\"");
                page.Write(DiagnosticId(id));
                page.Write("\">");
                Escape(page, TextListingRenderer.DiagnosticText(diagnostic));
                page.Write("</div>\n");
            }
            foreach (var (range, index) in _ranges[line.Number])
            {
                page.Write("<details class=\"obj\" id=\"");
                page.Write(RangePrefix);
                Number(page, index);
                page.Write("\"><summary>");
                Escape(page, StretchHeading(range, methods));
                page.Write("</summary>\n");
                if (range.Instructions is null) page.Write("<p class=\"np\">The object code section was not asked for.</p>\n");
                else Instructions(page, range.Instructions);
                page.Write("</details>\n");
            }
        }

        /// <summary>The DISPLAY directive's transfers at <paramref name="line"/> (§2023:7.3.12.4 GR2: "irrespective of whether it is
        /// suppressed by the LISTING directive"), outside any collapsed block, as the text listing prints them whether or not it prints
        /// their line. A run of hidden lines ends at a line with a transfer (<see cref="Write"/>); a SUPPRESS block cannot end there, so
        /// the open blocks from the outermost SUPPRESS one in are closed before the transfers, and the next line of those inclusions
        /// reopens them (<see cref="Enter"/>), each as its continuation.</summary>
        private void Displays(ListingLine line)
        {
            if (!_displays.Contains(line.Number)) return;
            int collapsed = _open.FindIndex(id => _inclusions[id].Suppress);
            if (collapsed >= 0)
            {
                for (int close = _open.Count - 1; close >= collapsed; close--) page.Write("</details>\n");
                _open.RemoveRange(collapsed, _open.Count - collapsed);
            }
            foreach (var display in _displays[line.Number])
            {
                page.Write("<div class=\"dsp\">");
                Escape(page, TextListingRenderer.DisplayText(display));
                page.Write("</div>\n");
            }
        }

        /// <summary><paramref name="text"/>, each 1-based character position in <paramref name="columns"/> marked (a character outside
        /// the Basic Multilingual Plane is one position, as every listing record's column counts).</summary>
        private void Marked(string text, HashSet<int> columns)
        {
            if (columns.Count == 0)
            {
                Escape(page, text);
                return;
            }
            int position = 1;
            for (int at = 0; at < text.Length; position++)
            {
                int width = char.IsHighSurrogate(text[at]) && at + 1 < text.Length && char.IsLowSurrogate(text[at + 1]) ? 2 : 1;
                bool mark = columns.Contains(position);
                if (mark) page.Write("<mark>");
                Escape(page, text.AsSpan(at, width));
                if (mark) page.Write("</mark>");
                at += width;
            }
            // A column one past the text's end (a diagnostic about a missing word) is marked as a space.
            if (columns.Contains(position)) page.Write("<mark> </mark>");
        }
    }

    private static void Instructions(TextWriter page, IReadOnlyList<ListingInstruction>? instructions)
    {
        if (instructions is null) return;
        page.Write("<table class=\"il\">\n<tbody>\n");
        foreach (var instruction in instructions)
        {
            page.Write("<tr><td>");
            Formatted(page, new IlOffset(instruction.Offset));   // no markup character in an offset
            page.Write("</td>");
            Cell(page, instruction.OpCode);
            Cell(page, instruction.Operand ?? "");
            page.Write("</tr>\n");
        }
        page.Write("</tbody>\n</table>\n");
    }

    /// <summary>A number as the page prints it, written without a string of its own (kb/Work PB5941: a number per line and per
    /// instruction).</summary>
    private static void Number(TextWriter page, int value) => Formatted(page, value);

    /// <summary><paramref name="value"/> formatted in the invariant culture straight to <paramref name="page"/>: for a value whose text
    /// holds no markup character (a number, an IL offset).</summary>
    private static void Formatted<T>(TextWriter page, T value) where T : ISpanFormattable
    {
        Span<char> text = stackalloc char[32];
        if (!value.TryFormat(text, out int length, default, CultureInfo.InvariantCulture))
            throw new InvalidOperationException($"'{value}' is longer than a number");
        page.Write(text[..length]);
    }

    /// <summary>The kind's name in lower case, which follows <c>k-</c> in a line's class on the page; made once per kind.</summary>
    private static string KindClass(ListingLineKind kind) => s_kindClasses[kind];

    private static readonly IReadOnlyDictionary<ListingLineKind, string> s_kindClasses =
        Enum.GetValues<ListingLineKind>().ToDictionary(k => k, k => k.ToString().ToLowerInvariant());

    private static string SeverityClass(ListingSeverity severity) => "sev-" + TextListingRenderer.SeverityWord(severity);

    /// <summary>The heading of a stretch of a statement's code: its column and the text listing's <see cref="ObjectCodeText.StretchText"/>.</summary>
    private static string StretchHeading(ListingObjectRange range, IReadOnlyDictionary<int, ListingMethod> methods) =>
        string.Create(CultureInfo.InvariantCulture, $"col {range.Column}  ") + ObjectCodeText.StretchText(range, methods[range.Method]);

    /// <summary>Open a table whose head row is <paramref name="headings"/>, and its body.</summary>
    private static void Head(TextWriter page, IEnumerable<string> headings, string? cssClass = null)
    {
        page.Write(cssClass is null ? "<table>\n<thead><tr>" : "<table class=\"" + cssClass + "\">\n<thead><tr>");
        foreach (string heading in headings)
        {
            page.Write("<th>");
            Escape(page, heading);
            page.Write("</th>");
        }
        page.Write("</tr></thead>\n<tbody>\n");
    }

    private static void Term(TextWriter page, string term, string value)
    {
        page.Write("<dt>");
        Escape(page, term);
        page.Write("</dt><dd>");
        Escape(page, value);
        page.Write("</dd>");
    }

    private static void Cell(TextWriter page, string text, string? cssClass = null)
    {
        page.Write(cssClass is null ? "<td>" : "<td class=\"" + cssClass + "\">");
        Escape(page, text);
        page.Write("</td>");
    }

    private static void Indented(TextWriter page, string name, int depth)
    {
        page.Write("<td class=\"nm\" style=\"padding-left:");
        page.Write((0.3 + 1.5 * depth).ToString("0.0", CultureInfo.InvariantCulture));
        page.Write("em\">");
        Escape(page, name);
        page.Write("</td>");
    }

    private static void Note(TextWriter page, string text)
    {
        page.Write("<p class=\"np\">");
        Escape(page, text);
        page.Write("</p>\n");
    }

    private static void Link(TextWriter page, string href, string text, string? cssClass = null)
    {
        page.Write("<a href=\"");
        page.Write(href);
        page.Write('"');
        if (cssClass is not null)
        {
            page.Write(" class=\"");
            page.Write(cssClass);
            page.Write('"');
        }
        page.Write('>');
        Escape(page, text);
        page.Write("</a>");
    }

    /// <summary>⛔ THE ONE ESCAPE: <paramref name="text"/> with the five characters markup gives a meaning replaced by their
    /// predefined entities, so every text of the model is printed as text, in an element or an attribute.</summary>
    public static void Escape(TextWriter page, ReadOnlySpan<char> text)
    {
        while (!text.IsEmpty)
        {
            int special = text.IndexOfAny(Markup);
            if (special < 0)
            {
                page.Write(text);
                return;
            }
            page.Write(text[..special]);
            page.Write(text[special] switch
            {
                '&' => "&amp;",
                '<' => "&lt;",
                '>' => "&gt;",
                '"' => "&quot;",
                _ => "&#39;",
            });
            text = text[(special + 1)..];
        }
    }

    /// <summary>The five characters markup gives a meaning (<see cref="Escape"/>).</summary>
    private static readonly System.Buffers.SearchValues<char> Markup = System.Buffers.SearchValues.Create("&<>\"'");

    private const string Style = """

        :root { --bg: #ffffff; --fg: #1d1d1f; --muted: #6e6e73; --rule: #d2d2d7; --err: #b3261e; --warn: #8a5a00; --info: #1f5fa8;
                --mark: #ffe08a; --link: #0b57d0; --head: #f5f5f7; }
        @media (prefers-color-scheme: dark) {
          :root { --bg: #161618; --fg: #e8e8ed; --muted: #98989d; --rule: #3a3a3c; --err: #ff6b5e; --warn: #f0b429; --info: #7ab4ff;
                  --mark: #6b5310; --link: #8ab4f8; --head: #1f1f22; } }
        html { scroll-padding-top: var(--navh, 5em); }
        body { margin: 0; background: var(--bg); color: var(--fg); font: 14px/1.4 system-ui, sans-serif; }
        header, main { padding: 0 16px; }
        h1 { font-size: 1.3em; } h2 { font-size: 1.1em; border-bottom: 1px solid var(--rule); padding-top: 1em; }
        a { color: var(--link); text-decoration: none; } a:hover { text-decoration: underline; }
        dl.id { display: grid; grid-template-columns: max-content 1fr; gap: 0 1em; } dt { color: var(--muted); } dd { margin: 0; }
        nav { position: sticky; top: 0; z-index: 1; display: flex; flex-wrap: wrap; gap: 0.4em 1em; align-items: center;
              padding: 6px 16px; background: var(--head); border-bottom: 1px solid var(--rule); }
        table { border-collapse: collapse; margin: 0.5em 0; } th, td { text-align: left; padding: 1px 8px; vertical-align: top; }
        th { color: var(--muted); font-weight: 600; border-bottom: 1px solid var(--rule); } td.n { text-align: right; }
        td.h, .src, pre, table.il, table.map, td.nm { font-family: ui-monospace, Consolas, monospace; }
        .src { font-size: 13px; overflow-x: auto; }
        .ln, .rp, .dg, .dsp { white-space: pre; }
        .ln .no { display: inline-block; width: 6ch; text-align: right; color: var(--muted); }
        .ln .dp, .ln .mk, .rp .mk { display: inline-block; width: 1ch; margin-left: 1ch; }
        .ln .fl { display: inline-block; width: 6ch; text-align: right; color: var(--muted); margin-right: 1ch; }
        .rp { padding-left: 9ch; color: var(--muted); } .rp .mk { width: 9ch; margin-left: 0; }
        .ln .mg { margin-left: 2ch; color: var(--muted); }
        tr.sub td:first-child { padding-left: 2em; }
        .k-comment .tx, .k-pageejectcomment .tx, .k-commententry .tx { color: var(--muted); font-style: italic; }
        .k-directive .tx { color: var(--info); }
        .pb { border-top: 1px dashed var(--rule); }
        .off { opacity: 0.55; }
        mark { background: var(--mark); color: inherit; }
        .dg { padding-left: 17ch; } .sev-error { color: var(--err); } .sev-warning { color: var(--warn); } .sev-info { color: var(--info); }
        .dsp { padding-left: 17ch; font-weight: 600; }
        details.inc { border-left: 2px solid var(--rule); margin-left: 8ch; } details.inc > summary { color: var(--muted); }
        details.hid > summary, details.obj > summary { color: var(--muted); padding-left: 17ch; font-size: 12px; }
        details.obj table { margin-left: 19ch; }
        .np { color: var(--muted); }
        .code { border-top: 1px solid var(--rule); } .code p { margin: 0.3em 0; }
        .filtered { display: none; }
        @media print { nav { display: none; } .pb { break-before: page; border: 0; } details > * { display: revert; } }

        """;

    private const string Script = """
        (function () {
          var filter = document.getElementById('filter'), only = document.getElementById('dgonly');
          function apply() {
            var text = filter.value.toLowerCase(), diagnostics = only.checked;
            document.querySelectorAll('.ln').forEach(function (line) {
              var shown = (text === '' || line.textContent.toLowerCase().indexOf(text) !== -1)
                && (!diagnostics || line.classList.contains('hasdg'));
              line.classList.toggle('filtered', !shown);
            });
          }
          filter.addEventListener('input', apply);
          only.addEventListener('change', apply);
          function every(open) { document.querySelectorAll('details').forEach(function (block) { block.open = open; }); }
          document.getElementById('expand').addEventListener('click', function () { every(true); });
          document.getElementById('collapse').addEventListener('click', function () { every(false); });
          function reveal() {
            var at = location.hash ? document.getElementById(decodeURIComponent(location.hash.slice(1))) : null;
            for (; at; at = at.parentElement) if (at.tagName === 'DETAILS') at.open = true;
          }
          window.addEventListener('hashchange', reveal);
          // The sticky nav covers the top of the viewport, and it wraps to more lines as the window narrows: every link
          // target (a section heading, a line, a data item) scrolls to just below the nav's height as rendered now.
          var nav = document.querySelector('nav');
          function clear() { document.documentElement.style.setProperty('--navh', (nav.offsetHeight + 8) + 'px'); }
          if (window.ResizeObserver) new ResizeObserver(clear).observe(nav);
          clear();
          reveal();
          if (location.hash) { var at = document.getElementById(decodeURIComponent(location.hash.slice(1))); if (at) at.scrollIntoView(); }
        })();
        """;
}
