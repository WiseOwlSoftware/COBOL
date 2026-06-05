// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using CobolSharp.Compiler.Semantics;
using Xunit;

namespace CobolSharp.Tests.Integration;

/// <summary>
/// Flagging-conformance harness (WS-FLAG, see docs/COBOL85_COMPLIANCE_PLAN.md §3). Asserts that the NIST
/// <c>…M</c> OBSOLETE flagging modules flag exactly their obsolete elements under <c>--standard cobol85</c>.
///
/// CobolSharp declares the **high/full COBOL-85 subset** (owner decision A, DEVLOG 328–329), so the *subset-level*
/// <c>…M</c> modules (IF401M/402M/403M, IX301M/401M, RL301M/401M, SQ401M, SM301M/401M, …) — which flag standard
/// features merely "above the minimum subset" — are documented N/A and intentionally not asserted here. Only the
/// genuine obsolete-element flagging (NC303M, SQ303M) applies.
/// </summary>
public sealed class FlaggingConformanceTests : EndToEndTestBase
{
    private static int Count(IReadOnlyList<string> diags, string code) =>
        diags.Count(d => d.Contains(code));

    [Fact]
    public void SQ303M_flags_both_obsolete_elements_under_cobol85()
    {
        var diags = CompileNistDiagnostics("SQ303M", DialectMode.StrictCobol85);
        // The two obsolete elements SQ303M presents — MULTIPLE FILE TAPE and OPEN … REVERSED — both surface as
        // the generic obsolete-element flag CBL3607. SQ303M expects exactly 2 OBSOLETE flags.
        Assert.Equal(2, Count(diags, "CBL3607"));
    }

    [Fact]
    public void NC303M_flags_alter_and_bare_goto_under_cobol85()
    {
        var diags = CompileNistDiagnostics("NC303M", DialectMode.StrictCobol85);
        Assert.Equal(1, Count(diags, "CBL3602")); // ALTER obsolete
        Assert.Equal(2, Count(diags, "CBL3606")); // bare/altered GO TO obsolete ×2
        // NOTE: NC303M's 4th expected flag — the DATE-COMPILED paragraph — is deferred. The Stage-0
        // ReferenceFormatProcessor comments that obsolete IDENTIFICATION paragraph out before parsing, so
        // flagging it needs preprocessor-diagnostic plumbing (tracked in WS-FLAG task #3).
    }
}
