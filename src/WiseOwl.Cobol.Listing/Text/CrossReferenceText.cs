// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using System.Text;

namespace WiseOwl.Cobol.Listing.Text;

/// <summary>
/// The words and rows of the cross-reference (DESIGN-listing.md section 6.7), read by the text listing and the HTML page alike: one
/// statement of each, so the two renderings cannot disagree.
/// </summary>
public static class CrossReferenceText
{
    /// <summary>The symbol table's headings: the symbol's id, its kind, its name, the line it is defined on, then its references.</summary>
    public static IReadOnlyList<string> SymbolHeadings { get; } = ["ID", "KIND", "NAME", "DEFINED", "REFERENCES"];

    /// <summary>The graphs' headings: the edge's source, the line of the reference, the verb and the target.</summary>
    public static IReadOnlyList<string> EdgeHeadings { get; } = ["FROM", "LINE", "VERB", "TO"];

    /// <summary>The PERFORM graph's title.</summary>
    public const string PerformsTitle = "PERFORM GRAPH";

    /// <summary>The CALL graph's title.</summary>
    public const string CallsTitle = "CALL GRAPH";

    /// <summary>The flag of a symbol defined and never referenced.</summary>
    public const string UnreferencedFlag = "UNREFERENCED";

    /// <summary>The one-letter mark of a use, as a reference's text prints it after its line: <c>D</c> defined, nothing for a read,
    /// <c>W</c> written, <c>M</c> modified.</summary>
    public static string UseMark(ListingUse use) => use switch
    {
        ListingUse.Defined => "D",
        ListingUse.Read => "",
        ListingUse.Written => "W",
        ListingUse.Modified => "M",
        _ => throw new ArgumentOutOfRangeException(nameof(use), use, "not a use"),
    };

    /// <summary>The word of a symbol's kind.</summary>
    public static string KindWord(ListingSymbolKind kind) => kind switch
    {
        ListingSymbolKind.Program => "PROGRAM",
        ListingSymbolKind.Data => "DATA",
        ListingSymbolKind.Condition => "COND",
        ListingSymbolKind.Index => "INDEX",
        ListingSymbolKind.Constant => "CONST",
        ListingSymbolKind.File => "FILE",
        ListingSymbolKind.Report => "REPORT",
        ListingSymbolKind.CommunicationDescription => "CD",
        ListingSymbolKind.Procedure => "PROC",
        ListingSymbolKind.SwitchCondition => "SWITCH",
        ListingSymbolKind.ClassName => "CLASS",
        ListingSymbolKind.Alphabet => "ALPHABET",
        ListingSymbolKind.NationalAlphabet => "ALPHABET-N",
        ListingSymbolKind.SymbolicCharacter => "SYMBOLIC",
        ListingSymbolKind.OrderingName => "ORDERING",
        ListingSymbolKind.DynamicLengthStructure => "DYN-LENGTH",
        ListingSymbolKind.Locale => "LOCALE",
        ListingSymbolKind.Mnemonic => "MNEMONIC",
        ListingSymbolKind.ReportGroup => "RPT-GROUP",
        ListingSymbolKind.SumCounter => "SUM",
        ListingSymbolKind.ReportVarying => "VARYING",
        ListingSymbolKind.ReportEntry => "RPT-ENTRY",
        ListingSymbolKind.ReportSumOperand => "SUM-OPND",
        ListingSymbolKind.ProgramPrototype => "PROG-PROTO",
        ListingSymbolKind.ExternalProgram => "EXTERNAL",
        ListingSymbolKind.FunctionPrototype => "FUNC-PROTO",
        ListingSymbolKind.DebugRegister => "DEBUG-REG",
        ListingSymbolKind.TypeName => "TYPE",
        ListingSymbolKind.CapacityRegister => "CAPACITY",
        ListingSymbolKind.ObjectClass => "OBJ-CLASS",
        ListingSymbolKind.Interface => "INTERFACE",
        ListingSymbolKind.ParameterName => "PARAMETER",
        ListingSymbolKind.CopyMember => "COPY",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "not a symbol kind"),
    };

    /// <summary>A reference as a symbol's list prints it, appended to <paramref name="row"/> with no string of its own: its listing line
    /// (<c>-</c> when no listed line holds it) and its use's mark.</summary>
    public static void AppendReference(StringBuilder row, ListingReference reference)
    {
        if (reference.Line is { } line) row.Append(CultureInfo.InvariantCulture, $"{line}");
        else row.Append(DataMapText.NumberText(null));
        row.Append(UseMark(reference.Use));
    }

    /// <summary>The line a symbol is defined on, as its row prints it.</summary>
    public static string DefinedText(ListingSymbol symbol) => Definition(symbol) is { } defined ? DataMapText.NumberText(defined.Line) : "-";

    /// <summary>The reference that is the symbol's definition, or null for a name the standard declares implicitly.</summary>
    public static ListingReference? Definition(ListingSymbol symbol)
    {
        for (int i = 0; i < symbol.References.Count; i++)
            if (symbol.References[i].Use == ListingUse.Defined) return symbol.References[i];
        return null;
    }

    /// <summary>The listing line a symbol is defined on, or null when it has no definition or no listed line holds it.</summary>
    public static int? DefinedLine(ListingSymbol symbol) => Definition(symbol)?.Line;

    /// <summary>The cross-reference's rows: a heading, then one row per symbol (its id, kind, name, definition line, then each
    /// reference's line and use mark, wrapped under the references column, and <see cref="UnreferencedFlag"/> for a name never
    /// referenced); then the PERFORM graph and the CALL graph, an edge per row.</summary>
    public static IEnumerable<string> Rows(ListingCrossReference xref, int width)
    {
        var h = SymbolHeadings;
        yield return string.Create(CultureInfo.InvariantCulture, $"{h[0],6}  {h[1],-10} {h[2],-30} {h[3],7}  {h[4]}");
        var text = new StringBuilder();   // one builder for every symbol: each row is made in it and cut as a string once
        foreach (var symbol in xref.Symbols)
        {
            text.Clear();
            text.Append(CultureInfo.InvariantCulture, $"{symbol.Id,6}  {KindWord(symbol.Kind),-10} {symbol.Name,-30} {DefinedText(symbol),7} ");
            for (int i = 0; i < symbol.References.Count; i++)
                if (symbol.References[i].Use != ListingUse.Defined) AppendReference(text.Append(' '), symbol.References[i]);
            if (symbol.Unreferenced) text.Append("  ").Append(UnreferencedFlag);
            while (text.Length > 0 && text[^1] == ' ') text.Length--;
            foreach (string piece in TextListingRenderer.Indented(text.ToString(), 0, 57, width)) yield return piece;
        }
        foreach (var row in Graph(PerformsTitle, xref.Performs, xref, width)) yield return row;
        foreach (var row in Graph(CallsTitle, xref.Calls, xref, width)) yield return row;
    }

    /// <summary>The name of the symbol an edge's end names, or <c>-</c>.</summary>
    public static string EndText(int? symbol, ListingCrossReference xref) =>
        symbol is { } id && id >= 1 && id <= xref.Symbols.Count ? xref.Symbols[id - 1].Name : "-";

    private static IEnumerable<string> Graph(string title, IReadOnlyList<ListingEdge> edges, ListingCrossReference xref, int width)
    {
        yield return "";
        yield return title;
        var h = EdgeHeadings;
        yield return string.Create(CultureInfo.InvariantCulture, $"{h[0],-30} {h[1],6}  {h[2],-8} {h[3]}");
        foreach (var edge in edges)
            foreach (string piece in TextListingRenderer.Indented(string.Create(CultureInfo.InvariantCulture,
                $"{EndText(edge.From, xref),-30} {DataMapText.NumberText(edge.Line),6}  {edge.Verb,-8} {EndText(edge.To, xref)}"), 0, 48, width))
                yield return piece;
    }
}
