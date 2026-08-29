// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using CobolNet.Runtime.Exceptions;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// DESIGN-locale-facility §10 T-C 4 — "every EC-LOCALE-* name registered in ExceptionCatalog has at least one
/// raise site": the standing answer to "registered but never raised". EC-LOCALE-SIZE sat registered with ZERO
/// raise sites from T1 until PB64 T6 gave it one (<c>CobolLocaleEdit.Format</c>'s §13.18.40.5 r14 b) branch), so
/// this landed WITH an empty exemption list — nothing is grandfathered.
/// <para>Mechanics: a raise site in this codebase is a call to the EC's <c>ExceptionEngine.&lt;Name&gt;Error</c>
/// method (the ONE throw discipline — every ambient gate goes through its named method), so the drift axis
/// checked here is that each registered level-3 EC-LOCALE name HAS such a method and a matching checking flag.
/// The method-has-a-caller half is covered by the behavior tests that FIRE each condition (risk R5):
/// EC-LOCALE-SIZE in <c>CobolLocaleEditTests</c>, MISSING/INVALID/INVALID-PTR/INCOMPATIBLE in the T1/T4/T5
/// goldens. EC-LOCALE-IMP is the implementor's level-3 escape (§14.6.13.1's *-IMP row, raisable via RAISE) and
/// is exempt from the method rule by that structure — the ONE deliberate carve-out, stated here rather than in
/// a side list.</para>
/// </summary>
public sealed class EcRaiseSiteDriftTests
{
    [Fact]
    public void EveryRegisteredLocaleEc_HasItsRaiseMethodAndCheckingFlag()
    {
        var engine = typeof(ExceptionState).Assembly.GetType("CobolNet.Runtime.Exceptions.ExceptionEngine")
            ?? typeof(ExceptionState);   // the engine type carries the instance members; ExceptionState forwards
        int checkedCount = 0;
        foreach (var ec in ExceptionCatalog.Level3Rows)
        {
            if (!ec.Name.StartsWith("EC-LOCALE", StringComparison.Ordinal)) continue;
            if (ec.Name == "EC-LOCALE-IMP") continue;   // the §14.6.13.1 *-IMP row — RAISE-only, no ambient gate
            // EC-LOCALE-MISSING → LocaleMissingError / LocaleMissingChecking, etc.
            string pascal = string.Concat(ec.Name["EC-".Length..].Split('-')
                .Select(part => char.ToUpperInvariant(part[0]) + part[1..].ToLowerInvariant()));
            Assert.True(FindMember(pascal + "Error"), $"{ec.Name} has no {pascal}Error raise method — registered but never raisable");
            Assert.True(FindMember(pascal + "Checking"), $"{ec.Name} has no {pascal}Checking flag");
            checkedCount++;
        }
        Assert.True(checkedCount >= 5, $"only {checkedCount} EC-LOCALE conditions checked — the catalog sweep is broken");

        bool FindMember(string name) =>
            typeof(ExceptionState).GetMember(name, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance).Length > 0
            || engine.GetMember(name, BindingFlags.Public | BindingFlags.Instance).Length > 0;
    }
}
