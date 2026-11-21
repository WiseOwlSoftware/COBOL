// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System.Collections.Generic;
using System.Linq;
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Binding.Procedure;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// THE ONE REACH WALK FOLLOWS CONTROL AS IT RUNS (kb/Work PB6007, PB6008; <c>ProcedureReach.Walk</c>). A path that leaves a range's
/// procedures by GO TO and comes back is followed (the COBOL-85 segmentation restrictions asked the named span alone and dropped it,
/// PB6007), and the fall-through of a GO TO's destination ends where the range's return mechanism does ("When control passes the last
/// statement in the output procedure, the return mechanism provides for termination of the merge", §1985:XI.4.1.4 GR9), at an
/// unconditional GO TO or at STOP RUN, never at the end of the program, so a procedure written after the range is reached only when
/// a path flows into it (the RETURN placement rule depended on where an unreached section stood, PB6008). The paragraphs are built
/// directly: a paragraph's pc is its index.
/// </summary>
public sealed class ProcedureReachFlowTests
{
    private static BoundParagraph P(string name, params BoundStatement[] statements) => new(name, [statements]);

    /// <summary>s19_p16's shape: <c>PERFORM O30 THRU O60</c>, O30-1 going to O20-1 outside the span, O20-1 going back to O30-X,
    /// which falls into O60-1.</summary>
    private static readonly IReadOnlyList<BoundParagraph> OutAndBack =
    [
        P("M-1", new BoundGoTo(1)),     // 0
        P("MRG-1", new BoundStop()),    // 1
        P("O20-1", new BoundGoTo(4)),   // 2
        P("O30-1", new BoundGoTo(2)),   // 3
        P("O30-X"),                     // 4
        P("O60-1"),                     // 5
        P("AFTER"),                     // 6
    ];

    [Fact]
    public void FlowFromEntry_FollowsAPathThatLeavesTheRangeAndComesBack()
    {
        var reached = ProcedureReach.FlowFromEntry(OutAndBack, PcRange.Of(3, 5)).Order().ToList();
        Assert.Equal([2, 3, 4, 5], reached);
    }

    [Fact]
    public void FlowFromEntry_StopsAtTheRangesLastParagraph()
    {
        var reached = ProcedureReach.FlowFromEntry(OutAndBack, PcRange.Of(3, 4)).Order().ToList();
        Assert.Equal([2, 3, 4], reached);
    }

    [Fact]
    public void FlowFromEntry_DoesNotFallThroughAnUnconditionalGoTo()
    {
        // O60-1 goes to O62-1 past an unreached section, which goes to O61-1, the range's last paragraph.
        IReadOnlyList<BoundParagraph> paragraphs = [P("O60-1", new BoundGoTo(3)), P("B10-1"), P("O61-1"), P("O62-1", new BoundGoTo(2))];
        Assert.Equal([0, 2, 3], ProcedureReach.FlowFromEntry(paragraphs, PcRange.Of(0, 2)).Order().ToList());
    }

    [Fact]
    public void Closure_GoToTheRangesOwnExit_DoesNotReachTheProcedureWrittenAfterIt()
    {
        // s19_p17's shape: output procedure O20 (O20-1 going to O20-X), then section O99, which nothing reaches.
        IReadOnlyList<BoundParagraph> paragraphs = [P("M-1", new BoundStop()), P("O20-1", new BoundGoTo(2)), P("O20-X"), P("O99-1"), P("O99-X")];
        var reached = ProcedureReach.Closure(paragraphs, [(PcRange.Of(1, 2), 0)], followGoTo: true).Keys.Order().ToList();
        Assert.Equal([1, 2], reached);
    }

    [Fact]
    public void Closure_GoToOutOfTheRange_FollowsItsFallThroughUntilItTransfersAway()
    {
        // OUT-PROC O1 goes to H1 after it; H1 falls into H2, which goes back to O9; Z is reached by no path; STOP RUN ends S1's flow.
        IReadOnlyList<BoundParagraph> paragraphs =
        [
            P("M-1", new BoundStop()), P("O1", new BoundGoTo(3)), P("O9"), P("H1"), P("H2", new BoundGoTo(2)), P("Z"),
        ];
        var reached = ProcedureReach.Closure(paragraphs, [(PcRange.Of(1, 2), 0)], followGoTo: true).Keys.Order().ToList();
        Assert.Equal([1, 2, 3, 4], reached);

        IReadOnlyList<BoundParagraph> stopped = [P("O1", new BoundGoTo(2)), P("O9"), P("S1", new BoundStop()), P("Z")];
        Assert.Equal([0, 1, 2], ProcedureReach.Closure(stopped, [(PcRange.Of(0, 1), 0)], followGoTo: true).Keys.Order().ToList());
    }

    [Fact]
    public void Closure_WithoutGoTo_KeepsTheLexicalModel()
    {
        IReadOnlyList<BoundParagraph> paragraphs = [P("O1", new BoundGoTo(2)), P("O9"), P("H1"), P("H2")];
        Assert.Equal([0, 1], ProcedureReach.Closure(paragraphs, [(PcRange.Of(0, 1), 0)]).Keys.Order().ToList());
    }
}
