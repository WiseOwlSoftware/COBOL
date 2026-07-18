// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The <c>&gt;&gt;PROPAGATE</c> directive (ISO §7.3.21; PHASE-12 wave 4/5): the COBOL-2002+ compiler directive that
/// controls automatic exception-condition propagation to the activating runtime element (GR1/GR2; default OFF,
/// GR4). This wave RECOGNIZES the directive and EDITION-GATES it (introduction gate, provisional COBOL-2002 per the
/// roadmap decision-1 policy — §7.3.21 is live in the 2023 spec, so the 2002-vs-2014 edge cannot be pinned in-repo;
/// the runtime propagation SEMANTICS are the deferred PHASE-13 EC work). Below 2002 it is COBOLNET0883, never a
/// silent stray token; at 2002+ a well-formed <c>&gt;&gt;PROPAGATE ON|OFF</c> is recognized-and-consumed and the
/// program compiles (the run behavior is the <c>propagate_directive</c> conformance corpus).
/// </summary>
public sealed class PropagateDirectiveTests
{
    private static string Prog(string directive) => directive + "\n" + """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. PROPG.
        PROCEDURE DIVISION.
        MAIN-PARA.
            DISPLAY "X".
            STOP RUN.
        """;

    /// <summary>A well-formed >>PROPAGATE ON/OFF is recognized (recognized-and-consumed) at COBOL-2002+.</summary>
    [Theory]
    [InlineData(2002, "      >>PROPAGATE ON")]
    [InlineData(2014, "      >>PROPAGATE ON")]
    [InlineData(2023, "      >>PROPAGATE OFF")]
    [InlineData(2014, "      >>PROPAGATE")]
    public void WellFormed_RecognizedAt2002Plus(int edition, string directive)
    {
        var (ok, diag) = EditionHarness.Compile(Prog(directive), edition);
        Assert.True(ok, $">>PROPAGATE must be recognized at COBOL-{edition}:\n{string.Join("\n", diag)}");
    }

    /// <summary>The introduction gate: >>PROPAGATE is rejected below 2002 with COBOLNET0883 (never a silent stray
    /// token that would surface as a generic parse error).</summary>
    [Fact]
    public void BelowIntroduction_Rejected0883()
    {
        var (ok, diag) = EditionHarness.Compile(Prog("      >>PROPAGATE ON"), 85);
        Assert.False(ok, ">>PROPAGATE must be rejected at COBOL-85 (introduced 2002)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET0883");
    }

    /// <summary>A malformed operand (not ON/OFF) is the §7.3.21.2 syntax diagnostic, never a silent accept.</summary>
    [Fact]
    public void MalformedOperand_Rejected0883()
    {
        var (ok, diag) = EditionHarness.Compile(Prog("      >>PROPAGATE MAYBE"), 2014);
        Assert.False(ok, ">>PROPAGATE with a non-ON/OFF operand must be rejected (ISO §7.3.21.2)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET0883");
    }
}
