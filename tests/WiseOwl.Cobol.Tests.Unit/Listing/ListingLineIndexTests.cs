// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Common;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Listing;
using WiseOwl.Cobol.Compiler.Listing;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit.Listing;

/// <summary>
/// The one index that places a RESULTANT line on a listing line (DESIGN-listing.md sections 4.1 and 4.4; section 10 item 7),
/// over a hand-built map: a source <c>s.cob</c> of five lines copies <c>A</c> at line 2 and <c>B</c> at line 4, and <c>B</c>
/// copies <c>A</c> again at its line 2, so <c>A</c> is listed twice and each copy has a right answer for "which block".
/// <code>
/// listing  1 s1 | 2 s2 | 3 A1 | 4 A2 | 5 s3 | 6 s4 | 7 B1 | 8 B2 | 9 A1 | 10 A2 | 11 B3 | 12 s5
///                        (inclusion 1)                (inclusion 2      (inclusion 3, in 2)   )
/// resultant r1 s1, r2 s2, r3 A1, r4 A2, r5 s2, r6 s3, r7 s4, r8 B1, r9 B2, r10 A1, r11 A2, r12 B2, r13 B3, r14 s4, r15 s5,
///           r16 other.cpy line 1 (a file no inclusion holds)
/// </code>
/// r2, r5, r9, r12 and r14 are the framing lines a COPY statement leaves around its library text: their origin is the COPY
/// statement's own line.
/// </summary>
public sealed class ListingLineIndexTests
{
    private const string S = "s.cob", A = "A.cpy", B = "B.cpy";

    private static LineDraft Draft(int number, int file, int fileLine, int? inclusion, int depth) => new()
    {
        Number = number, File = file, FileLine = fileLine, Inclusion = inclusion, Depth = depth,
        Form = ListingLineForm.Fixed, Kind = PhysicalLineKind.Source, Text = "",
    };

    private static TextInclusion Inclusion(int id, int? parent, int depth, int copyLine, string copyFile, string text, string file,
        int first, int last) =>
        new(id, parent, depth, new SourceOrigin(copyFile, copyLine), new SourceOrigin(copyFile, copyLine), text, null, file,
            TextInclusionOutcome.Found, first, last, Suppress: false);

    private static ListingLineIndex Index(int omitted = 0) => IndexAndMap(omitted).Index;

    private static (ListingLineIndex Index, SourceLineMap Map) IndexAndMap(int omitted = 0)
    {
        List<LineDraft> drafts =
        [
            Draft(1, 1, 1, null, 0), Draft(2, 1, 2, null, 0), Draft(3, 2, 1, 1, 1), Draft(4, 2, 2, 1, 1), Draft(5, 1, 3, null, 0),
            Draft(6, 1, 4, null, 0), Draft(7, 3, 1, 2, 1), Draft(8, 3, 2, 2, 1), Draft(9, 2, 1, 3, 2), Draft(10, 2, 2, 3, 2),
            Draft(11, 3, 3, 2, 1), Draft(12, 1, 5, null, 0),
        ];
        TextInclusion[] inclusions =
        [
            Inclusion(1, null, 1, 2, S, "A", A, 3, 4),
            Inclusion(2, null, 1, 4, S, "B", B, 8, 13),
            Inclusion(3, 2, 2, 2, B, "A", A, 10, 11),
        ];
        (string File, int Line)[] resultant =
        [
            (S, 1), (S, 2), (A, 1), (A, 2), (S, 2), (S, 3), (S, 4), (B, 1), (B, 2), (A, 1), (A, 2), (B, 2), (B, 3), (S, 4), (S, 5),
            ("other.cpy", 1),
        ];
        var origins = resultant.Select((o, i) =>
        {
            var origin = new LineOrigin(new SourceOrigin(o.File, o.Line));
            return i + 1 == omitted ? origin.AsOmitted() : origin;
        }).ToArray();
        var map = new SourceLineMap(origins);
        return (new ListingLineIndex(drafts, inclusions, S, map), map);
    }

    /// <summary>Each resultant line lands on the listing line of its own block: the second copy of A (inside B) on lines 9-10,
    /// never on the first copy's 3-4.</summary>
    [Fact]
    public void EachResultantLine_IsPlacedInTheInnermostInclusionThatHoldsIt()
    {
        var index = Index();
        Assert.Equal(16, index.ResultantLines);
        int?[] expected = [1, 2, 3, 4, 2, 5, 6, 7, 8, 9, 10, 8, 11, 6, 12];
        Assert.Equal(expected, Enumerable.Range(1, 15).Select(r => index.Of(r)));
    }

    /// <summary>A framing line inside a nested inclusion's parent range (r9, r12: B's line 2, the nested COPY statement) belongs
    /// to the nearest enclosing text that is its file, B, not the nested A; a framing line of a top-level COPY (r2, r5, r14)
    /// belongs to the source.</summary>
    [Fact]
    public void AFramingLine_BelongsToTheTextThatHoldsItsCopyStatement()
    {
        var index = Index();
        Assert.Equal(8, index.Of(9));
        Assert.Equal(8, index.Of(12));
        Assert.Equal([2, 2, 6], new[] { 2, 5, 14 }.Select(r => index.Of(r)));
    }

    /// <summary>A line whose origin is a file no inclusion holds, and a line outside the map, name no listing line.</summary>
    [Fact]
    public void ALineOfNoListedText_NamesNoListingLine()
    {
        var index = Index();
        Assert.Null(index.Of(16));
        Assert.Null(index.Of(17));
        Assert.Null(index.Of(0));
    }

    /// <summary>A physical line of a given inclusion (null: the source) is found by its block, not by its file.</summary>
    [Fact]
    public void APhysicalLine_IsFoundInItsOwnBlock()
    {
        var index = Index();
        Assert.Equal(3, index.Of(1, 1));
        Assert.Equal(9, index.Of(3, 1));
        Assert.Equal(12, index.Of(null, 5));
        Assert.Null(index.Of(1, 3));
        Assert.Null(index.Of(4, 1));
    }

    /// <summary>The index answers the map's omission of a resultant line, and no other line's.</summary>
    [Fact]
    public void Omission_IsTheMapsAnswer()
    {
        var index = Index(omitted: 10);
        Assert.Equal([10], Enumerable.Range(1, 16).Where(index.IsOmitted));
    }

    /// <summary>A diagnostic's location is placed by the one index (DESIGN-listing.md section 6.3): one located from a resultant line
    /// of the index's own frame in the block that line belongs to (A's line 1 from r10 in the copy inside B, listing line 9; from r3
    /// in the first copy, line 3); the same physical place with no resultant line, or located through another map's numbering (a
    /// text stage's own frame), in the first text that lists it; a line no listed text has, nowhere.</summary>
    [Fact]
    public void ALocation_IsPlacedInTheBlockOfItsResultantLine_OnlyInTheIndexsFrame()
    {
        var (index, map) = IndexAndMap();
        Assert.Equal(9, index.Place(map.Locate(10, 0, S)));
        Assert.Equal(3, index.Place(map.Locate(3, 0, S)));
        Assert.Equal(10, index.Place(map.Locate(11, 0, S)));
        Assert.Equal(3, index.Place(new SourceLocation(A, 0, 0, 0)));
        var anotherFrame = new SourceLineMap([.. Enumerable.Range(1, 16).Select(r => new LineOrigin(map.Origin(r)!.Value))]);
        Assert.NotEqual(map.Frame, anotherFrame.Frame);
        Assert.Equal(3, index.Place(anotherFrame.Locate(10, 0, S)));
        Assert.Equal(12, index.Place(new SourceLocation(S, 0, 4, 0)));
        Assert.Equal(7, index.Place(new SourceLocation(B, 0, 0, 0)));
        Assert.Null(index.Place(new SourceLocation("other.cpy", 0, 0, 0)));
        Assert.Null(index.Place(new SourceLocation(A, 0, 6, 0)));
    }
}