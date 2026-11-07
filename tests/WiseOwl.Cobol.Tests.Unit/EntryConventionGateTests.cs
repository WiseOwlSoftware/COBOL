// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Compiler;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>kb/Work PB4611: the OPTIONS paragraph's ENTRY-CONVENTION clause is a COBOL-2002 clause
/// (ISO/IEC 1989:2002 11.8.5, <c>ENTRY-CONVENTION IS entry-convention-name-1</c>); only its COBOL alternative is a
/// COBOL-2014 addition (the 2014 FCD format, and §2023:11.9.7.4 GR2). So at <c>--std 2002</c> a named convention draws the
/// COBOL2385 refusal of §2023:11.9.7.4 GR3 (this implementation defines no entry-convention-name) ALONE, with no COBOL0900
/// "requires COBOL-2014" beside it, and <c>ENTRY-CONVENTION IS COBOL</c> draws the edition gate once. The conformance
/// refusals pin each code's presence; this pins the ABSENCE of the false one, which a refusal test cannot.</summary>
public sealed class EntryConventionGateTests
{
    private static string Source(string convention) =>
        "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. PB4611W.\n       OPTIONS.\n"
        + $"           ENTRY-CONVENTION IS {convention}.\n       PROCEDURE DIVISION.\n           STOP RUN.\n";

    private static List<string> ErrorsOf(string convention, int dialect)
    {
        string dir = ScratchDirectory.Create("WiseOwlCobol_PB4611W_");
        try
        {
            string src = Path.Combine(dir, "pb4611w.cob");
            File.WriteAllText(src, Source(convention));
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "pb4611w.dll"), DialectLevel: dialect, CheckOnly: true, SourceFormat: InitialReferenceFormat.Auto));
            return [.. r.Errors];
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    [Theory]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void ANamedConvention_DrawsTheNoConventionRefusalAlone_FromTheEditionTheClauseIsIn(int dialect)
    {
        var errors = ErrorsOf("STDCALL", dialect);
        Assert.Single(errors, e => e.Contains("COBOL2385"));
        Assert.DoesNotContain(errors, e => e.Contains("COBOL0900"));
    }

    [Theory]
    [InlineData(2002, true)]
    [InlineData(2014, false)]
    [InlineData(2023, false)]
    public void TheCobolAlternative_DrawsTheEditionGate_BelowTheEditionItWasAdded(int dialect, bool gated)
    {
        var errors = ErrorsOf("COBOL", dialect);
        Assert.Equal(gated, errors.Any(e => e.Contains("COBOL0900") && e.Contains("ENTRY-CONVENTION IS COBOL requires COBOL-2014")));
        Assert.DoesNotContain(errors, e => e.Contains("COBOL2385"));
    }
}
