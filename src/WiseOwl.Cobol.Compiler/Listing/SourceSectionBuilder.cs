// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Listing;

namespace WiseOwl.Cobol.Compiler.Listing;

/// <summary>One source line while the listing is being built: what <see cref="SourceSectionBuilder"/> reads off the files,
/// then what the later builders decide of it (its program, its replacement, its visibility).</summary>
internal sealed class LineDraft
{
    public required int Number { get; init; }
    public required int File { get; init; }
    public required int FileLine { get; init; }
    public required int? Inclusion { get; init; }
    public required int Depth { get; init; }
    public required ListingLineForm Form { get; init; }
    public required PhysicalLineKind Kind { get; set; }

    /// <summary>Whether the line was a comment-entry candidate the parse read as a comment-entry paragraph's header
    /// (<see cref="SourceSectionBuilder.SettleCandidates"/>): its settled <see cref="Kind"/> is source, and the rest of its program
    /// text from <see cref="ReferenceFormatMap.DiscardedAt"/> is the comment-entry.</summary>
    public bool HeadsCarriedCommentEntry { get; set; }
    public required string Text { get; init; }
    public bool Omitted { get; set; }
    public bool Listed { get; set; } = true;
    public bool PageBreakBefore { get; set; }
    public int? Program { get; set; }
    public ListingReplacement Replacement { get; set; }
    public string? Replaced { get; set; }

    /// <summary>The ids of the diagnostics placed on the line (<see cref="DiagnosticSectionBuilder"/>), in listing order; null
    /// while none is, which is nearly every line, so a line with none allocates nothing for them.</summary>
    public List<int>? Diagnostics { get; private set; }

    /// <summary>Place diagnostic <paramref name="id"/> on the line.</summary>
    public void AddDiagnostic(int id) => (Diagnostics ??= []).Add(id);

    /// <summary>The line's margin (<see cref="LineAnnotationBuilder"/>), or null.</summary>
    public ListingMargin? Margin { get; set; }

    /// <summary>What the compiler read each stretch of the line's text as (<see cref="SyntaxSpanBuilder"/>), or null while none.</summary>
    public ListingSpan[]? Spans { get; set; }

    /// <summary>The names written on the line (<see cref="CrossReferenceBuilder.MarkLines"/>), or null while none, which is every line
    /// of a listing without the cross-reference, so such a line allocates nothing for them.</summary>
    public List<ListingNameUse>? Names { get; private set; }

    /// <summary>Place a name written on the line.</summary>
    public void AddName(ListingNameUse name) => (Names ??= []).Add(name);

    /// <summary>The finished model line.</summary>
    public ListingLine ToLine() => new(Number, File, FileLine, Inclusion, Depth, Form, ListingLineKinds.Of(Kind), Text, Omitted,
        Listed, PageBreakBefore, Program, Replacement, Replaced, Diagnostics is { } ids ? ids.ToArray() : NoDiagnostics, Margin,
        Names is { } names ? names.ToArray() : NoNames, Spans ?? NoSpans);

    private static readonly ListingNameUse[] NoNames = [];

    private static readonly int[] NoDiagnostics = [];
    private static readonly ListingSpan[] NoSpans = [];
}

/// <summary>
/// ⛔ THE ONE MAP from the front end's <see cref="PhysicalLineKind"/> to the model's <see cref="ListingLineKind"/>
/// (DESIGN-listing.md section 4.2): the Listing assembly references nothing, so it declares its own enum, and this switch is
/// total — <c>PhysicalLineKindDriftTests.TheKindMap_IsTotal_BothWays</c> holds every member of either enum to it. A comment-entry candidate has no image:
/// the builder settles it from the parse first (<see cref="SourceSectionBuilder.SettleCandidates"/>).
/// </summary>
internal static class ListingLineKinds
{
    public static ListingLineKind Of(PhysicalLineKind kind) => kind switch
    {
        PhysicalLineKind.Source => ListingLineKind.Source,
        PhysicalLineKind.Comment => ListingLineKind.Comment,
        PhysicalLineKind.PageEjectComment => ListingLineKind.PageEjectComment,
        PhysicalLineKind.DebuggingLine => ListingLineKind.DebuggingLine,
        PhysicalLineKind.Continuation => ListingLineKind.Continuation,
        PhysicalLineKind.Directive => ListingLineKind.Directive,
        PhysicalLineKind.CommentEntry => ListingLineKind.CommentEntry,
        PhysicalLineKind.CcvsExcluded => ListingLineKind.CcvsExcluded,
        PhysicalLineKind.NotAnIndicator => ListingLineKind.NotAnIndicator,
        PhysicalLineKind.Blank => ListingLineKind.Blank,
        PhysicalLineKind.CommentEntryCandidate => throw new InvalidOperationException(
            "a comment-entry candidate reaches the model unsettled: SourceSectionBuilder.SettleCandidates decides it first"),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "a physical line kind with no listing kind"),
    };
}

/// <summary>
/// The source section's lines in LISTING ORDER (DESIGN-listing.md section 4.4): the main source's physical lines in order and,
/// after the last physical line of a COPY statement, that inclusion's lines (recursively), each one level deeper. Every line's
/// text and kind is the front end's (<see cref="ReferenceFormatMap.PhysicalText"/>, <see cref="ReferenceFormatMap.KindAt"/>):
/// nothing here reads a column of a line.
/// </summary>
internal static class SourceSectionBuilder
{
    /// <summary>The drafts of every physical line of every file read, numbered in listing order, and the inclusion each
    /// line belongs to. <paramref name="fileIds"/> numbers each file (the source is 1).</summary>
    public static List<LineDraft> Build(FrontendFacts facts, IReadOnlyList<TextInclusion> inclusions,
        IReadOnlyDictionary<string, int> fileIds)
    {
        var drafts = new List<LineDraft>();
        var children = inclusions.ToLookup(i => i.Parent ?? 0);
        Emit(facts.SourcePath, inclusion: 0, depth: 0);
        return drafts;

        void Emit(string file, int inclusion, int depth)
        {
            var map = facts.Formats.TryGetValue(file, out var m) ? m
                : throw new InvalidOperationException($"the listing has no reference-format map of '{file}', which the text stages read");
            var after = new Dictionary<int, List<TextInclusion>>();
            foreach (var child in children[inclusion])
            {
                if (child.CopyLast.File != file || child.CopyLast.Line < 1 || child.CopyLast.Line > map.PhysicalText.Count)
                    throw new InvalidOperationException(
                        $"COPY {child.TextName} ends at {child.CopyLast.File}:{child.CopyLast.Line}, not on a line of '{file}' that holds it");
                if (!after.TryGetValue(child.CopyLast.Line, out var list)) after[child.CopyLast.Line] = list = [];
                list.Add(child);
            }
            for (int n = 1; n <= map.PhysicalText.Count; n++)
            {
                drafts.Add(new LineDraft
                {
                    Number = drafts.Count + 1, File = fileIds[file], FileLine = n, Inclusion = inclusion == 0 ? null : inclusion,
                    Depth = depth, Form = map.IsFixedAt(n) ? ListingLineForm.Fixed : ListingLineForm.Free, Kind = map.KindAt(n),
                    Text = map.PhysicalText[n - 1],
                });
                if (!after.TryGetValue(n, out var copied)) continue;
                foreach (var child in copied)
                    if (child.Outcome == TextInclusionOutcome.Found && child.File is { } library) Emit(library, child.Id, depth + 1);
            }
        }
    }

    /// <summary>Settle each <see cref="PhysicalLineKind.CommentEntryCandidate"/> line from the parse (DESIGN-listing.md section
    /// 4.4.1): a candidate line that heads a comment-entry paragraph the parse read is the paragraph's header (source, the rest
    /// of its text the comment-entry: <see cref="LineDraft.HeadsCarriedCommentEntry"/>), the candidate lines after it are its
    /// comment-entry, and any other candidate line is source text.</summary>
    public static void SettleCandidates(List<LineDraft> drafts, ISet<int> commentEntryHeads)
    {
        bool inEntry = false;
        foreach (var draft in drafts)
        {
            if (draft.Kind != PhysicalLineKind.CommentEntryCandidate)
            {
                inEntry = false;
                continue;
            }
            if (commentEntryHeads.Contains(draft.Number))
            {
                inEntry = true;
                draft.Kind = PhysicalLineKind.Source;
                draft.HeadsCarriedCommentEntry = true;
            }
            else draft.Kind = inEntry ? PhysicalLineKind.CommentEntry : PhysicalLineKind.Source;
        }
    }

    /// <summary>Replace each DATE-COMPILED paragraph (§1985:VI.3.4.4 GR1: "it is replaced during compilation with a paragraph of
    /// the form" <c>DATE-COMPILED. current date.</c>): its first line prints the replacement in place of the written text from
    /// the paragraph word on, and the further lines of its comment-entry are replaced with it. <paramref name="words"/> are the
    /// listing line and the 0-based index into that line's text of each paragraph word.</summary>
    public static void ReplaceDateCompiled(List<LineDraft> drafts, IEnumerable<(int Line, int Index)> words, DateTimeOffset instant)
    {
        foreach (var (line, index) in words)
        {
            var head = drafts[line - 1];
            head.Replacement = ListingReplacement.DateCompiled;
            head.Replaced = head.Text[..Math.Clamp(index, 0, head.Text.Length)] + ListingTimestamp.DateCompiledParagraph(instant);
            for (int n = line; n < drafts.Count; n++)
            {
                var next = drafts[n];
                if (next.Inclusion != head.Inclusion || next.File != head.File) break;
                if (next.Kind == PhysicalLineKind.CommentEntry) next.Replacement = ListingReplacement.DateCompiledEntry;
                else if (next.Kind is not (PhysicalLineKind.Comment or PhysicalLineKind.PageEjectComment)) break;
            }
        }
    }
}
