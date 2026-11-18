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
    public int? Of(int line)
    {
        if (_lineMap.Origin(line) is not { } origin) return null;
        int inclusion = _innermost[line];
        // A line inside an inclusion's range whose origin is another file (a framing line of a nested COPY) belongs to the
        // nearest enclosing text that is that file.
        while (inclusion != 0 && _inclusions[inclusion].File != origin.File) inclusion = _inclusions[inclusion].Parent ?? 0;
        if (inclusion == 0 && origin.File != _sourceFile) return null;
        return _listingLine.TryGetValue((inclusion, origin.Line), out int number) ? number : null;
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
