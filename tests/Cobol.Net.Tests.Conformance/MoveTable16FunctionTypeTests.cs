// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// kb/Work PB73 (adjudicated 2026-08-18) — a function's ISO §15.2 TYPE decides its §14.9.25.3 Table-16 row as a
/// MOVE sender: an INTEGER function (item 5, "no digits to the right of the decimal point") is the Integer row and
/// moves to a character receiver; a NUMERIC function (item 4) is the NONINTEGER row and moves only to a numeric or
/// numeric-edited receiver — the principle §8.4.3.2.3 SR11 states for the integer-operand positions. The former
/// admission of every function survives ONLY under --permissive, as a warning, rendering the CONFORMANCE.md item-92
/// literal text. (The golden <c>pb73_table16_function_type_and_boolean_view</c> pins the admitted cells and the
/// §8.4.3.3.4 GR2 display-form boolean view; the negatives <c>pb73-move-*</c> pin the strict refusals.)
/// </summary>
public sealed class MoveTable16FunctionTypeTests
{
    private static string Prog(string move) => $$"""
               IDENTIFICATION DIVISION.
               PROGRAM-ID. PB73PERM.
               DATA DIVISION.
               WORKING-STORAGE SECTION.
               01 X10 PIC X(10).
               PROCEDURE DIVISION.
                   {{move}}
                   DISPLAY "[" X10 "]".
                   STOP RUN.
        """;

    /// <summary>Strict (the default axis): a NUMERIC-typed function into an alphanumeric receiver is Table 16's
    /// Noninteger × Alphanumeric "No" — COBOLNET0819 — even when the particular reference is integer-valued
    /// (SQRT(16) = 4): the row is the TYPE's, per SR11's principle.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2023)]
    public void NumericFunction_ToAlphanumeric_IsRefusedStrict(int edition)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Prog("MOVE FUNCTION SQRT(16) TO X10"), edition);
        Assert.False(ok, "a NUMERIC-typed function is the Noninteger sender (ISO §14.9.25.3 Table 16, §15.2 item 4)");
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0819");
    }

    /// <summary>--permissive keeps the earlier admission — with a warning naming the rule — and renders the
    /// function's literal text (the item-92 form: the significant digits, no padding).</summary>
    [Fact]
    public void NumericFunction_ToAlphanumeric_IsAdmittedPermissive_AsLiteralText()
    {
        var (ok, errors, warnings) = EditionHarness.CompileFull(Prog("MOVE FUNCTION SQRT(16) TO X10"), 2023, permissive: true);
        Assert.True(ok, "under --permissive the numeric-function MOVE is admitted: " + string.Join("\n", errors));
        Assert.Contains(warnings, w => w.Contains("COBOLNET0819", StringComparison.Ordinal) && w.Contains("--permissive", StringComparison.Ordinal));
        var (ran, stdout, detail) = EditionHarness.CompileAndRun(Prog("MOVE FUNCTION SQRT(16) TO X10"), 2023, permissive: true);
        Assert.True(ran, detail);
        Assert.Equal("[4         ]", stdout.Trim());
    }

    /// <summary>The Integer row is untouched on both axes: an INTEGER function moves to a character receiver
    /// (item 92's own example, <c>MOVE FUNCTION ORD("A") TO PIC X(10)</c> → 66), and MAX over integer
    /// arguments resolves to the integer type per call (the ONE <c>IntrinsicResultType</c> reader).</summary>
    [Theory]
    [InlineData("MOVE FUNCTION ORD(\"A\") TO X10", "[66        ]")]
    [InlineData("MOVE FUNCTION MAX(3 -14 8) TO X10", "[8         ]")]
    [InlineData("MOVE FUNCTION ABS(-7) TO X10", "[7         ]")]
    public void IntegerFunction_ToAlphanumeric_IsLegalStrict(string move, string expected)
    {
        var (ran, stdout, detail) = EditionHarness.CompileAndRun(Prog(move), 2023);
        Assert.True(ran, detail);
        Assert.Equal(expected, stdout.Trim());
    }
}
