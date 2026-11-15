// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Common;

namespace WiseOwl.Cobol.Frontend.Preprocessor;

/// <summary>Where a text stands with respect to the AREAS of a fixed-form line (kb/Work PB3147). ANSI X3.23-1985 split
/// the program-text area into area A (positions 8–11, <see cref="FixedFormLine.AreaAWidth"/>) and area B (positions 12 to
/// margin R) and placed every item by where it BEGINS (an area-A item by the area it starts in, other text by the line it begins); ISO/IEC 1989:2002 removed the split (ISO §2023:6.3.1), so only
/// a COBOL-85 reading asks.</summary>
public enum AreaPlacement
{
    /// <summary>The text has no area: its line was read in free form (§2023:6.4.1), or no source line holds it.</summary>
    None,

    /// <summary>The text is the first text of its fixed-form line, and it begins in area A.</summary>
    BeginsLineInAreaA,

    /// <summary>The text is the first text of its fixed-form line, and it begins in area B.</summary>
    BeginsLineInAreaB,

    /// <summary>The text follows other text of its fixed-form line, and it begins in area A (positions 8-11): the
    /// standard says where an area-A item "starts", not that it begins the line (§1985:IV.7.3.3), so `X. Y.` with Y in
    /// position 11 places Y in area A (kb/Work PB5497).</summary>
    WithinLineInAreaA,

    /// <summary>The text follows other text of its fixed-form line, and it begins in area B.</summary>
    WithinLineInAreaB,
}

/// <summary>The COBOL-85 area of a fixed-form position (<see cref="ReferenceFormatMap.AreaAt"/>; kb/Work PB5313).</summary>
public enum FixedFormArea
{
    /// <summary>The position has no area: its line was read in free form (§2023:6.4.1), or no source line holds it.</summary>
    None,

    /// <summary>Area A, positions 8–11.</summary>
    A,

    /// <summary>Area B, position 12 to margin R.</summary>
    B,
}

/// <summary>
/// THE AREA OF A RESULTANT POSITION (kb/Work PB3147, PB5313): the one composition of the compilation's resultant-line map
/// with the reference-format map of every text it read. An item's place in the RESULTANT program decides it, because the
/// syntactic correctness of the program "cannot be determined until all COPY and REPLACE statements have been completely
/// processed" (§1985:XII.2.4 GR8): WHETHER a token begins its line is a fact of the resultant lines, which its asker reads
/// from the token stream, and the AREA it begins in is that of the place it was laid out at
/// (<see cref="OriginSpan.LaidOutAt"/>) — where the user wrote it, through COPY, REPLACE and the fixed-form logical
/// conversion alike, except that a word of pseudo-text-2 a replacement places takes the area it was written in
/// (§1985:XII.2.4 GR9: it "begins in the same area of the resultant program as it appears in pseudo-text-2") while
/// identifier-2, literal-2 and word-2 take the area of the text they replace (the same rule). Built by the front end for
/// the parse it returns (<see cref="Frontend.Areas"/>); the COBOL-85 area-placement rules are its consumer.
/// </summary>
public sealed class FixedFormAreas(SourceLineMap lineMap, IReadOnlyDictionary<string, ReferenceFormatMap> formats)
    : IMarginRCutLocator
{
    /// <summary>No text has an area — the map of a compilation that read no fixed-form text.</summary>
    public static FixedFormAreas None { get; } = new(SourceLineMap.Identity("<source>", 0), new Dictionary<string, ReferenceFormatMap>());

    /// <summary>Where the text at 0-based resultant <paramref name="column"/> of 1-based resultant
    /// <paramref name="line"/> (an ANTLR token's position) stands with respect to COBOL-85's areas, given whether it
    /// <paramref name="beginsLine"/> — is the first text of its resultant line. <paramref name="writtenElsewhere"/> is the
    /// place the text was laid out at when that is not where it stands (<see cref="OriginSpan.Layout"/>), so a diagnostic
    /// can name the place whose area it judged.</summary>
    public AreaPlacement PlacementAt(int line, int column, bool beginsLine, out SourcePoint? writtenElsewhere)
    {
        writtenElsewhere = null;
        if (lineMap.PointAt(line, column) is not { } span) return AreaPlacement.None;
        var at = span.LaidOutAt;
        if (!formats.TryGetValue(at.Origin.File, out var map)) return AreaPlacement.None;
        writtenElsewhere = span.Layout;
        return map.AreaAt(at.Origin.Line, at.Column) switch
        {
            FixedFormArea.None => AreaPlacement.None,
            FixedFormArea.A => beginsLine ? AreaPlacement.BeginsLineInAreaA : AreaPlacement.WithinLineInAreaA,
            _ => beginsLine ? AreaPlacement.BeginsLineInAreaB : AreaPlacement.WithinLineInAreaB,
        };
    }

    /// <inheritdoc cref="IMarginRCutLocator.MarginRCutAt"/>
    /// <remarks>The line's cut is its <see cref="ReferenceFormatMap.CutAt"/> (kb/Work PB2555); a line a text stage omitted
    /// (<see cref="SourceLineMap.IsOmitted"/>) has none: the compiler never read what it holds (kb/Work PB4412).</remarks>
    public LocatedMarginRCut? MarginRCutAt(int line, int column)
    {
        if (lineMap.IsOmitted(line)) return null;
        var at = lineMap.Locate(line, column, fallbackFile: "");
        return formats.TryGetValue(at.FileName, out var map) && map.CutAt(at.Line + 1) is { } cut
            ? new LocatedMarginRCut(at, cut)
            : null;
    }
}
