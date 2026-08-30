// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The Tier-C mixed-usage-group image boundary (rearchitecture PHASE-11 Step C) — the loud-failure lock. A GROUP
/// with a non-image-capable leaf has no whole-group image, so the verbs that need one stage LOUD by name
/// (COBOLNET_DESIGN §1.4, §4.2). Step C routed the ~12 scattered emit guards through the ONE
/// <c>TierCIsland.Reason</c> source, and these facts are "a lock to flip against" — kb/Work PB164 wave 1
/// FLIPPED the COMP-5/BINARY-CHAR..DOUBLE arms (their Binary byte form was pinned all along; such a group now
/// crosses MOVE/CALL — <c>2023/pb164_comp5_group_image</c> pins the working behavior), so the island's TRUE
/// remaining boundary after wave 2 (the IEEE float pin) is an INDEX leaf, and the lock fixture pins THAT. One fact pins the still-open
/// COMP-5 DISPLAY leg by name (PB164's remaining DISPLAY half), so the residue stays measured, not assumed.
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
           05 WS-G-N USAGE INDEX.
        01 WS-SRC  PIC X(7) VALUE "HELLOXX".
        01 WS-DEST PIC X(7).
        01 WS-CNT  PIC 9(2).
        PROCEDURE DIVISION.
        MAIN.
        {proc}
            STOP RUN.
        """;

    /// <summary>A mixed-usage-group shape must compile (the emit guard is a runtime LoudStmt/LoudValue, not a
    /// bind error) and fail LOUD at run time through the ONE Tier-C reason, never a silent wrong value.</summary>
    private static void AssertLoudTierC(string proc)
    {
        var (ok, _, detail) = new CobolNetCompiler(2023).CompileAndRun(Program(proc));
        Assert.False(ok, "a mixed-usage group without a character image shall fail loud (§1.4)");
        Assert.Contains("Tier-C", detail);
    }

    [Fact] public void DisplayWholeGroup_FailsLoud() => AssertLoudTierC("    DISPLAY WS-G.");
    [Fact] public void MoveIntoGroup_FailsLoud() => AssertLoudTierC("    MOVE WS-SRC TO WS-G.");
    [Fact] public void MoveGroupToElementary_FailsLoud() => AssertLoudTierC("    MOVE WS-G TO WS-DEST.");
    [Fact] public void StringIntoGroup_FailsLoud() => AssertLoudTierC("    STRING WS-SRC DELIMITED BY SIZE INTO WS-G.");
    [Fact] public void InspectGroup_FailsLoud() => AssertLoudTierC("    INSPECT WS-G REPLACING ALL \"A\" BY \"B\".");
    [Fact] public void AcceptIntoGroup_FailsLoud() => AssertLoudTierC("    ACCEPT WS-G.");

    /// <summary>PB164's DISPLAY leg for a COMP-5 leaf CLOSED with the image widening: the group has a whole
    /// image now, and DISPLAY transfers a group's character content VERBATIM (the A.1 item-56 determination —
    /// a group is class alphanumeric), so the COMP-5 leaf's two's-complement bytes appear raw in the output,
    /// exactly as GnuCOBOL renders such a group (the split-latitude tiebreaker). GR-14.9.11.4-4's COMP-5
    /// residue is discharged; the INDEX display leg remains with the island facts above (floats closed with wave 2).</summary>
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
