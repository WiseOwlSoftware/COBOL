// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ISO §14.9.25.4 GR6 d) — AN ALPHANUMERIC OR NATIONAL SENDING OPERAND MOVING TO A NUMERIC OR NUMERIC-EDITED
/// RECEIVER. GR6 d) 3 makes it "an unsigned integer of category numeric" whose SIZE the standard caps at the
/// rightmost 31 character positions (sub-rule a for a data item, sub-rule c for a literal); GR6 d) 1's closing
/// sentence sets EC-DATA-INCOMPATIBLE when its content "would result in a false value in a numeric class
/// condition". Both halves were missing (kb/Work PB426 + PB844), and both are one mechanism: the same decode, at
/// the same five emit sites.
///
/// <para>The SIZE half is measured by goldens at the editions where its receiver exists
/// (85/pb426_alnum_sender_31_character_cap, 2002/pb426_alnum_sender_31_digit_receiver) and by the per-edition
/// sweep below, which is what proves the rule is not accidentally gated — the standard states it identically at
/// all four editions, and the wrap it replaced was measured at all four.</para>
///
/// <para>The RAISE half can only live here: EC-DATA-INCOMPATIBLE is fatal (§14.6.13.1.1 Table 13), so the run
/// unit terminates abnormally and no <c>.out</c> file can record it. Its positive control — checking ON, all-digit
/// content, nothing raised — is 2002/pb426_alnum_sender_data_incompatible, so the raise is never pinned by a test
/// that has only ever seen the failing case.</para>
/// </summary>
public sealed class MoveAlphanumericSenderTests
{
    private const string A40 = "1234567890123456789012345678901234567890";

    private static string Prog(string id, string ws, string proc, string turn = "") => $"""
        {turn}
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {id}.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {ws}
        PROCEDURE DIVISION.
        MAIN.
        {proc}
            STOP RUN.
        """;

    private static void AssertRuns(int std, string id, string ws, string proc, string expected, string turn = "")
    {
        var (ok, stdout, detail) = new CobolNetCompiler(std).CompileAndRun(Prog(id, ws, proc, turn));
        Assert.True(ok, $"[--std {std}] must compile and run: {detail}\nstdout:\n{stdout}");
        Assert.Equal(expected, stdout);
    }

    private static void AssertFatalIncompatible(string id, string ws, string proc, int std = 2023)
    {
        var (ok, stdout, detail) = new CobolNetCompiler(std).CompileAndRun(
            Prog(id, ws, proc, ">>TURN EC-DATA-INCOMPATIBLE CHECKING ON"));
        Assert.False(ok, $"[--std {std}] §14.9.25.4 GR6 d) 1 requires EC-DATA-INCOMPATIBLE to be set to exist; "
                         + $"the run completed with stdout:\n{stdout}");
        Assert.Contains("EC-DATA-INCOMPATIBLE", detail);
    }

    // ── GR6 d) 3 · the size rule, at every edition ───────────────────────────────────────────────────────────

    // A 40-character all-digit sender into PIC 9(9): the operand is the rightmost 31 character positions,
    // "0123456789012345678901234567890", and §14.6.8.2 GR4 truncates high-order into nine digit positions ->
    // 234567890. Uncapped, the accumulator consumed all forty and WRAPPED the signed Int128 -> 838277934.
    // Run at ALL FOUR editions: the clause is edition-invariant and the defect was measured at all four, so a
    // fix that reached only the default edition would be a fix for a quarter of the compiler.
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void AlphanumericSenderOverThirtyOneCharacters_UsesTheRightmostThirtyOne_AtEveryEdition(int std)
        => AssertRuns(std, $"PB426S{std}",
            $"""
            01 A40 PIC X(40) VALUE "{A40}".
            01 R9  PIC 9(9).
            """,
            "    MOVE A40 TO R9\n    DISPLAY R9",
            "234567890");

    // Sub-rule c — the LITERAL twin, a separate sentence of the standard and a separate emit site (the literal
    // operand, not a field read). Same window, same answer.
    [Theory]
    [InlineData(85)]
    [InlineData(2023)]
    public void AlphanumericLiteralOverThirtyOneCharacters_UsesTheRightmostThirtyOne(int std)
        => AssertRuns(std, $"PB426L{std}",
            "01 R9  PIC 9(9).",
            $"    MOVE \"{A40}\" TO R9\n    DISPLAY R9",
            "234567890");

    // THE WINDOW IS OVER CHARACTER POSITIONS, NOT OVER DIGIT CHARACTERS. S40's rightmost 31 positions hold only
    // "1234"; reading all forty (or capping at 31 DIGITS, which never fires for a 13-digit operand) answers
    // 543211234. §14.6.13.2's "a non-digit position contributes no digit" is what makes the two readings differ.
    [Fact]
    public void TheCapCountsCharacterPositions_NotDigitCharacters()
        => AssertRuns(2023, "PB426CHR",
            """
            01 S40 PIC X(40) VALUE "987654321AAAAAAAAAAAAAAAAAAAAAAAAAAA1234".
            01 R9  PIC 9(9).
            """,
            "    MOVE S40 TO R9\n    DISPLAY R9",
            "000001234");

    // A sender EXACTLY at the cap is unwindowed — the control that fails an off-by-one window.
    [Fact]
    public void ASenderExactlyAtThirtyOneCharacters_IsNotWindowed()
        => AssertRuns(2002, "PB426AT31",
            """
            01 A31 PIC X(31) VALUE "1234567890123456789012345678901".
            01 R31 PIC 9(31).
            """,
            "    MOVE A31 TO R31\n    DISPLAY R31",
            "1234567890123456789012345678901");

    // The reference-modified sender is another shape of the SAME operand through another emit site (§8.4.3.3.4
    // GR6 — a reference-modified result is category alphanumeric), and the numeric-EDITED receiver is a third
    // (§14.9.25.3 Table 16 admits alphanumeric -> numeric-edited; §14.9.25.4 GR5 edits the unsigned integer into
    // the mask). Both windowed.
    [Fact]
    public void ReferenceModifiedAndNumericEditedArms_TakeTheSameWindow()
        => AssertRuns(2023, "PB426SHAPES",
            """
            01 S40 PIC X(40) VALUE "987654321AAAAAAAAAAAAAAAAAAAAAAAAAAA1234".
            01 R9  PIC 9(9).
            01 NE  PIC ZZZZZZZZ9.
            """,
            """
                MOVE S40 (1:40) TO R9
                MOVE S40 TO NE
                DISPLAY R9
                DISPLAY NE
            """,
            "000001234\n     1234");

    // §14.9.48.4 GR11 c) transfers UNSTRING's examined characters "according to the rules for the MOVE
    // statement", so the window reaches the UNSTRING receiver dispatch too. (Only the plain-numeric arm is
    // reachable: §14.9.48.3 SR4 screens a numeric-edited INTO receiver out before emit.)
    [Fact]
    public void UnstringIntoANumericReceiver_TakesTheSameWindow()
        => AssertRuns(2023, "PB426UNSTR",
            """
            01 USRC PIC X(41) VALUE "987654321AAAAAAAAAAAAAAAAAAAAAAAAAAA1234,".
            01 R9   PIC 9(9).
            """,
            """
                UNSTRING USRC DELIMITED BY "," INTO R9
                DISPLAY R9
            """,
            "000001234");

    // ⛔ THE SIBLING THE SPLIT PROTECTS. A numeric item's own character image (here a group-aliased
    // PIC S9(31) SIGN TRAILING SEPARATE leaf, whose image IS 32 characters) is decoded by the storage-form
    // bridges — CobolTable.Occ, CobolString.RefModPosition, INSPECT's re-store — and its size is fixed by its
    // own PICTURE, so GR6 d) 3 asks nothing of it and CobolNum.DigitMagnitude, not FromAlphanumeric, is what
    // those bridges call. This pins the whole 31-digit magnitude through the INSPECT re-store.
    [Fact]
    public void ANumericItemsOwnImage_IsDecodedWithoutTheAlphanumericSizeRule()
        => AssertRuns(2023, "PB426BRIDGE",
            """
            01 G3.
               05 N31 PIC S9(31) SIGN TRAILING SEPARATE
                  VALUE +1111111111111111111111111111111.
            01 SAVE-G3 PIC X(32).
            """,
            """
                MOVE G3 TO SAVE-G3
                INSPECT N31 REPLACING ALL "1" BY "2"
                DISPLAY N31
            """,
            "2222222222222222222222222222222+");

    // ── GR6 d) 1 · the exception condition ──────────────────────────────────────────────────────────────────

    // The three shapes kb/Work PB844 measured answering "   0" / "000" in silence. Every one is a VALID move
    // (§14.9.25.3 Table 16), so a diagnostic would be wrong; what the standard requires is the condition.
    [Fact]
    public void AlphanumericLiteralWithNonNumericContent_IntoNumericEdited_RaisesDataIncompatible()
        => AssertFatalIncompatible("PB844LIT", "01 NE PIC ZZZ9.", "    MOVE \"Q\" TO NE\n    DISPLAY NE");

    [Fact]
    public void AlphanumericItemWithNonNumericContent_IntoNumericEdited_RaisesDataIncompatible()
        => AssertFatalIncompatible("PB844ITM",
            """
            01 AQ PIC X VALUE "Q".
            01 NE PIC ZZZ9.
            """, "    MOVE AQ TO NE\n    DISPLAY NE");

    [Fact]
    public void AlphanumericItemWithNonNumericContent_IntoNumeric_RaisesDataIncompatible()
        => AssertFatalIncompatible("PB844NUM",
            """
            01 AQ PIC X VALUE "Q".
            01 N3 PIC 9(3).
            """, "    MOVE AQ TO N3\n    DISPLAY N3");

    // ⛔ THE CLASS CONDITION IS OVER THE WHOLE CONTENT, NOT OVER THE CAPPED WINDOW. GR6 d) 1 speaks of "the
    // content of the sending operand" and is stated before d) 3 narrows it, so a non-digit in a position the
    // window discards still makes the condition exist. An implementation that tested the window instead would
    // pass every test above and fail only here.
    [Fact]
    public void ANonDigitOutsideTheThirtyOneCharacterWindow_StillRaises()
        => AssertFatalIncompatible("PB844OUT",
            """
            01 QLO PIC X(40) VALUE "Q123456789012345678901234567890123456789".
            01 R9  PIC 9(9).
            """, "    MOVE QLO TO R9\n    DISPLAY R9");

    // The UNSTRING channel raises for the same reason it windows — §14.9.48.4 GR11 c).
    [Fact]
    public void UnstringIntoANumericReceiver_RaisesOnNonNumericContent()
        => AssertFatalIncompatible("PB844UNS",
            """
            01 USRC PIC X(3) VALUE "Q,".
            01 N3   PIC 9(3).
            """, "    UNSTRING USRC DELIMITED BY \",\" INTO N3\n    DISPLAY N3");

    // ⛔ THE OVER-RAISE GUARD, AND IT HAD TO BE BUILT ON A REACHABLE SITE. GR6 d)'s scope is "when a numeric or
    // numeric-edited item is the receiving item", so the SAME incompatible content read in a numeric context
    // that is NOT a MOVE raises nothing — the standard asks no numeric question of it there.
    //
    // ⚠ The first version of this guard used an alphanumeric receiver, a class condition and a literal
    // subscript, and it was VACUOUS: none of those three reaches the alphanumeric-to-numeric decode at all, so
    // it passed with the checked read wired in unconditionally (MEASURED — `AlphanumericChecked => true` left it
    // green). Under STRICT conformance every remaining reader of that decode IS a MOVE-rules channel, because
    // §8.8.1.1 bars an alphanumeric arithmetic operand outright; the leniency behind --permissive is the one
    // reachable non-MOVE site, and that is what this measures. It FAILS with the unconditional reading.
    [Fact]
    public void AnArithmeticOperandIsNotAMove_AndRaisesNothing()
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(Prog("PB844ARITH",
            """
            01 AQ PIC X VALUE "Q".
            01 R  PIC 9(6).
            """,
            "    COMPUTE R = AQ + 1\n    DISPLAY \"R=\" R",
            ">>TURN EC-DATA-INCOMPATIBLE CHECKING ON"), 2023, permissive: true);
        Assert.True(ok, "§14.9.25.4 GR6 d) 1 is a MOVE rule — an arithmetic operand is outside its scope and "
                        + $"must not raise: {detail}\nstdout:\n{stdout}");
        Assert.Equal("R=000001", stdout.Trim());
    }

    // Checking OFF is the default, and the standard makes the result of the reference UNDEFINED in exactly this
    // case — so the tolerant deterministic decode stands and legal programs that never enable checking are
    // byte-identical to a pre-fix build here. (The VALUE is still the windowed one: that half is not optional.)
    [Fact]
    public void WithCheckingOff_TheDeterministicDecodeStands()
        => AssertRuns(2023, "PB844OFF",
            """
            01 AQ  PIC X VALUE "Q".
            01 QLO PIC X(40) VALUE "Q123456789012345678901234567890123456789".
            01 N3  PIC 9(3).
            01 R9  PIC 9(9).
            """,
            """
                MOVE AQ TO N3
                MOVE QLO TO R9
                DISPLAY N3
                DISPLAY R9
            """,
            "000\n123456789");
}
