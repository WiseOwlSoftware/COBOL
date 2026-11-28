// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Listing;

namespace WiseOwl.Cobol.Compiler.Listing;

/// <summary>What the text stages' directive hooks mean for the listing's lines (DESIGN-listing.md sections 4.4 and 4.5), each
/// in LISTING LINE numbers: the facts <see cref="ListingVisibility"/> decides visibility and page breaks from.</summary>
/// <param name="States">The LISTING directive's state from each listing line on (inclusive), in listing order: the state before
/// the first is ON (§2023:7.3.18.3 GR2).</param>
/// <param name="DirectiveLines">The LISTING directive lines, each listed whatever the state (§2023:7.3.18.3 GR3).</param>
/// <param name="PageLines">The PAGE directive lines processed (§2023:7.3.19.4 GR2).</param>
/// <param name="Suppressed">The lines of library text a COPY statement with the SUPPRESS phrase incorporated, directly or
/// through a COPY inside it (§2023:7.2.3.4 GR4).</param>
internal sealed record DirectiveEffects(IReadOnlyList<(int From, bool On)> States, IReadOnlySet<int> DirectiveLines,
    IReadOnlySet<int> PageLines, IReadOnlySet<int> Suppressed)
{
    /// <summary>No directive hook acted: every line is listed, at <c>--std 85</c> always (no directive exists there).</summary>
    public static readonly DirectiveEffects None = new([], new HashSet<int>(), new HashSet<int>(), new HashSet<int>());
}

/// <summary>
/// ⛔ THE ONE PLACEMENT OF THE TEXT STAGES' DIRECTIVE HOOKS (DESIGN-listing.md section 4.4): each fact the trace recorded where a
/// stage acted becomes a listing line through the one index (<see cref="ListingLineIndex"/>). It decides nothing: the LISTING
/// state at every change is the conditional-compilation driver's (the one holder of the state, which a PUSH saves, a POP
/// restores and the end of a library text restores, §2023:7.2.3.4 GR5), and this replays it. A recorded fact no listing line
/// holds is a defect of the trace, so it fails loudly.
/// </summary>
internal static class DirectiveEffectsBuilder
{
    /// <summary>The effects of <paramref name="trace"/>'s LISTING, PAGE and SUPPRESS facts on <paramref name="drafts"/>.</summary>
    public static DirectiveEffects Build(TextStageTrace trace, IReadOnlyList<TextInclusion> inclusions, ListingLineIndex index,
        IReadOnlyList<LineDraft> drafts)
    {
        var parents = inclusions.ToDictionary(i => i.Id, i => i.Parent);
        // The last listing line of each inclusion's block, its nested inclusions' lines included.
        var lastLine = new Dictionary<int, int>();
        var suppressedInclusions = inclusions.Where(i => i.Suppress).Select(i => i.Id).ToHashSet();
        var suppressed = new HashSet<int>();
        foreach (var draft in drafts)
        {
            for (int? inclusion = draft.Inclusion; inclusion is { } id; inclusion = parents[id])
            {
                lastLine[id] = draft.Number;
                if (suppressedInclusions.Contains(id)) suppressed.Add(draft.Number);
            }
        }

        var states = new List<(int From, bool On)>();
        var directiveLines = new HashSet<int>();
        foreach (var change in trace.ListingSwitches)
        {
            switch (change.Cause)
            {
                case TextListingSwitchCause.Directive:
                    int directive = At(index, change.Inclusion, change.Line, "LISTING directive");
                    directiveLines.Add(directive);
                    states.Add((directive + 1, change.On));
                    break;
                case TextListingSwitchCause.Pop:
                    states.Add((At(index, change.Inclusion, change.Line, "POP directive") + 1, change.On));
                    break;
                case TextListingSwitchCause.ImplicitPop:
                    states.Add((index.Of(change.Line) ?? throw Unplaced($"implicit POP ALL at resultant line {change.Line}"), change.On));
                    break;
                case TextListingSwitchCause.InclusionEnd:
                    int inclusionId = change.Inclusion ?? throw Unplaced("the end of an inclusion that names none");
                    int end = lastLine.TryGetValue(inclusionId, out int last) ? last
                        : At(index, parents[inclusionId], inclusions.First(i => i.Id == inclusionId).CopyLast.Line, "COPY statement");
                    states.Add((end + 1, change.On));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(trace), change.Cause, "a LISTING state change with no placement");
            }
        }
        // Stable: two changes that govern from the same line act in the order the driver made them.
        var ordered = states.Select((s, n) => (s, n)).OrderBy(p => p.s.From).ThenBy(p => p.n).Select(p => p.s).ToList();
        var pages = trace.PageDirectives.Select(p => At(index, p.Inclusion, p.Line, "PAGE directive")).ToHashSet();
        return new DirectiveEffects(ordered, directiveLines, pages, suppressed);
    }

    /// <summary>The DISPLAY directive transfers to the listing, each at its directive's listing line, in the order processed
    /// (§2023:7.3.12.4 GR1, GR4).</summary>
    public static List<ListingDisplay> Displays(TextStageTrace trace, ListingLineIndex index) =>
        [.. trace.Displays.Select(d => new ListingDisplay(At(index, d.Inclusion, d.Line, "DISPLAY directive"), d.Text))];

    /// <summary>Mark each line a replacing action changed (<see cref="ListingReplacement.Replacing"/>) with the resultant text
    /// compiled from it: every RESULTANT line (of <paramref name="resultantText"/>) the index places on it, in order. A
    /// DATE-COMPILED line keeps its own replacement.</summary>
    public static void MarkReplaced(TextStageTrace trace, ListingLineIndex index, IReadOnlyList<LineDraft> drafts,
        string resultantText)
    {
        if (trace.ReplacedLines.Count == 0) return;
        var marked = new HashSet<int>();
        foreach (var replaced in trace.ReplacedLines)
        {
            int line = replaced.ResultantLine > 0
                ? index.Of(replaced.ResultantLine, replaced.Origin)
                    ?? throw Unplaced($"REPLACE match on {replaced.Origin.File}:{replaced.Origin.Line} (resultant line {replaced.ResultantLine})")
                : At(index, replaced.Inclusion, replaced.Origin.Line, "COPY REPLACING match");
            if (drafts[line - 1].Replacement == ListingReplacement.None) marked.Add(line);
        }
        var compiled = marked.ToDictionary(line => line, _ => new List<string>());
        string[] resultant = resultantText.Split('\n');
        for (int r = 1; r <= Math.Min(index.ResultantLines, resultant.Length); r++)
            if (index.Of(r) is { } line && compiled.TryGetValue(line, out var texts)) texts.Add(Compiled(resultant[r - 1]));
        foreach (var (line, texts) in compiled)
        {
            drafts[line - 1].Replacement = ListingReplacement.Replacing;
            drafts[line - 1].Replaced = string.Join('\n', texts);
        }
    }

    /// <summary>A resultant line as the listing prints it: the debugging-line carrier the text stages write ahead of a debugging
    /// line's text (<see cref="ReferenceFormatProcessor.DebugLineCarrier"/>) is the compiler's own mark, not text, and trailing
    /// spaces print as nothing.</summary>
    private static string Compiled(string resultantLine) =>
        resultantLine.Replace(ReferenceFormatProcessor.DebugLineCarrier, "", StringComparison.Ordinal).TrimEnd(' ');

    private static int At(ListingLineIndex index, int? inclusion, int fileLine, string what) =>
        index.Of(inclusion, fileLine) ?? throw Unplaced($"{what} on line {fileLine} of inclusion {inclusion?.ToString() ?? "(source)"}");

    private static InvalidOperationException Unplaced(string what) =>
        new($"the text stages recorded a {what} that no listing line holds (DESIGN-listing.md section 4.4)");
}
