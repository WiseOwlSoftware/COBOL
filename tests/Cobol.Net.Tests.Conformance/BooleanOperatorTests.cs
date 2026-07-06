// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The COBOL-2002 BOOLEAN OPERATORS B-AND / B-OR / B-XOR / B-NOT (Phase-4 track (a) increment 2; ISO §8.7.2 /
/// §8.8.2 boolean expressions, §14.9.8 Format-2 COMPUTE, §8.8.4.2.2 the boolean relation, §8.8.4.3 the simple
/// boolean condition). End-to-end facts over the ISO Annex A Table A.2 oracle
/// (<c>1100 B-AND 0101 = 0100</c> / <c>B-OR = 1101</c> / <c>B-XOR = 1001</c> / <c>B-NOT 1100 = 0011</c>) plus the
/// constraint band (COBOLNET1511) and the edition gates. Covers COMPUTE Format 2, the boolean relation
/// (§8.8.4.2.2, equality-only), and the simple boolean condition (§8.8.4.3) — including the UNPARENTHESIZED
/// forms, admitted by the <c>boolExprAhead()</c> predicate that leaves <c>comparisonExpression</c> untouched.
/// (Runtime bit logic is unit-tested in <c>CobolBoolTests</c>.)
/// </summary>
public sealed class BooleanOperatorTests
{
    private static string Prog(string pid, string proc) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {pid}.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 A PIC 1(4) VALUE B"1100".
        01 B PIC 1(4) VALUE B"0101".
        01 R PIC 1(4).
        01 F PIC 1 VALUE B"1".
        01 G PIC 1 VALUE B"0".
        PROCEDURE DIVISION.
        MAIN.
            {proc}
            STOP RUN.
        """;

    [Theory]
    [InlineData("COMPUTE R = A B-AND B. DISPLAY R.", "0100")]   // Annex A Table A.2 oracle
    [InlineData("COMPUTE R = A B-OR B. DISPLAY R.", "1101")]
    [InlineData("COMPUTE R = A B-XOR B. DISPLAY R.", "1001")]
    [InlineData("COMPUTE R = B-NOT A. DISPLAY R.", "0011")]
    [InlineData("COMPUTE R = (A B-AND B) B-OR B\"0010\". DISPLAY R.", "0110")]   // nesting + precedence
    [InlineData("COMPUTE R = A B-AND ALL B\"1\". DISPLAY R.", "1100")]           // ALL B"…" materialization (GR2)
    public void ComputeFormat2_MatchesOracle(string proc, string expected)
    {
        var (ok, stdout, detail) = new CobolNetCompiler(2002).CompileAndRun(Prog("BOP01", proc));
        Assert.True(ok, detail);
        Assert.Equal(expected, stdout.TrimEnd('\n'));
    }

    [Theory]
    [InlineData("IF F DISPLAY \"Y\" ELSE DISPLAY \"N\".", "Y")]                 // simple condition (item true)
    [InlineData("IF G DISPLAY \"Y\" ELSE DISPLAY \"N\".", "N")]                 // simple condition (item false)
    [InlineData("IF F B-AND G DISPLAY \"Y\" ELSE DISPLAY \"N\".", "N")]         // 1 AND 0 = false (unparenthesized)
    [InlineData("IF F B-OR G DISPLAY \"Y\" ELSE DISPLAY \"N\".", "Y")]          // 1 OR 0 = true
    [InlineData("IF B-NOT G DISPLAY \"Y\" ELSE DISPLAY \"N\".", "Y")]           // NOT 0 = true
    [InlineData("IF (F B-AND G) DISPLAY \"Y\" ELSE DISPLAY \"N\".", "N")]       // parenthesized
    public void SimpleBooleanCondition_EvaluatesCorrectly(string proc, string expected)
    {
        // A boolean expression used directly as a condition (§8.8.4.3.4 GR1 — true iff the value is 1), gated
        // by the boolExprAhead() predicate so a normal comparison is unaffected (comparisonExpression untouched).
        var (ok, stdout, detail) = new CobolNetCompiler(2002).CompileAndRun(Prog("BOP02", proc));
        Assert.True(ok, detail);
        Assert.Equal(expected, stdout.TrimEnd('\n'));
    }

    [Theory]
    [InlineData("IF A B-AND B = B\"0100\" DISPLAY \"Y\" ELSE DISPLAY \"N\".", "Y")]   // relation, unparenthesized subject
    [InlineData("IF (A B-AND B) = B\"0100\" DISPLAY \"Y\" ELSE DISPLAY \"N\".", "Y")] // parenthesized subject
    [InlineData("IF A B-XOR B = B\"1111\" DISPLAY \"Y\" ELSE DISPLAY \"N\".", "N")]   // inequality
    public void BooleanRelation_EqualityOnly_EvaluatesCorrectly(string proc, string expected)
    {
        var (ok, stdout, detail) = new CobolNetCompiler(2002).CompileAndRun(Prog("BOP0R", proc));
        Assert.True(ok, detail);
        Assert.Equal(expected, stdout.TrimEnd('\n'));
    }

    [Fact]
    public void NormalComparison_WithBooleanGrammarPresent_StillBinds()
    {
        // The boolExprAhead() predicate must NOT disturb ordinary comparisons (incl. subscripted) — the
        // DEVLOG-621 regression guard: `IF ELEM(I) = x` / SEARCH WHEN must keep parsing at 2002+.
        var (ok, stdout, detail) = new CobolNetCompiler(2002).CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. BOP0C.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 T. 05 ELEM PIC 9 OCCURS 3 VALUE 5.
            01 I PIC 9 VALUE 2.
            PROCEDURE DIVISION.
            MAIN.
                IF ELEM(I) = 5 DISPLAY "SUB-EQ" ELSE DISPLAY "SUB-NE".
                STOP RUN.
            """);
        Assert.True(ok, detail);
        Assert.Equal("SUB-EQ", stdout.TrimEnd('\n'));
    }

    [Fact]
    public void NumericComparison_Unaffected_ByTheBooleanAlt()
    {
        // A plain numeric relation still binds through the normal channel at 2002+ (the boolean alt unwraps a
        // B-op-free operand back to its normal binding).
        var (ok, stdout, detail) = new CobolNetCompiler(2002).CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. BOP03.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 X PIC 9(3) VALUE 42.
            PROCEDURE DIVISION.
            MAIN.
                IF X = 42 DISPLAY "EQ" ELSE DISPLAY "NE".
                IF X > 40 DISPLAY "GT" ELSE DISPLAY "LE".
                STOP RUN.
            """);
        Assert.True(ok, detail);
        Assert.Equal("EQ\nGT", stdout.TrimEnd('\n'));
    }

    [Theory]
    [InlineData("COMPUTE R ROUNDED = A B-AND B.")]                       // SR: no ROUNDED on F2
    [InlineData("COMPUTE R = A B-AND B ON SIZE ERROR DISPLAY \"E\".")]   // SR: no SIZE ERROR on F2
    public void BooleanCompute_RoundedOrSizeError_Rejected1511(string proc)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Prog("BOP04", proc), 2002);
        Assert.False(ok, "ROUNDED / ON SIZE ERROR is invalid on a boolean COMPUTE (ISO §14.9.8 Format 2)");
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET1511");
    }

    [Fact]
    public void BooleanComputeReceiver_NotBoolean_Rejected1511()
    {
        var (ok, errors, _) = EditionHarness.CompileFull("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. BOP05.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 A PIC 1(4) VALUE B"1100".
            01 B PIC 1(4) VALUE B"0101".
            01 N PIC X(4).
            PROCEDURE DIVISION.
            MAIN.
                COMPUTE N = A B-AND B.
                STOP RUN.
            """, 2002);
        Assert.False(ok, "a boolean COMPUTE receiver shall be an elementary boolean item (ISO §14.9.8 SR2)");
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET1511");
    }

    [Fact]
    public void BooleanRelation_OrderingOperator_Rejected1511()
    {
        var (ok, errors, _) = EditionHarness.CompileFull(
            Prog("BOP06", "IF (A B-AND B) < B\"1111\" DISPLAY \"X\"."), 2002);
        Assert.False(ok, "boolean relations admit only [NOT] EQUAL (ISO §8.8.4.2.2 Format 2)");
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET1511");
    }

    [Fact]
    public void NonBooleanOperand_InBooleanExpression_Rejected1511()
    {
        var (ok, errors, _) = EditionHarness.CompileFull("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. BOP07.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 A PIC 1(4) VALUE B"1100".
            01 N PIC 9(4) VALUE 5.
            01 R PIC 1(4).
            PROCEDURE DIVISION.
            MAIN.
                COMPUTE R = A B-AND N.
                STOP RUN.
            """, 2002);
        Assert.False(ok, "a boolean expression admits boolean operands only (ISO §8.8.2)");
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET1511");
    }

    [Fact]
    public void BooleanOperator_At85_Rejected()
    {
        // The operator is unavailable at COBOL-85 ({is2002()}? off) — the COMPUTE F2 alt is dead, so the
        // boolean expression does not parse. (The word B-AND stays a legal user word at 85 — see below.)
        var (ok, _, _) = EditionHarness.CompileFull(Prog("BOP08", "COMPUTE R = A B-AND B."), 85);
        Assert.False(ok, "the boolean operators are a 2002 introduction — rejected at --std 85");
    }

    [Fact]
    public void BAndAsUserWord_CompilesAt85_Rejected0901At2002()
    {
        const string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. BOP09.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 B-AND PIC 9 VALUE 5.
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY B-AND.
                STOP RUN.
            """;
        var (ok85, _, detail85) = new CobolNetCompiler(85).CompileAndRun(src);
        Assert.True(ok85, $"B-AND is a user word at COBOL-85: {detail85}");
        var (ok2002, errors, _) = EditionHarness.CompileFull(src, 2002);
        Assert.False(ok2002, "B-AND is reserved at 2002 — a user-word use is rejected");
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0901");
    }
}
