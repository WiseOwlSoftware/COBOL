// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ISO §8.4.3.2.3 SR6 as a CATALOG-DRIVEN sweep (kb/Work PB61, row SR-8.4.3.2.3-6): "If a function's definition
/// permits arguments and a left parenthesis immediately follows … intrinsic-function-name-1, the left parenthesis
/// is always treated as the left parenthesis of that function's arguments." So for EVERY catalogued intrinsic
/// whose definition permits arguments (MaxArgs &gt; 0), in BOTH reference forms, <c>NAME (1:4)</c> opens an
/// argument list, and <c>1:4</c> is not an argument (SR8) — the diagnostic is COBOLNET1543 (the argument-list
/// verdict), never the §15.3 arity error about an empty list the user did not write, and never the undefined-name
/// verdict of the data path. A function whose definition permits NO arguments (MaxArgs 0) is outside SR6, so
/// <c>(1:4)</c> there IS a reference modification of the result (§8.4.3.3.3 SR2 then decides on class).
/// <para>Before PB61 only the reserved-word RANDOM drew 1543: <c>FUNCTION UPPER-CASE (1:4)</c> drew "takes 1
/// argument(s); 0 given" (the argument list bound BEFORE SR6 was asked) and keyword-omitted
/// <c>UPPER-CASE (1:4)</c> under FUNCTION ALL INTRINSIC died as "'UPPER-CASE(1:4)' is not defined". The sweep
/// reads the catalog SOURCE for the row set (the <c>IntrinsicArgumentClassDriftTests</c> precedent — the table
/// is not enumerable by design), so a function added later is swept automatically.</para>
/// </summary>
public sealed class FunctionRefModSr6SweepTests
{
    private static readonly Regex Row = new(
        "Add\\(new\\(\"(?<n>[A-Z0-9-]+)\",\\s*IntrinsicType\\.\\w+,\\s*IntrinsicArity\\.\\w+,"
        + "\\s*(?<min>[-\\w]+),\\s*(?<max>[-\\w]+),",
        RegexOptions.Compiled);

    private static List<(string Name, int Min, string Max)> CatalogRows()
    {
        string src = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "Binding", "IntrinsicCatalog.cs"));
        var rows = Row.Matches(src).Select(m => (m.Groups["n"].Value, int.Parse(m.Groups["min"].Value), m.Groups["max"].Value)).ToList();
        Assert.True(rows.Count >= 79, $"only {rows.Count} catalog rows parsed — the Add(new(...)) shape changed; fix the regex.");
        return rows;
    }

    private static string Program(string pid, string repository, string statement) => $$"""
               IDENTIFICATION DIVISION.
               PROGRAM-ID. {{pid}}.
               ENVIRONMENT DIVISION.
               CONFIGURATION SECTION.
               REPOSITORY.
               {{repository}}
               DATA DIVISION.
               WORKING-STORAGE SECTION.
               01 T4 PIC X(4).
               PROCEDURE DIVISION.
                   {{statement}}
                   STOP RUN.
               END PROGRAM {{pid}}.
        """;

    public static IEnumerable<object[]> ArgumentPermittingFunctions() =>
        CatalogRows().Where(r => r.Max != "0").Select(r => new object[] { r.Name });

    public static IEnumerable<object[]> ZeroArgumentFunctions() =>
        CatalogRows().Where(r => r.Max == "0").Select(r => new object[] { r.Name });

    /// <summary>The FUNCTION-keyword form: <c>MOVE FUNCTION name (1:4) TO T4</c> — SR6/SR8, COBOLNET1543, and NOT
    /// the arity error (1504) that a bind-before-SR6 ordering produces.</summary>
    [Theory]
    [MemberData(nameof(ArgumentPermittingFunctions))]
    public void KeywordForm_BareRefModAfterArgumentPermittingFunction_IsAnArgumentListError(string name)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(
            Program("SR6K", "    FUNCTION ALL INTRINSIC.", $"MOVE FUNCTION {name} (1:4) TO T4"), 2023);
        Assert.False(ok, $"FUNCTION {name} (1:4) must be rejected (ISO §8.4.3.2.3 SR6/SR8)");
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET1543");
        Assert.DoesNotContain(errors, e => e.Contains("COBOLNET1504"));   // the arity error about an unwritten list
    }

    /// <summary>The keyword-OMITTED form under FUNCTION ALL INTRINSIC: <c>MOVE name (1:4) TO T4</c> — the same
    /// SR6/SR8 verdict, never the data path's "'name(1:4)' is not defined" (COBOLNET1639).</summary>
    [Theory]
    [MemberData(nameof(ArgumentPermittingFunctions))]
    public void KeywordOmittedForm_BareRefModAfterArgumentPermittingFunction_IsAnArgumentListError(string name)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(
            Program("SR6O", "    FUNCTION ALL INTRINSIC.", $"MOVE {name} (1:4) TO T4"), 2023);
        Assert.False(ok, $"{name} (1:4) under FUNCTION ALL INTRINSIC must be rejected (ISO §8.4.3.2.3 SR6/SR8)");
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET1543");
        Assert.DoesNotContain(errors, e => e.Contains("COBOLNET1639"));
        Assert.DoesNotContain(errors, e => e.Contains("COBOLNET1504"));
    }

    /// <summary>A zero-argument function is OUTSIDE SR6: <c>FUNCTION name (1:4)</c> is a reference modification of
    /// the RESULT — accepted for an alphanumeric result (§8.4.3.3.3 SR2), rejected as COBOLNET1629 (SR2's class
    /// rule) for a numeric one, and a NATIONAL result then meets Table 16 at the MOVE (COBOLNET0819) — in no case
    /// the SR6 verdict, and never the arity error.</summary>
    [Theory]
    [MemberData(nameof(ZeroArgumentFunctions))]
    public void ZeroArgumentFunction_RefModIsNotAnArgumentList(string name)
    {
        var (_, errors, _) = EditionHarness.CompileFull(
            Program("SR6Z", "    FUNCTION ALL INTRINSIC.", $"MOVE FUNCTION {name} (1:4) TO T4"), 2023);
        Assert.DoesNotContain(errors, e => e.Contains("COBOLNET1543"));
        Assert.DoesNotContain(errors, e => e.Contains("COBOLNET1504"));
    }
}
