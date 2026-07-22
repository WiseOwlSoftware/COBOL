// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Linq;
using CobolNet.Editions;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Preprocessor;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The <c>&gt;&gt;COBOL-WORDS</c> directive (ISO §7.3.10) — Increment A: the text-stage parser
/// (<see cref="CobolWordsDirectiveProcessor"/>), the resulting <see cref="CobolWordsMap"/> composition, the
/// SR1/SR2/SR5 syntax rules, and the introduction gate (COBOLNET0900 below 2023). Design SSOT:
/// <c>docs/rearchitecture/DESIGN-cobol-words-directive.md</c>.
/// </summary>
public sealed class CobolWordsDirectiveTests
{
    private static (CobolWordsMap Map, DiagnosticBag Diags) Run(string src, int std = 2023)
    {
        var bag = new DiagnosticBag();
        var (_, map) = CobolWordsDirectiveProcessor.Process(src, std, permissive: false, bag, "t.cob");
        return (map, bag);
    }

    private static bool Has(DiagnosticBag b, string code) => b.Diagnostics.Any(d => d.Code == code);

    // ── the four options build the map ──────────────────────────────────────────────────────────────────────

    [Fact] // GR2 — EQUATE literal-2 becomes a synonym for literal-1.
    public void Equate_BuildsSynonym()
    {
        var (map, diags) = Run(">>COBOL-WORDS EQUATE \"DISPLAY\" WITH \"SHOW\"\n");
        Assert.False(diags.HasErrors);
        Assert.Equal("DISPLAY", map.Synonyms["SHOW"]);
        Assert.False(map.IsEmpty);
    }

    [Fact] // GR3 — UNDEFINE literal-3 loses reserved status.
    public void Undefine_BuildsDeReserved()
    {
        var (map, diags) = Run(">>COBOL-WORDS UNDEFINE \"MOVE\"\n");
        Assert.False(diags.HasErrors);
        Assert.Contains("MOVE", map.DeReserved);
        Assert.Empty(map.Synonyms);
    }

    [Fact] // GR4 — SUBSTITUTE literal-5 takes over; literal-4 becomes a user word (de-reserved).
    public void Substitute_BuildsSynonymAndDeReserved()
    {
        var (map, diags) = Run(">>COBOL-WORDS SUBSTITUTE \"MOVE\" BY \"MOVE-IT\"\n");
        Assert.False(diags.HasErrors);
        Assert.Equal("MOVE", map.Synonyms["MOVE-IT"]);
        Assert.Contains("MOVE", map.DeReserved);
    }

    [Fact] // GR5 — RESERVE literal-6 becomes reserved.
    public void Reserve_BuildsReserved()
    {
        var (map, diags) = Run(">>COBOL-WORDS RESERVE \"FOO\"\n");
        Assert.False(diags.HasErrors);
        Assert.Contains("FOO", map.Reserved);
    }

    [Fact] // SR2 — the literal content is case-insensitive (upper-cased).
    public void Literals_AreCaseInsensitive()
    {
        var (map, _) = Run(">>COBOL-WORDS EQUATE \"display\" WITH \"show\"\n");
        Assert.Equal("DISPLAY", map.Synonyms["SHOW"]);
    }

    // ── malformed directives → COBOLNET1623, no op collected ─────────────────────────────────────────────────

    [Theory]
    [InlineData(">>COBOL-WORDS BOGUS \"FOO\"\n")]                       // unknown option
    [InlineData(">>COBOL-WORDS EQUATE \"MOVE\"\n")]                     // EQUATE missing WITH literal-2
    [InlineData(">>COBOL-WORDS EQUATE \"MOVE\" BY \"MOVE-IT\"\n")]      // wrong join word (BY not WITH)
    [InlineData(">>COBOL-WORDS SUBSTITUTE \"MOVE\" WITH \"MOVE-IT\"\n")]// wrong join word (WITH not BY)
    [InlineData(">>COBOL-WORDS RESERVE FOO\n")]                         // SR2: unquoted literal
    [InlineData(">>COBOL-WORDS RESERVE X\"C1\"\n")]                     // SR2: hex-prefixed literal
    [InlineData(">>COBOL-WORDS RESERVE \"FO O\"\n")]                    // SR2: embedded space
    [InlineData(">>COBOL-WORDS\n")]                                     // no option
    public void Malformed_Emits1623_AndCollectsNoOp(string src)
    {
        var (map, diags) = Run(src);
        Assert.True(Has(diags, "COBOLNET1623"));
        Assert.True(map.IsEmpty);
    }

    [Fact] // SR4 — a new word that is not a well-formed user-defined word is rejected.
    public void Sr4_BadUserWord_Rejected1623()
    {
        var (_, diags) = Run(">>COBOL-WORDS RESERVE \"-BAD\"\n");
        Assert.True(Has(diags, "COBOLNET1623"));
    }

    [Fact] // SR5 — the same COBOL word may appear in at most one directive's literals.
    public void Sr5_DuplicateWord_Rejected1623()
    {
        var (_, diags) = Run(">>COBOL-WORDS RESERVE \"FOO\"\n>>COBOL-WORDS EQUATE \"MOVE\" WITH \"FOO\"\n");
        Assert.True(Has(diags, "COBOLNET1623"));
    }

    [Fact] // SR1 — a directive after the first IDENTIFICATION DIVISION is illegal.
    public void Sr1_AfterIdDivision_Rejected1623()
    {
        var (_, diags) = Run("IDENTIFICATION DIVISION.\nPROGRAM-ID. P.\n>>COBOL-WORDS RESERVE \"FOO\"\n");
        Assert.True(Has(diags, "COBOLNET1623"));
    }

    [Fact] // SR1 — a directive BEFORE the first IDENTIFICATION DIVISION is legal.
    public void Sr1_BeforeIdDivision_Accepted()
    {
        var (map, diags) = Run(">>COBOL-WORDS RESERVE \"FOO\"\nIDENTIFICATION DIVISION.\nPROGRAM-ID. P.\n");
        Assert.False(diags.HasErrors);
        Assert.Contains("FOO", map.Reserved);
    }

    // ── edition gate ────────────────────────────────────────────────────────────────────────────────────────

    [Fact] // §7.3.10 is a COBOL-2023 addition — below 2023 the directive word is COBOLNET0900.
    public void Below2023_Emits0900()
    {
        var (_, diags) = Run(">>COBOL-WORDS RESERVE \"FOO\"\n", std: 2014);
        Assert.True(Has(diags, "COBOLNET0900"));
    }

    [Fact] // at 2023 the directive word introduces no gate diagnostic.
    public void At2023_NoGate()
    {
        var (_, diags) = Run(">>COBOL-WORDS RESERVE \"FOO\"\n", std: 2023);
        Assert.False(Has(diags, "COBOLNET0900"));
    }

    // ── mechanics ───────────────────────────────────────────────────────────────────────────────────────────

    [Fact] // the directive line is blanked (line-count preserving — the >>TURN H3 discipline).
    public void DirectiveLine_IsBlanked_LineCountPreserved()
    {
        const string src = ">>COBOL-WORDS RESERVE \"FOO\"\nIDENTIFICATION DIVISION.\n";
        var bag = new DiagnosticBag();
        var (outText, _) = CobolWordsDirectiveProcessor.Process(src, 2023, false, bag, "t.cob");
        Assert.Equal(src.Count(c => c == '\n'), outText.Count(c => c == '\n'));
        Assert.DoesNotContain("COBOL-WORDS", outText);
    }

    [Fact] // no >>COBOL-WORDS directive ⇒ the empty map (the zero-overhead invariant).
    public void NoDirective_EmptyMap()
    {
        var (map, diags) = Run("IDENTIFICATION DIVISION.\nPROGRAM-ID. P.\n");
        Assert.True(map.IsEmpty);
        Assert.False(diags.HasErrors);
    }
}
