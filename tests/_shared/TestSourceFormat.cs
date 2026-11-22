// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Preprocessor;

namespace WiseOwl.Cobol.Tests.Shared;

/// <summary>
/// ⛔ THE REFERENCE FORMAT A TEST'S OWN PROGRAM TEXT IS READ IN (kb/Work PB1757, R57, PB6033). No test harness lets the
/// detector (<see cref="InitialReferenceFormat.Auto"/>) choose: a heuristic can misread conforming text (the sequence area may
/// hold any character, §2023:6.3.2, and text past margin R is legal), so a test that relied on it did not know which reference
/// format it exercised; R57 measured the old corpus and found 2,905 of 3,576 goldens read as free form that way. Each source a
/// test compiles therefore STATES its format:
/// <list type="bullet">
/// <item>a conformance-suite case states it in its header (<c>*&gt; source-format:</c>, which the generator requires and
/// <c>ConformanceSuiteRunner</c> passes);</item>
/// <item>a NIST CCVS program and a sample are fixed form (<see cref="InitialReferenceFormat.Fixed"/>, the CLI's default,
/// §2023:7.3.24.3 GR2);</item>
/// <item>program text written inside a C# test is free form (<see cref="Inline"/>): it is written from column 1 with no
/// sequence or indicator area, and free form is the documented selection available at every edition
/// (<see cref="InitialReferenceFormat.Free"/>; docs/CONFORMANCE.md DOC-A.1-158). A test whose program exercises a fixed-form
/// rule (the indicator area, margin R, a debugging line, a continuation) passes <see cref="InitialReferenceFormat.Fixed"/>
/// explicitly.</item>
/// </list>
/// <c>TestSourceFormatDriftTests</c> holds that <see cref="InitialReferenceFormat.Auto"/> appears under <c>tests/</c> only in
/// the tests of the detector itself.
/// </summary>
public static class TestSourceFormat
{
    /// <summary>The format of program text written inside a C# test: free form, stated.</summary>
    public const InitialReferenceFormat Inline = InitialReferenceFormat.Free;

    /// <summary>The reference format a conformance-suite case's text STATES in its header (<c>*&gt; source-format: fixed</c>),
    /// the one <c>ConformanceSuiteRunner</c> compiles it in: a C# test that compiles a case's text compiles it in that format,
    /// never as <see cref="Inline"/> text (kb/Work PB6037: a fixed-form case read free form at <c>--std 85</c> is no 1985
    /// witness, kb/Work PB5754). A case whose header states no format is refused: the generator requires the line.</summary>
    public static InitialReferenceFormat OfSuiteCase(string text)
    {
        var stated = System.Text.RegularExpressions.Regex.Match(text, @"^\s*\*>\s*source-format:\s*(\S+)",
            System.Text.RegularExpressions.RegexOptions.Multiline);
        if (stated.Success && InitialReferenceFormatOption.TryParse(stated.Groups[1].Value, out var format)) return format;
        throw new InvalidOperationException("the conformance-suite case states no readable '*> source-format:' line in its header");
    }
}
