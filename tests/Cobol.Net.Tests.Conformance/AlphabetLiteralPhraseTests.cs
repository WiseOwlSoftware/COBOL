// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ THE SR14 c-SERIES — the ALPHABET clause's NATIONAL literal-phrase syntax rules — plus the two structural
/// facts that made kb/Work PB770 possible in the first place.
/// <para>The negative corpus carries the b-series (the ALPHANUMERIC arm) and SR14 a on both arms; these facts
/// carry the c-series, whose messages differ from the b-series only in the operand class and the sub-rule letter.
/// They are xUnit facts rather than corpus entries for one reason: they assert the EXACT WORDING that names the
/// construct and the rule, and the c-series' whole point is that one builder now answers for both classes — a
/// per-file <c>.err</c> substring per case would be five near-identical programs pinning five near-identical
/// strings, where one class can state the pairing.</para>
/// <para><b>Why the wording is the assertion.</b> Before PB770 the ALPHABET clause borrowed the CLASS clause's
/// ordinal helper, so an out-of-range ALPHABET ordinal was reported as <c>COBOLNET1671: CLASS : … §12.3.7.3
/// SR17 b2</c> on a program with no CLASS clause and under a rule about a different construct. A test that
/// asserted only "it is rejected" would have called that green
/// (<c>feedback_a_real_clause_can_answer_a_different_question</c>).</para>
/// </summary>
public sealed class AlphabetLiteralPhraseTests
{
    private static string Prog(string pid, string specialNames) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {pid}.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        SPECIAL-NAMES.
            {specialNames}
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 FILLER PIC X.
        PROCEDURE DIVISION.
            STOP RUN.
        """;

    private static void Rejects(string pid, string specialNames, string expected, int edition = 2023)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Prog(pid, specialNames), edition);
        Assert.False(ok, $"[{pid}] must be REJECTED at --std {edition}");
        EditionHarness.AssertHasDiagnostic(errors, expected);
    }

    /// <summary>§12.3.7.3 SR14 c1 — "<i>Each numeric literal shall be an unsigned integer and shall have a value
    /// within the range of one through the maximum number of characters in the native national character set.</i>"
    /// The native national set is the 65,536 UTF-16 code units (D-N1), so 0 and 65,537 are both outside it and
    /// the ordinals are 1-based.</summary>
    [Theory]
    [InlineData("PB770C1A", "0", "the ordinal 0 does not exist in the native national character set")]
    [InlineData("PB770C1B", "65537", "the ordinal 65537 does not exist in the native national character set")]
    public void NationalOrdinal_OutsideTheNativeSet_IsSR14c1(string pid, string ordinal, string expected)
    {
        Rejects(pid, $"ALPHABET NALF FOR NATIONAL IS {ordinal} THRU 5.", expected);
        Rejects(pid + "R", $"ALPHABET NALF FOR NATIONAL IS {ordinal} THRU 5.", "ISO §12.3.7.3 SR14 c1");
    }

    /// <summary>§12.3.7.3 SR14 c1, the FIRST half of its one sentence: "<i>Each numeric literal shall be an
    /// UNSIGNED integer and shall have a value within the range …</i>". The two halves are separate obligations and
    /// only one of them is a range: <c>int.TryParse</c> accepts a leading sign, so <c>+5</c> read as ordinal 5 and
    /// only a NEGATIVE value was caught - by the range half, which is a different rule answering for this one.</summary>
    [Theory]
    [InlineData("PB770SGA", "+5", false)]
    [InlineData("PB770SGB", "-5", false)]
    [InlineData("PB770SGN", "+5", true)]
    public void SignedOrdinal_IsSR14b1c1_UnsignedHalf(string pid, string ordinal, bool national)
    {
        string clause = national ? $"ALPHABET NALF FOR NATIONAL IS {ordinal} THRU 9."
                                 : $"ALPHABET ALF IS {ordinal} THRU 9.";
        Rejects(pid, clause, $"{ordinal} - each numeric literal shall be an UNSIGNED integer".Replace(" - ", " — "));
        Rejects(pid + "R", clause, $"ISO §12.3.7.3 SR14 {(national ? "c1" : "b1")}");
    }

    /// <summary>§12.3.7.3 SR14 c2 — "<i>Each noninteger literal shall be a national literal.</i>" An ALPHANUMERIC
    /// literal in the FOR NATIONAL branch is the violation; so is a noninteger NUMERIC literal, which is neither
    /// the c1 ordinal nor a national literal.</summary>
    [Theory]
    [InlineData("PB770C2A", "\"A\" ALSO N\"B\"")]
    [InlineData("PB770C2B", "1.5 ALSO N\"B\"")]
    public void NationalNonintegerLiteral_OfTheWrongClass_IsSR14c2(string pid, string phrase) =>
        Rejects(pid, $"ALPHABET NALF FOR NATIONAL IS {phrase}.", "ISO §12.3.7.3 SR14 c2");

    /// <summary>§12.3.7.3 SR14 c3 — "<i>Each national literal, when a THROUGH or ALSO phrase is specified, shall
    /// be one character in length.</i>" Both phrases, because the b-series arm silently DROPPED the whole entry
    /// under exactly this shape (PB770 leg b) and one phrase's fix is not the other's.</summary>
    [Theory]
    [InlineData("PB770C3A", "N\"AB\" THRU N\"C\"")]
    [InlineData("PB770C3B", "N\"AB\" ALSO N\"C\"")]
    public void NationalMultiCharacterOperand_UnderThroughOrAlso_IsSR14c3(string pid, string phrase) =>
        Rejects(pid, $"ALPHABET NALF FOR NATIONAL IS {phrase}.", "ISO §12.3.7.3 SR14 c3");

    /// <summary>§12.3.7.3 SR15 on the NATIONAL side — code-name-2. The alphanumeric twin is the negative-corpus
    /// entry <c>pb770-alphabet-unsupported-code-name</c>; this is the arm that names code-name-2 and the national
    /// coded character sets, and it is here because ONE check now answers for both (the arms had two).</summary>
    [Fact]
    public void NationalUnsupportedCodeName_IsSR15() =>
        Rejects("PB770C15", "ALPHABET NALF FOR NATIONAL IS EBCDIC.",
            "COBOLNET1907: ALPHABET NALF FOR NATIONAL IS EBCDIC: not a supported code-name");

    /// <summary>⛔ THE ARM-PAIRING FACT. §12.3.7.3 SR14 a is stated ONCE, above the b-series and the c-series, so
    /// it governs both classes; §12.3.7.4 GR7 k is likewise stated once "<i>where the native coded character set
    /// is the type … being defined, either alphanumeric or national</i>". This is the fact that fails if either
    /// arm is ever given its own builder again — the shape that produced PB770
    /// (<c>feedback_two_arm_dispatch</c>, fifth instance): both arms carried the same
    /// <c>// SR14a duplicate — first wins (diagnostic later)</c> deferral, and the alphanumeric one had no
    /// b-series at all.</summary>
    [Theory]
    [InlineData("PB770DUPA", "ALPHABET ALF IS \"A\" ALSO \"B\", \"A\".", "ALPHABET ALF: the character 'A'")]
    [InlineData("PB770DUPN", "ALPHABET ALF FOR NATIONAL IS N\"A\" ALSO N\"B\", N\"A\".",
        "ALPHABET ALF FOR NATIONAL: the character 'A'")]
    public void DuplicateCharacter_IsSR14a_OnBothArms(string pid, string clause, string expected) =>
        Rejects(pid, clause, expected + " is specified more than once");

    /// <summary>The POSITIVE CONTROL for every fact above: a phrase whose characters are all distinct, in both
    /// classes, compiles. Without it a regression that rejected EVERY literal phrase would read as green
    /// (<c>feedback_green_gates_arent_evidence</c>).</summary>
    [Theory]
    [InlineData("PB770OKA", "ALPHABET ALF IS \"A\" ALSO \"B\", \"1\" THRU \"9\", 305 THRU 300.")]
    [InlineData("PB770OKN", "ALPHABET ALF FOR NATIONAL IS N\"A\" ALSO N\"B\", N\"1\" THRU N\"9\", 305 THRU 300.")]
    public void DistinctCharacters_AreAccepted_OnBothArms(string pid, string clause)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Prog(pid, clause), 2023);
        Assert.True(ok, $"[{pid}] must COMPILE: {string.Join("\n", errors)}");
    }

    /// <summary>⛔ THE MISFILING FACT (kb/Work PB770 leg d). An ALPHABET ordinal violation shall NOT be reported
    /// under the CLASS clause's descriptor, message or rule number, and a CURRENCY SIGN literal shall not be
    /// silently resolved as a native ordinal — both used to happen, because one general "literal characters"
    /// helper with optional arguments defaulted to the CLASS clause's identity for every caller that did not pass
    /// a class-name. §12.3.7.3 SR18: "<i>Literal-7 shall be an alphanumeric or national literal that is not a
    /// figurative constant.</i>"</summary>
    [Fact]
    public void NoClauseInheritsTheClassClausesRuleNumber()
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Prog("PB770MISF", "ALPHABET ALF IS 0 THRU 5."), 2023);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET1906: ALPHABET ALF: the ordinal 0");
        Assert.DoesNotContain(errors, e => e.Contains("SR17", System.StringComparison.Ordinal));
        Assert.DoesNotContain(errors, e => e.Contains("CLASS ", System.StringComparison.Ordinal));

        var (curOk, curErrors, _) = EditionHarness.CompileFull(Prog("PB770CUR", "CURRENCY SIGN IS 65."), 2023);
        Assert.False(curOk, "a numeric CURRENCY SIGN literal-7 violates ISO §12.3.7.3 SR18");
        EditionHarness.AssertHasDiagnostic(curErrors, "literal-7 shall be an alphanumeric or national literal");
        Assert.DoesNotContain(curErrors, e => e.Contains("SR17", System.StringComparison.Ordinal));
    }
}
