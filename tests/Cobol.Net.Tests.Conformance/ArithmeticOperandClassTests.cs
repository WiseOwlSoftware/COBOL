// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ ISO §8.8.1.1 — THE CLASS OF AN ARITHMETIC OPERAND (DA6). "An arithmetic expression may be an identifier
/// referencing a <b>numeric data item</b>, a numeric literal, the figurative constant ZERO (ZEROS, ZEROES) …".
/// A group item (class alphanumeric, §8.5), an elementary alphanumeric or national item, and a reference-modified
/// slice (§8.4.2.4) are therefore NOT permissible arithmetic operands.
/// <para>
/// COBOL.NET accepted all of them and decoded their digit characters — and did so INCONSISTENTLY: a group of
/// <c>PIC X</c> leaves computed, while a group of <c>PIC 9</c> leaves compiled and then THREW at run time. The
/// operand whose digits were unambiguous failed and the merely-textual one succeeded. Owner decision 2026-07-29:
/// reject under strict conformance, keep the leniency DIALECT-GATED behind <c>--permissive</c>.
/// </para>
/// <para>
/// The NEGATIVE half (strict rejection at all four editions) lives in the negative corpus as
/// <c>da6-group-numeric-operand</c> / <c>da6-alphanumeric-numeric-operand</c> / <c>da6-refmod-numeric-operand</c>.
/// THIS file owns the two facts a reject-only fixture cannot express: that the extension still WORKS under
/// <c>--permissive</c>, and that it is CONSISTENT there — which is the actual defect being repaired.
/// </para>
/// </summary>
public sealed class ArithmeticOperandClassTests
{
    private static string Program(string ws, string body) => $"""
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ARITHOPCLASS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
{ws}
       PROCEDURE DIVISION.
       MAIN.
{body}
           STOP RUN.
""";

    /// <summary>⛔ THE FACT THE WHOLE FIX EXISTS FOR. Two groups holding the same digits — one with an alphanumeric
    /// leaf, one with a numeric-DISPLAY leaf — must behave IDENTICALLY. Before DA6 the first computed 001235 and the
    /// second threw <c>NotImplementedCobolFeatureException</c> at run time, so the leniency was not a leniency: it
    /// was a coin toss decided by the leaf's USAGE. Under <c>--permissive</c> both now decode their digits.</summary>
    [Fact]
    public void Permissive_BothGroupKinds_DecodeIdentically()
    {
        const string body = "           COMPUTE R = G + 1.\n           DISPLAY \"R=\" R.";
        var (okAlnum, outAlnum, dAlnum) = EditionHarness.CompileAndRun(
            Program("       01 G.\n          05 A PIC X(4) VALUE \"1234\".\n       01 R PIC 9(6).", body),
            2023, permissive: true);
        var (okNum, outNum, dNum) = EditionHarness.CompileAndRun(
            Program("       01 G.\n          05 A PIC 9(2) VALUE 12.\n          05 B PIC 9(2) VALUE 34.\n       01 R PIC 9(6).", body),
            2023, permissive: true);

        Assert.True(okAlnum, dAlnum);
        Assert.True(okNum, dNum);
        Assert.Equal("R=001235", outAlnum.Trim());
        Assert.Equal(outAlnum.Trim(), outNum.Trim());   // the consistency that was missing
    }

    /// <summary>Every alphanumeric shape §8.8.1.1 bars is rejected under STRICT conformance, and the rule is
    /// EDITION-INVARIANT — §8.8.1.1 is unchanged at 85/2002/2014/2023, so there is no introduction axis and no
    /// gate. Rejecting only the group shape would have left the elementary and reference-modified shapes accepted,
    /// which is the same inconsistency in a new place.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void Strict_RejectsEveryAlphanumericOperandShape(int edition)
    {
        foreach (string stmt in new[] { "COMPUTE R = G + 1.", "COMPUTE R = X + 1.", "COMPUTE R = X(1:2) + 1." })
        {
            var (ok, errors, _) = EditionHarness.CompileFull(Program(
                "       01 G.\n          05 A PIC X(4) VALUE \"1234\".\n"
                + "       01 X PIC X(4) VALUE \"0012\".\n       01 R PIC 9(6).",
                "           " + stmt), edition);
            Assert.False(ok, $"[std {edition}] '{stmt}' must be rejected — ISO §8.8.1.1");
            EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0844");
        }
    }

    /// <summary>⛔ THE FALSE-POSITIVE GUARD, and the reason this rule cannot live at the expression leaf. The
    /// grammar production is named <c>arithmeticExpression</c> but is REUSED as the generic argument expression, so
    /// an intrinsic argument travels the same spine. An argument's legality comes from the FUNCTION's own §15.x
    /// argument rule, which for the string functions admits alphanumeric data — §8.8.1.1 does not govern it.
    /// <para>A first attempt enforced the rule unconditionally inside the leaf and produced 79 conformance failures,
    /// every one of them a legal call like these. These stay legal under STRICT conformance, with no
    /// <c>--permissive</c> needed.</para></summary>
    [Theory]
    [InlineData("FUNCTION LENGTH(S)", "5")]
    [InlineData("FUNCTION ORD(C)", "66")]
    [InlineData("FUNCTION NUMVAL(N)", "12")]
    public void Strict_StillAcceptsAlphanumericIntrinsicArguments(string call, string expectedPrefix)
    {
        var (ok, output, detail) = EditionHarness.CompileAndRun(Program(
            "       01 S PIC X(5) VALUE \"HELLO\".\n       01 C PIC X VALUE \"A\".\n"
            + "       01 N PIC X(2) VALUE \"12\".\n       01 R PIC 9(6).",
            $"           COMPUTE R = {call}.\n           DISPLAY \"R=\" R."), 2023);
        Assert.True(ok, $"'{call}' is a legal §15.x alphanumeric ARGUMENT and must compile under strict: {detail}");
        Assert.Contains(expectedPrefix, output);
    }

    /// <summary>A genuinely NUMERIC operand is untouched by any of this — the control that proves the screen is not
    /// simply rejecting everything.</summary>
    [Fact]
    public void Strict_AcceptsNumericOperands()
    {
        var (ok, output, detail) = EditionHarness.CompileAndRun(Program(
            "       01 N PIC 9(4) VALUE 12.\n       01 M PIC S9(3)V9 VALUE 7.\n       01 R PIC 9(6).",
            "           COMPUTE R = N + M + 1.\n           DISPLAY \"R=\" R."), 2023);
        Assert.True(ok, detail);
        Assert.Equal("R=000020", output.Trim());   // 12 + 7 + 1 — both operands are class NUMERIC
    }

    /// <summary>
    /// ⛔ A NUMERIC-EDITED OPERAND IS REJECTED (owner decision 2026-08-02, REVERSING DA6's admission — the
    /// control above used to be <c>01 E PIC ZZ9 VALUE 7</c> and asserted it de-edited).
    /// <para>
    /// The old reading took de-editing to make a numeric-edited item a legal arithmetic operand. It does not:
    /// §8.8.1.1 admits "an identifier referencing a <b>numeric data item</b>", §8.5.2.13 calls this a
    /// "<b>numeric-edited</b> data item" — a distinct defined term — and §8.5.2.1 Table 2 puts that category in
    /// class ALPHANUMERIC (usage display) or NATIONAL, never numeric. Every de-editing rule in the standard is a
    /// MOVE/editing rule (§14.9.25.4 GR6d1), and GR6d1 has to GRANT de-editing for the MOVE, which would be
    /// unnecessary if it were generally available. The sibling `IntrinsicArgumentRules` 'n' screen had already
    /// refuted this same reading, so the two rested on readings of §8.8.1.1 that could not both be right.
    /// </para>
    /// <para>⚠ Both external oracles were consulted before landing: NO NIST program depends on it (the whole
    /// NIST corpus stayed green through the flip), and GnuCOBOL's own suite exercises de-editing only under
    /// MOVE — every one of its cases is titled "MOVE with de-editting to …".</para>
    /// </summary>
    [Fact]
    public void Strict_RejectsNumericEditedOperand()
    {
        var (ok, output, detail) = EditionHarness.CompileAndRun(Program(
            "       01 N PIC 9(4) VALUE 12.\n       01 E PIC ZZ9 VALUE 7.\n       01 R PIC 9(6).",
            "           COMPUTE R = N + E + 1.\n           DISPLAY \"R=\" R."), 2023);
        Assert.False(ok, $"a numeric-edited arithmetic operand must be rejected (ISO §8.8.1.1); got: {output}");
        Assert.Contains("COBOLNET0844", detail + output);
    }
}
