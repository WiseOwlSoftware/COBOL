// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using CobolNet.Binding;
using CobolNet.Runtime;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE BIND-TIME RENDERER NAMES A FIELD AND LETS C# OVERLOAD RESOLUTION SUPPLY THE CONVERSION — SO THE SET OF
/// CARRIERS IT MAY NAME IS THE RUNTIME METHOD'S OVERLOAD SET, AND NOTHING ELSE (kb/Work PB201).
/// </summary>
/// <remarks>
/// <para>
/// <c>ReferenceResolver.PositionRead</c> emits <c>CobolTable.Occ(&lt;field&gt;)</c> for a subscript and
/// <c>CobolString.RefModPosition(&lt;field&gt;, scale)</c> for a scaled reference-modification bound. That is a
/// deliberate design: the post-bind whole-group analysis has not yet chosen the field's storage form when the
/// text is produced, so ONE text has to serve a <c>long</c> field and the <c>string</c> image it may become, and
/// the C# compiler picks the conversion later. The bet is only good for carriers the method DECLARES a parameter
/// for. It did not hold: <c>DataItem.ElementType</c> also produces <c>double</c>/<c>float</c> (a COMP-1/COMP-2
/// leaf), <c>Int128</c>/<c>ulong</c>/<c>UInt128</c> (the &gt;18-digit and unsigned-binary tiers),
/// <c>ManagedPointer</c>, <c>ProgramPointer</c>, an object-reference type and a group's per-program
/// <c>record struct</c> — and every one of those emitted C# that did not compile (CS1503, or CS0103 for a
/// class-tier BASED group whose name is not a field at all).
/// </para>
/// <para>
/// ⚠ This is the drift guard the fix is paired with, not a restatement of it. The fix reads two lists; adding an
/// <c>Occ</c> overload without widening them leaves the fast path routing a carrier it could now render, and
/// widening them without the overload puts the CS1503 back. Both directions fail here. The THIRD assertion is
/// the two-arm one (<c>two_arm_dispatch</c>): <c>PositionRead</c> uses ONE carrier list for the subscript and the
/// reference-modification arms, so <c>CobolTable.Occ</c> and <c>CobolString.RefModPosition</c> must admit the
/// same scaled carriers — a carrier added to one and not the other would make the single list wrong for
/// whichever arm lost the race.
/// </para>
/// <para>
/// PROVEN TO FAIL, both directions, before being trusted: adding <c>"double"</c> to
/// <c>ReferenceResolver.UnscaledPositionCarriers</c> made the first assertion red naming "double" as the extra
/// carrier, and deleting <c>CobolString.RefModPosition(Int128,int)</c> made the two-arm one red.
/// </para>
/// </remarks>
public sealed class PositionCarrierOverloadDriftTests
{
    /// <summary>The <c>DataItem.ElementType</c> spelling of a CLR type — the C# keyword where one exists, the
    /// type name otherwise. Written HERE rather than reused from the compiler so the test is an independent
    /// second opinion about the spelling, not an echo of it.</summary>
    private static string Spelling(Type t) => t == typeof(long) ? "long"
        : t == typeof(ulong) ? "ulong"
        : t == typeof(string) ? "string"
        : t == typeof(double) ? "double"
        : t == typeof(float) ? "float"
        : t == typeof(decimal) ? "decimal"
        : t == typeof(object) ? "object"
        : t.Name;

    /// <summary>The first-parameter spellings of every public static overload of <paramref name="name"/> on
    /// <paramref name="host"/> whose parameter count is <paramref name="arity"/>.</summary>
    private static SortedSet<string> FirstParameterCarriers(Type host, string name, int arity) =>
        new(host.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name == name && m.GetParameters().Length == arity)
                .Select(m => Spelling(m.GetParameters()[0].ParameterType)));

    /// <summary>An UNSCALED position operand renders <c>CobolTable.Occ(path)</c>, so the carriers the fast path
    /// may name are exactly the one-argument overloads' parameter types.</summary>
    [Fact]
    public void UnscaledCarriers_AreExactlyTheOneArgumentOccOverloads()
    {
        var declared = new SortedSet<string>(ReferenceResolver.UnscaledPositionCarriers);
        var actual = FirstParameterCarriers(typeof(CobolTable), nameof(CobolTable.Occ), 1);
        Assert.True(declared.SetEquals(actual),
            $"ReferenceResolver.UnscaledPositionCarriers = [{string.Join(", ", declared)}] but "
            + $"CobolTable.Occ(x) accepts [{string.Join(", ", actual)}]. A carrier declared here with no overload "
            + "puts the CS1503 back (kb/Work PB201); an overload with no entry here leaves the fast path routing "
            + "work it could now render.");
    }

    /// <summary>A SCALED position operand renders <c>CobolTable.Occ(path, scale)</c> — the arity that carries the
    /// §8.4.2.3.4 GR1b integrality test — so the scaled carrier list is the two-argument overload set.
    /// <para>⚠ It is deliberately NOT nested with the unscaled list in either direction, and the asymmetry is the
    /// rule rather than an oversight. <c>Int128</c> must be here because the D18 §15.4 segment temp is a 30-digit
    /// / scale-9 item and <c>MaterializeViaFragment</c> reads it back through this very method; the unsigned
    /// BINARY-CAPACITY carriers are unscaled-only because a scaled operand HAS an integrality question and the
    /// D18 route is the right place to answer it. Asserting a subset relation either way would therefore pin an
    /// accident, not a rule — so the only assertion is the one that matters: each list is exactly its own
    /// arity's overload set.</para></summary>
    [Fact]
    public void ScaledCarriers_AreExactlyTheTwoArgumentOccOverloads()
    {
        var declared = new SortedSet<string>(ReferenceResolver.ScaledPositionCarriers);
        var actual = FirstParameterCarriers(typeof(CobolTable), nameof(CobolTable.Occ), 2);
        Assert.True(declared.SetEquals(actual),
            $"ReferenceResolver.ScaledPositionCarriers = [{string.Join(", ", declared)}] but "
            + $"CobolTable.Occ(x, int) accepts [{string.Join(", ", actual)}].");
    }

    /// <summary>⛔ NO FLOAT CARRIER AT EITHER ARITY, AND THIS IS A CONFORMANCE RULE, NOT A CAPABILITY GAP. A
    /// <c>double</c>/<c>float</c> operand can be fractional, and §8.4.2.3.4 GR1b sets EC-BOUND-SUBSCRIPT when the
    /// expression "does not result in an integer" — a test <c>Occ</c> performs only in its SCALED arity, over an
    /// unscaled/scale pair a float has no equivalent of. So a float position operand must keep routing to the D18
    /// §15.4 temp, where the rule is applied exactly once, to the result. An <c>Occ(double)</c> overload added for
    /// convenience would silently truncate <c>E(FUNCTION SQRT(2))</c> to occurrence 1 and raise nothing; this test
    /// is what makes that a deliberate decision to reverse rather than an easy one to slip in.</summary>
    [Fact]
    public void NoFloatCarrier_AtEitherArity()
    {
        foreach (int arity in new[] { 1, 2 })
        {
            var occ = FirstParameterCarriers(typeof(CobolTable), nameof(CobolTable.Occ), arity);
            Assert.DoesNotContain("double", occ);
            Assert.DoesNotContain("float", occ);
        }
        Assert.DoesNotContain("double", ReferenceResolver.UnscaledPositionCarriers);
        Assert.DoesNotContain("double", ReferenceResolver.ScaledPositionCarriers);
    }

    /// <summary>⛔ THE TWO-ARM ASSERTION. <c>PositionRead</c> asks ONE question — "may the fast path name this
    /// carrier at this scale" — and then emits <c>CobolTable.Occ</c> for a subscript or
    /// <c>CobolString.RefModPosition</c> for a reference-modification bound. A single admission list is only
    /// correct while the two methods admit the same carriers.</summary>
    [Fact]
    public void SubscriptAndRefModArms_AdmitTheSameScaledCarriers()
    {
        var occ = FirstParameterCarriers(typeof(CobolTable), nameof(CobolTable.Occ), 2);
        var refmod = FirstParameterCarriers(typeof(CobolString), nameof(CobolString.RefModPosition), 2);
        Assert.True(occ.SetEquals(refmod),
            $"CobolTable.Occ(x, int) accepts [{string.Join(", ", occ)}] but "
            + $"CobolString.RefModPosition(x, int) accepts [{string.Join(", ", refmod)}]. "
            + "ReferenceResolver.PositionRead screens both arms with ONE list, so they must not diverge.");
    }
}
