// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.Exceptions;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The Format-3 (exception-checking) PERFORM interceptor frame stack (ISO/IEC 1989:2023 §14.9.28.4 GR17–GR22) —
/// the runtime-additive half (Increment 1 of the pc-range interceptor, design SSOT
/// <c>PHASE-13-c5-perform-format3-DESIGN.md</c> §9.2). Exercises <see cref="ExceptionEngine.RunTopFrame"/>'s
/// innermost→outermost walk, GR17 fall-through to an outer PERFORM, GR18 WHEN OTHER as the matcher's own fallback,
/// GR21 transparency (a handling frame does not re-catch its own re-raise), and — the subtle one — the DEFERRED
/// <see cref="PerformFrame.Handling"/> clear that keeps a skipped inner frame transparent while a selected OUTER
/// handler runs (a suspended inner imp-1 must not re-catch). No generated code / no RunUnit needed — the engine is
/// directly constructible and the frame surface is public.
/// </summary>
public sealed class PerformFrameStackTests
{
    private static PerformFrame Match(string ec, int action) =>
        new() { Matcher = (raised, _) => raised == ec ? action : PerformFrame.NoMatch };

    [Fact]
    public void EmptyStack_ReturnsNoMatch_NotHandled()
    {
        var e = new ExceptionEngine();
        int a = e.RunTopFrame("EC-BOUND-SUBSCRIPT", null, out bool handled);
        Assert.False(handled);
        Assert.Equal(PerformFrame.NoMatch, a);
    }

    [Fact]
    public void SingleFrameMatch_ReturnsActionAndHandled()
    {
        var e = new ExceptionEngine();
        e.PushPerformFrame(Match("EC-BOUND-SUBSCRIPT", -1));
        int a = e.RunTopFrame("EC-BOUND-SUBSCRIPT", null, out bool handled);
        Assert.True(handled);
        Assert.Equal(-1, a);   // handled, continue (GR20 nonfatal / fatal-terminate decided at the raise site)
    }

    [Fact]
    public void SingleFrameNoMatch_FallsThroughToUse()
    {
        var e = new ExceptionEngine();
        e.PushPerformFrame(Match("EC-BOUND-SUBSCRIPT", -1));
        int a = e.RunTopFrame("EC-SIZE-OVERFLOW", null, out bool handled);
        Assert.False(handled);                       // → caller runs __EcDispatch (USE), GR17 "otherwise"
        Assert.Equal(PerformFrame.NoMatch, a);
    }

    [Fact]
    public void InnermostFrameWins_OuterNotConsulted()
    {
        var e = new ExceptionEngine();
        bool outerRan = false;
        e.PushPerformFrame(new PerformFrame { Matcher = (_, _) => { outerRan = true; return -1; } });   // outer
        e.PushPerformFrame(Match("EC-BOUND-SUBSCRIPT", -2));                                             // inner (top)
        int a = e.RunTopFrame("EC-BOUND-SUBSCRIPT", null, out bool handled);
        Assert.True(handled);
        Assert.Equal(-2, a);           // the inner PERFORM (its imp-1 is executing) handles — GR17
        Assert.False(outerRan);        // the outer frame is never consulted once the inner matches
    }

    [Fact]
    public void InnerNoMatch_FallsToOuterFrame()
    {
        // An EC raised in the inner PERFORM's imp-1 that only the OUTER PERFORM's WHEN names must reach the outer
        // handler (GR17 applies to whichever PERFORM's imp-1 is executing; the inner simply does not name it).
        var e = new ExceptionEngine();
        e.PushPerformFrame(Match("EC-SIZE-OVERFLOW", 7));      // outer names EC-SIZE-OVERFLOW → pc 7
        e.PushPerformFrame(Match("EC-BOUND-SUBSCRIPT", -1));   // inner (top) names only EC-BOUND-SUBSCRIPT
        int a = e.RunTopFrame("EC-SIZE-OVERFLOW", null, out bool handled);
        Assert.True(handled);
        Assert.Equal(7, a);
    }

    [Fact]
    public void ReRaiseInsideOwnHandler_NotReCaught_Gr21()
    {
        // GR21: an exception condition raised during a WHEN handler (imp-2) is NOT re-caught by the same PERFORM.
        // The frame is marked Handling while its matcher runs, so the re-entrant RunTopFrame skips it.
        var e = new ExceptionEngine();
        int reAction = 123;
        bool reHandled = true;
        e.PushPerformFrame(new PerformFrame
        {
            Matcher = (raised, _) =>
            {
                if (raised != "EC-BOUND-SUBSCRIPT") return PerformFrame.NoMatch;
                reAction = e.RunTopFrame("EC-BOUND-SUBSCRIPT", null, out reHandled);   // re-raise inside imp-2
                return -1;
            }
        });
        int a = e.RunTopFrame("EC-BOUND-SUBSCRIPT", null, out bool handled);
        Assert.True(handled);
        Assert.Equal(-1, a);
        Assert.False(reHandled);                         // GR21 — the same PERFORM did not re-catch
        Assert.Equal(PerformFrame.NoMatch, reAction);
    }

    [Fact]
    public void SkippedInnerFrame_StaysTransparent_WhileOuterHandlerRuns_DeferredClear()
    {
        // The load-bearing nesting case (distinguishes the correct DEFERRED Handling-clear from an eager clear):
        // stack [outer, inner]; raise EC-X → inner does not name it, outer does. While OUTER's handler runs it
        // re-raises EC-INNER, which inner WOULD name — but inner's imp-1 is SUSPENDED (we are deep in a raise from
        // it), so inner must NOT re-catch. Because RunTopFrame marks every frame it visits Handling for the whole
        // resolution, both inner and outer are Handling during outer's handler ⇒ the re-raise falls to USE.
        var e = new ExceptionEngine();
        int reAction = 42;
        bool reHandled = true;
        e.PushPerformFrame(new PerformFrame     // outer (pushed first)
        {
            Matcher = (raised, _) =>
            {
                if (raised != "EC-X") return PerformFrame.NoMatch;
                reAction = e.RunTopFrame("EC-INNER", null, out reHandled);   // re-raise inside OUTER's handler
                return -1;
            }
        });
        e.PushPerformFrame(Match("EC-INNER", -1));   // inner (top) names EC-INNER
        int a = e.RunTopFrame("EC-X", null, out bool handled);
        Assert.True(handled);
        Assert.Equal(-1, a);
        Assert.False(reHandled);                     // inner did NOT catch EC-INNER (deferred clear kept it Handling)
        Assert.Equal(PerformFrame.NoMatch, reAction);
    }

    [Fact]
    public void PushPop_Balances()
    {
        var e = new ExceptionEngine();
        e.PushPerformFrame(Match("EC-BOUND-SUBSCRIPT", -1));
        e.PopPerformFrame();
        int a = e.RunTopFrame("EC-BOUND-SUBSCRIPT", null, out bool handled);
        Assert.False(handled);
        Assert.Equal(PerformFrame.NoMatch, a);
    }

    [Fact]
    public void HandlingClearedAfterResolution_FrameReusable()
    {
        // After a full RunTopFrame resolution the frame's Handling flag is cleared, so a subsequent, independent
        // raise (the next statement in imp-1) is dispatched normally.
        var e = new ExceptionEngine();
        e.PushPerformFrame(Match("EC-BOUND-SUBSCRIPT", -1));
        Assert.True(e.RunTopFrame("EC-BOUND-SUBSCRIPT", null, out _) == -1);
        int a = e.RunTopFrame("EC-BOUND-SUBSCRIPT", null, out bool handled);   // second raise
        Assert.True(handled);
        Assert.Equal(-1, a);
    }
}
