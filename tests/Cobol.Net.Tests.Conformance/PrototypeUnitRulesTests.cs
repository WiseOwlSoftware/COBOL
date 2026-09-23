// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ISO §10.6 — the prototype source-unit rules, over EVERY prototype kind (kb/Work PB894). §10.6.2 SR4 says its
/// six restrictions "apply to program prototypes, function prototypes, and method prototypes", so each restriction
/// is asserted on a PROGRAM prototype AND on a FUNCTION prototype: a screen that one arm reaches and the other
/// does not is exactly the two-arm shape this repository keeps producing. Each program carries ONE violation, and
/// the legal twin (<see cref="LinkageAndHeaderOnly_AdmittedSpecialNames_Compile"/>) proves the screen does not
/// over-reject what SR4 c) and e) admit.
/// </summary>
public sealed class PrototypeUnitRulesTests
{
    private const string PrototypeBody = "COBOLNET2272";
    private const string PrototypeUnitFormat = "COBOLNET2274";

    private static string Prototype(bool function, string name, string options = "", string environment = "",
        string data = "", string procedure = "", bool endMarker = true)
    {
        string id = function ? "FUNCTION-ID" : "PROGRAM-ID";
        string end = function ? "END FUNCTION" : "END PROGRAM";
        string returning = function ? " RETURNING L-R" : "";
        return $"""
            IDENTIFICATION DIVISION.
            {id}. {name} IS PROTOTYPE.
            {options}
            {environment}
            DATA DIVISION.
            {data}
            LINKAGE SECTION.
            01 L-N PIC 9(4).
            01 L-R PIC 9(4).
            PROCEDURE DIVISION USING L-N{returning}.
            {procedure}
            {(endMarker ? $"{end} {name}." : "")}
            IDENTIFICATION DIVISION.
            PROGRAM-ID. M{name}.
            PROCEDURE DIVISION.
            MAIN-PARA.
                STOP RUN.
            END PROGRAM M{name}.

            """;
    }

    /// <summary>SR4 a) — "The identification division shall not contain an ARITHMETIC clause."</summary>
    [Theory]
    [InlineData(false, "PUA1")]
    [InlineData(true, "FUA1")]
    public void Sr4a_ArithmeticClause_IsRefused(bool function, string name) =>
        EditionHarness.AssertHasDiagnostic(EditionHarness.GetDiagnostics(
            Prototype(function, name, options: "OPTIONS.\n    ARITHMETIC IS STANDARD-DECIMAL."), 2023), PrototypeBody);

    /// <summary>SR4 b) — "The environment division shall not contain an object-computer paragraph."</summary>
    [Theory]
    [InlineData(false, "PUB1")]
    [InlineData(true, "FUB1")]
    public void Sr4b_ObjectComputer_IsRefused(bool function, string name) =>
        EditionHarness.AssertHasDiagnostic(EditionHarness.GetDiagnostics(
            Prototype(function, name, environment: "ENVIRONMENT DIVISION.\nCONFIGURATION SECTION.\nOBJECT-COMPUTER. X."),
            2023), PrototypeBody);

    /// <summary>SR4 c) — the SPECIAL-NAMES paragraph admits only ALPHABET, CURRENCY, DECIMAL-POINT, LOCALE and
    /// SYMBOLIC CHARACTERS; a CLASS clause is none of them.</summary>
    [Theory]
    [InlineData(false, "PUC1")]
    [InlineData(true, "FUC1")]
    public void Sr4c_ClassClauseInSpecialNames_IsRefused(bool function, string name) =>
        EditionHarness.AssertHasDiagnostic(EditionHarness.GetDiagnostics(
            Prototype(function, name,
                environment: "ENVIRONMENT DIVISION.\nCONFIGURATION SECTION.\nSPECIAL-NAMES.\n    CLASS DIGIT-CLASS IS \"0\" THRU \"9\"."),
            2023), PrototypeBody);

    /// <summary>SR4 d) — "The environment division shall not contain an input-output section."</summary>
    [Theory]
    [InlineData(false, "PUD1")]
    [InlineData(true, "FUD1")]
    public void Sr4d_InputOutputSection_IsRefused(bool function, string name) =>
        EditionHarness.AssertHasDiagnostic(EditionHarness.GetDiagnostics(
            Prototype(function, name, environment: "ENVIRONMENT DIVISION.\nINPUT-OUTPUT SECTION."), 2023),
            PrototypeBody);

    /// <summary>SR4 e) — "The data division may contain only a linkage section."</summary>
    [Theory]
    [InlineData(false, "PUE1", "WORKING-STORAGE SECTION.\n01 W PIC 9.")]
    [InlineData(true, "FUE1", "WORKING-STORAGE SECTION.\n01 W PIC 9.")]
    [InlineData(false, "PUE2", "LOCAL-STORAGE SECTION.\n01 W PIC 9.")]
    [InlineData(true, "FUE2", "LOCAL-STORAGE SECTION.\n01 W PIC 9.")]
    public void Sr4e_NonLinkageSection_IsRefused(bool function, string name, string data) =>
        EditionHarness.AssertHasDiagnostic(EditionHarness.GetDiagnostics(Prototype(function, name, data: data), 2023),
            PrototypeBody);

    /// <summary>SR4 f) — "The procedure division shall contain only a procedure division header." Before PB894
    /// a FUNCTION prototype's statements compiled clean and silently vanished (a prototype emits no body).</summary>
    [Theory]
    [InlineData(false, "PUF1")]
    [InlineData(true, "FUF1")]
    public void Sr4f_ProcedureStatements_AreRefused(bool function, string name) =>
        EditionHarness.AssertHasDiagnostic(EditionHarness.GetDiagnostics(
            Prototype(function, name, procedure: "P-MAIN.\n    MOVE 1 TO L-R."), 2023), PrototypeBody);

    /// <summary>§10.6.1 — the prototype formats print the end marker unbracketed.</summary>
    [Theory]
    [InlineData(false, "PUG1")]
    [InlineData(true, "FUG1")]
    public void MissingEndMarker_IsRefused(bool function, string name) =>
        EditionHarness.AssertHasDiagnostic(EditionHarness.GetDiagnostics(
            Prototype(function, name, endMarker: false), 2023), PrototypeUnitFormat);

    /// <summary>The admitted twin: a LINKAGE SECTION, a header-only procedure division and the SR4 c) clauses
    /// compile, for both kinds. ⛔ The assertion is that the COMPILE SUCCEEDS, not merely that 2272 is absent.</summary>
    [Theory]
    [InlineData(false, "PUH1")]
    [InlineData(true, "FUH1")]
    public void LinkageAndHeaderOnly_AdmittedSpecialNames_Compile(bool function, string name)
    {
        var (ok, diagnostics) = EditionHarness.Compile(Prototype(function, name,
            environment: "ENVIRONMENT DIVISION.\nCONFIGURATION SECTION.\nSPECIAL-NAMES.\n    DECIMAL-POINT IS COMMA."),
            2023);
        EditionHarness.AssertNoDiagnostic(diagnostics, PrototypeBody);
        Assert.True(ok, string.Join("\n", diagnostics));
    }
}
