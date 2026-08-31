// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ EVERY <c>OperandContext</c> DECLARES BOTH AXES (kb/Work PB169–PB172 — the burn-down cluster's spine).
///
/// <para>An operand slot in this compiler used to acquire ISO §8.8.1.1 BY DEFAULT — by being handed to
/// <c>BindExpr</c> — instead of declaring which rule governs it, and the two questions it actually has to answer
/// ("does §8.8.1.1's class screen apply here?" and "does §13.18.38.3 r7 admit an index-name here?") were
/// recorded in four different places. All four defects of the cluster are one site answering one axis wrongly or
/// not at all. <c>OperandContextRules.Rules()</c> is the table; this test is what keeps it total.</para>
///
/// <para>⚠ THE COMPILER CANNOT BE THE WHOLE GUARD, which is why this file exists. An enum switch expression
/// needs a discard arm at all (CS8509 covers the undeclared-value space, and this repository builds with
/// <c>TreatWarningsAsErrors</c>), so <c>Rules()</c> has one — a THROW, never a default answer, because a
/// defaulting arm would hand a new context whichever axis the author happened to prefer, silently. A throw is
/// only observable when the path RUNS, and a rarely-taken context could ship un-declared. Enumerating
/// <c>Enum.GetValues</c> here makes a member without a row fail at TEST time instead.</para>
/// </summary>
public sealed class OperandContextRulesTests
{
    private static Type EnumType() =>
        typeof(CobolNet.Binding.Bound.StatementBinder).Assembly
            .GetType("CobolNet.Binding.Procedure.OperandContext", throwOnError: true)!;

    private static MethodInfo RulesMethod() =>
        typeof(CobolNet.Binding.Bound.StatementBinder).Assembly
            .GetType("CobolNet.Binding.Procedure.OperandContextRules", throwOnError: true)!
            .GetMethod("Rules", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)!;

    /// <summary>Every member has a row. A member without one throws
    /// <see cref="System.Runtime.CompilerServices.SwitchExpressionException"/>, which this turns into a named
    /// failure pointing at the file to edit.</summary>
    [Fact]
    public void EveryOperandContextMember_DeclaresItsTwoAxes()
    {
        var rules = RulesMethod();
        foreach (object member in Enum.GetValues(EnumType()))
        {
            var ex = Record.Exception(() => rules.Invoke(null, [member]));
            Assert.True(ex is null,
                $"OperandContext.{member} has no row in OperandContextRules.Rules(). Every operand context must "
                + "declare BOTH axes — whether ISO §8.8.1.1's class screen governs it, and whether "
                + "§13.18.38.3 r7's index-name screen applies — because a context that declares neither acquires "
                + $"§8.8.1.1 by default, which is the whole shape of kb/Work PB169–PB172. ({ex?.InnerException?.GetType().Name})");
        }
    }

    /// <summary>⛔ THE ROWS ARE PINNED TO THEIR CLAUSES, not merely present. A total table whose values are wrong
    /// is exactly as harmful as a missing row — and the two exempt contexts are exempt for a REASON that is
    /// citable: CALL … BY VALUE is governed by §14.9.4.3 SR22 and an intrinsic argument by the function's own
    /// §15.x argument rule, both NARROWER than §8.8.1.1, so quoting §8.8.1.1 at their programmer names a rule
    /// that was not broken (the COBOLNET1628 lesson). ArithmeticIndexWindow is the r7 window, so the index-name
    /// screen must NOT fire there — and CallByValue is likewise exempt because SR22 screens the operand itself.
    /// </summary>
    /// <para>⛔ AXIS 3 IS NOT A CONSEQUENCE OF AXIS 1, and this row set is where that was measured. §13.18.60.3
    /// SR10 admits a class-INDEX DATA item "in a SEARCH or SET statement, a relation condition, an intrinsic
    /// function argument, an inline method invocation argument, the USING phrase of a procedure division header,
    /// or the USING phrase of a CALL or INVOKE statement" — an enumeration of CONTEXTS, not a property of the
    /// operand. Deriving it from "class index is not class numeric" rejected <c>SET IN1 TO IDN1</c> in eight NIST
    /// programs, every one a SET statement SR10 names outright.</para>
    /// <remarks>⛔ THE ROW SET IS DERIVED FROM <c>Enum.GetValues</c>, NOT HAND-LISTED (kb/Work PB224). As four
    /// <c>InlineData</c> rows it was total over the members that HAPPENED to be written down: a fifth
    /// <c>OperandContext</c> member could take any axis values it liked and this fact stayed green, which is the
    /// transposition hazard the fact exists to catch, one member later. The expectations still have to be
    /// written by hand — they are the CLAUSES, and deriving them from the table under test would measure
    /// nothing — but MEMBERSHIP is now mechanical, so a new member fails here until someone reads §8.8.1.1,
    /// §13.18.38.3 r7 and §13.18.60.3 SR10 for it.</remarks>
    private static readonly Dictionary<string, (bool NumericClassScreen, bool IndexNameScreen, bool IndexDataItemAdmitted)>
        ExpectedAxes = new(StringComparer.Ordinal)
        {
            ["Arithmetic"] = (true, true, false),
            ["FunctionArgument"] = (false, true, true),   // SR10: "an intrinsic function argument"
            ["CallByValue"] = (false, false, true),       // SR10: "the USING phrase of a CALL"
            ["ArithmeticIndexWindow"] = (true, false, true),
        };

    public static TheoryData<string> AllMembers()
    {
        var d = new TheoryData<string>();
        foreach (object m in Enum.GetValues(EnumType())) d.Add(m.ToString()!);
        return d;
    }

    [Theory]
    [MemberData(nameof(AllMembers))]
    public void TheThreeAxes_AreWhatTheClausesSay(string member)
    {
        Assert.True(ExpectedAxes.TryGetValue(member, out var want),
            $"OperandContext.{member} has no expected-axes row in this test. A new operand context must have its "
            + "THREE axes read off the standard before it is trusted: does ISO §8.8.1.1's class screen govern the "
            + "slot, does §13.18.38.3 r7 admit an index-NAME there, and does §13.18.60.3 SR10 admit an index DATA "
            + "item there. The two index lists are genuinely DIFFERENT (a subscript and PERFORM VARYING are r7's "
            + "and not SR10's; an intrinsic argument and CALL USING are SR10's and not r7's), so a transposed row "
            + "is a real and silent hazard — see kb/Work PB215.");
        object value = Enum.Parse(EnumType(), member);
        object row = RulesMethod().Invoke(null, [value])!;
        var t = row.GetType();
        Assert.Equal(want.NumericClassScreen, (bool)t.GetField("Item1")!.GetValue(row)!);
        Assert.Equal(want.IndexNameScreen, (bool)t.GetField("Item2")!.GetValue(row)!);
        Assert.Equal(want.IndexDataItemAdmitted, (bool)t.GetField("Item3")!.GetValue(row)!);
    }

    /// <summary>⛔ AND NO SITE MAY ASK THE QUESTION ITS OWN WAY AGAIN. The defect shape the table replaces is a
    /// screen keyed on a hand-written member LIST (<c>context is OperandContext.Arithmetic or
    /// OperandContext.ArithmeticIndexWindow</c>) — PB155 widened one such list and not its twin, and PB172 is the
    /// twin that was never widened. Every consumer must read <c>Rules()</c>, so "which arm did I fix" stops being
    /// a question that can be asked.
    /// <para>⛔ THE ARITY AND THE SCOPE ARE BOTH LOAD-BEARING, AND BOTH WERE WRONG (kb/Work PB224). The first cut
    /// required at least one <c>or</c> (<c>(…)+</c>) and scanned ONE file — so it could not have fired on the
    /// defect its own docstring names: PB172's site was <c>if (context is OperandContext.Arithmetic &amp;&amp; …)</c>,
    /// a SINGLE member with no <c>or</c>, and a screen written the same way in <c>ReferenceResolver</c> or any
    /// other binder was invisible. A drift guard that cannot match its own witness is the green-gate-over-the-
    /// wrong-subject shape. Now: any identifier, one member or many, anywhere under <c>Binding/</c>.</para></summary>
    [Fact]
    public void NoScreenKeysOnAHandWrittenContextList()
    {
        string bindingRoot = Path.Combine(TestRepo.Root, "src", "Cobol.Net.Compiler", "Binding");
        var offenders = new List<string>();
        foreach (string file in Directory.EnumerateFiles(bindingRoot, "*.cs", SearchOption.AllDirectories))
        {
            string src = File.ReadAllText(file);
            // Strip doc comments and line comments: the enum's own documentation legitimately names its members.
            src = Regex.Replace(src, @"^\s*///.*$", "", RegexOptions.Multiline);
            src = Regex.Replace(src, @"//[^\n]*", "");
            offenders.AddRange(Regex.Matches(src,
                    @"\b[A-Za-z_][A-Za-z0-9_]*\s+is\s+(not\s+)?OperandContext\.[A-Za-z]+(\s+or\s+OperandContext\.[A-Za-z]+)*")
                .Select(m => $"{Path.GetFileName(file)}: {m.Value}"));
        }
        Assert.True(offenders.Count == 0,
            "a §8.8.1.1 / r7 / SR10 screen is keyed on a hand-written OperandContext test instead of Rules(): "
            + string.Join(" ;; ", offenders)
            + " — that is the two-arm shape kb/Work PB172 records (one list widened, its twin not). Read the "
            + "axis off OperandContextRules.Rules() so all three screens move together.");
    }
}
