// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// ⛔ A CURRENCY SIGN CLAUSE REFUSED FOR ITS CURRENCY STRING STILL NAMES ITS PICTURE SYMBOL (kb/Work PB1920). In the
/// PICTURE SYMBOL form (ISO §12.3.7.3 SR23: "literal-7 is the currency string and literal-8 is the associated currency
/// symbol") the two operands carry rules of their own, so a literal-7 refused under SR18 (a figurative constant),
/// SR23 a) (no non-space character), SR23 b) (a digit or one of + - , . *) or §8.4.3.10.3 SR1 (NULL) leaves a valid
/// literal-8 standing. <c>DataBinder.SwitchBindCurrency</c> used to return before registering it, so every PICTURE
/// using the symbol drew COBOL0808 / COBOL0819 behind the clause's own error. Each shape now compiles to exactly the
/// clause's diagnostic.
/// </summary>
public sealed class CurrencySignRefusalTests
{
    private static string Program(string literal7) =>
        "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. PB1920T.\n"
        + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       SPECIAL-NAMES.\n"
        + $"           CURRENCY SIGN IS {literal7} WITH PICTURE SYMBOL \"#\".\n"
        + "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n"
        + "       01 A PIC #9.99.\n       01 B PIC ##9.99.\n"
        + "       PROCEDURE DIVISION.\n           DISPLAY A B\n           STOP RUN.\n";

    [Theory]
    [InlineData("SPACE", "COBOL0892", "SR18")]
    [InlineData("\"   \"", "COBOL0890", "r23a")]
    [InlineData("\"E1\"", "COBOL0890", "r23b")]
    [InlineData("NULL", "COBOL2576", "§8.4.3.10.3 SR1")]
    public void ARefusedCurrencyString_IsTheClausesOneDiagnostic(string literal7, string code, string rule)
    {
        var (ok, errors) = EditionHarness.Compile(Program(literal7), 2023);
        Assert.False(ok);
        Assert.True(errors.Count == 1, $"expected the clause's one diagnostic; got:\n{string.Join("\n", errors)}");
        EditionHarness.AssertHasDiagnostic(errors, code);
        EditionHarness.AssertHasDiagnostic(errors, rule);
    }

    /// <summary>A later clause that binds the same symbol to a valid string takes it over: the refused clause states no
    /// string, so §12.3.7.3 SR21 ("unless they specify identical currency strings") finds no conflict, and the
    /// valid clause's string and class are the ones its PICTUREs read.</summary>
    [Fact]
    public void ALaterValidClause_BindsTheSymbolWithoutAnSr21Conflict()
    {
        string source =
            "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. PB1920U.\n"
            + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       SPECIAL-NAMES.\n"
            + "           CURRENCY SIGN IS \"E1\" WITH PICTURE SYMBOL \"#\"\n"
            + "           CURRENCY SIGN IS \"EUR\" WITH PICTURE SYMBOL \"#\".\n"
            + "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       01 A PIC #9.99.\n"
            + "       PROCEDURE DIVISION.\n           DISPLAY A\n           STOP RUN.\n";
        var (ok, errors) = EditionHarness.Compile(source, 2023);
        Assert.False(ok);
        Assert.True(errors.Count == 1, $"expected only the first clause's SR23 b) error; got:\n{string.Join("\n", errors)}");
        EditionHarness.AssertHasDiagnostic(errors, "r23b");
    }
}
