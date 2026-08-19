// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CobolNet;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The §A.4.9 locale-module disposition's SECOND half (fix-queue PB27 §③): a function that is documented
/// non-support (rejected by COBOLNET1518 at every edition) must NOT also assert an INTRODUCTION EDITION — and,
/// since kb/Work PB64 T4 made the four locale FUNCTIONS live, the converse: their D8 edition window IS enforced
/// again (COBOLNET1502 below it, the function binding at and after it). What still carries the non-support
/// disposition is the LOCALE keyword PHRASE of LOWER-CASE / UPPER-CASE / NUMVAL-C / TEST-NUMVAL-C (T5/T6), and
/// that phrase draws 1518 at every edition with no 1502 beside it.
/// <para>
/// ⛔ COBOLNET1502 STATES A FACT ABOUT THE STANDARD — "was introduced by ISO/IEC 1989:{year}" — AND FOR THIS
/// FAMILY THAT YEAR IS UNVERIFIABLE. The 2023 standard carries no introduction record: §8.11 lists the intrinsic
/// NAMES with no edition data, Annex E covers only 2014→2023 and does not mention these functions, the repo
/// holds no 2002 or 2014 text (`specs-private/` is the 2023 PDF alone), the reserved-word tables do not carry
/// intrinsic names (§8.11, not §8.9), and GnuCOBOL's testsuite pins no `-std=` on them. Every source was
/// checked; the value in the catalog is hand-assigned.
/// </para>
/// <para>
/// ⚠ AND THE CLAIM IS REDUNDANT: `Bind = Unsupported` rejects the reference at EVERY edition, so no `--std`
/// makes the program compile and the edition sentence adds nothing actionable — only risk. The suppression keys
/// on <c>Bind</c>, so implementing the locale module restores the gate, at which point the introduction year
/// would have to be verified. That is the forcing function, and it is why this is a suppression rather than a
/// deletion of the data.
/// </para>
/// </summary>
public sealed class LocaleModuleNonSupportTests
{
    private static IReadOnlyList<string> CompileErrors(string procedureBody, int edition)
    {
        string dir = Path.Combine(Path.GetTempPath(), "cn_a49_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            string src = Path.Combine(dir, "a49.cob");
            File.WriteAllText(src,
                "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. A49NS.\n"
                + "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n"
                + "       01 R PIC X(20).\n       01 S PIC 9(8) VALUE 3600.\n"
                + "       PROCEDURE DIVISION.\n       MAIN.\n" + procedureBody + "           STOP RUN.\n");
            return CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "a49.dll"), DialectLevel: edition, CheckOnly: true)).Errors;
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ } }
    }

    /// <summary>At EVERY edition the non-support diagnostic is the whole story for a still-unclaimed element — the
    /// LOCALE phrase of LOWER-CASE (T5) — and never an edition claim beside it.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void ALocalePhrase_ReportsNonSupportOnly_AtEveryEdition(int edition)
    {
        var errors = CompileErrors("           MOVE FUNCTION LOWER-CASE(R LOCALE FR) TO R.\n", edition);
        Assert.Contains(errors, e => e.Contains("COBOLNET1518"));
        Assert.DoesNotContain(errors, e => e.Contains("COBOLNET1502"));
    }

    /// <summary>⚙ THE SUPPRESSION LIFTED (kb/Work PB64 T4 — the forcing function the class summary promised): the four
    /// locale functions bind Runtime, so their D8 window is enforced again — LOCALE-TIME-FROM-SECONDS (2014, kb/Work
    /// R28's provisional edge) is COBOLNET1502 below 2014 and binds at 2014/2023; LOCALE-DATE (2002) likewise below
    /// 2002. Never 1518 any more.</summary>
    [Theory]
    [InlineData(85, true)]
    [InlineData(2002, true)]
    [InlineData(2014, false)]
    [InlineData(2023, false)]
    public void ALocaleFunction_KeepsItsEditionWindow_NowThatItIsLive(int edition, bool rejected)
    {
        var errors = CompileErrors("           MOVE FUNCTION LOCALE-TIME-FROM-SECONDS(S) TO R.\n", edition);
        Assert.DoesNotContain(errors, e => e.Contains("COBOLNET1518"));
        if (rejected) Assert.Contains(errors, e => e.Contains("COBOLNET1502") && e.Contains("LOCALE-TIME-FROM-SECONDS"));
        else Assert.DoesNotContain(errors, e => e.Contains("LOCALE-TIME-FROM-SECONDS"));
    }

    /// <summary>⛔ THE FAILING DIRECTION, which is what keeps the suppression scoped to non-support rather than
    /// quietly disabling the D8 edition window for every function. TRIM is §15.90, IntroducedIn 2014 and fully
    /// SUPPORTED, so below 2014 it must still be rejected BY EDITION.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    public void ASupportedFunction_KeepsItsEditionGate(int edition)
    {
        var errors = CompileErrors("           MOVE FUNCTION TRIM(\"  ab  \") TO R.\n", edition);
        Assert.Contains(errors, e => e.Contains("COBOLNET1502") && e.Contains("TRIM"));
    }

    /// <summary>…and at its own edition that same function compiles, so the gate is a WINDOW and not a ban.</summary>
    [Fact]
    public void ASupportedFunction_CompilesAtItsOwnEdition()
    {
        var errors = CompileErrors("           MOVE FUNCTION TRIM(\"  ab  \") TO R.\n", 2014);
        Assert.DoesNotContain(errors, e => e.Contains("COBOLNET1502"));
    }
}
