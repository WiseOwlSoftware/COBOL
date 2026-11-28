// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using WiseOwl.Cobol.Compiler.Binding;
using WiseOwl.Cobol.Runtime;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE BIND-TIME RENDERER NAMES A FIELD AND LETS C# OVERLOAD RESOLUTION SUPPLY THE CONVERSION — SO THE SET OF
/// CARRIERS IT MAY NAME IS THE RUNTIME METHOD'S OVERLOAD SET, AND NOTHING ELSE (kb/Work PB201, PB1117).
/// </summary>
/// <remarks>
/// <para>
/// <c>SubscriptBinder.PositionRead</c> emits <c>CobolPosition&lt;PositionUse.Subscript&gt;.Read(&lt;field&gt;, _P_n)</c> for a
/// NUMERIC subscript, <c>CobolPosition&lt;PositionUse.RefMod&gt;.Read(&lt;field&gt;, _P_n)</c> for a NUMERIC reference-modification bound (the item's own
/// profile carries its scale, its sign and the byte form §2023:14.6.13.2 GR2's check reads — kb/Work PB1117), and
/// the profile-less <c>CobolPosition.Digits(&lt;field&gt;)</c> only for a NON-numeric <c>--permissive</c> operand. That is a
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
/// overload without widening them leaves the fast path routing a carrier it could now render, and widening them
/// without the overload puts the CS1503 back. Both directions fail here. The subscript and reference-modification
/// arms cannot diverge: since kb/Work PB2645 they are one generic read whose use is a type argument (they were
/// <c>CobolTable.Occ</c> and <c>CobolString.RefModPosition</c>, held together by a two-arm assertion here).
/// </para>
/// <para>
/// PROVEN TO FAIL, both directions, before being trusted: adding <c>"double"</c> to the numeric list made the
/// first assertion red naming "double" as the extra carrier.
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

    /// <summary>The first-parameter spellings of every public static one-argument overload of
    /// <paramref name="name"/> on <paramref name="host"/>.</summary>
    private static SortedSet<string> OneArgumentCarriers(Type host, string name) =>
        new(host.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name == name && m.GetParameters().Length == 1)
                .Select(m => Spelling(m.GetParameters()[0].ParameterType)));

    /// <summary>The first-parameter spellings of every public static overload of <paramref name="name"/> whose
    /// SECOND parameter is an <c>in NumProfile</c> — the numeric position read's arity.</summary>
    private static SortedSet<string> ProfileArityCarriers(Type host, string name) =>
        new(host.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name == name && m.GetParameters() is { Length: 2 } ps
                            && (ps[1].ParameterType.IsByRef ? ps[1].ParameterType.GetElementType() : ps[1].ParameterType)
                               == typeof(NumProfile))
                .Select(m => Spelling(m.GetParameters()[0].ParameterType)));

    /// <summary>The one numeric position read, at either use (the use is a type argument, so both closed types share
    /// one overload set by construction, kb/Work PB2645).</summary>
    private static readonly Type NumericRead = typeof(CobolPosition<PositionUse.Subscript>);

    /// <summary>A NUMERIC position operand renders <c>CobolPosition&lt;use&gt;.Read(path, _P_n)</c>, so the numeric
    /// carrier list is exactly the profile-arity overload set.</summary>
    [Fact]
    public void NumericCarriers_AreExactlyTheProfileArityReadOverloads()
    {
        var declared = new SortedSet<string>(SubscriptBinder.NumericPositionCarriers);
        var actual = ProfileArityCarriers(NumericRead, nameof(CobolPosition<PositionUse.Subscript>.Read));
        Assert.True(declared.SetEquals(actual),
            $"SubscriptBinder.NumericPositionCarriers = [{string.Join(", ", declared)}] but "
            + $"CobolPosition<use>.Read(x, in NumProfile) accepts [{string.Join(", ", actual)}]. A carrier declared here with "
            + "no overload puts the CS1503 back (kb/Work PB201); an overload with no entry here leaves the fast path "
            + "routing work it could now render.");
    }

    /// <summary>A NON-numeric (<c>--permissive</c>) position operand renders <c>CobolPosition.Digits(path)</c>, so its
    /// carrier list is exactly that method's one-argument overload set.</summary>
    [Fact]
    public void NonNumericCarriers_AreExactlyTheDigitsOverloads()
    {
        var declared = new SortedSet<string>(SubscriptBinder.NonNumericPositionCarriers);
        var actual = OneArgumentCarriers(typeof(CobolPosition), nameof(CobolPosition.Digits));
        Assert.True(declared.SetEquals(actual),
            $"SubscriptBinder.NonNumericPositionCarriers = [{string.Join(", ", declared)}] but "
            + $"CobolPosition.Digits(x) accepts [{string.Join(", ", actual)}].");
    }

    /// <summary>⛔ THE PROFILE-LESS ARITY IS NOT A NUMERIC READ (kb/Work PB1117). A numeric item read without its
    /// profile cannot apply §2023:14.6.13.2 GR2 to a character image and cannot see its sign or scale, which is
    /// exactly how EC-DATA-INCOMPATIBLE became unreachable on the item-identification lane. So the digit decode may
    /// not accept a carrier only a NUMERIC item has (the wide and unsigned-binary tiers), and no profile-less scale
    /// arity of the numeric read may come back.</summary>
    [Fact]
    public void NoProfileLessNumericArity()
    {
        var one = OneArgumentCarriers(typeof(CobolPosition), nameof(CobolPosition.Digits));
        foreach (string numericOnly in new[] { "Int128", "ulong", "UInt128" })
            Assert.DoesNotContain(numericOnly, one);
        Assert.DoesNotContain(NumericRead.GetMethods(BindingFlags.Public | BindingFlags.Static), m =>
            m.Name == nameof(CobolPosition<PositionUse.Subscript>.Read)
            && m.GetParameters() is { Length: 2 } ps && ps[1].ParameterType == typeof(int));
    }

    /// <summary>⛔ NO FLOAT CARRIER AT EITHER ARITY, AND THIS IS A CONFORMANCE RULE, NOT A CAPABILITY GAP. A
    /// <c>double</c>/<c>float</c> operand can be fractional, and §2023:8.4.2.3.4 GR1b sets EC-BOUND-SUBSCRIPT when the
    /// expression "does not result in an integer" — a test <c>Read</c> performs over an unscaled/profile pair a
    /// float has no equivalent of. So a float position operand must keep routing to the D18 §2023:15.4 temp, where the
    /// rule is applied exactly once, to the result. A <c>Read(double)</c> overload added for convenience would
    /// silently truncate <c>E(FUNCTION SQRT(2))</c> to occurrence 1 and raise nothing; this test is what makes that
    /// a deliberate decision to reverse rather than an easy one to slip in.</summary>
    [Fact]
    public void NoFloatCarrier_AtEitherArity()
    {
        foreach (var set in new[] { OneArgumentCarriers(typeof(CobolPosition), nameof(CobolPosition.Digits)),
                                    ProfileArityCarriers(NumericRead, nameof(CobolPosition<PositionUse.Subscript>.Read)) })
        {
            Assert.DoesNotContain("double", set);
            Assert.DoesNotContain("float", set);
        }
        Assert.DoesNotContain("double", SubscriptBinder.NumericPositionCarriers);
        Assert.DoesNotContain("double", SubscriptBinder.NonNumericPositionCarriers);
    }
}
