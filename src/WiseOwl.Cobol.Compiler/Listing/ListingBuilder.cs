// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using WiseOwl.Cobol.Frontend.Preprocessor;

namespace WiseOwl.Cobol.Listing.Building;

/// <summary>
/// ⛔ THE ONE CONSTRUCTOR OF A <see cref="ListingDocument"/> (DESIGN-listing.md section 4.6). It reads only the collector
/// (<see cref="ListingFacts"/>) and places every position through the one index (<see cref="ListingLineIndex"/>); each part
/// has its own builder (<see cref="SourceSectionBuilder"/>, <see cref="ProgramRangeBuilder"/>, <see cref="DirectiveEffectsBuilder"/>,
/// <see cref="ListingVisibility"/>, <see cref="DiagnosticSectionBuilder"/>, <see cref="StatisticsSectionBuilder"/>; the options
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
        ListingVisibility.Apply(drafts, programs.Where(p => p.Depth == 0).Select(p => p.FirstLine),
            DirectiveEffectsBuilder.Build(frontend.Trace, inclusions, index, drafts));
        var displays = DirectiveEffectsBuilder.Displays(frontend.Trace, index);
        var diagnostics = DiagnosticSectionBuilder.Build(facts.Diagnostics, frontend, index, fileIds, drafts);
        var wanted = facts.Request.Sections;
        if (wanted.Contains(ListingSection.Diagnostics))
            foreach (var diagnostic in diagnostics)
                if (diagnostic.Line is { } placed) drafts[placed - 1].AddDiagnostic(diagnostic.Id);
        List<ListingLine> lines = [.. drafts.Select(d => d.ToLine())];

        var edition = facts.Edition;
        var header = new ListingHeader(CompilerVersion, edition.Year, edition.StandardName, instant, frontend.SourcePath, outcome);
        var copyLines = inclusions.Select(i => new ListingInclusion(i.Id, i.Parent, i.Depth,
            CopyLine(index, i, i.CopyFirst.Line), CopyLine(index, i, i.CopyLast.Line), i.TextName,
            i.LibraryName, i.File is { } f ? fileIds[f] : null, i.Outcome switch
            {
                TextInclusionOutcome.Found => ListingInclusionOutcome.Found,
                TextInclusionOutcome.Circular => ListingInclusionOutcome.Circular,
                _ => ListingInclusionOutcome.NotFound,
            }, i.Suppress)).ToList();
        return new ListingDocument(header, files, copyLines,
            wanted.Contains(ListingSection.Source) ? lines : [],
            programs,
            wanted.Contains(ListingSection.Diagnostics) ? diagnostics : [],
            wanted.Contains(ListingSection.Source) ? displays : [],
            wanted.Contains(ListingSection.Options) ? facts.Options : [],
            wanted.Contains(ListingSection.Statistics)
                ? StatisticsSectionBuilder.Build(lines, files, diagnostics, programs, units, index) : null,
            [.. wanted.Each().Select(section => StatusOf(section, units))]);
    }

    /// <summary>Whether <paramref name="section"/> could be produced (DESIGN-listing.md section 4.3): every section of this step
    /// stands on the source text, which a listing always has, but the program map stands on the parse.</summary>
    private static ListingSectionStatus StatusOf(ListingSection section, Frontend.SourceUnitFacts? units) => section switch
    {
        ListingSection.Programs when units is null => new(section, false, "the source was not parsed"),
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

    /// <summary>The compiler's informational version, which the page heading and the options section print.</summary>
    internal static readonly string CompilerVersion =
        typeof(ListingBuilder).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(ListingBuilder).Assembly.GetName().Version?.ToString()
        ?? throw new InvalidOperationException("the compiler assembly carries no version");
}
