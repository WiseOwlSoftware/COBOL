// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Symbols;

namespace WiseOwl.Cobol.Listing.Building;

/// <summary>
/// ⛔ THE CROSS-REFERENCE (docs/rearchitecture/DESIGN-listing.md sections 6.7 and 6.7.1): a VIEW of the finished symbol table's entries
/// and the occurrences the bind recorded of them, each placed on its listing line through the one index, and of the inclusions (the
/// copy members). It derives no reference: every position and use is the table's. The PERFORM and CALL graphs are views of the
/// procedure and program references. The joins onto the source lines (each line's names, a statement line's margin) are made here
/// once (<see cref="MarkLines"/>), so no renderer joins two sections.
/// </summary>
internal static class CrossReferenceBuilder
{
    /// <summary>The cross-reference of <paramref name="symbols"/> and <paramref name="inclusions"/>; <paramref name="dataItems"/> is the
    /// data map's numbering when the listing holds the data map (each symbol it lists links to its entry), else null.</summary>
    public static ListingCrossReference Build(SymbolTableSnapshot symbols, IReadOnlyList<ListingInclusion> inclusions,
        ListingLineIndex index, FrontendFacts frontend, IReadOnlyList<LineDraft> drafts, IReadOnlyDictionary<int, int>? dataItems)
    {
        var drafted = new List<(string Fold, int? DefinedLine, string Name, ListingSymbolKind Kind, int? Program, int? DataItem,
            List<ListingReference> References, bool Unreferenced)>();
        foreach (var entry in symbols.Entries)
        {
            // A FILLER entry has no name to cross-reference (no reference can name it).
            if (entry.Class == NameClass.Data && entry.Occurrences.Count == 0 && IsFiller(entry.Name)) continue;
            var references = new List<ListingReference>();
            var defined = DataMapBuilder.Place(entry.DeclaredAt, index, frontend, drafts);
            if (entry.DeclaredAt.IsSet) references.Add(new ListingReference(0, defined.Line, defined.Column, entry.Name.Length, ListingUse.Defined, null));
            foreach (var occurrence in entry.Occurrences)
            {
                var at = DataMapBuilder.Place(occurrence.At, index, frontend, drafts);
                references.Add(new ListingReference(0, at.Line, at.Column, occurrence.Length, UseOf(occurrence.Use), occurrence.Verb));
            }
            references.Sort(static (a, b) => (a.Line ?? int.MaxValue, a.Column ?? 0).CompareTo((b.Line ?? int.MaxValue, b.Column ?? 0)));
            drafted.Add((entry.Fold, defined.Line, entry.Name, KindOf(entry.Class), defined.Program,
                dataItems is not null && dataItems.TryGetValue(entry.Id, out int item) ? item : null, references,
                entry.DeclaredAt.IsSet && entry.Occurrences.Count == 0));
        }
        // The copy members: a library text each COPY statement names (DESIGN-listing.md section 6.7), its references the COPY lines.
        foreach (var member in inclusions.GroupBy(static i => (Fold: i.TextName.ToUpperInvariant(), Library: i.LibraryName?.ToUpperInvariant())))
        {
            var first = member.First();
            string name = first.LibraryName is { } library ? first.TextName + " OF " + library : first.TextName;
            drafted.Add((member.Key.Fold, null, name, ListingSymbolKind.CopyMember, null, null,
                [.. member.Select(static i => new ListingReference(0, i.CopyLine, null, 0, ListingUse.Read, "COPY"))], false));
        }
        var ordered = drafted.OrderBy(static d => d.Fold, StringComparer.Ordinal).ThenBy(static d => d.DefinedLine ?? int.MaxValue).ToList();
        var listed = new List<ListingSymbol>(ordered.Count);
        foreach (var d in ordered)
        {
            int id = listed.Count + 1;
            listed.Add(new ListingSymbol(id, d.Name, d.Kind, d.Program, d.DataItem,
                [.. d.References.Select(r => r with { Symbol = id })], d.Unreferenced));
        }
        return new ListingCrossReference(listed,
            Performs(listed, ProcedureAt(listed), line => drafts[line - 1].Program), Calls(listed, drafts));
    }

    /// <summary>The one map from the symbol table's class to the listing's kind, total both ways (<c>CrossReferenceKindDriftTests</c>).</summary>
    internal static ListingSymbolKind KindOf(NameClass @class) => @class switch
    {
        NameClass.Program => ListingSymbolKind.Program,
        NameClass.Data => ListingSymbolKind.Data,
        NameClass.Condition => ListingSymbolKind.Condition,
        NameClass.Index => ListingSymbolKind.Index,
        NameClass.Constant => ListingSymbolKind.Constant,
        NameClass.File => ListingSymbolKind.File,
        NameClass.Report => ListingSymbolKind.Report,
        NameClass.CommunicationDescription => ListingSymbolKind.CommunicationDescription,
        NameClass.Procedure => ListingSymbolKind.Procedure,
        NameClass.SwitchCondition => ListingSymbolKind.SwitchCondition,
        NameClass.ClassName => ListingSymbolKind.ClassName,
        NameClass.Alphabet => ListingSymbolKind.Alphabet,
        NameClass.NationalAlphabet => ListingSymbolKind.NationalAlphabet,
        NameClass.SymbolicCharacter => ListingSymbolKind.SymbolicCharacter,
        NameClass.OrderingName => ListingSymbolKind.OrderingName,
        NameClass.DynamicLengthStructure => ListingSymbolKind.DynamicLengthStructure,
        NameClass.Locale => ListingSymbolKind.Locale,
        NameClass.Mnemonic => ListingSymbolKind.Mnemonic,
        NameClass.ReportGroup => ListingSymbolKind.ReportGroup,
        NameClass.SumCounter => ListingSymbolKind.SumCounter,
        NameClass.ReportVarying => ListingSymbolKind.ReportVarying,
        NameClass.ReportEntry => ListingSymbolKind.ReportEntry,
        NameClass.ReportSumOperand => ListingSymbolKind.ReportSumOperand,
        NameClass.ProgramPrototype => ListingSymbolKind.ProgramPrototype,
        NameClass.FunctionPrototype => ListingSymbolKind.FunctionPrototype,
        NameClass.DebugRegister => ListingSymbolKind.DebugRegister,
        NameClass.TypeName => ListingSymbolKind.TypeName,
        NameClass.CapacityRegister => ListingSymbolKind.CapacityRegister,
        NameClass.ObjectClass => ListingSymbolKind.ObjectClass,
        NameClass.Interface => ListingSymbolKind.Interface,
        NameClass.ParameterName => ListingSymbolKind.ParameterName,
        _ => throw new ArgumentOutOfRangeException(nameof(@class), @class, "a name class with no cross-reference kind"),
    };

    /// <summary>The one map from the symbol table's use to the listing's.</summary>
    internal static ListingUse UseOf(SymbolUse use) => use switch
    {
        SymbolUse.Defined => ListingUse.Defined,
        SymbolUse.Read => ListingUse.Read,
        SymbolUse.Written => ListingUse.Written,
        SymbolUse.Modified => ListingUse.Modified,
        _ => throw new ArgumentOutOfRangeException(nameof(use), use, "not a use"),
    };

    private static bool IsFiller(string name) => string.Equals(name, "FILLER", StringComparison.OrdinalIgnoreCase);

    /// <summary>The procedure a listing line stands in: the procedure defined last at or before it in the same program.</summary>
    private static Func<int?, int?, int?> ProcedureAt(IReadOnlyList<ListingSymbol> symbols)
    {
        var procedures = symbols.Where(static s => s.Kind == ListingSymbolKind.Procedure)
            .Select(static s => (s.Id, s.Program, Line: s.References.FirstOrDefault(static r => r.Use == ListingUse.Defined)?.Line))
            .Where(static p => p.Line is not null).OrderBy(static p => p.Line).ToList();
        return (line, program) =>
        {
            int? found = null;
            foreach (var p in procedures)
            {
                if (p.Line > line) break;
                if (p.Program == program) found = p.Id;
            }
            return found;
        };
    }

    /// <summary>The PERFORM graph: an edge for each reference of a procedure-name written in a PERFORM statement, from the procedure
    /// <paramref name="procedureAt"/> answers for the reference's line in the program <paramref name="programOf"/> answers for it, in
    /// listing order.</summary>
    private static List<ListingEdge> Performs(IReadOnlyList<ListingSymbol> symbols, Func<int?, int?, int?> procedureAt,
        Func<int, int?> programOf)
    {
        var edges = new List<ListingEdge>();
        foreach (var symbol in symbols.Where(static s => s.Kind == ListingSymbolKind.Procedure))
            foreach (var reference in symbol.References)
                if (reference is { Verb: "PERFORM", Line: { } line })
                    edges.Add(new ListingEdge(procedureAt(line, programOf(line)), symbol.Id, line, "PERFORM"));
        edges.Sort(static (a, b) => (a.Line ?? int.MaxValue).CompareTo(b.Line ?? int.MaxValue));
        return edges;
    }

    /// <summary>The CALL graph: each reference to a program, program-prototype or function-prototype name written in a CALL or
    /// CANCEL statement or activating a function, from the source unit the line stands in.</summary>
    private static List<ListingEdge> Calls(IReadOnlyList<ListingSymbol> symbols, IReadOnlyList<LineDraft> drafts)
    {
        var unitSymbol = new Dictionary<int, int>();
        foreach (var s in symbols)
            if (s.Kind == ListingSymbolKind.Program && s.Program is { } program) unitSymbol.TryAdd(program, s.Id);
        var edges = new List<ListingEdge>();
        foreach (var symbol in symbols)
            if (symbol.Kind is ListingSymbolKind.Program or ListingSymbolKind.ProgramPrototype or ListingSymbolKind.FunctionPrototype)
                foreach (var reference in symbol.References)
                    if (reference is { Use: not ListingUse.Defined, Line: { } line, Verb: { } verb }
                        && (symbol.Kind == ListingSymbolKind.FunctionPrototype || verb is "CALL" or "CANCEL"))
                        edges.Add(new ListingEdge(drafts[line - 1].Program is { } p && unitSymbol.TryGetValue(p, out int unit) ? unit : null,
                            symbol.Id, line, verb));
        edges.Sort(static (a, b) => (a.Line ?? int.MaxValue).CompareTo(b.Line ?? int.MaxValue));
        return edges;
    }

    /// <summary>
    /// ⛔ THE JOINS OF THE CROSS-REFERENCE ONTO THE SOURCE LINES (DESIGN-listing.md section 4.2): each line's names
    /// (<see cref="ListingLine.Names"/>, every reference placed on it, in column order) and, on a line that declares no data item and
    /// writes references, its margin: the listing lines that define the names it uses (section 6.2).
    /// </summary>
    public static void MarkLines(ListingCrossReference xref, IReadOnlyList<LineDraft> drafts)
    {
        foreach (var symbol in xref.Symbols)
            foreach (var reference in symbol.References)
                if (reference is { Line: { } line, Column: { } column } && symbol.Kind != ListingSymbolKind.CopyMember)
                    drafts[line - 1].AddName(new ListingNameUse(column, reference.Length, symbol.Id, reference.Use));
        var definedOn = xref.Symbols.ToDictionary(static s => s.Id,
            static s => s.References.FirstOrDefault(static r => r.Use == ListingUse.Defined)?.Line);
        foreach (var draft in drafts)
        {
            if (draft.Names is not { Count: > 0 } names) continue;
            names.Sort(static (a, b) => a.Column.CompareTo(b.Column));
            if (draft.Margin is not null) continue;
            var definitions = new List<int>();
            foreach (var name in names)
                if (name.Use != ListingUse.Defined && definedOn[name.Symbol] is { } at && at != draft.Number && !definitions.Contains(at))
                    definitions.Add(at);
            if (definitions.Count > 0) draft.Margin = new ListingMargin(null, null, null, null, definitions);
        }
    }
}
