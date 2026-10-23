// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A BOOLEAN OPERAND THAT IS A SOLE IDENTIFIER IS CLASSIFIED AND BOUND IN ONE PLACE, WHOEVER HOSTS IT (kb/Work
/// PB3565). ISO §8.8.2 admits "an identifier referencing a boolean data item" and "a boolean expression enclosed in
/// parentheses" as a boolean expression, §15.2 2) makes a boolean function one such identifier and §13.10.4 1) makes
/// a boolean constant-name the boolean literal it stands for. COMPUTE's sole right-hand operand parses as Format 1's
/// arithmetic expression, so the binder re-routes it to Format 2; that re-route read an unparenthesized DATA
/// reference only, and a sole boolean function, a parenthesized boolean item and a boolean constant-name were each
/// refused with COBOLNET1511 on legal source.
/// <para>The shape that makes the next host automatic: <c>ConditionBinder.BindSoleBooleanOperand</c> and its
/// diagnostic-free twin <c>IsSoleBooleanOperand</c> are the only readers of the sole-identifier forms; the boolean
/// expression's own leaf (<c>BindBoolOperandValue</c> and the routing predicate <c>IsBooleanValueOperand</c>) and
/// COMPUTE's re-route all ask them. This pins that wiring: the re-route may not read <c>SoleDataRef</c> or
/// <c>SoleFunctionCall</c> itself again. It cannot see a host that calls the pair and ignores the answer; the
/// golden <c>pb3565_compute_boolean_sole_identifier</c> covers the behaviour.</para>
/// </summary>
public sealed class SoleBooleanOperandDriftTests
{
    private static string Source(string file) => StripComments(File.ReadAllText(
        TestRepo.Src("Cobol.Net.Compiler", "Binding", "Procedure", "Verbs", file)));

    /// <summary>Drop line and block comments so a rule NAMED in prose is never mistaken for a rule APPLIED in code.</summary>
    private static string StripComments(string s) =>
        Regex.Replace(Regex.Replace(s, @"/\*.*?\*/", " ", RegexOptions.Singleline), @"//[^\n]*", " ");

    [Theory]
    [InlineData("ConditionBinder.cs", "BindSoleBooleanOperand")]   // the boolean expression's own leaf
    [InlineData("ConditionBinder.cs", "IsSoleBooleanOperand")]     // the routing predicate IsBooleanValueOperand
    [InlineData("ArithmeticBinder.cs", "BindSoleBooleanOperand")]  // COMPUTE's Format 1 to Format 2 re-route
    [InlineData("ArithmeticBinder.cs", "IsSoleBooleanOperand")]
    public void EveryHostOfASoleBooleanOperand_AsksTheOneClassifier(string file, string member)
    {
        Assert.True(Regex.IsMatch(Source(file), $@"\b{member}\s*\("),
            $"{file} hosts a sole boolean operand (ISO §8.8.2) but never calls {member}: the sole data-name, the "
            + "sole boolean function and the boolean constant-name would be re-derived here, which is how COMPUTE "
            + "came to refuse `COMPUTE B8 = FUNCTION BOOLEAN-OF-INTEGER(5, 8)` (kb/Work PB3565).");
    }

    [Theory]
    [InlineData(@"\bSoleDataRef\s*\(")]
    [InlineData(@"\bSoleFunctionCall\s*\(")]
    public void TheComputeReroute_DoesNotReadASoleIdentifierItself(string reader)
    {
        Assert.False(Regex.IsMatch(Source("ArithmeticBinder.cs"), reader),
            $"ArithmeticBinder reads '{reader}' itself: COMPUTE's sole right-hand operand is classified and bound by "
            + "ConditionBinder.IsSoleBooleanOperand / BindSoleBooleanOperand, the same reader the boolean "
            + "expression's own leaf uses (kb/Work PB3565).");
    }
}
