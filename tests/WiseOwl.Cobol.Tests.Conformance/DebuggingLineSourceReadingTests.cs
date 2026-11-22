// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;
using WiseOwl.Cobol.Frontend.Preprocessor;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// ⭐ THE EXACT ERROR SET of the two COBOL-85 debugging-line shapes whose goldens can only assert that a diagnostic APPEARS
/// (<c>EditionHarness.AssertHasDiagnostic</c>; the wiseowl/diagnostics schema has no absent or exact form). §1985:VI.7.3 reads the
/// debugging lines of a unit without WITH DEBUGGING MODE as source too (COBOL7061, kb/Work PB5990); that reading must blame a
/// hidden line only for an error ON a hidden line, and must not ask again of a line COBOL7060 already refused. A revert of either
/// filter in <c>Frontend.ReportDebuggingLinesBreakingTheProgramAsSource</c> adds a COBOL7061 these tests count.
/// </summary>
public sealed class DebuggingLineSourceReadingTests
{
    private static string[] Errors(params string[] lines)
    {
        var (_, errors, _) = EditionHarness.CompileFull(string.Join(Environment.NewLine, lines) + Environment.NewLine, 85, format: InitialReferenceFormat.Fixed);
        return [.. errors];
    }

    /// <summary>A valid D line followed by a plain line that breaks a separator rule (the plain line's own COBOL2633): the valid
    /// debugging line is not blamed, so the one error is COBOL2633 (review R2-2).</summary>
    [Fact]
    public void AValidDebuggingLine_IsNotBlamedForAnErrorOnAPlainLine()
    {
        var errors = Errors(
            "000100 IDENTIFICATION DIVISION.", "000200 PROGRAM-ID. PB5990G.", "000300 ENVIRONMENT DIVISION.",
            "000400 CONFIGURATION SECTION.", "000500 SOURCE-COMPUTER. COMPUTER-1.", "000600 OBJECT-COMPUTER. COMPUTER-1.",
            "000700 PROCEDURE DIVISION.", "000800 P1.", "000900     DISPLAY \"START\".",
            "001000D    DISPLAY \"OK\".", "001100     DISPLAY \"A\"\"B\"'C'.", "001200     STOP RUN.");
        Assert.Single(errors);
        Assert.Contains("COBOL2633", errors[0]);
        Assert.DoesNotContain(errors, e => e.Contains("COBOL7061"));
    }

    /// <summary>A D line before the end of OBJECT-COMPUTER in a unit without the mode is COBOL7060 once: it stays a comment in the
    /// as-source reading, so one cause draws one code (review R1-1). The line is no valid source where it stands, so a reading
    /// that took it as source would add COBOL7061.</summary>
    [Fact]
    public void AMisplacedDebuggingLine_IsReportedOnce_AsCobol7060()
    {
        var errors = Errors(
            "000100 IDENTIFICATION DIVISION.", "000200 PROGRAM-ID. PB5990E.", "000300D    DISPLAY \"X\".",
            "000400 ENVIRONMENT DIVISION.", "000500 CONFIGURATION SECTION.", "000600 SOURCE-COMPUTER. COMPUTER-1.",
            "000700 OBJECT-COMPUTER. COMPUTER-1.", "000800 PROCEDURE DIVISION.", "000900 P1.",
            "001000     DISPLAY \"END\".", "001100     STOP RUN.");
        Assert.Single(errors);
        Assert.Contains("COBOL7060", errors[0]);
        Assert.DoesNotContain(errors, e => e.Contains("COBOL7061"));
    }
}
