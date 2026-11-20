// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using WiseOwl.Cobol.Frontend.Common;
using WiseOwl.Cobol.Frontend.Preprocessor;

namespace WiseOwl.Cobol.Listing.Building;

/// <summary>
/// ⛔ THE ONE CONSTRUCTOR OF A <see cref="ListingDocument"/> (DESIGN-listing.md section 4.6). It reads only the collector
/// (<see cref="ListingFacts"/>) and places every position through the one index (<see cref="ListingLineIndex"/>); each part
/// has its own builder (<see cref="SourceSectionBuilder"/>, <see cref="ProgramRangeBuilder"/>, <see cref="DirectiveEffectsBuilder"/>,
/// <see cref="ListingVisibility"/>, <see cref="DiagnosticSectionBuilder"/>, <see cref="StatisticsSectionBuilder"/>,
/// <see cref="DataMapBuilder"/>, <see cref="ObjectCodeMapBuilder"/>; the options
/// rows come built, from the composition root's <c>OptionsSectionBuilder</c>).
/// <para>The request's sections (<see cref="ListingRequest.Sections"/>) decide what the document HOLDS, never what is computed
/// for the sections that are held: the lines are placed, decided and counted whether or not the source section is asked for,
/// since the program ranges, the diagnostics and the statistics stand on them. One request, one document (DESIGN-listing.md
/// section 13 item 2), so every format holds the same sections.</para>
/// </summary>
internal static class ListingBuilder
{
    /// <summary>Build the listing of a compilation that read its source (<see cref="ListingFacts.Frontend"/> is set) and
    /// reached <paramref name="outcome"/>.</summary>
    public static ListingDocument Build(ListingFacts facts, ListingOutcome outcome)
    {
        var frontend = facts.Frontend ?? throw new InvalidOperationException("no listing without source text (DESIGN-listing.md section 4.6)");
        // The compilation's one instant (DESIGN-listing.md section 5): the emitter's WHEN-COMPILED, if it ran, captured it first.
        var instant = facts.CompilationTime();

        var inclusions = frontend.Trace.Inclusions;
        var fileIds = new Dictionary<string, int>(StringComparer.Ordinal) { [frontend.SourcePath] = 1 };
        foreach (var inclusion in inclusions)
            if (inclusion.File is { } file) fileIds.TryAdd(file, fileIds.Count + 1);
        var files = fileIds.Select(f => new ListingFile(f.Value, f.Key, f.Value == 1 ? ListingFileRole.Source : ListingFileRole.Library,
            facts.Inputs.FileDigests.TryGetValue(Path.GetFullPath(f.Key), out var digest) ? digest
                : throw new InvalidOperationException($"the compilation recorded no digest of '{f.Key}', which it read"))).ToList();
        // Each listed file by its full path: what a PDB's document name and a backend diagnostic's file resolve to
        // (SourceDocuments.PathOf; DESIGN-listing.md section 6.9). Two names of one file are one file, listed by the first.
        var filesByFullPath = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string file in fileIds.Keys) filesByFullPath.TryAdd(Path.GetFullPath(file), file);

        var drafts = SourceSectionBuilder.Build(frontend, inclusions, fileIds);
        var index = new ListingLineIndex(drafts, inclusions, frontend.SourcePath, frontend.LineMap);
        for (int r = 1; r <= index.ResultantLines; r++)
            if (index.IsOmitted(r) && index.Of(r) is { } omitted) drafts[omitted - 1].Omitted = true;

        var units = frontend.Units;
        var programs = new List<ListingProgram>();
        if (units is not null)
        {
            SourceSectionBuilder.SettleCandidates(drafts,
                units.CommentEntryParagraphs.Select(p => index.Of(p.Line)).OfType<int>().ToHashSet());
            SourceSectionBuilder.ReplaceDateCompiled(drafts, DateCompiledWords(units, index, frontend), instant);
            programs = ProgramRangeBuilder.Build(units, index, drafts);
        }
        else SourceSectionBuilder.SettleCandidates(drafts, new HashSet<int>());
        DirectiveEffectsBuilder.MarkReplaced(frontend.Trace, index, drafts, frontend.ResultantText);
        // What the compiler read each stretch of a line as (section 7.2), for the source section only: no other part reads it.
        if (facts.Request.Sections.Contains(ListingSection.Source))
            SyntaxSpanBuilder.Mark(drafts, frontend, index, fileIds.ToDictionary(f => f.Value, f => f.Key));
        ListingVisibility.Apply(drafts, programs.Where(p => p.Depth == 0).Select(p => p.FirstLine),
            DirectiveEffectsBuilder.Build(frontend.Trace, inclusions, index, drafts));
        var displays = DirectiveEffectsBuilder.Displays(frontend.Trace, index);
        var diagnostics = DiagnosticSectionBuilder.Build(facts.Diagnostics, frontend, index, fileIds, filesByFullPath, drafts);
        var wanted = facts.Request.Sections;
        // The symbol table's views (DESIGN-listing.md sections 6.5 and 6.6), when the bind finished it.
        var dataMap = wanted.Contains(ListingSection.DataMap) && facts.Symbols is { } symbols
            ? DataMapBuilder.Build(symbols, index, frontend, drafts) : null;
        if (dataMap is not null) LineAnnotationBuilder.MarkMargins(dataMap, drafts);
        if (facts.Symbols is { } table)
        {
            var attributes = DataMapBuilder.ProgramAttributesOf(table, index, frontend, drafts);
            programs = [.. programs.Select(p => attributes.TryGetValue(p.Id, out var a) ? p with { Attributes = a } : p)];
        }
        // The object-code map (DESIGN-listing.md section 6.9), read from the assembly and its PDB only when a section asks for it.
        var objectCode = ListingFacts.ReadsObjectCode(facts.Request) && facts.ObjectCode is (var assembly, string pdb)
            ? ObjectCodeMapBuilder.Build(ObjectCodeMapReader.Read(assembly, pdb, wanted.Contains(ListingSection.ObjectCode),
                    SourceDocuments.RootOf(frontend.SourcePath)), assembly,
                index, frontend, filesByFullPath, inclusions)
            : null;
        if (wanted.Contains(ListingSection.Diagnostics))
            foreach (var diagnostic in diagnostics)
                if (diagnostic.Line is { } placed) drafts[placed - 1].AddDiagnostic(diagnostic.Id);
        var copyLines = inclusions.Select(i => new ListingInclusion(i.Id, i.Parent, i.Depth,
            CopyLine(index, i, i.CopyFirst.Line), CopyLine(index, i, i.CopyLast.Line), i.TextName,
            i.LibraryName, i.File is { } f ? fileIds[f] : null, i.Outcome switch
            {
                TextInclusionOutcome.Found => ListingInclusionOutcome.Found,
                TextInclusionOutcome.Circular => ListingInclusionOutcome.Circular,
                _ => ListingInclusionOutcome.NotFound,
            }, i.Suppress)).ToList();
        // The cross-reference (DESIGN-listing.md section 6.7), a view of the finished table's occurrences, and its joins onto the lines.
        var crossReference = wanted.Contains(ListingSection.CrossReference) && facts.Symbols is { } referenced
            ? CrossReferenceBuilder.Build(referenced, copyLines, inclusions.ToDictionary(static i => i.Id, static i => (i.MemberName, i.MemberFold)),
                index, frontend, drafts,
                dataMap is null ? null : DataMapBuilder.ItemIds(referenced))
            : null;
        if (crossReference is not null) CrossReferenceBuilder.MarkLines(crossReference, drafts);
        List<ListingLine> lines = [.. drafts.Select(d => d.ToLine())];

        var edition = facts.Edition;
        var header = new ListingHeader(CompilerVersion, edition.Year, edition.StandardName, instant, frontend.SourcePath, outcome,
            ParseOf(units));
        return new ListingDocument(header, files, copyLines,
            wanted.Contains(ListingSection.Source) ? lines : [],
            programs,
            wanted.Contains(ListingSection.Diagnostics) ? diagnostics : [],
            wanted.Contains(ListingSection.Diagnostics) ? DiagnosticSectionBuilder.FlaggedConstructs(diagnostics) : [],
            wanted.Contains(ListingSection.Source) ? displays : [],
            wanted.Contains(ListingSection.Options) ? facts.Options : [],
            wanted.Contains(ListingSection.Statistics)
                ? StatisticsSectionBuilder.Build(lines, files, diagnostics, programs, units, index) : null,
            dataMap,
            crossReference,
            objectCode,
            [.. wanted.Each().Select(section => StatusOf(section, units, facts.Symbols, facts.ObjectCode, outcome))]);
    }

    /// <summary>Whether <paramref name="section"/> could be produced (DESIGN-listing.md section 4.3): the source, diagnostics,
    /// options and statistics stand on the source text, which a listing always has; the program map stands on the parse, and the
    /// data map on the finished symbol table, which only a bind that accepted the program makes; the object-code map and the object
    /// code on the assembly and its PDB, which only a compilation that wrote them has.</summary>
    private static ListingSectionStatus StatusOf(ListingSection section, Frontend.SourceUnitFacts? units,
        Symbols.SymbolTableSnapshot? symbols, (string Assembly, string? Pdb)? objectCode, ListingOutcome outcome) => section switch
    {
        ListingSection.ObjectMap or ListingSection.ObjectCode when objectCode is null => new(section, false, outcome switch
        {
            ListingOutcome.FrontendError => "the source was not parsed",
            ListingOutcome.BindError => "the bind refused the program",
            ListingOutcome.BackendError => "the backend produced no assembly",
            ListingOutcome.OutputNotWritable => "the assembly was not written",
            _ => "check-only compile: no code generated",
        }),
        ListingSection.ObjectMap or ListingSection.ObjectCode when objectCode is { Pdb: null } =>
            new(section, false, "the backend wrote no PDB"),
        ListingSection.ObjectMap or ListingSection.ObjectCode => new(section, true, null),
        ListingSection.Programs when units is null => new(section, false, "the source was not parsed"),
        ListingSection.DataMap or ListingSection.CrossReference when symbols is null => new(section, false, outcome switch
        {
            ListingOutcome.FrontendError => "the source was not parsed",
            _ => "the bind refused the program",
        }),
        ListingSection.DataMap or ListingSection.CrossReference => new(section, true, null),
        ListingSection.Source or ListingSection.Programs or ListingSection.Diagnostics or ListingSection.Options
            or ListingSection.Statistics => new(section, true, null),
        _ => throw new ArgumentOutOfRangeException(nameof(section), section, "a listing section with no builder"),
    };

    /// <summary>The listing line of physical <paramref name="fileLine"/> of the text that holds the COPY statement of
    /// <paramref name="inclusion"/> (its parent inclusion, or the source).</summary>
    private static int CopyLine(ListingLineIndex index, TextInclusion inclusion, int fileLine) =>
        index.Of(inclusion.Parent, fileLine)
            ?? throw new InvalidOperationException($"COPY {inclusion.TextName} stands on line {fileLine}, which the listing does not hold");

    /// <summary>The listing line and the index into its text of each DATE-COMPILED paragraph word: the word's resultant
    /// position, placed through the index, and its physical column converted to an index of the expanded text.</summary>
    private static IEnumerable<(int Line, int Index)> DateCompiledWords(Frontend.SourceUnitFacts units, ListingLineIndex index,
        FrontendFacts frontend)
    {
        foreach (var word in units.DateCompiled)
            if (index.Of(word.Line) is { } line && index.PointAt(word.Line, word.Column) is { } point
                && frontend.Formats.TryGetValue(point.Origin.File, out var map))
                yield return (line, map.ExpandedIndexAt(point.Origin.Line, point.Column));
    }

    /// <summary>The compiler's version as the listing spells it (<see cref="VersionOf"/>), which the page heading and the
    /// options section print.</summary>
    internal static readonly string CompilerVersion = VersionOf(typeof(ListingBuilder).Assembly);

    /// <summary>⛔ THE ONE SPELLING OF A VERSION IN THE LISTING: <paramref name="assembly"/>'s informational version, else its
    /// assembly version. The page heading's compiler and the options section's compiler and runtime rows all read it, so the rows
    /// of one listing spell their versions alike (kb/Work PB5925 review R1-2).</summary>
    internal static string VersionOf(Assembly assembly) =>
        assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? assembly.GetName().Version?.ToString()
        ?? throw new InvalidOperationException($"the assembly {assembly.GetName().Name} carries no version");

    /// <summary>What the parse made of the source: the front end's own verdict, carried on <see cref="Frontend.SourceUnitFacts"/>.</summary>
    private static ListingParse ParseOf(Frontend.SourceUnitFacts? units) =>
        units is null ? ListingParse.NotParsed : units.Recovered ? ListingParse.Recovered : ListingParse.Parsed;
}
