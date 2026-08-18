// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// kb/Work PB92 — the SET LOCALE formats (ISO §14.9.39 Formats 11 set-locale / 12 save-locale) are Annex A.4.9 item 9
/// of the optional locale module COBOL.NET does not provide; the conformant disposition (§4.2.7 / §A.4.1) is ONE
/// COBOLNET1518 — never "'LOCALE' is not defined" plus false 0901s (Format 11 before) or a bare parse error (Format 12
/// before). Below 2002 LOCALE / LC_* / USER-DEFAULT are ordinary user words (§8.9 reserves them from 2002), so a
/// COBOL-85 program that names a data item LOCALE keeps its Format-1 SET.
/// </summary>
public sealed class SetLocaleDispositionTests
{
    [Theory]
    [InlineData("SET LOCALE LC_ALL TO USER-DEFAULT.")]
    [InlineData("SET LOCALE LC_TIME TO SYSTEM-DEFAULT.")]
    [InlineData("SET LOCALE USER-DEFAULT TO WS-L.")]
    [InlineData("SET WS-L TO LOCALE LC_ALL.")]
    [InlineData("SET WS-L TO LOCALE USER-DEFAULT.")]
    public void SetLocale_IsExactlyOneDocumentedNonSupportDiagnostic(string stmt)
    {
        string src = $$"""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. PB92T.
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
                   01 WS-L PIC X(20).
                   PROCEDURE DIVISION.
                       {{stmt}}
                       STOP RUN.
            """;
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2023);
        Assert.False(ok, "SET LOCALE is A.4.9 item 9 documented non-support");
        Assert.Single(errors);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET1518");
    }

    /// <summary>At COBOL-85 LOCALE is a user word: `SET LOCALE TO 5` over an integer item named LOCALE is Format 1
    /// (§14.9.39 — an elementary integer item receiver) and runs; the locale arms are edition-gated off.</summary>
    [Fact]
    public void At85_LocaleIsAUserWord_SetFormat1Runs()
    {
        string src = """
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. PB92T85.
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
                   01 LOCALE PIC 9.
                   PROCEDURE DIVISION.
                       SET LOCALE TO 5.
                       DISPLAY LOCALE.
                       STOP RUN.
            """;
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(src, 85);
        Assert.True(ok, detail);
        Assert.Equal("5", stdout.Trim());
    }
}
