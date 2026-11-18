// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Common;
using WiseOwl.Cobol.Frontend.Preprocessor;

namespace WiseOwl.Cobol.Listing.Building;

/// <summary>
/// ⛔ ONE INDEX PLACES EVERY POSITION (DESIGN-listing.md section 4.1): a RESULTANT line (a token's line, a statement's cursor)
/// becomes a listing line through this index and nowhere else. A resultant line lies in the innermost inclusion whose
/// resultant range holds it (<see cref="TextInclusion.Holds"/>), so a copybook copied twice is two blocks with a right answer
/// for "which one"; its origin (<see cref="SourceLineMap.Origin"/>) names the physical line within that inclusion.
/// </summary>
internal sealed class ListingLineIndex
{
    private readonly Dictionary<(int Inclusion, int FileLine), int> _listingLine = [];
    private readonly Dictionary<int, TextInclusion> _inclusions;
    private readonly int[] _innermost;   // per 1-based resultant line: the innermost inclusion holding it, 0 for none
    private readonly string _sourceFile;
    private readonly SourceLineMap _lineMap;

    public ListingLineIndex(IReadOnlyList<LineDraft> drafts, IReadOnlyList<TextInclusion> inclusions, string sourceFile,
        SourceLineMap lineMap)
    {
        foreach (var draft in drafts) _listingLine[(draft.Inclusion ?? 0, draft.FileLine)] = draft.Number;
        _inclusions = inclusions.ToDictionary(i => i.Id);
        _sourceFile = sourceFile;
        _lineMap = lineMap;
        _innermost = new int[lineMap.Count + 1];
        foreach (var inclusion in inclusions.Where(i => i.ResultantFirst > 0).OrderBy(i => i.Depth))
            for (int r = inclusion.ResultantFirst; r <= Math.Min(inclusion.ResultantLast, lineMap.Count); r++) _innermost[r] = inclusion.Id;
    }

    /// <summary>How many resultant lines there are.</summary>
    public int ResultantLines => _lineMap.Count;

    /// <summary>The listing line of 1-based RESULTANT <paramref name="line"/>, or null when it names no listed physical line.</summary>
    public int? Of(int line) => _lineMap.Origin(line) is { } origin ? Of(line, origin) : null;

    /// <summary>The listing line of physical <paramref name="origin"/> in the text RESULTANT <paramref name="line"/> belongs to:
    /// the innermost inclusion holding the line whose file is the origin's, else the source; null when that text lists no such
    /// line. A resultant line's own origin is the physical line of its first stretch; a position further along it (a continuation
    /// line's text, joined onto it) may name another physical line of the same text, and is placed in the same inclusion.</summary>
    public int? Of(int line, SourceOrigin origin)
    {
        if (line < 1 || line >= _innermost.Length) return null;
        int inclusion = _innermost[line];
        // A line inside an inclusion's range whose origin is another file (a framing line of a nested COPY) belongs to the
        // nearest enclosing text that is that file.
        while (inclusion != 0 && _inclusions[inclusion].File != origin.File) inclusion = _inclusions[inclusion].Parent ?? 0;
        if (inclusion == 0 && origin.File != _sourceFile) return null;
        return _listingLine.TryGetValue((inclusion, origin.Line), out int number) ? number : null;
    }

    /// <summary>⛔ THE PLACE OF A DIAGNOSTIC'S LOCATION (DESIGN-listing.md section 6.3): the listing line of the physical line
    /// <paramref name="at"/> names. A location located from a resultant line of THIS index's frame (<see cref="SourceLocation.Frame"/>,
    /// the map the listed pass's parse and bind located through) is placed in the inclusion that resultant line belongs to, so a
    /// diagnostic in the second copy of a copybook copied twice is placed in the second block; any other (a text stage's own
    /// numbering, or none) in the first text that lists that physical line. Null when no listed text holds it.</summary>
    public int? Place(SourceLocation at)
    {
        var origin = new SourceOrigin(at.FileName, at.Line + 1);
        if (at.Frame == _lineMap.Frame && at.ResultantLine > 0 && Of(at.ResultantLine, origin) is { } placed) return placed;
        if (origin.File == _sourceFile && Of(null, origin.Line) is { } inSource) return inSource;
        foreach (var inclusion in _inclusions.Values.Where(i => i.File == origin.File).OrderBy(i => i.Id))
            if (Of(inclusion.Id, origin.Line) is { } inCopy) return inCopy;
        return null;
    }

    /// <summary>The listing line of 1-based <paramref name="fileLine"/> of <paramref name="inclusion"/> (null: the source).</summary>
    public int? Of(int? inclusion, int fileLine) =>
        _listingLine.TryGetValue((inclusion ?? 0, fileLine), out int number) ? number : null;

    /// <summary>The physical point of RESULTANT <paramref name="line"/> and 0-based <paramref name="column"/>: where a token
    /// was laid out, in its file's own column (<see cref="OriginSpan.LaidOutAt"/>).</summary>
    public SourcePoint? PointAt(int line, int column) => _lineMap.PointAt(line, column)?.LaidOutAt;

    /// <summary>Whether a text stage omitted RESULTANT <paramref name="line"/> (a false conditional-compilation branch).</summary>
    public bool IsOmitted(int line) => _lineMap.IsOmitted(line);
}
