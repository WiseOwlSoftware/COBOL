// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Frontend.Common;

namespace WiseOwl.Cobol.Frontend.Preprocessor;

/// <summary>
/// The reference format in effect at each physical line of ONE source file or library text (kb/Work PB1067) — what
/// the §2023:6.5 logical conversion decided while it read that text: the initial format, then each change a
/// <c>&gt;&gt;SOURCE FORMAT</c> directive (§2023:7.3.24.3 GR1) or a <c>&gt;&gt;POP</c> of the format (§2023:7.3.22) makes.
/// <para>Its asker is the COPY statement: "The default reference format of library text is the reference format that
/// was in effect for the COPY statement that resulted in processing of this library text" (§2023:7.3.24.3 GR3). The COPY
/// statement's physical line — its <see cref="Common.SourceOrigin"/> — is looked up in the map of the text it was
/// written in, so a COPY inside library text asks that library text's map
/// (<see cref="LibraryTextDefaultAt"/>).</para>
/// </summary>
public sealed class ReferenceFormatMap
{
    // (first 1-based physical line, fixed form?, was the format DETECTED rather than stated?) in ascending line order;
    // element 0 starts at line 1.
    private readonly (int FromLine, bool Fixed, bool Detected)[] _changes;

    private readonly int[] _directiveLines;

    // Per 0-based physical line read in FIXED form: where its program text was cut at margin R, or null when it was not or
    // the file uses the positions past margin R for identification tags (kb/Work PB2555).
    private readonly MarginRCut?[] _cuts;

    // Per 0-based physical line: the map from an index of its expanded text to the physical column, null on a line that held
    // no tab and no supplementary-plane character (kb/Work PB5503).
    private readonly ColumnMap?[] _columns;

    // Per 0-based physical line: what the logical conversion decided the line is (DESIGN-listing.md section 4.4.1).
    private readonly PhysicalLineKind[] _kinds;

    // Per 1-based physical line: where the conversion discarded the rest of its program text, and as what; null without a listing.
    private readonly IReadOnlyDictionary<int, (int Column, SourceTextClass Class)>? _discarded;

    private ReferenceFormatMap((int FromLine, bool Fixed, bool Detected)[] changes, int[] directiveLines,
        ReadOnlySpan<PhysicalLine> lines, PhysicalLineKind[] kinds, IReadOnlyDictionary<int, (int Column, SourceTextClass Class)>? discarded)
    {
        _discarded = discarded;
        if (kinds.Length != lines.Length)
            throw new ArgumentException($"one kind per physical line: {kinds.Length} kinds for {lines.Length} lines", nameof(kinds));
        _changes = changes;
        _directiveLines = directiveLines;
        _kinds = kinds;
        var physicalText = new string[lines.Length];
        _cuts = new MarginRCut?[lines.Length];
        _columns = new ColumnMap?[lines.Length];
        int tagShaped = 0;   // the fixed-form lines that carry ONE word of identification-area width past margin R
        for (int i = 0; i < lines.Length; i++)
        {
            physicalText[i] = lines[i].Text;
            _columns[i] = lines[i].Columns;
            if (!At(i + 1).Fixed) continue;
            var fixedLine = new FixedFormLine(lines[i].Text);
            _cuts[i] = CutOf(lines[i], fixedLine);
            if (_cuts[i] is { FitsIdentificationArea: true }) tagShaped++;
        }
        // A file in which such a word recurs past margin R uses those positions for identification (program name, card
        // number, a revision mark), whatever the tags spell; what it cuts is not its statements. A tag is counted on a line
        // WITHOUT program text too (a tagged blank line), and a file that tags only some of its lines is still a file
        // that tags (kb/Work PB2555, PB4372).
        if (tagShaped >= MarginRCut.RecurrenceThatMakesATag)
            for (int i = 0; i < _cuts.Length; i++)
                if (_cuts[i] is { FitsIdentificationArea: true }) _cuts[i] = null;
        PhysicalText = physicalText;
    }

    /// <summary>The text of each PHYSICAL line of this file, as the line-entry stage settled it (<see cref="PhysicalLines"/>:
    /// terminators gone, tabs expanded), 0-based. The line-entry stage is the ONLY place a text becomes lines
    /// (<c>PhysicalLinesDriftTests</c>), so a later stage that must look at the physical text of a file — the front end's
    /// search for an END-PERFORM phrase the parse could not read (kb/Work PB1066) — reads it HERE, never splits the file
    /// again.</summary>
    public IReadOnlyList<string> PhysicalText { get; }

    /// <summary>The 1-based physical lines of this text that hold a WRITTEN directive the reference format answers to —
    /// a <c>&gt;&gt;SOURCE FORMAT</c>, <c>&gt;&gt;PUSH</c> or <c>&gt;&gt;POP</c> line — ascending, whether or not it
    /// changed the format. What decides whether a §2023:14.9.28.4 GR14 implicit PUSH ALL / POP ALL bracket can matter to the
    /// normalizer at all: a bracket that encloses no such line restores exactly the format it saved
    /// (<see cref="ImplicitFormatOps.Place"/>; kb/Work PB1066).</summary>
    public IReadOnlyList<int> DirectiveLines => _directiveLines;

    /// <summary>Whether 1-based physical <paramref name="line"/> of this text is a written <c>&gt;&gt;SOURCE FORMAT</c>,
    /// <c>&gt;&gt;PUSH</c> or <c>&gt;&gt;POP</c> line (<see cref="DirectiveLines"/>). The conversion DISCARDS a
    /// <c>&gt;&gt;SOURCE FORMAT</c> line (§2023:6.5 GR1) and leaves a blank one in its place, so a later stage that must ask
    /// whether a blank line is one — the COPY or REPLACE statement a directive interrupts (§2023:7.3.3 SR8b, kb/Work
    /// PB1353 — asks the line's origin here.</summary>
    public bool HoldsDirectiveAt(int line) => Array.BinarySearch(_directiveLines, line) >= 0;

    /// <summary>A text read in <paramref name="initialFixed"/> from line 1 — <paramref name="detected"/> when no
    /// directive or COPY statement stated it — and then in each format of <paramref name="changes"/> (a directive
    /// states each) from its 1-based physical line on (<paramref name="changes"/> ascending).
    /// <paramref name="directiveLines"/> are the lines of its written format directives (<see cref="DirectiveLines"/>) and
    /// <paramref name="lines"/> its physical lines, as the line-entry stage settled them (<see cref="PhysicalText"/>,
    /// <see cref="AreaAt"/>), and <paramref name="kinds"/> what the conversion decided each one is, one per line
    /// (<see cref="KindAt"/>), and <paramref name="discarded"/>, for a source listing, where it discarded a line's text as a
    /// comment or a comment-entry (<see cref="DiscardedAt"/>).</summary>
    public static ReferenceFormatMap Create(bool initialFixed, bool detected, IEnumerable<(int FromLine, bool Fixed)> changes,
        IEnumerable<int> directiveLines, ReadOnlySpan<PhysicalLine> lines, PhysicalLineKind[] kinds,
        IReadOnlyDictionary<int, (int Column, SourceTextClass Class)>? discarded = null)
        => new([(1, initialFixed, detected), .. changes.Select(c => (c.FromLine, c.Fixed, false))], [.. directiveLines],
            lines, kinds, discarded);

    /// <summary>Where the §2023:6.5 logical conversion discarded the rest of the program text of 1-based physical
    /// <paramref name="line"/>, and as what: the 0-based physical column an inline comment (§2023:6.5 GR3) or the comment-entry of
    /// the paragraph the line heads begins at. Null when it discarded none, and always when the conversion was not asked to record
    /// it, which only a compilation producing a source listing asks (DESIGN-listing.md section 7.2). On a comment-entry candidate
    /// line (<see cref="PhysicalLineKind.CommentEntryCandidate"/>) it is where the comment-entry begins IF the line heads a
    /// comment-entry paragraph, which only the parse of each copy of the line settles: the caller asks it of a candidate the
    /// parse read as a header, and of no other candidate.</summary>
    public (int Column, SourceTextClass Class)? DiscardedAt(int line) =>
        _discarded is not null && _discarded.TryGetValue(line, out var discarded) ? discarded : null;

    /// <summary>What 1-based physical <paramref name="line"/> of this text is, as the §2023:6.5 logical conversion decided
    /// when it read the line (<see cref="PhysicalLineKind"/>; DESIGN-listing.md section 4.4.1): the one reader of a line's
    /// indicator area records its answer here, so the source listing never reads the columns again. A line that is not
    /// one of this text's is a caller's fault and throws.</summary>
    public PhysicalLineKind KindAt(int line)
        => line >= 1 && line <= _kinds.Length ? _kinds[line - 1]
            : throw new ArgumentOutOfRangeException(nameof(line), line, $"this text has {_kinds.Length} physical lines");

    /// <summary>Whether 1-based physical <paramref name="line"/> was read in fixed form.</summary>
    public bool IsFixedAt(int line) => At(line).Fixed;

    /// <summary>The 0-based index into the EXPANDED text of 1-based physical <paramref name="line"/>
    /// (<see cref="PhysicalText"/>, tabs expanded) of the character at 0-based PHYSICAL <paramref name="column"/>, the
    /// column a source map names (<see cref="Common.SourcePoint"/>; kb/Work PB5503). A line that is not one of this text's
    /// reads the column as it is.</summary>
    public int ExpandedIndexAt(int line, int column)
        => line >= 1 && line <= _columns.Length ? _columns[line - 1]?.ToExpanded(column) ?? column : column;

    /// <summary>⛔ THE ONE COLUMN A LISTING RECORD NAMES (DESIGN-listing.md sections 6.3, 6.6 and 6.9): the 1-based CHARACTER position
    /// in the expanded text of 1-based physical <paramref name="line"/> (<see cref="PhysicalText"/>, tabs expanded) of the character at
    /// 0-based physical <paramref name="column"/>. <see cref="ExpandedIndexAt"/> counts UTF-16 code units, so a supplementary-plane
    /// character before the column (an emoji in a literal) is one character but two units; this counts it once, as the text listing's
    /// caret and every reader of a JSON column do (listing step 3 review lead R1-1). A line that is not one of this text's reads the
    /// column as it is.</summary>
    public int CharacterPositionAt(int line, int column)
    {
        int index = ExpandedIndexAt(line, column);
        if (line < 1 || line > PhysicalText.Count) return index + 1;
        string text = PhysicalText[line - 1];
        int pairs = 0;
        for (int at = 1; at < Math.Min(index, text.Length); at++)
            if (char.IsLowSurrogate(text[at]) && char.IsHighSurrogate(text[at - 1])) pairs++;
        return index - pairs + 1;
    }

    /// <summary>Where the areas of 1-based physical <paramref name="line"/> begin and end in its expanded text
    /// (<see cref="PhysicalText"/>), as the one reader of fixed-form columns reads them (<see cref="FixedFormLine"/>, kb/Work PB1966);
    /// null when the line was read in free form, which has no areas (§2023:6.4.1), or is not a line of this text. What the source
    /// listing colours the areas of a line by (DESIGN-listing.md section 7.2): a result of the reader, not a second reader.</summary>
    public FixedFormRegions? RegionsAt(int line)
    {
        if (line < 1 || line > PhysicalText.Count || !At(line).Fixed) return null;
        var fixedLine = new FixedFormLine(PhysicalText[line - 1]);
        return new FixedFormRegions(fixedLine.IndicatorIndex, fixedLine.ProgramTextAreaIndex, fixedLine.MarginRIndex);
    }

    /// <summary>The COBOL-85 area the text at 0-based physical <paramref name="column"/> of 1-based physical
    /// <paramref name="line"/> stands in (kb/Work PB3147, PB5313): area A (positions 8–11) or area B (position 12 to
    /// margin R), tabs expanded (<see cref="ProgramTextOffsetAt"/>); <see cref="FixedFormArea.None"/> when the line was
    /// read in free form (which has no areas, §2023:6.4.1) or is not a line of this text. The column is the one a source
    /// map names (<see cref="Common.SourcePoint"/>).</summary>
    public FixedFormArea AreaAt(int line, int column)
    {
        if (line < 1 || line > _columns.Length || !At(line).Fixed) return FixedFormArea.None;
        return FixedFormLine.IsAreaAOffset(ProgramTextOffsetAt(line, column)) ? FixedFormArea.A : FixedFormArea.B;
    }

    /// <summary>The 0-based offset into the program-text area (position 8 is offset 0) of the character at 0-based physical
    /// <paramref name="column"/> of 1-based physical <paramref name="line"/> — the offset the character stands at once every tab
    /// is expanded (<see cref="PhysicalLines"/>), which is what area A (<see cref="FixedFormLine.IsAreaAOffset"/>) and the
    /// room before margin R are measured in, and never the file's own column, where a tab counts one (kb/Work PB5503).
    /// Negative before the program-text area. A line that is not one of this text's reads the column as it is.</summary>
    public int ProgramTextOffsetAt(int line, int column)
    {
        if (line < 1 || line > _columns.Length) return column - FixedFormLine.SourceAreaStart;
        int index = _columns[line - 1]?.ToExpanded(column) ?? column;
        string text = PhysicalText[line - 1];
        int position = index <= text.Length
            ? CharacterPositions.PositionAt(text, index)
            : CharacterPositions.Count(text) + (index - text.Length);   // past the line's end: space-filled to margin R
        return position - FixedFormLine.SourceAreaStart;
    }

    /// <summary>Where the program text of 1-based physical <paramref name="line"/> was cut at margin R (kb/Work PB2555), or
    /// null when it was not: the line was read in free form, it is not a line of this text, or it carries nothing past margin R
    /// that the reference format drops as program text (<see cref="MarginRCut"/>).</summary>
    public MarginRCut? CutAt(int line) => line >= 1 && line <= _cuts.Length ? _cuts[line - 1] : null;

    /// <summary>The cut of the physical <paramref name="line"/>, read in fixed form as <paramref name="fixedLine"/>, or null when
    /// it has none: its text past margin R is blank, it is a comment line, or its program-text area ends in an inline
    /// comment (§2023:6.3.7.3), whose rest the margin cuts.</summary>
    private static MarginRCut? CutOf(PhysicalLine line, FixedFormLine fixedLine)
    {
        if (CobolSpace.IsBlank(fixedLine.BeyondMarginR)) return null;
        if (fixedLine.Indicator.Value is '*' or '/') return null;
        string area = fixedLine.ProgramText;
        if (CobolSourceText.InlineCommentAt(area) < area.Length) return null;
        return new MarginRCut(PhysicalLine.ColumnOf(line.Columns, fixedLine.MarginRIndex), FixedFormLine.MarginR,
            fixedLine.BeyondMarginR.ToString().TrimSpacesEnd(), fixedLine.ProgramTextStart >= 0);
    }

    /// <summary>The initial reference format of library text copied by a COPY statement at 1-based physical line
    /// <paramref name="line"/> of this text — §2023:7.3.24.3 GR3's "reference format that was in effect for the COPY
    /// statement" — or null when this text holds no format to hand on and the library text's own is detected.
    /// <para>A format a directive stated, or one the library text inherited, is handed on as the rule says. A
    /// DETECTED format comes only from the documented <c>--source-format auto</c> extension over §2023:7.3.24.3 GR2's
    /// fixed-form default (text is classified by its structure — kb/Work PB1362): a text detected FIXED showed the fixed
    /// column structure and hands fixed form on; a text detected FREE showed only that it is not column-bound —
    /// seven-space indentation is legal in both formats — which says nothing of the library text, so that library text
    /// is classified by its own structure, exactly as the compilation group was.</para></summary>
    public bool? LibraryTextDefaultAt(int line)
    {
        var (_, isFixed, detected) = At(line);
        return detected && !isFixed ? null : isFixed;
    }

    private (int FromLine, bool Fixed, bool Detected) At(int line)
    {
        int lo = 0, hi = _changes.Length - 1;
        while (lo < hi)
        {
            int mid = (lo + hi + 1) >> 1;
            if (_changes[mid].FromLine <= line) lo = mid; else hi = mid - 1;
        }
        return _changes[lo];
    }
}

/// <summary>The areas of one fixed-form physical line as 0-based indexes of its expanded text (<see cref="ReferenceFormatMap.RegionsAt"/>;
/// §2023:6.3.1): the sequence number area is <c>[0, Indicator)</c>, the indicator area <c>[Indicator, ProgramText)</c>, the
/// program-text area <c>[ProgramText, MarginR)</c> and what follows margin R <c>[MarginR, end)</c>. A line shorter than an area
/// ends it early: each index is at most the line's length.</summary>
/// <param name="Indicator">Where the indicator area begins.</param>
/// <param name="ProgramText">Where the program-text area begins.</param>
/// <param name="MarginR">Where the program-text area ends.</param>
public readonly record struct FixedFormRegions(int Indicator, int ProgramText, int MarginR);
