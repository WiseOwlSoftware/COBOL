// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE SCOPE OF A PROGRAM-NAME (ISO §2023:8.4.6.3) IS WRITTEN ONCE, in <see cref="ProgramNameScope.FirstVisible"/>, its arms in
/// their order with the COMMON-program exception, and EVERY scope reader asks it (kb/Work PB1460, PB5965): the run-time resolver
/// (<c>ProgramTable.ResolveVisible</c>), the compiler's literal-target scope the listing's CALL graph reads
/// (<c>BinderDriver.ProgramVisibleFrom</c>) and the AS NESTED table (<c>BinderDriver.NestedCallablesOf</c>). The bind-time table used
/// to admit every COMMON child of every ancestor, so a CALL the resolver would never satisfy compiled clean; the three copies of the
/// arms could drift the same way.
/// </summary>
[Trait("Sweep", "tree")]
public sealed class ProgramNameScopeDriftTests
{
    private sealed record P(string Name, P? Container);

    /// <summary>The rule by value over a generated containment chain: OUTER ⊃ Q (common) ⊃ Q1 ⊃ Q2, and a sibling S.</summary>
    [Theory]
    [InlineData("S", false, true)]    // a program elsewhere in the container: always referable
    [InlineData("OUTER", false, true)]
    [InlineData("Q", false, false)]   // the common program itself …
    [InlineData("Q1", false, false)]  // … and every program within it: only when RECURSIVE
    [InlineData("Q2", false, false)]
    [InlineData("Q", true, true)]
    [InlineData("Q2", true, true)]
    public void CommonProgramException_ByValue(string caller, bool recursive, bool referable)
    {
        var outer = new P("OUTER", null);
        var q = new P("Q", outer);
        var q1 = new P("Q1", q);
        var progs = new Dictionary<string, P>
        {
            ["OUTER"] = outer, ["Q"] = q, ["Q1"] = q1, ["Q2"] = new("Q2", q1), ["S"] = new("S", outer),
        };
        Assert.Equal(referable, ProgramNameScope.CommonProgramReferable(q, recursive, progs[caller], p => p.Container));
    }

    private sealed class N(string name, bool common = false, bool recursive = false)
    {
        public string Name { get; } = name;
        public bool Common { get; } = common;
        public bool Recursive { get; } = recursive;
        public N? Container { get; private set; }
        public List<N> Children { get; } = [];
        public N Add(N child) { child.Container = this; Children.Add(child); return child; }
    }

    /// <summary>⛔ kb/Work PB5965: the arms of the whole rule by value, in their order. OUTER ⊃ {C (common), K (common), D ⊃ {K}, R
    /// (recursive)}; a contained program wins over a COMMON one of the same name (arm 1 before arm 3), the nearest container first.</summary>
    [Theory]
    [InlineData("OUTER", "D", "D", ProgramNameArm.Contained)]
    [InlineData("D", "C", "C", ProgramNameArm.Common)]
    [InlineData("C", "C", null, null)]                         // the COMMON exception: C is not recursive
    [InlineData("R", "R", "R", ProgramNameArm.RecursiveSelf)]
    [InlineData("D", "D", null, null)]                         // D is not recursive and not COMMON
    [InlineData("D", "K", "D.K", ProgramNameArm.Contained)]    // D's own K, before OUTER's COMMON K
    [InlineData("R", "K", "K", ProgramNameArm.Common)]
    public void FirstVisible_AsksTheArmsInOrder(string caller, string name, string? reached, ProgramNameArm? arm)
    {
        var outer = new N("OUTER");
        var progs = new Dictionary<string, N> { ["OUTER"] = outer };
        progs["C"] = outer.Add(new N("C", common: true));
        progs["K"] = outer.Add(new N("K", common: true));
        progs["D"] = outer.Add(new N("D"));
        progs["D.K"] = progs["D"].Add(new N("K"));
        progs["R"] = outer.Add(new N("R", recursive: true));
        ProgramNameArm? seen = null;
        var found = ProgramNameScope.FirstVisible(progs[caller], (n, a) => { if (n.Name != name) return false; seen = a; return true; },
            n => n.Children, n => n.Container, n => n.Recursive, n => n.Common);
        Assert.Same(reached is null ? null : progs[reached], found);
        Assert.Equal(arm, seen);
    }

    /// <summary>Every scope reader asks the one rule and none re-spells an arm: the run-time resolver once, the compiler's literal scope
    /// and AS NESTED table once each (kb/Work PB5965); the COMMON exception is asked only inside it.</summary>
    [Theory]
    [InlineData("WiseOwl.Cobol.Compiler", "Binding", "BinderDriver.cs", 2)]
    [InlineData("WiseOwl.Cobol.Runtime", "Control", "ProgramTable.cs", 1)]
    public void EveryScope_AsksTheOneRule(string project, string dir, string file, int readers)
    {
        string text = File.ReadAllText(Path.Combine(TestRepo.Root, "src", project, dir, file));
        Assert.Equal(readers, text.Split("ProgramNameScope.FirstVisible(").Length - 1);
        Assert.DoesNotContain("CommonProgramReferable(", text);
        Assert.DoesNotContain("onOwnChain", text);
    }
}
