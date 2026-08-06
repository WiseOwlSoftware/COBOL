// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Diagnostics;
using Xunit;
using CnFrontend = CobolNet.Frontend.Frontend;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The edition gate on the §12.3.7 SPECIAL-NAMES <c>LOCALE</c> clause (fix-queue PB25).
///
/// <para>The clause is an <b>§A.4.9 item 10</b> optional-locale element, so at 2002+ it must PARSE and be
/// DIAGNOSED as documented non-support (COBOLNET1518) rather than fail as a parse error — non-support is
/// conformant per §4.2.7 / §A.4.1 only when it is diagnosed.</para>
///
/// <para>⛔ WHY THE GATE IS LOAD-BEARING, AND WHY IT IS TESTED. <b>LOCALE is reserved only from 2002</b>
/// (§8.9; <c>reserved-words.json</c> r85 = false, r2002/r2014/r2023 = true). At COBOL-85 it is an ordinary
/// user-defined word, so <c>SPECIAL-NAMES. LOCALE IS FOO.</c> is a perfectly legal implementor-switch entry
/// there and must keep parsing as one. A predicate that fired at every edition would turn legal 85 source into a
/// documented-non-support error — the exact class of breakage <c>edition_gate_sweep</c> exists for.</para>
/// </summary>
public sealed class SpecialNamesLocaleEditionTests
{
    private static DiagnosticBag Parse(string specialNames, int edition)
    {
        string src = "IDENTIFICATION DIVISION.\nPROGRAM-ID. SNLOCALE.\nENVIRONMENT DIVISION.\n"
            + "CONFIGURATION SECTION.\nSPECIAL-NAMES.\n" + specialNames
            + "DATA DIVISION.\nWORKING-STORAGE SECTION.\n01 X PIC X(3) VALUE \"AbC\".\n"
            + "PROCEDURE DIVISION.\nMAIN.\n    DISPLAY X\n    STOP RUN.\n";
        string path = Path.Combine(Path.GetTempPath(), "cn_snloc_" + Guid.NewGuid().ToString("N")[..8] + ".cob");
        File.WriteAllText(path, src);
        try
        {
            var diags = new DiagnosticBag();
            new CnFrontend { DialectLevel = edition }.Parse(path, diags);
            return diags;
        }
        finally { try { File.Delete(path); } catch { /* best-effort */ } }
    }

    /// <summary>At 2002+ the clause PARSES — the binder is what rejects it, with the cited code. A parse error
    /// here would mean the diagnostic never gets the chance to name §A.4.9.</summary>
    [Theory]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void TheLocaleClause_Parses_SoTheBinderCanDiagnoseIt(int edition)
    {
        var diags = Parse("    LOCALE FR IS \"fr_FR\".\n", edition);
        Assert.False(diags.HasErrors,
            "the SPECIAL-NAMES LOCALE clause must PARSE at 2002+ so it can be diagnosed as §A.4.9 documented "
            + "non-support; a parse error is an unexplained rejection. Got: "
            + string.Join(" | ", diags.Diagnostics.Select(d => d.ToString())));
    }

    /// <summary>⛔ THE COUNTERPART THAT MUST NOT BREAK: at COBOL-85, LOCALE is a user word, so
    /// <c>LOCALE IS FOO</c> is an ordinary implementor-switch entry and still parses.</summary>
    [Fact]
    public void At85_LocaleIsAnOrdinaryUserWord_AndTheSwitchEntryStillParses()
    {
        var diags = Parse("    LOCALE IS FOO.\n", 85);
        Assert.False(diags.HasErrors,
            "`SPECIAL-NAMES. LOCALE IS FOO.` is legal COBOL-85 (LOCALE is not reserved until 2002, §8.9) and must "
            + "still parse as an implementor-switch entry. Got: "
            + string.Join(" | ", diags.Diagnostics.Select(d => d.ToString())));
    }

    /// <summary>…and the same bare switch shape is still a switch entry at 2023, because the predicate requires a
    /// locale-NAME after the word. Without this, the predicate would annex `LOCALE IS mnemonic` as a malformed
    /// locale clause at every modern edition.</summary>
    [Fact]
    public void At2023_ABareLocaleIsSwitchEntry_IsNotAnnexedAsALocaleClause()
    {
        var diags = Parse("    LOCALE IS FOO.\n", 2023);
        Assert.False(diags.HasErrors,
            "`LOCALE IS FOO` has no locale-name, so it is not the §12.3.7 LOCALE clause and must fall through to "
            + "implementorSwitchEntry. Got: " + string.Join(" | ", diags.Diagnostics.Select(d => d.ToString())));
    }
}
