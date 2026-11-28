// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend;
using WiseOwl.Cobol.Frontend.Common;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Listing;

namespace WiseOwl.Cobol.Compiler.Listing;

/// <summary>
/// ⛔ THE ONE BUILDER OF A LINE'S SPANS (DESIGN-listing.md section 7.2; kb/Work PB5949): what the compiler read each stretch of a
/// listed line as, made once, here, so the HTML page colours the text and the JSON carries the classes and neither renderer
/// decides one. Two inputs, both the front end's own answers and neither a pattern over the text:
/// <list type="bullet">
///   <item>the REFERENCE FORMAT of the line (<see cref="ReferenceFormatMap.RegionsAt"/>, <see cref="ReferenceFormatMap.KindAt"/>,
///     <see cref="ReferenceFormatMap.DiscardedAt"/>): the sequence number area, the indicator, the text past margin R, the whole
///     program text of a comment line, a comment-entry line or a directive line, and the inline comment or comment-entry the
///     conversion discarded from a line, which no token reads;</item>
///   <item>the LEXER'S TOKENS and inline comments of the listed pass, classified at the edition in force
///     (<see cref="FrontendFacts.TextClasses"/>), each placed on the physical lines it came from through the line map's origin
///     spans and the one index (<see cref="ListingLineIndex.Of(int, SourceOrigin)"/>), so a token in the second copy of a
///     copybook colours the second block, and a literal continued onto the next line colours both lines.</item>
/// </list>
/// Text a REPLACE or COPY REPLACING placed (an origin span with a layout of its own) stands at the place of the text it replaced,
/// whose characters the line still shows as written, so it colours nothing: the line's written text was not what was lexed there.
/// </summary>
internal static class SyntaxSpanBuilder
{
    /// <summary>Give each draft of <paramref name="drafts"/> its spans. <paramref name="files"/> names each file id's path.</summary>
    public static void Mark(List<LineDraft> drafts, FrontendFacts frontend, ListingLineIndex index, IReadOnlyDictionary<int, string> files)
    {
        var pieces = new List<(int Start, int End, SourceTextClass Class)>?[drafts.Count];
        var wholeLine = new bool[drafts.Count];
        for (int n = 0; n < drafts.Count; n++)
        {
            var draft = drafts[n];
            var map = frontend.Formats[files[draft.File]];
            var regions = map.RegionsAt(draft.FileLine);
            int textEnd = draft.Text.Length;
            int programStart = regions?.ProgramText ?? 0, programEnd = regions?.MarginR ?? textEnd;
            if (regions is { } r)
            {
                Area(n, 0, r.Indicator, SourceTextClass.SequenceArea, onlyIfVisible: true);
                Area(n, r.Indicator, r.ProgramText, SourceTextClass.Indicator, onlyIfVisible: true);
                Area(n, r.MarginR, textEnd, SourceTextClass.IdentificationArea, onlyIfVisible: true);
            }
            SourceTextClass? whole = draft.Kind switch
            {
                PhysicalLineKind.Comment or PhysicalLineKind.PageEjectComment or PhysicalLineKind.CcvsExcluded => SourceTextClass.Comment,
                PhysicalLineKind.CommentEntry => SourceTextClass.CommentEntry,
                PhysicalLineKind.Directive => SourceTextClass.Directive,
                _ => null,
            };
            if (whole is { } cls)
            {
                wholeLine[n] = true;
                Area(n, programStart, programEnd, cls, onlyIfVisible: true);
            }
            // The rest of the line's program text the conversion discarded as an inline comment or a comment-entry, which no
            // token reads; a carried candidate's only when the parse read it as a comment-entry paragraph's header.
            else if (map.DiscardedAt(draft.FileLine) is { } discarded
                && (map.KindAt(draft.FileLine) != PhysicalLineKind.CommentEntryCandidate || draft.HeadsCarriedCommentEntry))
                Area(n, map.ExpandedIndexAt(draft.FileLine, discarded.Column), programEnd, discarded.Class, onlyIfVisible: true);
        }

        var lineStarts = LineStarts(frontend.ResultantText);
        if (frontend.TextClasses is { } classes)
        {
            foreach (var span in classes) Place(span);

            void Place(SourceTextSpan span)
            {
                int line = span.Line, column = span.Column, remaining = span.Length;
                while (remaining > 0 && line >= 1 && line <= lineStarts.Count)
                {
                    int lineLength = (line < lineStarts.Count ? lineStarts[line] - 1 : frontend.ResultantText.Length) - lineStarts[line - 1];
                    int take = Math.Min(remaining, Math.Max(lineLength - column, 0));
                    if (take > 0) PlaceOnLine(line, column, column + take, span.Class);
                    remaining -= take + 1;   // and the line end the stretch crosses
                    line++;
                    column = 0;
                }
            }
        }

        for (int n = 0; n < drafts.Count; n++)
            if (pieces[n] is { } list) drafts[n].Spans = Finish(drafts[n].Text, list);

        // Whether the written text [start, end) of a line is, character for character, the resultant text from `at` on.
        bool Written(string text, int start, int end, int at)
        {
            if (end - start != Math.Min(end, text.Length) - start) return false;
            return text.AsSpan(start, end - start).SequenceEqual(frontend.ResultantText.AsSpan(at, Math.Min(end - start, frontend.ResultantText.Length - at)));
        }

        void Area(int n, int start, int end, SourceTextClass cls, bool onlyIfVisible)
        {
            string text = drafts[n].Text;
            end = Math.Min(end, text.Length);
            if (start >= end) return;
            if (onlyIfVisible && text.AsSpan(start, end - start).IsWhiteSpace()) return;
            (pieces[n] ??= []).Add((start, end, cls));
        }

        // The resultant columns [from, to) of resultant line `line`, placed on each physical line their origin spans name. The
        // resultant columns are UTF-16 indexes, the spans' source columns CHARACTER POSITIONS (a tab one, a supplementary-plane
        // character one), and the line's text, the expanded text, UTF-16 again: each stretch is placed from the expanded index
        // of its first unit (StretchStart) one unit for one, which within one origin span is exact, and the stretches one
        // token's text makes on one line (a surrogate pair begins a span at each of its units) are one piece.
        void PlaceOnLine(int line, int from, int to, SourceTextClass cls)
        {
            var spans = frontend.LineMap.SpansAt(line);
            int lastN = -1;   // the draft of the piece this token last added, which a stretch that follows it on extends
            for (int k = 0; k < spans.Length; k++)
            {
                var stretch = spans[k];
                int stretchEnd = k + 1 < spans.Length ? spans[k + 1].ResultColumn : int.MaxValue;
                int a = Math.Max(from, stretch.ResultColumn), b = Math.Min(to, stretchEnd);
                if (a >= b || stretch.Layout is not null) continue;
                if (index.Of(line, stretch.Origin) is not { } listing) continue;
                int n = listing - 1;
                if (wholeLine[n]) continue;
                var map = frontend.Formats[files[drafts[n].File]];
                int start = StretchStart(spans, k, map) + (a - stretch.ResultColumn);
                int end = start + (b - a);
                // A character the text stages wrote (the period they close an emptied comment-entry paragraph with) stands at a
                // place whose written character is another: the written text there was not what the lexer read.
                if (!Written(drafts[n].Text, start, end, lineStarts[line - 1] + a)) continue;
                var list = pieces[n] ??= [];
                if (lastN == n && list[^1].End == start) list[^1] = (list[^1].Start, end, cls);
                else list.Add((start, end, cls));
                lastN = n;
            }
        }
    }

    /// <summary>The 0-based index into the expanded text of its physical line of the first unit of origin span
    /// <paramref name="k"/> of <paramref name="spans"/>. Its source column names a character position, whose expanded text begins
    /// at <see cref="ReferenceFormatMap.ExpandedIndexAt"/>; a span that begins INSIDE that text (the second unit of a surrogate
    /// pair, a space after the first of a tab's expansion: the reference format splits a span at each, kb/Work PB1801) begins as
    /// many units further on as the units before it, of the same line, that map to the same position.</summary>
    private static int StretchStart(ReadOnlySpan<OriginSpan> spans, int k, ReferenceFormatMap map)
    {
        var stretch = spans[k];
        int within = 0;
        for (int j = k - 1, unit = stretch.ResultColumn - 1; j >= 0; unit--)
        {
            while (j >= 0 && spans[j].ResultColumn > unit) j--;
            if (j < 0 || spans[j].Origin != stretch.Origin || spans[j].Layout is not null
                || spans[j].SourceColumn + (unit - spans[j].ResultColumn) != stretch.SourceColumn)
                break;
            within++;
        }
        return map.ExpandedIndexAt(stretch.Origin.Line, stretch.SourceColumn) + within;
    }

    /// <summary>The spans of a line from its pieces (0-based indexes of its expanded text): in column order, the first of two that
    /// overlap kept (an embedded block's one token spans the text tokens it stands for), each clipped to the text and counted in
    /// CHARACTER positions, a supplementary-plane character one, as every column of the listing is.</summary>
    private static ListingSpan[] Finish(string text, List<(int Start, int End, SourceTextClass Class)> pieces)
    {
        pieces.Sort((x, y) => x.Start != y.Start ? x.Start.CompareTo(y.Start) : y.End.CompareTo(x.End));
        var spans = new List<ListingSpan>(pieces.Count);
        int covered = 0;
        bool pairs = text.AsSpan().IndexOfAnyInRange('\uD800', '\uDBFF') >= 0;
        foreach (var (start, rawEnd, cls) in pieces)
        {
            int end = Math.Min(rawEnd, text.Length);
            if (start < covered || start >= end) continue;
            int column = Position(start), past = Position(end);
            spans.Add(new ListingSpan(column, past - column, ListingSpanClasses.Of(cls)));
            covered = end;
        }
        return [.. spans];

        // The 1-based character position of 0-based index `at`.
        int Position(int at)
        {
            if (!pairs) return at + 1;
            int surrogates = 0;
            for (int i = 1; i < at; i++)
                if (char.IsLowSurrogate(text[i]) && char.IsHighSurrogate(text[i - 1])) surrogates++;
            return at - surrogates + 1;
        }
    }

    private static List<int> LineStarts(string text)
    {
        var starts = new List<int> { 0 };
        for (int at = text.IndexOf('\n'); at >= 0; at = text.IndexOf('\n', at + 1)) starts.Add(at + 1);
        return starts;
    }
}

/// <summary>
/// ⛔ THE ONE MAP from the front end's <see cref="SourceTextClass"/> to the model's <see cref="ListingSpanClass"/>
/// (DESIGN-listing.md section 7.2): the Listing assembly references nothing, so it declares its own enum, and this switch is total;
/// <c>SourceTextClassDriftTests.TheSpanClassMap_IsTotal_BothWays</c> holds every member of either enum to it.
/// </summary>
internal static class ListingSpanClasses
{
    public static ListingSpanClass Of(SourceTextClass cls) => cls switch
    {
        SourceTextClass.SequenceArea => ListingSpanClass.SequenceArea,
        SourceTextClass.Indicator => ListingSpanClass.Indicator,
        SourceTextClass.IdentificationArea => ListingSpanClass.IdentificationArea,
        SourceTextClass.Comment => ListingSpanClass.Comment,
        SourceTextClass.CommentEntry => ListingSpanClass.CommentEntry,
        SourceTextClass.Directive => ListingSpanClass.Directive,
        SourceTextClass.ReservedWord => ListingSpanClass.ReservedWord,
        SourceTextClass.ContextWord => ListingSpanClass.ContextWord,
        SourceTextClass.FunctionName => ListingSpanClass.FunctionName,
        SourceTextClass.UserWord => ListingSpanClass.UserWord,
        SourceTextClass.LevelNumber => ListingSpanClass.LevelNumber,
        SourceTextClass.AlphanumericLiteral => ListingSpanClass.AlphanumericLiteral,
        SourceTextClass.NationalLiteral => ListingSpanClass.NationalLiteral,
        SourceTextClass.BooleanLiteral => ListingSpanClass.BooleanLiteral,
        SourceTextClass.NumericLiteral => ListingSpanClass.NumericLiteral,
        SourceTextClass.FigurativeConstant => ListingSpanClass.FigurativeConstant,
        SourceTextClass.Picture => ListingSpanClass.Picture,
        SourceTextClass.Separator => ListingSpanClass.Separator,
        SourceTextClass.Operator => ListingSpanClass.Operator,
        SourceTextClass.EmbeddedText => ListingSpanClass.EmbeddedText,
        SourceTextClass.Unrecognized => ListingSpanClass.Unrecognized,
        _ => throw new ArgumentOutOfRangeException(nameof(cls), cls, "a source text class with no listing span class"),
    };
}
