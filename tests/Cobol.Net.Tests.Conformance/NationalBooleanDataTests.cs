// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// NATIONAL (ISO §8.5.2.10, PIC N / USAGE NATIONAL) and BOOLEAN (§8.5.2.5, PIC 1 / USAGE BIT) declaration and
/// clause facts — Phase 4a M2-DATA-3/4. The declaration band: COBOLNET0881 for the illegal USAGE×PICTURE shapes
/// (§13.18.60.4 SR5 :22722 / SR12 :22744 / SR20 :22764 and the picture-less elementary forms), COBOLNET0898 for
/// VALUE category mismatches (§13.18.63 SR5 :23232 / SR10 :23254) and boolean THROUGH (SR29 :23327),
/// COBOLNET0899 for the NAMED staged legs (national-edited pictures, national-form numerics/boolean, FD national
/// records) and the loud byte-addressed guards (D-N2: REDEFINES / EXTERNAL cells refuse a 2-byte national leaf).
/// The positive half locks JUSTIFIED (§13.18.32 SR3/GR1-2 :19264–19273), level-88 boolean conditions, national
/// relation ordering (§8.8.4.2.9/10 :9692–9715), and the group character image (a national/boolean leaf is
/// string-stored, so groups containing them MOVE/DISPLAY in char space).
/// </summary>
public sealed class NationalBooleanDataTests
{
    private static string Prog(string pid, string ws, string proc = "") => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {pid}.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {ws}
        PROCEDURE DIVISION.
        MAIN.
            {proc}
            STOP RUN.
        """;

    // ── COBOLNET0881 — the illegal USAGE × PICTURE declaration shapes (ISO §13.18.60.4) ────────────────────

    public static TheoryData<string, string, string> IllegalUsageShapes() => new()
    {
        // SR20 (:22764): PIC N ⇒ only NATIONAL may be specified — an explicit non-NATIONAL usage is illegal.
        { "NBDAT01", "01 WS-A PIC N(4) USAGE DISPLAY.", "PIC N with explicit USAGE DISPLAY (SR20)" },
        // SR5 (:22722): USAGE BIT ⇒ the PICTURE shall be a boolean picture.
        { "NBDAT02", "01 WS-B PIC X(4) USAGE BIT.", "USAGE BIT with an alphanumeric PICTURE (SR5)" },
        // A picture-less ELEMENTARY USAGE NATIONAL/BIT entry has no legal shape (§13.18.40 GR9/GR8 — the
        // category needs its picture; unlike BINARY-CHAR there is no picture-less factory).
        { "NBDAT03", "01 WS-C USAGE NATIONAL.", "picture-less elementary USAGE NATIONAL" },
        { "NBDAT04", "01 WS-D USAGE BIT.", "picture-less elementary USAGE BIT" },
        // SR12 (:22744) + §13.18.40.3 SR30 (:20395): USAGE NATIONAL never combines with an alphabetic/
        // alphanumeric PICTURE.
        { "NBDAT05", "01 WS-E PIC A(4) USAGE NATIONAL.", "USAGE NATIONAL with an alphabetic PICTURE (SR12/SR30)" },
    };

    [Theory]
    [MemberData(nameof(IllegalUsageShapes))]
    public void IllegalUsagePictureShape_Rejects0881(string pid, string wsEntry, string shape)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Prog(pid, wsEntry), 2002);
        Assert.False(ok, $"{shape} must be rejected (ISO §13.18.60.4)");
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0881");
    }

    // ── COBOLNET0898 — VALUE clause category mismatches (ISO §13.18.63 SR5/SR10, both directions) ──────────

    public static TheoryData<string, string, string> IllegalValueShapes() => new()
    {
        // SR5 (:23232): a national item's VALUE literal shall be a national literal (or a legal figurative).
        { "NBDAT06", "01 WS-F PIC N(4) VALUE \"AB\".", "alphanumeric VALUE literal on a national item (SR5)" },
        // ...and an N"…" literal is only for national receivers.
        { "NBDAT07", "01 WS-G PIC X(4) VALUE N\"AB\".", "national VALUE literal on an alphanumeric item (SR5)" },
        // SR10 (:23254): a boolean item's VALUE literal shall be a boolean literal.
        { "NBDAT08", "01 WS-H PIC 1(2) VALUE \"01\".", "quoted-alphanumeric VALUE on a boolean item (SR10)" },
        { "NBDAT09", "01 WS-I PIC X(4) VALUE B\"01\".", "boolean VALUE literal on an alphanumeric item (SR10)" },
        // SR10: length shall not exceed the item's size.
        { "NBDAT10", "01 WS-J PIC 1(2) VALUE B\"0101\".", "boolean VALUE longer than the item (SR10)" },
        // SR29 (:23327): THROUGH phrases shall not be specified for boolean values (level-88).
        { "NBDAT11", "01 WS-K PIC 1(2).\n   88 WS-K-RANGE VALUE B\"00\" THRU B\"11\".",
          "boolean level-88 VALUE THRU (SR29)" },
    };

    [Theory]
    [MemberData(nameof(IllegalValueShapes))]
    public void IllegalValueShape_Rejects0898(string pid, string wsEntry, string shape)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Prog(pid, wsEntry), 2002);
        Assert.False(ok, $"{shape} must be rejected (ISO §13.18.63)");
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0898");
    }

    // ── COBOLNET0899 — the NAMED staged legs stay loud at every national-bearing edition ───────────────────

    public static TheoryData<string, string, string> StagedShapes() => new()
    {
        // NATIONAL-EDITED pictures (§13.18.40.4 GR10 — N with B/0//; §8.5.2.11): the national-edited-2002
        // pending registry row.
        { "NBDAT12", "01 WS-L PIC N(2)B0.", "national-edited PICTURE (GR10)" },
        // National-form NUMERIC (§13.18.60.4 SR12 — PIC 9 USAGE NATIONAL is legal, staged: national digits).
        { "NBDAT13", "01 WS-M PIC 9(3) USAGE NATIONAL.", "national-form numeric (SR12)" },
        // National-form BOOLEAN (SR12 — PIC 1 USAGE NATIONAL is legal, staged).
        { "NBDAT14", "01 WS-N PIC 1 USAGE NATIONAL.", "national-form boolean (SR12)" },
    };

    [Theory]
    [MemberData(nameof(StagedShapes))]
    public void StagedNationalShape_0899AtNationalBearingEditions(string pid, string wsEntry, string shape)
    {
        foreach (int edition in new[] { 2002, 2023 })
        {
            var (ok, errors, _) = EditionHarness.CompileFull(Prog(pid + edition, wsEntry), edition);
            Assert.False(ok, $"{shape} must not compile silently at --std {edition} (staged Phase 4a residue)");
            EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0899");
            EditionHarness.AssertHasDiagnostic(errors, "national");
        }
    }

    /// <summary>An FD record with a national leaf is staged loud (direct 0899): the record codec is Latin-1
    /// single-byte, and the documented 2-byte national character (D-N1/D-N2, §13.18.60.4 GR8 + §8.1.2) has no
    /// byte-record layout yet — Phase 4a residue #9.</summary>
    [Fact]
    public void FdRecord_WithNationalLeaf_Rejects0899()
    {
        const string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. NBDAT15.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT F ASSIGN TO "NBDAT15.DAT".
            DATA DIVISION.
            FILE SECTION.
            FD F.
            01 F-REC.
               05 F-N PIC N(4).
               05 F-X PIC X(4).
            PROCEDURE DIVISION.
            MAIN.
                STOP RUN.
            """;
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2002);
        Assert.False(ok, "a national leaf in a file record must be staged loud (D-N2; Phase 4a residue)");
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0899");
        EditionHarness.AssertHasDiagnostic(errors, "record");
    }

    /// <summary>REDEFINES over a national item OVERLAYS ITS BYTES, and the overlay is the UTF-16BE pair
    /// serialization (kb/Work PB231 — RESIDUE-11 discharged; these two tests USED to pin the refusal, which is
    /// exactly the shape feedback_green_test_can_hold_a_gap_open warns about: a green test pinning a loud stage
    /// reads as a decision).
    /// <para>The derivation: §13.18.44.4 GR1 associates the storage of the two entries over "an area sufficient
    /// to contain the number of bits required by the data item referenced by the subject of the entry", and
    /// §13.18.60.4 GR8 leaves a national character's size to the implementor — COBOL.NET pins TWO bytes,
    /// UTF-16BE (D-N1). So <c>01 A PIC N(4).</c> is an EIGHT-byte area and <c>01 B REDEFINES A PIC X(8).</c>
    /// overlays it exactly. N"AB" is U+0041 U+0042, whose bytes are 00 41 00 42 — the high-order byte FIRST, so
    /// B's second character is "A" and its fourth is "B". Reading them positionally is the assertion: a
    /// little-endian pair or a one-byte-per-position layout would put "A" in position 1.</para></summary>
    [Fact]
    public void Redefines_OverNationalItem_OverlaysItsUtf16BeBytes()
    {
        string src = Prog("NBDAT16", """
            01 A PIC N(4).
            01 B REDEFINES A PIC X(8).
            """, """
            MOVE N"AB" TO A
            DISPLAY "B2=[" B(2:1) "] B4=[" B(4:1) "] LEN=" FUNCTION LENGTH(B).
            """);
        var (ok, output, detail) = new CobolNetCompiler(2002).CompileAndRun(src);
        Assert.True(ok, $"REDEFINES over a national item is legal source — §13.18.44.3 restricts data-name-2 "
            + $"only by class object/pointer, strong typing and variable length (SR14/SR17), never by a "
            + $"national usage: {detail}");
        Assert.Contains("B2=[A] B4=[B] LEN=8", output, StringComparison.Ordinal);
    }

    /// <summary>An EXTERNAL group containing a national leaf is ORDINARY cell-backed storage (kb/Work PB231 —
    /// the same discharge as the REDEFINES twin above, through the same gate: §13.18.22 conditions EXTERNAL on
    /// nothing subordinate at all, and the run-unit cell is a byte area in which a national position takes its
    /// two §13.18.60.4 GR8 bytes). The alphanumeric leaf after it proves the DISPLACEMENT: GX starts at byte 8,
    /// not byte 4, so writing GN cannot disturb it.</summary>
    [Fact]
    public void ExternalGroup_WithNationalLeaf_IsCellBacked()
    {
        string src = Prog("NBDAT17", """
            01 G EXTERNAL.
               05 GN PIC N(4).
               05 GX PIC X(4).
            """, """
            MOVE N"WXYZ" TO GN
            MOVE "abcd" TO GX
            DISPLAY "GN=[" GN "] GX=[" GX "]".
            """);
        var (ok, output, detail) = new CobolNetCompiler(2002).CompileAndRun(src);
        Assert.True(ok, $"an EXTERNAL record with a national leaf is legal source: {detail}");
        Assert.Contains("GN=[WXYZ] GX=[abcd]", output, StringComparison.Ordinal);
    }

    // ── COBOLNET0844 — boolean relation misuse (ISO §8.8.4.2) ───────────────────────────────────────────────

    /// <summary>Boolean operands compare with Format 2 EQUALITY only (§8.8.4.2 F2 :9566–9581); an ordering
    /// operator on a boolean operand is COBOLNET0844 (§8.8.4.2.2 — F1 SR2/SR3 exclude class boolean
    /// :9608–9610).</summary>
    [Fact]
    public void BooleanOrderingRelation_Rejects0844()
    {
        string src = Prog("NBDAT18", "01 BW PIC 1(2) VALUE B\"01\".", """
            IF BW < B"1" DISPLAY "LT=YES" END-IF.
            """);
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2002);
        Assert.False(ok, "an ordering relation on boolean operands must be rejected (ISO §8.8.4.2.2)");
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0844");
    }

    /// <summary>A boolean operand may compare only with another boolean operand (F2 — both operands of class
    /// boolean); a boolean-vs-alphanumeric relation mix is COBOLNET0844.</summary>
    [Fact]
    public void BooleanVsAlphanumericRelation_Rejects0844()
    {
        string src = Prog("NBDAT19", "01 BW PIC 1(2) VALUE B\"01\".\n01 AX PIC X(2) VALUE \"01\".", """
            IF BW = AX DISPLAY "EQ=YES" END-IF.
            """);
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2002);
        Assert.False(ok, "a boolean-vs-alphanumeric relation must be rejected (ISO §8.8.4.2 F2)");
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0844");
    }

    // ── Positive facts ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>JUSTIFIED RIGHT on national and boolean receivers (ISO §13.18.32 SR3 — JUSTIFIED is legal for
    /// boolean/national items; GR1/2 :19264–19273 — left fill with NATIONAL SPACES / BIT ZEROS). The "!"
    /// sentinel keeps the national right end observable.</summary>
    [Fact]
    public void JustifiedRight_NationalAndBoolean_LeftFill()
    {
        string src = Prog("NBDAT20", """
            01 NJ PIC N(4) JUSTIFIED RIGHT.
            01 BJ PIC 1(4) JUSTIFIED RIGHT.
            """, """
            MOVE N"AB" TO NJ.
            DISPLAY "NJ=" NJ "!".
            MOVE B"11" TO BJ.
            DISPLAY "BJ=" BJ.
            """);
        var (ok, stdout, detail) = new CobolNetCompiler(2002).CompileAndRun(src);
        Assert.True(ok, detail);
        Assert.Equal("NJ=  AB!\nBJ=0011", stdout);
    }

    /// <summary>A level-88 condition with a boolean-literal VALUE (ISO §13.18.63 SR10 + §8.8.4.5): the
    /// condition holds exactly when the parent equals the boolean value — the boolean equality relation
    /// (§8.8.4.2.8, by VALUE regardless of usage).</summary>
    [Fact]
    public void Level88_BooleanValue_ConditionTrueAndFalse()
    {
        string src = Prog("NBDAT21", """
            01 FLAG PIC 1 VALUE B"0".
               88 IS-ON VALUE B"1".
            """, """
            IF IS-ON DISPLAY "ON=YES" ELSE DISPLAY "ON=NO" END-IF.
            MOVE B"1" TO FLAG.
            IF IS-ON DISPLAY "ON=YES" ELSE DISPLAY "ON=NO" END-IF.
            """);
        var (ok, stdout, detail) = new CobolNetCompiler(2002).CompileAndRun(src);
        Assert.True(ok, detail);
        Assert.Equal("ON=NO\nON=YES", stdout);
    }

    /// <summary>National relation ordering and the space-extension rule (ISO §8.8.4.2.9/10 :9692–9715): the
    /// shorter operand right-extends with national spaces (so N(4) "AB␠␠" = N"AB"), and ordering follows the
    /// national program collating sequence (D-N3 — the UTF-16 ordinal default; the ALPHANUMERIC program
    /// collating sequence never applies).</summary>
    [Fact]
    public void NationalComparison_OrderingAndSpaceExtension()
    {
        string src = Prog("NBDAT22", "01 NA PIC N(4) VALUE N\"AB\".", """
            IF NA = N"AB" DISPLAY "EQ=YES" ELSE DISPLAY "EQ=NO" END-IF.
            IF NA < N"AC" DISPLAY "LT=YES" ELSE DISPLAY "LT=NO" END-IF.
            IF N"AC" > NA DISPLAY "GT=YES" ELSE DISPLAY "GT=NO" END-IF.
            """);
        var (ok, stdout, detail) = new CobolNetCompiler(2002).CompileAndRun(src);
        Assert.True(ok, detail);
        Assert.Equal("EQ=YES\nLT=YES\nGT=YES", stdout);
    }

    /// <summary>A group containing national and boolean leaves keeps the CHARACTER IMAGE (D-N1/D-B1: both
    /// categories are string-stored, so IsCharacterImage holds): the group DISPLAYs as the concatenated char
    /// image (§14.9.11 GR6) and group-MOVEs verbatim to an alphanumeric receiver (§14.9.25 GR6b — a group
    /// move is a move without conversion).</summary>
    [Fact]
    public void GroupWithNationalAndBooleanLeaves_MovesAndDisplaysAsCharImage()
    {
        string src = Prog("NBDAT23", """
            01 G.
               05 GN PIC N(3).
               05 GB PIC 1(3).
               05 GX PIC X(2).
            01 H PIC X(8).
            """, """
            MOVE N"AB" TO GN.
            MOVE B"101" TO GB.
            MOVE "ZZ" TO GX.
            DISPLAY "G=" G.
            MOVE G TO H.
            DISPLAY "H=" H.
            """);
        var (ok, stdout, detail) = new CobolNetCompiler(2002).CompileAndRun(src);
        Assert.True(ok, detail);
        Assert.Equal("G=AB 101ZZ\nH=AB 101ZZ", stdout);
    }
}
