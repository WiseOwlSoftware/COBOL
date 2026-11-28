// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Listing;
using WiseOwl.Cobol.Compiler.Listing;
using WiseOwl.Cobol.Listing.Text;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit.Listing;

/// <summary>
/// The layout of a method's exception-handling structure in the object code (DESIGN-listing.md section 6.9, kb/Work PB5951), over
/// hand-built clauses. The expected lines are written by hand from ECMA-335: the textual form and its order are II.19's (a protected
/// block's <c>.try</c>, then each handler of the clauses that protect it; a filter clause's filter block before its handler), the
/// nesting is I.12.4.2.7's (two blocks are disjoint or one contains the other; a clause nested in another comes first in the body's
/// table, II.25.4.6), and an offset is the boundary's own. Nothing is read off a listing to be compared with itself.
/// </summary>
public sealed class ExceptionRegionLayoutTests
{
    private static ListingExceptionRegion Catch(int tryStart, int tryEnd, int handlerStart, int handlerEnd, string type) =>
        new(ListingRegionKind.Catch, tryStart, tryEnd, handlerStart, handlerEnd, null, type);

    private static ListingExceptionRegion Finally(int tryStart, int tryEnd, int handlerStart, int handlerEnd) =>
        new(ListingRegionKind.Finally, tryStart, tryEnd, handlerStart, handlerEnd, null, null);

    private static ListingExceptionRegion Fault(int tryStart, int tryEnd, int handlerStart, int handlerEnd) =>
        new(ListingRegionKind.Fault, tryStart, tryEnd, handlerStart, handlerEnd, null, null);

    private static ListingExceptionRegion Filter(int tryStart, int tryEnd, int filterStart, int handlerStart, int handlerEnd) =>
        new(ListingRegionKind.Filter, tryStart, tryEnd, handlerStart, handlerEnd, filterStart, null);

    private static ListingRegionMark Mark(int offset, ListingRegionBoundary boundary, int region, int depth) => new(offset, boundary, region, depth);

    /// <summary>C#'s <c>try { try { } catch (A) { } } finally { }</c>: the inner clause first in the table (II.25.4.6). At 0 both blocks
    /// open, the outer first; at 10 the inner try closes and its catch opens; at 20 the catch closes, then the outer try (innermost first),
    /// then the finally opens; it closes at 30.</summary>
    [Fact]
    public void NestedRegions_OpenOutermostFirst_AndCloseInnermostFirst()
    {
        var regions = new[] { Catch(0, 10, 10, 20, "A"), Finally(0, 20, 20, 30) };
        Assert.Equal(
        [
            Mark(0, ListingRegionBoundary.TryStart, 2, 0),
            Mark(0, ListingRegionBoundary.TryStart, 1, 1),
            Mark(10, ListingRegionBoundary.TryEnd, 1, 1),
            Mark(10, ListingRegionBoundary.HandlerStart, 1, 1),
            Mark(20, ListingRegionBoundary.HandlerEnd, 1, 1),
            Mark(20, ListingRegionBoundary.TryEnd, 2, 0),
            Mark(20, ListingRegionBoundary.HandlerStart, 2, 0),
            Mark(30, ListingRegionBoundary.HandlerEnd, 2, 0),
        ], ExceptionRegionLayout.Marks(regions, "T::M"));
    }

    /// <summary>Two clauses one after the other, the second's protected block starting where the first's handler ends (8), and a third
    /// after a gap: at a shared offset the block ending closes before the block starting opens, and every line is at depth 0.</summary>
    [Fact]
    public void AdjacentRegions_CloseBeforeTheNextOpens()
    {
        var regions = new[] { Catch(0, 5, 5, 8, "A"), Finally(8, 12, 12, 14), Fault(20, 22, 22, 24) };
        Assert.Equal(
        [
            Mark(0, ListingRegionBoundary.TryStart, 1, 0),
            Mark(5, ListingRegionBoundary.TryEnd, 1, 0),
            Mark(5, ListingRegionBoundary.HandlerStart, 1, 0),
            Mark(8, ListingRegionBoundary.HandlerEnd, 1, 0),
            Mark(8, ListingRegionBoundary.TryStart, 2, 0),
            Mark(12, ListingRegionBoundary.TryEnd, 2, 0),
            Mark(12, ListingRegionBoundary.HandlerStart, 2, 0),
            Mark(14, ListingRegionBoundary.HandlerEnd, 2, 0),
            Mark(20, ListingRegionBoundary.TryStart, 3, 0),
            Mark(22, ListingRegionBoundary.TryEnd, 3, 0),
            Mark(22, ListingRegionBoundary.HandlerStart, 3, 0),
            Mark(24, ListingRegionBoundary.HandlerEnd, 3, 0),
        ], ExceptionRegionLayout.Marks(regions, "T::M"));
    }

    /// <summary>Three clauses protecting one block (two catches and a filter, C#'s <c>catch (A) catch (B) catch when</c>): one
    /// <c>.try</c>, named by the first clause, then each handler in the table's order, the filter clause's filter block before its
    /// handler.</summary>
    [Fact]
    public void ClausesSharingAProtectedBlock_PrintOneTry_ThenEachHandlerInOrder()
    {
        var regions = new[] { Catch(0, 10, 10, 14, "A"), Catch(0, 10, 14, 18, "B"), Filter(0, 10, 18, 22, 26) };
        var marks = ExceptionRegionLayout.Marks(regions, "T::M");
        Assert.Equal(
        [
            Mark(0, ListingRegionBoundary.TryStart, 1, 0),
            Mark(10, ListingRegionBoundary.TryEnd, 1, 0),
            Mark(10, ListingRegionBoundary.HandlerStart, 1, 0),
            Mark(14, ListingRegionBoundary.HandlerEnd, 1, 0),
            Mark(14, ListingRegionBoundary.HandlerStart, 2, 0),
            Mark(18, ListingRegionBoundary.HandlerEnd, 2, 0),
            Mark(18, ListingRegionBoundary.FilterStart, 3, 0),
            Mark(22, ListingRegionBoundary.FilterEnd, 3, 0),
            Mark(22, ListingRegionBoundary.HandlerStart, 3, 0),
            Mark(26, ListingRegionBoundary.HandlerEnd, 3, 0),
        ], marks);
        Assert.Equal([".try {", "} // end .try", "catch A {", "} // end handler", "catch B {", "} // end handler", "filter {",
            "} // end filter", "{ // handler", "} // end handler"], marks.Select(m => ObjectCodeText.RegionText(m, regions)));
    }

    /// <summary>A protected block nested in a handler (a try inside a finally): it opens one level deeper than the handler's line.</summary>
    [Fact]
    public void ARegionInsideAHandler_StandsOneLevelDeeper()
    {
        var regions = new[] { Fault(12, 14, 14, 16), Finally(0, 10, 10, 20) };
        Assert.Equal(
        [
            Mark(0, ListingRegionBoundary.TryStart, 2, 0),
            Mark(10, ListingRegionBoundary.TryEnd, 2, 0),
            Mark(10, ListingRegionBoundary.HandlerStart, 2, 0),
            Mark(12, ListingRegionBoundary.TryStart, 1, 1),
            Mark(14, ListingRegionBoundary.TryEnd, 1, 1),
            Mark(14, ListingRegionBoundary.HandlerStart, 1, 1),
            Mark(16, ListingRegionBoundary.HandlerEnd, 1, 1),
            Mark(20, ListingRegionBoundary.HandlerEnd, 2, 0),
        ], ExceptionRegionLayout.Marks(regions, "T::M"));
    }

    /// <summary>ECMA-335 I.12.4.2.7: blocks are disjoint or nested. A body whose blocks overlap is no structure ILDASM's form can print,
    /// and fails loudly, naming the method and both blocks; so does a block that covers no IL.</summary>
    [Fact]
    public void OverlappingBlocks_FailLoudly()
    {
        var overlap = Assert.Throws<InvalidDataException>(() =>
            ExceptionRegionLayout.Marks([Catch(0, 10, 10, 20, "A"), Catch(5, 15, 15, 25, "B")], "T::M"));
        Assert.Contains("T::M", overlap.Message);
        Assert.Throws<InvalidDataException>(() => ExceptionRegionLayout.Marks([Finally(4, 4, 4, 6)], "T::M"));
    }

    /// <summary>A body with no clause has no lines.</summary>
    [Fact]
    public void ABodyWithNoClause_HasNoLines() => Assert.Empty(ExceptionRegionLayout.Marks([], "T::M"));

    /// <summary>Which stretch prints each line (section 6.9): an opening line, the one holding its offset; a closing line, the one holding
    /// the block's last instruction (one at the end of the body, the stretch ending there); one whose code no stretch holds, the next
    /// stretch, before its first instruction (here the <c>.try</c> at 0, before the method's first visible point at 2, and the lines at
    /// 16 after the gap 14-16 between the stretches 10-14 and 20-30, which the map left a stretch out of); and each stretch begins at the
    /// depth the earlier stretches' lines leave.</summary>
    [Fact]
    public void EachLine_PrintsInOneStretch_AndEachStretchKnowsItsDepth()
    {
        // try 0-16 { ... } finally 16-30; the stretches 2-10, 10-14 and 20-30 (16-20 is a stretch the map does not place).
        var regions = new[] { Finally(0, 16, 16, 30) };
        var marks = ExceptionRegionLayout.Marks(regions, "T::M");
        var (lines, depths) = ExceptionRegionLayout.Place([(2, 10), (10, 14), (20, 30)], marks, 30);
        Assert.Equal([Mark(0, ListingRegionBoundary.TryStart, 1, 0)], lines[0]);
        Assert.Empty(lines[1]);
        Assert.Equal([Mark(16, ListingRegionBoundary.TryEnd, 1, 0), Mark(16, ListingRegionBoundary.HandlerStart, 1, 0),
            Mark(30, ListingRegionBoundary.HandlerEnd, 1, 0)], lines[2]);
        Assert.Equal([0, 1, 1], depths);   // before the .try; inside it; inside it, before its end prints

        // Code after the last stretch: its lines print after that stretch's last instruction.
        var (tail, _) = ExceptionRegionLayout.Place([(0, 4)], ExceptionRegionLayout.Marks([Finally(6, 8, 8, 10)], "T::M"), 12);
        Assert.Equal(4, tail[0].Count);
    }

    /// <summary>A block inside one stretch opens and closes in it, its closing line at the stretch's end printing after the stretch's
    /// last instruction, never at the top of the next stretch (PB5951 review round 1: an OPEN's <c>finally</c> left its stretch unclosed
    /// and the next statement's stretch began with a close); a block ending at a stretch boundary closes in the stretch before it, and
    /// one spanning a boundary leaves the next stretch inside it.</summary>
    [Fact]
    public void ABlockClosesInTheStretchHoldingItsLastInstruction()
    {
        // try 0-4 finally 4-8, all in the stretch 0-8; the stretch 8-12 holds none of it and begins outside every block.
        var inside = ExceptionRegionLayout.Marks([Finally(0, 4, 4, 8)], "T::M");
        var (lines, depths) = ExceptionRegionLayout.Place([(0, 8), (8, 12)], inside, 12);
        Assert.Equal(inside, lines[0]);
        Assert.Empty(lines[1]);
        Assert.Equal([0, 0], depths);

        // try 0-10 in the stretch 0-10, its handler 10-20 in the stretch 10-20: the try closes after the first stretch's last
        // instruction, the handler opens in the second, and the second begins outside every block.
        var boundary = ExceptionRegionLayout.Marks([Catch(0, 10, 10, 20, "E")], "T::M");
        (lines, depths) = ExceptionRegionLayout.Place([(0, 10), (10, 20)], boundary, 20);
        Assert.Equal([Mark(0, ListingRegionBoundary.TryStart, 1, 0), Mark(10, ListingRegionBoundary.TryEnd, 1, 0)], lines[0]);
        Assert.Equal([Mark(10, ListingRegionBoundary.HandlerStart, 1, 0), Mark(20, ListingRegionBoundary.HandlerEnd, 1, 0)], lines[1]);
        Assert.Equal([0, 0], depths);

        // try 0-15 spans the stretches 0-10 and 10-20: the second begins inside it.
        var spanning = ExceptionRegionLayout.Marks([Finally(0, 15, 15, 20)], "T::M");
        (lines, depths) = ExceptionRegionLayout.Place([(0, 10), (10, 20)], spanning, 20);
        Assert.Equal([Mark(0, ListingRegionBoundary.TryStart, 1, 0)], lines[0]);
        Assert.Equal(3, lines[1].Count);
        Assert.Equal([0, 1], depths);
    }

    /// <summary>The text object code (section 6.9): each region line before the first instruction at or after its offset, the lines past
    /// the last instruction after it, each indented two columns per level under the stretch's heading, and each instruction at the depth
    /// the lines before it leave, from the stretch's own depth.</summary>
    [Fact]
    public void TheTextObjectCode_PrintsTheRegionLines_AmongTheInstructions()
    {
        var regions = new[] { Catch(0, 4, 4, 6, "System.Exception"), Finally(0, 6, 6, 7) };
        var marks = ExceptionRegionLayout.Marks(regions, "T::M");
        var map = new ListingObjectCodeMap("p.dll", [new ListingMethod(1, 0x06000001, "P", "M", 8, regions)],
            [new ListingObjectRange(1, 12, 1, 0, 8,
                [new ListingInstruction(0, "nop", null), new ListingInstruction(2, "leave.s", "IL_0006"), new ListingInstruction(4, "pop", null),
                 new ListingInstruction(5, "leave.s", "IL_0008"), new ListingInstruction(6, "endfinally", null), new ListingInstruction(7, "ret", null)],
                marks, 0)]);
        string pad = new(' ', 13);
        Assert.Equal(
        [
            "     1   12  P::M  IL_0000-IL_0008",
            pad + ".try {",
            pad + "  .try {",
            pad + "    IL_0000  nop",
            pad + "    IL_0002  leave.s      IL_0006",
            pad + "  } // end .try",
            pad + "  catch System.Exception {",
            pad + "    IL_0004  pop",
            pad + "    IL_0005  leave.s      IL_0008",
            pad + "  } // end handler",
            pad + "} // end .try",
            pad + "finally {",
            pad + "  IL_0006  endfinally",
            pad + "} // end handler",
            pad + "IL_0007  ret",
        ], ObjectCodeText.CodeRows(map, [], 132));
    }

    /// <summary>A stretch that begins inside a block its method opened in an earlier stretch prints its instructions at that depth.</summary>
    [Fact]
    public void AStretchInsideABlock_StartsAtItsDepth()
    {
        var range = new ListingObjectRange(1, 12, 1, 4, 6, [new ListingInstruction(4, "nop", null)], [], 2);
        Assert.Equal([(2, "nop")], ObjectCodeText.CodeLines(range).Select(l => (l.Depth, l.Instruction!.Value.OpCode)));
    }
}
