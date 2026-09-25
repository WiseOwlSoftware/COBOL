// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ A SPECIAL-NAMES CLAUSE THAT REFERENCES A NAME ANOTHER CLAUSE DECLARES binds in DEPENDENCY order, never source
/// order (kb/Work PB1558). Every pair here is written BOTH ways round and must compile BOTH ways, so a reference
/// that is resolved during the source-order walk fails the first time its declaring clause comes second.
/// <para>The pair that was broken is the one ISO/IEC 1989:2023 makes a forward reference in every conforming
/// program: §12.3.7.2 prints alphabet-name-clause BEFORE the LOCALE clause and §5.2.1 requires "the sequence given
/// in the general format", so <c>ALPHABET … IS LOCALE locale-name-2</c> (§12.3.7.3 SR24 — "Locale-name-2 shall be a
/// locale-name defined by the LOCALE clause") always names a LATER clause. The binder resolved it in source order and
/// refused it with COBOLNET1664 "no LOCALE clause is in scope"; only the non-format order compiled.</para>
/// <para>The CLASS … IN and SYMBOLIC CHARACTERS … IN pairs (alphabet-name-4 / alphabet-name-3 naming an ALPHABET
/// clause) were already deferred (kb/Work PB110); they are here so all three references stay order-free
/// together.</para>
/// </summary>
public sealed class SpecialNamesClauseOrderTests
{
    private static string Prog(string pid, string first, string second) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {pid}.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        SPECIAL-NAMES.
            {first}
            {second}.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 FILLER PIC X.
        PROCEDURE DIVISION.
            STOP RUN.
        """;

    [Theory]
    // ALPHABET → LOCALE (§12.3.7.3 SR24), both classes; the "A" order is the FORMAT order (ALPHABET first).
    [InlineData("PB1558LA", "ALPHABET LA IS LOCALE US", "LOCALE US IS \"en-US\"")]
    [InlineData("PB1558LN", "ALPHABET LN FOR NATIONAL IS LOCALE US", "LOCALE US IS \"en-US\"")]
    // CLASS … IN → ALPHABET (§12.3.7.3 SR17 b1) and SYMBOLIC CHARACTERS … IN → ALPHABET (SR16 e1).
    [InlineData("PB1558CI", "ALPHABET AL IS \"C\" \"B\" \"A\"", "CLASS CL IS 1 THRU 3 IN AL")]
    [InlineData("PB1558SI", "ALPHABET AL IS \"C\" \"B\" \"A\"", "SYMBOLIC CHARACTERS SC IS 1 IN AL")]
    public void ForwardAndBackwardReferences_BothCompile(string pid, string clauseA, string clauseB)
    {
        foreach (var (suffix, first, second) in new[] { ("A", clauseA, clauseB), ("B", clauseB, clauseA) })
        {
            var (ok, errors, _) = EditionHarness.CompileFull(Prog(pid + suffix, first, second), 2023);
            Assert.True(ok, $"[{pid}{suffix}] must COMPILE with '{first}' before '{second}': {string.Join("\n", errors)}");
        }
    }

    /// <summary>The negative control: the lookup that now runs after the whole paragraph still REFUSES a name no
    /// LOCALE clause declares, and names what IS declared — so the order-free acceptance above is not a lookup that
    /// stopped checking.</summary>
    [Fact]
    public void UndeclaredLocaleName_IsStillSR24_AfterTheWholeParagraph()
    {
        var (ok, errors, _) = EditionHarness.CompileFull(
            Prog("PB1558UN", "ALPHABET LA IS LOCALE SV", "LOCALE US IS \"en-US\""), 2023);
        Assert.False(ok, "an undeclared locale-name-2 must be REJECTED");
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET1664: ALPHABET LA IS LOCALE SV: 'SV' is not a locale-name");
        EditionHarness.AssertHasDiagnostic(errors, "declared: US");
    }
}
