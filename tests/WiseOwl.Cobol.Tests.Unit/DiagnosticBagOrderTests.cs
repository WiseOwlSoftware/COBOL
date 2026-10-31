// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Common;
using WiseOwl.Cobol.Frontend.Diagnostics;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// <see cref="DiagnosticBag.AddBeforeEffects"/> places each cause before the first effect it can explain, a tie in position
/// going to the cause, and keeps the causes that share an effect in the order given (kb/Work PB4200). The order a
/// compilation prints its diagnostics in is this compiler's presentation choice, so the expectations here are the documented
/// contract of the method, each computed by hand from the positions written.
/// </summary>
public sealed class DiagnosticBagOrderTests
{
    private static Diagnostic At(string code, string file, int line, int column) =>
        new(code, DiagnosticSeverity.Error, code, new SourceLocation(file, 0, line, column), default);

    private static string[] Order(DiagnosticBag bag) => [.. bag.Diagnostics.Select(d => d.Code)];

    private static DiagnosticBag Holding(params Diagnostic[] effects)
    {
        var bag = new DiagnosticBag();
        foreach (var effect in effects) bag.Add(effect);
        return bag;
    }

    [Fact]
    public void TwoCausesAtOnePosition_KeepTheOrderGiven_BeforeTheEffectTheyTieWith()
    {
        var bag = Holding(At("E1", "a.cob", 7, 5));
        bag.AddBeforeEffects([At("C1", "a.cob", 7, 5), At("C2", "a.cob", 7, 5)], 0);
        Assert.Equal(["C1", "C2", "E1"], Order(bag));
    }

    [Fact]
    public void ThreeCausesAtOnePosition_KeepTheOrderGiven()
    {
        var bag = Holding(At("E1", "a.cob", 7, 5));
        bag.AddBeforeEffects([At("C1", "a.cob", 6, 1), At("C2", "a.cob", 6, 1), At("C3", "a.cob", 6, 1)], 0);
        Assert.Equal(["C1", "C2", "C3", "E1"], Order(bag));
    }

    [Fact]
    public void CausesWithNoEffectAfterThem_AreAppendedInTheOrderGiven()
    {
        var bag = Holding(At("E1", "a.cob", 2, 1));
        bag.AddBeforeEffects([At("C1", "a.cob", 9, 1), At("C2", "a.cob", 9, 1)], 0);
        Assert.Equal(["E1", "C1", "C2"], Order(bag));
    }

    [Fact]
    public void CausesAtDifferentPositions_EachReadsBeforeItsOwnEffect()
    {
        var bag = Holding(At("E1", "a.cob", 3, 1), At("E2", "a.cob", 8, 1));
        bag.AddBeforeEffects([At("C1", "a.cob", 3, 1), At("C2", "a.cob", 8, 1), At("C3", "a.cob", 8, 1)], 0);
        Assert.Equal(["C1", "E1", "C2", "C3", "E2"], Order(bag));
    }

    [Fact]
    public void ACauseGivenOutOfSourceOrder_StillReadsBeforeItsOwnEffect()
    {
        var bag = Holding(At("E1", "a.cob", 3, 1), At("E2", "a.cob", 8, 1));
        bag.AddBeforeEffects([At("C2", "a.cob", 8, 1), At("C1", "a.cob", 3, 1)], 0);
        Assert.Equal(["C1", "E1", "C2", "E2"], Order(bag));
    }

    [Fact]
    public void AnEffectInAnotherFile_IsNeverCompared()
    {
        var bag = Holding(At("E1", "a.cob", 1, 1), At("X", "b.cpy", 99, 1), At("E2", "a.cob", 9, 1));
        bag.AddBeforeEffects([At("C1", "a.cob", 5, 1), At("C2", "a.cob", 5, 1)], 0);
        Assert.Equal(["E1", "X", "C1", "C2", "E2"], Order(bag));
    }

    [Fact]
    public void AnEffectBeforeFrom_IsNeverMovedPast()
    {
        var bag = Holding(At("OLD", "a.cob", 9, 1), At("E1", "a.cob", 9, 1));
        bag.AddBeforeEffects([At("C1", "a.cob", 1, 1), At("C2", "a.cob", 1, 1)], 1);
        Assert.Equal(["OLD", "C1", "C2", "E1"], Order(bag));
    }

    [Fact]
    public void NoCauses_LeavesTheBagAsItWas()
    {
        var bag = Holding(At("E1", "a.cob", 1, 1), At("E2", "a.cob", 2, 1));
        bag.AddBeforeEffects([], 0);
        Assert.Equal(["E1", "E2"], Order(bag));
    }
}
