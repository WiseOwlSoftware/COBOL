// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The Tier-C mixed-usage-group image boundary (rearchitecture PHASE-11 Step C) — the loud-failure lock. A GROUP
/// with a non-image-capable leaf has no whole-group image, so the verbs that need one stage LOUD by name
/// (COBOLNET_DESIGN §1.4, §4.2). Step C routed the ~12 scattered emit guards through the ONE
/// <c>TierCIsland.Reason</c> source, and these facts are "a lock to flip against" — kb/Work PB164 wave 1
/// FLIPPED the COMP-5/BINARY-CHAR..DOUBLE arms, wave 2 the floats, and the R40 owner decision the INDEX leaf
/// (the 8-byte occurrence-number image; <c>DisplayIndexGroup_RendersVerbatimBytes</c> pins THAT working
/// behavior below), so the island's remaining boundaries are the VARIABLE-LENGTH group (the primary lock
/// fixture) and a POINTER/OBJECT-CLASS leaf (the R40 fleet's correction — every NUMERIC kind is in, the
/// pointer/object categories are not; <c>DisplayPointerGroup_FailsLoud</c>/<c>MovePointerGroup_FailsLoud</c>
/// pin that arm). DISPLAY of a COMPOSABLE variable-length group is NOT here: it renders the documented A.1 item-57
/// format (<c>2023/pb164_vlg_display</c>); the DISPLAY loud lives on the UNCOMPOSABLE shape
/// (<c>DisplayOdoGroupWithDynamicMember_FailsLoudNotCs1061</c>). ACCEPT/STRING receivers are BIND-screened
/// by their own syntax rules (§14.9.1.3 SR6 / §14.9.43.3 SR11) and pinned as such.
/// </summary>
public sealed class TierCRejectionTests
{
    private static string Program(string proc) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. TIERCREJ.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 WS-G.
           05 WS-G-A PIC X(3).
           05 WS-G-D PIC X DYNAMIC LENGTH.
        01 WS-SRC  PIC X(7) VALUE "HELLOXX".
        01 WS-DEST PIC X(7).
        01 WS-CNT  PIC 9(2).
        PROCEDURE DIVISION.
        MAIN.
        {proc}
            STOP RUN.
        """;

    /// <summary>A variable-length-group shape must compile (the emit guard is a runtime LoudStmt/LoudValue, not
    /// a bind error) and fail LOUD at run time through the ONE Tier-C reason, never a silent wrong value.</summary>
    private static void AssertLoudTierC(string proc)
    {
        var (ok, _, detail) = new CobolNetCompiler(2023).CompileAndRun(Program(proc));
        Assert.False(ok, "a variable-length group without a whole-group image shall fail loud (§1.4)");
        Assert.Contains("Tier-C", detail);
    }

    /// <summary>A receiver its own SYNTAX RULE bars (§14.9.1.3 SR6 ACCEPT / §14.9.43.3 SR11 STRING) fails at
    /// BIND with the rule's diagnostic — earlier and more precise than the runtime island.</summary>
    private static void AssertBindRejected(string proc)
    {
        var (ok, _, detail) = new CobolNetCompiler(2023).CompileAndRun(Program(proc));
        Assert.False(ok, "a variable-length-group receiver its syntax rule bars shall fail at bind");
        Assert.Contains("variable-length", detail);
    }

    [Fact] public void MoveIntoGroup_FailsLoud() => AssertLoudTierC("    MOVE WS-SRC TO WS-G.");
    [Fact] public void MoveGroupToElementary_FailsLoud() => AssertLoudTierC("    MOVE WS-G TO WS-DEST.");
    [Fact] public void InspectGroup_FailsLoud() => AssertLoudTierC("    INSPECT WS-G REPLACING ALL \"A\" BY \"B\".");
    [Fact] public void StringIntoGroup_BindRejected() => AssertBindRejected("    STRING WS-SRC DELIMITED BY SIZE INTO WS-G.");
    [Fact] public void AcceptIntoGroup_BindRejected() => AssertBindRejected("    ACCEPT WS-G.");

    /// <summary>The POINTER/OBJECT-CLASS arm of the island (the R40 fleet's correction — the leaf-kind
    /// boundary did NOT close entirely: pointer/object categories have no character image and R40's pin
    /// covers the numeric kinds only). Compiles, throws Tier-C, names the pointer/object mechanism.
    ///
    /// <para>⛔ THE GROUP IS A STRONG TYPEDEF, AND THAT IS FORCED BY THE STANDARD, not a stylistic choice.
    /// This fixture was written <c>01 WS-GP. 05 WS-GP-P USAGE POINTER.</c> — an ordinary group with a pointer
    /// member — which ISO §13.18.60.3 SR14 does not permit: "A USAGE clause with the MESSAGE-TAG, OBJECT
    /// REFERENCE, POINTER, FUNCTION-POINTER, or PROGRAM-POINTER phrase may be specified only for an elementary
    /// data item at level 1 or an elementary data item subordinate to a type declaration that includes the
    /// STRONG phrase." The declaration screen (kb/Work PB183, COBOLNET1724) now rejects that at compile time, so
    /// the fixture was NONCONFORMING SOURCE and these tests were passing on a program the standard forbids.
    /// A STRONG typedef is the ONE conforming spelling of "a group whose leaf is class pointer", so it is what
    /// the arm must be exercised through. Measured: both legs still reach the Tier-C stage with the identical
    /// message — no strong-type MOVE guard intercepts the MOVE leg — so the boundary under test is unchanged
    /// and this is a source repair, not a weakened assertion. ⛔ Do NOT "fix" a future failure here by relaxing
    /// the SR14 screen; the screen is right and the program was wrong.</para></summary>
    private static void AssertPointerGroupLoud(string proc)
    {
        var (ok, _, detail) = new CobolNetCompiler(2023).CompileAndRun($$"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TIERCRE5.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 GPT IS TYPEDEF STRONG.
               05 WS-GP-A PIC X(3).
               05 WS-GP-P USAGE POINTER.
            01 WS-GP TYPE GPT.
            01 WS-DST PIC X(7).
            PROCEDURE DIVISION.
            MAIN.
            {{proc}}
                STOP RUN.
            """);
        Assert.False(ok, "a pointer-leafed group has no whole-group image — loud (§1.4)");
        Assert.Contains("Tier-C", detail);
        Assert.Contains("pointer/object", detail);
    }

    [Fact] public void DisplayPointerGroup_FailsLoud() => AssertPointerGroupLoud("    DISPLAY WS-GP.");
    [Fact] public void MovePointerGroup_FailsLoud() => AssertPointerGroupLoud("    MOVE WS-GP TO WS-DST.");

    /// <summary>The R40 leg, pinned WORKING: an INDEX-leaf group displays its verbatim content — the leaf's
    /// occurrence number as 8 big-endian two's-complement bytes (the R40 pin; A.1 items 56 + 211). SET (one
    /// of the references §13.18.60.3 SR10 permits) seeds the value.</summary>
    [Fact]
    public void DisplayIndexGroup_RendersVerbatimBytes()
    {
        var (ok, stdout, _) = new CobolNetCompiler(2023).CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TIERCRE4.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-T.
               05 WS-E PIC X OCCURS 5 INDEXED BY IX.
            01 WS-GI.
               05 WS-GI-A PIC X(3) VALUE "ABC".
               05 WS-GI-N USAGE INDEX.
            PROCEDURE DIVISION.
            MAIN.
                SET IX TO 3.
                SET WS-GI-N TO IX.
                DISPLAY WS-GI.
                STOP RUN.
            """);
        Assert.True(ok, "an INDEX-leaf group DISPLAYs its verbatim content (the R40 pin)");
        Assert.StartsWith("ABC", stdout);
        for (int i = 3; i < 10; i++) Assert.Equal('\0', stdout[i]);   // occurrence number 3, 8 bytes big-endian
        Assert.Equal((char)3, stdout[10]);
    }

    /// <summary>PB164's DISPLAY leg for a COMP-5 leaf CLOSED with the image widening: the group has a whole
    /// image now, and DISPLAY transfers a group's character content VERBATIM (the A.1 item-56 determination —
    /// a group is class alphanumeric), so the COMP-5 leaf's two's-complement bytes appear raw in the output,
    /// exactly as GnuCOBOL renders such a group (the split-latitude tiebreaker). GR-14.9.11.4-4's COMP-5
    /// residue is discharged; the floats closed with wave 2 and the INDEX leg with R40 — every leaf-kind
    /// display leg is now a WORKING pin.</summary>
    [Fact]
    public void DisplayComp5Group_RendersVerbatimBytes()
    {
        var (ok, stdout, _) = new CobolNetCompiler(2023).CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TIERCRE2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-G5.
               05 WS-G5-A PIC X(3) VALUE "ABC".
               05 WS-G5-N USAGE COMP-5 PIC 9(4) VALUE 7.
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY WS-G5.
                STOP RUN.
            """);
        Assert.True(ok, "an image-capable group DISPLAYs its verbatim content (kb/Work PB164 wave 1)");
        Assert.StartsWith("ABC", stdout);
        Assert.Equal('\0', stdout[3]);   // 0x0007 big-endian, verbatim
        Assert.Equal('\a', stdout[4]);
    }

    /// <summary>kb/Work PB176 — a group with BOTH an OCCURS DEPENDING table and a dynamic member must COMPILE
    /// and stage the runtime Tier-C loud. Before the <c>PlaceRenderer.GroupImage</c> capability guard (the
    /// SEVENTH two-arm-dispatch instance — the write twin <c>WriteGroupImage</c> was guarded, the read side
    /// was not), the ODO sender path emitted <c>.AsImage()</c> on a struct that never receives one, and this
    /// legal source failed BACKEND compilation with CS1061 — the loud-failure rule violated in the worst
    /// direction. The lock pins the restored posture: compiles, throws Tier-C, names the dynamic mechanism.</summary>
    [Fact]
    public void DisplayOdoGroupWithDynamicMember_FailsLoudNotCs1061()
    {
        var (ok, _, detail) = new CobolNetCompiler(2023).CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TIERCRE3.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-GO.
               05 WS-GO-N PIC 9(1) VALUE 2.
               05 WS-GO-D PIC X DYNAMIC LENGTH.
               05 WS-GO-T PIC X(3) OCCURS 1 TO 5 DEPENDING ON WS-GO-N.
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY WS-GO.
                STOP RUN.
            """);
        Assert.False(ok, "an ODO group with a dynamic member has no whole-group image — loud, never CS1061 (kb/Work PB176)");
        Assert.Contains("Tier-C", detail);
        Assert.Contains("dynamic", detail);
    }
}
