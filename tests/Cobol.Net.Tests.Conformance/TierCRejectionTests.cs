// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The Tier-C mixed-usage-group image boundary (rearchitecture PHASE-11 Step C) — the loud-failure lock. A GROUP
/// with a non-character-imageable leaf (a COMP-5 / INDEX / float / BINARY-* leaf under IsImageCapable) has no
/// whole-group character image, so the verbs that need one stage LOUD by name (COBOLNET_DESIGN §1.4, §4.2). Step
/// C routed the ~12 scattered emit guards through the ONE <c>TierCIsland.Reason</c> source, PRESERVING each
/// predicate — a behavior-neutral message collapse. These facts pin that every previously-guarded shape STILL
/// fails loud through the one reason (the "Tier-C" substring), so the consolidation is proven neutral and the
/// future confined-<c>byte[]</c> codec (Step D — deferred, DESIGN-data-model §2.3) has a lock to flip against.
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
           05 WS-G-N USAGE COMP-5 PIC 9(4).
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
}
