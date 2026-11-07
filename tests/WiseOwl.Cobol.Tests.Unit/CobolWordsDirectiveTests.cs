// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System;
using System.IO;
using System.Linq;
using WiseOwl.Cobol;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Frontend.Diagnostics;
using WiseOwl.Cobol.Frontend.Parsing;
using WiseOwl.Cobol.Frontend.Preprocessor;
using Xunit;
using WiseOwl.Cobol.Tests.Shared;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// The <c>&gt;&gt;COBOL-WORDS</c> directive (ISO §2023:7.3.10) — Increment A: the text-stage parser
/// (<see cref="CobolWordsDirectiveProcessor"/>), the resulting <see cref="CobolWordsMap"/> composition, the
/// SR1/SR2/SR5 syntax rules, and the introduction gate (COBOL0900 below 2023). Design SSOT:
/// <c>docs/rearchitecture/DESIGN-cobol-words-directive.md</c>.
/// </summary>
public sealed class CobolWordsDirectiveTests
{
    private static (CobolWordsMap Map, DiagnosticBag Diags) Run(string src, int std = 2023)
    {
        var bag = new DiagnosticBag();
        // The PIPELINE order (Frontend.Preprocess): the conditional-compilation driver runs first and owns the
        // ONE directive-word edition gate (kb/Work PB725 — one rule, one place, at the point a >>WORD is
        // recognized); this stage then parses the >>COBOL-WORDS lines the driver left in the text. Running the
        // stage alone would test a path the compiler does not have.
        string text = ConditionalCompilationProcessor.Process(
            src, WiseOwl.Cobol.Frontend.Frontend.LeftDirectives, bag, "t.cob", std);
        // Then the site stage, which records the directive sites and consumes PUSH/POP into the ops the state stages
        // replay (kb/Work PB1377) — and, after this stage has read the unit boundary with the group's own synonyms, the
        // ONE judge of §2023:7.3.10.3 SR1's placement from the row's directivePlacement data (kb/Work PB1373).
        var (sited, sites, stackOps) = DirectiveSiteProcessor.Process(text, bag, "t.cob");
        var (_, map, firstUnitLine) = CobolWordsDirectiveProcessor.Process(sited, bag, "t.cob", stackOps: stackOps);
        DirectiveSiteProcessor.JudgeFirstUnitPlacement(sites, firstUnitLine, bag, "t.cob");
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

    // ── malformed directives → COBOL1623, no op collected ─────────────────────────────────────────────────

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
        Assert.True(Has(diags, "COBOL1623"));
        Assert.True(map.IsEmpty);
    }

    [Fact] // SR4 — a new word that is not a well-formed user-defined word is rejected.
    public void Sr4_BadUserWord_Rejected1623()
    {
        var (_, diags) = Run(">>COBOL-WORDS RESERVE \"-BAD\"\n");
        Assert.True(Has(diags, "COBOL1623"));
    }

    [Fact] // SR5 — the same COBOL word may appear in at most one directive's literals.
    public void Sr5_DuplicateWord_Rejected1623()
    {
        var (_, diags) = Run(">>COBOL-WORDS RESERVE \"FOO\"\n>>COBOL-WORDS EQUATE \"MOVE\" WITH \"FOO\"\n");
        Assert.True(Has(diags, "COBOL1623"));
    }

    [Fact] // SR1 — a directive after the first IDENTIFICATION DIVISION is illegal: COBOL2652, the ONE placement screen.
    public void Sr1_AfterIdDivision_RejectedAsAPlacementViolation()
    {
        var (_, diags) = Run("IDENTIFICATION DIVISION.\nPROGRAM-ID. P.\n>>COBOL-WORDS RESERVE \"FOO\"\n");
        Assert.True(Has(diags, "COBOL2652"));
        Assert.False(Has(diags, "COBOL1623"));   // the directive stage no longer writes the rule a second time
    }

    [Fact] // §2023:7.3.22.3 SR2 / §2023:7.3.20.3 SR2 — a PUSH / POP NAMING COBOL-WORDS inherits its placement rule.
    public void PushOrPopNamingCobolWords_AfterIdDivision_Rejected()
    {
        foreach (string op in (string[])["PUSH", "POP"])
        {
            var (_, diags) = Run($">>PUSH COBOL-WORDS\nIDENTIFICATION DIVISION.\nPROGRAM-ID. P.\n>>{op} COBOL-WORDS\n");
            Assert.True(Has(diags, "COBOL2652"), op);
        }

        // and the control: the same PUSH/POP BEFORE the first unit is legal.
        var (_, ok) = Run(">>PUSH COBOL-WORDS\n>>POP COBOL-WORDS\nIDENTIFICATION DIVISION.\nPROGRAM-ID. P.\n");
        Assert.False(ok.HasErrors);
    }

    [Fact] // SR1 — a directive BEFORE the first IDENTIFICATION DIVISION is legal.
    public void Sr1_BeforeIdDivision_Accepted()
    {
        var (map, diags) = Run(">>COBOL-WORDS RESERVE \"FOO\"\nIDENTIFICATION DIVISION.\nPROGRAM-ID. P.\n");
        Assert.False(diags.HasErrors);
        Assert.Contains("FOO", map.Reserved);
    }

    // ── kb/Work PB1373: the operand is read as §2023:8.3.5 separators and SR2 literals, the boundary as the group's own words ─

    [Theory] // §2023:8.3.5 2) — a comma or semicolon followed by a space is a separator, usable wherever a space is.
    [InlineData(">>COBOL-WORDS EQUATE \"DISPLAY\", WITH \"SHOW\"\n")]
    [InlineData(">>COBOL-WORDS EQUATE \"DISPLAY\"; WITH \"SHOW\"\n")]
    [InlineData(">>COBOL-WORDS EQUATE, \"DISPLAY\" WITH, \"SHOW\"\n")]
    public void SeparatorCommaOrSemicolon_IsASeparatorSpace(string src)
    {
        var (map, diags) = Run(src);
        Assert.False(diags.HasErrors);
        Assert.Equal("DISPLAY", map.Synonyms["SHOW"]);
    }

    [Theory] // §2023:7.3.3 SR3/SR4 — a comma or semicolon that ENDS the directive is not a space; and one glued to the
    // next word separates nothing (§2023:8.3.5 2) needs the space after it).
    [InlineData(">>COBOL-WORDS RESERVE \"ZQX\";\n")]
    [InlineData(">>COBOL-WORDS RESERVE \"ZQX\",\n")]
    [InlineData(">>COBOL-WORDS EQUATE \"DISPLAY\",WITH \"SHOW\"\n")]
    public void CommaOrSemicolonThatSeparatesNothing_IsMalformed1623(string src)
    {
        var (map, diags) = Run(src);
        Assert.True(Has(diags, "COBOL1623"));
        Assert.True(map.IsEmpty);
    }

    [Fact] // §2023:8.3.3.2.3 — a comma INSIDE a literal is the literal's own character, never a separator.
    public void CommaInsideALiteral_IsNotASeparator()
    {
        var (_, diags) = Run(">>COBOL-WORDS RESERVE \"A, B\"\n");
        // a space inside the one literal (SR2) — not a split operand, which would be an arity error of another sentence
        Assert.Contains(diags.Diagnostics, d => d.Code == "COBOL1623" && d.Message.Contains("space-free"));
    }

    [Theory] // SR2 — "Each literal shall be an alphanumeric literal": no closing delimiter, or one the next character
    // does not separate from what follows (§2023:8.3.5 5)), is no literal at all.
    [InlineData(">>COBOL-WORDS RESERVE \"FOO\n", "no closing quotation symbol")]
    [InlineData(">>COBOL-WORDS RESERVE 'FOO\n", "no closing quotation symbol")]
    [InlineData(">>COBOL-WORDS RESERVE \"FOO\"BAR\n", "followed by 'B'")]
    [InlineData(">>COBOL-WORDS EQUATE \"DISPLAY\"WITH \"SHOW\"\n", "followed by 'W'")]
    public void MalformedLiteral_IsRejectedWithItsReason(string src, string reason)
    {
        var (map, diags) = Run(src);
        Assert.Contains(diags.Diagnostics, d => d.Code == "COBOL1623" && d.Message.Contains(reason));
        Assert.True(map.IsEmpty);
    }

    [Fact] // §2023:8.3.3.2.3 3) — two contiguous quotation symbols are ONE character of the content.
    public void DoubledQuotationSymbol_IsOneCharacterOfTheLiteral()
    {
        var (_, diags) = Run(">>COBOL-WORDS RESERVE \"A\"\"B\"\n");
        Assert.Contains(diags.Diagnostics, d => d.Code == "COBOL1623" && d.Message.Contains("'A\"B'"));
    }

    [Fact] // §2023:8.3.2.1 + §2023:8.3.2.2 — 63 characters is the ceiling of the fresh word; 64 is not a user-defined word.
    public void FreshWord_IsHeldToTheSixtyThreeCharacterCeiling()
    {
        var (map, ok) = Run($">>COBOL-WORDS RESERVE \"{new string('A', 63)}\"\n");
        Assert.False(ok.HasErrors);
        Assert.Single(map.Reserved);

        var (_, tooLong) = Run($">>COBOL-WORDS RESERVE \"{new string('A', 64)}\"\n");
        Assert.Contains(tooLong.Diagnostics, d => d.Code == "COBOL1623" && d.Message.Contains("64 characters"));
    }

    [Theory] // §2023:8.3.2.1 — hyphen and underscore are word characters (not first or last); §2023:8.3.2.2 wants a LETTER.
    [InlineData("MY_SHOW", true)]
    [InlineData("MY-SHOW", true)]
    [InlineData("A1", true)]
    [InlineData("123", false)]
    [InlineData("_SHOW", false)]
    [InlineData("SHOW_", false)]
    public void FreshWord_Shape(string word, bool legal)
    {
        var (_, diags) = Run($">>COBOL-WORDS RESERVE \"{word}\"\n");
        Assert.Equal(!legal, Has(diags, "COBOL1623"));
    }

    [Theory] // §2023:7.3.10.3 SR1 + §2023:7.3.10.4 GR2 — the unit's header, spelled with a word the group's own EQUATE made a synonym,
    // IS the first IDENTIFICATION DIVISION: a directive after it is a placement violation, as after the spelled-out header.
    [InlineData(">>COBOL-WORDS EQUATE \"IDENTIFICATION\" WITH \"IDENT\"\nIDENT DIVISION.\n>>COBOL-WORDS RESERVE \"ZQX\"\nPROGRAM-ID. P.\n")]
    [InlineData(">>COBOL-WORDS EQUATE \"IDENTIFICATION\" WITH \"IDENT\"\nident division.\n>>COBOL-WORDS RESERVE \"ZQX\"\nPROGRAM-ID. P.\n")]
    [InlineData(">>COBOL-WORDS SUBSTITUTE \"IDENTIFICATION\" BY \"IDENT\"\nIDENT DIVISION.\n>>COBOL-WORDS RESERVE \"ZQX\"\nPROGRAM-ID. P.\n")]
    [InlineData(">>COBOL-WORDS EQUATE \"DIVISION\" WITH \"DIV\"\nIDENTIFICATION DIV.\n>>COBOL-WORDS RESERVE \"ZQX\"\nPROGRAM-ID. P.\n")]
    [InlineData(">>COBOL-WORDS EQUATE \"PROGRAM-ID\" WITH \"PID\"\nPID. P.\n>>COBOL-WORDS RESERVE \"ZQX\"\n")]
    public void Sr1_UnitHeaderSpelledWithAGroupSynonym_ClosesTheRegion(string src)
    {
        var (_, diags) = Run(src);
        Assert.True(Has(diags, "COBOL2652"));
    }

    [Fact] // the synonym is read at the line it is written on: before its EQUATE, IDENT is no header.
    public void Sr1_ASynonymNotYetEquated_DoesNotCloseTheRegion()
    {
        var (_, diags) = Run("IDENT DIVISION.\n>>COBOL-WORDS EQUATE \"IDENTIFICATION\" WITH \"IDENT\"\n");
        Assert.False(Has(diags, "COBOL2652"));   // (the IDENT line is a parse error of its own, not this stage's)
    }

    [Fact] // §2023:7.3.20.4 GR1 — a synonym a POP withdrew before the line is no synonym there: IDENT is no header.
    public void Sr1_ASynonymThatAPopWithdrew_DoesNotCloseTheRegion()
    {
        var (_, diags) = Run(
            ">>PUSH COBOL-WORDS\n>>COBOL-WORDS EQUATE \"IDENTIFICATION\" WITH \"IDENT\"\n>>POP COBOL-WORDS\n"
            + "IDENT DIVISION.\n>>COBOL-WORDS RESERVE \"ZQX\"\n");
        Assert.False(Has(diags, "COBOL2652"));
    }

    // ── edition gate ────────────────────────────────────────────────────────────────────────────────────────

    [Fact] // §2023:7.3.10 is a COBOL-2023 addition — below 2023 the directive word is COBOL0900.
    public void Below2023_Emits0900()
    {
        var (_, diags) = Run(">>COBOL-WORDS RESERVE \"FOO\"\n", std: 2014);
        Assert.True(Has(diags, "COBOL0900"));
    }

    [Fact] // at 2023 the directive word introduces no gate diagnostic.
    public void At2023_NoGate()
    {
        var (_, diags) = Run(">>COBOL-WORDS RESERVE \"FOO\"\n", std: 2023);
        Assert.False(Has(diags, "COBOL0900"));
    }

    // ── mechanics ───────────────────────────────────────────────────────────────────────────────────────────

    [Fact] // the directive line is blanked (line-count preserving — the >>TURN H3 discipline).
    public void DirectiveLine_IsBlanked_LineCountPreserved()
    {
        const string src = ">>COBOL-WORDS RESERVE \"FOO\"\nIDENTIFICATION DIVISION.\n";
        var bag = new DiagnosticBag();
        var (outText, _, _) = CobolWordsDirectiveProcessor.Process(src, bag, "t.cob");
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

    // ══ Increment B — RESERVE/UNDEFINE via the composed ReservedWordSet + SR3/SR4 category validation ══════════

    // ── ReservedWordSet.Compose (ISO §2023:7.3.10.4 GR3/GR5) ─────────────────────────────────────────────────────

    [Fact] // an empty map composes to the Default set (byte-identical).
    public void Compose_EmptyMap_IsDefault()
    {
        Assert.Same(ReservedWordSet.Default, ReservedWordSet.Compose(CobolWordsMap.Empty));
    }

    [Fact] // GR5 — RESERVE makes a fresh word reject when used as a user-defined word.
    public void Compose_Reserve_RejectsTheNewWord()
    {
        var map = new CobolWordsMap([new CobolWordsOp(CobolWordsAction.Reserve, null, "ZZBAR", default)]);
        Assert.True(ReservedWordSet.Compose(map).RejectsAt("ZZBAR", 2023));
        Assert.False(ReservedWordSet.Default.RejectsAt("ZZBAR", 2023));   // not reserved without the directive
    }

    [Fact] // GR3 — UNDEFINE de-reserves a base-reserved word (RejectsAt flips false).
    public void Compose_Undefine_SuppressesABaseReservedWord()
    {
        Assert.True(ReservedWordSet.Default.RejectsAt("ACCEPT", 2023));   // ACCEPT is high-confidence reserved
        var map = new CobolWordsMap([new CobolWordsOp(CobolWordsAction.Undefine, "ACCEPT", null, default)]);
        Assert.False(ReservedWordSet.Compose(map).RejectsAt("ACCEPT", 2023));
    }

    // ── CobolKeywordTokens (the reverse vocab map) ──────────────────────────────────────────────────────────

    [Fact]
    public void Keyword_Reserved_And_Context_AreKeywords_UserWordIsNot()
    {
        Assert.True(CobolKeywordTokens.TryTokenType("MOVE", out _));       // a hard reserved word
        Assert.True(CobolKeywordTokens.TryTokenType("display", out _));    // case-insensitive
        Assert.False(CobolKeywordTokens.TryTokenType("ZZUSERWORD", out _));
        Assert.True(CobolKeywordTokens.TryTokenType("DISPLAY", out int t) && t > 0);
    }

    // ── end-to-end through the compiler (RESERVE 0901 · SR3/SR4 1623) ───────────────────────────────────────

    private static IReadOnlyList<string> CompileErrors(string source) => Compile(source).Errors;

    private static CompilerDriver.Result Compile(string source, bool permissive = false)
    {
        string dir = ScratchDirectory.Create("WiseOwlCobol_CWords_");
        try
        {
            string src = Path.Combine(dir, "cw.cob");
            File.WriteAllText(src, source);
            return CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "cw.dll"), DialectLevel: 2023, CheckOnly: true, SourceFormat: InitialReferenceFormat.Auto,
                Permissive: permissive));
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    /// <summary>kb/Work PB4360 (ISO §2023:7.3.10.4 GR3, GR4): a verb a directive withdrew, written as a verb, is a user-defined
    /// word and the statement is a syntax error; COBOL5926 beside it names the directive, once, at the directive's line.</summary>
    [Theory]
    [InlineData("UNDEFINE \"DISPLAY\"", "DISPLAY \"X\".", ">>COBOL-WORDS UNDEFINE \"DISPLAY\" withdrew DISPLAY (ISO §2023:7.3.10.4 GR3)")]
    [InlineData("SUBSTITUTE \"DISPLAY\" BY \"SHOW\"", "DISPLAY \"X\".",
        ">>COBOL-WORDS SUBSTITUTE \"DISPLAY\" BY \"SHOW\" withdrew DISPLAY, and SHOW now stands for it (ISO §2023:7.3.10.4 GR4)")]
    [InlineData("UNDEFINE \"MOVE\"", "MOVE 1 TO R. MOVE 2 TO R.", ">>COBOL-WORDS UNDEFINE \"MOVE\" withdrew MOVE (ISO §2023:7.3.10.4 GR3)")]
    public void AWithdrawnVerbWrittenAsAVerb_IsNamedBesideTheSyntaxError(string directive, string statements, string named)
    {
        var r = Compile($"       >>COBOL-WORDS {directive}\n       IDENTIFICATION DIVISION.\n       PROGRAM-ID. CWPB4360.\n"
            + "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       01 R PIC 9.\n"
            + $"       PROCEDURE DIVISION.\n       MAIN.\n           {statements}\n           STOP RUN.\n");
        Assert.Contains(r.Errors, e => e.Contains("COBOL0001"));   // the source is still no statement
        var note = Assert.Single(r.Warnings, w => w.Contains("COBOL5926"));
        Assert.Contains(named, note);
        Assert.Contains("(1,", note);   // at the directive's own line
    }

    /// <summary>The control (kb/Work PB4360): a syntax error with no withdrawn word beside it draws no COBOL5926, and a
    /// withdrawn word used as the user-defined word it now is compiles.</summary>
    [Fact]
    public void ASyntaxErrorAwayFromTheWithdrawnWord_DrawsNoNote_AndTheWordIsAUserWord()
    {
        var unrelated = Compile("       >>COBOL-WORDS UNDEFINE \"DISPLAY\"\n       IDENTIFICATION DIVISION.\n"
            + "       PROGRAM-ID. CWPB4360B.\n       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       01 R PIC 9.\n"
            + "       PROCEDURE DIVISION.\n       MAIN.\n           MOVE 1 R.\n           STOP RUN.\n");
        Assert.Contains(unrelated.Errors, e => e.Contains("COBOL5506"));
        Assert.DoesNotContain(unrelated.Warnings, w => w.Contains("COBOL5926"));

        var used = Compile("       >>COBOL-WORDS UNDEFINE \"DISPLAY\"\n       IDENTIFICATION DIVISION.\n"
            + "       PROGRAM-ID. CWPB4360C.\n       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       01 DISPLAY PIC 9.\n"
            + "       PROCEDURE DIVISION.\n       MAIN.\n           MOVE 1 TO DISPLAY.\n           STOP RUN.\n");
        Assert.Empty(used.Errors);
    }

    private const string WordsTail = "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. CWPB3119.\n"
        + "       PROCEDURE DIVISION.\n       MAIN.\n           STOP RUN.\n";

    /// <summary>kb/Work PB3119 — a literal slot that names a variable whose DEFINE was REFUSED adds nothing to the one
    /// report that DEFINE made (§2023:7.3.11.4 GR1 lets the name stand in the slot; the DEFINE already failed). The control is
    /// the same slot over a name nobody wrote, which still says it was never defined.</summary>
    [Fact]
    public void LiteralSlotNamingARefusedDefine_AddsNoSecondError()
    {
        var errors = CompileErrors("       >>DEFINE W AS 1 +\n       >>COBOL-WORDS UNDEFINE W\n" + WordsTail);
        Assert.Single(errors);
        Assert.Contains("COBOL1619", errors[0]);

        var control = CompileErrors("       >>COBOL-WORDS UNDEFINE W\n" + WordsTail);
        Assert.Contains(control, e => e.Contains("COBOL1623") && e.Contains("defined before the directive"));
    }

    [Fact] // a refused REDEFINITION changed nothing: the name still stands for the value it had, and its slot is judged on it
    public void LiteralSlotNamingARefusedRedefinition_StillReadsTheEarlierValue()
    {
        var errors = CompileErrors(
            "       >>DEFINE W AS \"MOVE\"\n       >>DEFINE W AS 1 + OVERRIDE\n       >>COBOL-WORDS UNDEFINE W\n" + WordsTail);
        Assert.Single(errors);
        Assert.Contains("COBOL1619", errors[0]);
    }

    [Fact] // the refusal belongs to the LAST directive that wrote the name: an OFF after it makes the name genuinely undefined
    public void LiteralSlotNamingAnOffAfterARefusal_IsUndefined()
    {
        var errors = CompileErrors(
            "       >>DEFINE W AS 1 +\n       >>DEFINE W OFF\n       >>COBOL-WORDS UNDEFINE W\n" + WordsTail);
        Assert.Contains(errors, e => e.Contains("COBOL1623"));
    }

    [Fact] // GR5 end-to-end — a RESERVE'd word used as a user-defined name is COBOL0901-rejected.
    public void Reserve_UsedAsUserWord_Rejected0901()
    {
        var errors = CompileErrors(
            "       >>COBOL-WORDS RESERVE \"FOO\"\n" +
            "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. CWE1.\n" +
            "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       01 FOO PIC X(3) VALUE \"ABC\".\n" +
            "       PROCEDURE DIVISION.\n       MAIN.\n           DISPLAY FOO.\n           STOP RUN.\n");
        Assert.Contains(errors, e => e.Contains("COBOL0901") && e.Contains("FOO"));
    }

    private static string SynonymProgram(string directive, string dataName, string paragraph = "MAIN") =>
        $"       >>COBOL-WORDS {directive}\n       IDENTIFICATION DIVISION.\n       PROGRAM-ID. CWPB4488.\n"
        + $"       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       01 {dataName} PIC X VALUE \"C\".\n"
        + $"       PROCEDURE DIVISION.\n       {paragraph}.\n           STOP RUN.\n";

    /// <summary>kb/Work PB4488 (ISO §2023:7.3.10.4 GR2, GR4; §2023:8.3.2.1 1): a synonym of a RESERVED word is that reserved word and
    /// no user-defined word, whether or not the lexer has a token to retype it to. The ten §2023:8.9 words the lexer carries
    /// no token for (COPY, REPLACE, LOCALE, NESTED, ANYCASE, SYSTEM-DEFAULT, USER-DEFAULT, EXCEPTION-OBJECT, COMMA, MINUS)
    /// by EQUATE, and COPY and LOCALE by SUBSTITUTE.</summary>
    [Theory]
    [InlineData("EQUATE \"COPY\" WITH \"SYN\"", "COPY")]
    [InlineData("EQUATE \"REPLACE\" WITH \"SYN\"", "REPLACE")]
    [InlineData("EQUATE \"LOCALE\" WITH \"SYN\"", "LOCALE")]
    [InlineData("EQUATE \"NESTED\" WITH \"SYN\"", "NESTED")]
    [InlineData("EQUATE \"ANYCASE\" WITH \"SYN\"", "ANYCASE")]
    [InlineData("EQUATE \"SYSTEM-DEFAULT\" WITH \"SYN\"", "SYSTEM-DEFAULT")]
    [InlineData("EQUATE \"USER-DEFAULT\" WITH \"SYN\"", "USER-DEFAULT")]
    [InlineData("EQUATE \"EXCEPTION-OBJECT\" WITH \"SYN\"", "EXCEPTION-OBJECT")]
    [InlineData("EQUATE \"COMMA\" WITH \"SYN\"", "COMMA")]
    [InlineData("EQUATE \"MINUS\" WITH \"SYN\"", "MINUS")]
    [InlineData("SUBSTITUTE \"COPY\" BY \"SYN\"", "COPY")]
    [InlineData("SUBSTITUTE \"LOCALE\" BY \"SYN\"", "LOCALE")]
    public void ASynonymOfAReservedWord_IsNoUserDefinedWord(string directive, string original)
    {
        var errors = CompileErrors(SynonymProgram(directive, "SYN"));
        var refusal = Assert.Single(errors, e => e.Contains("COBOL0901"));
        Assert.Contains($"'SYN' is a synonym for the reserved word {original}", refusal);
        // …and the same word as a paragraph-name, the other user-defined-word slot the funnel reads.
        Assert.Contains(CompileErrors(SynonymProgram(directive, "R", "SYN")), e => e.Contains("COBOL0901"));
    }

    /// <summary>The lexer's side of the same rule, which the overlay leaves alone (kb/Work PB4488): the synonym of a word the
    /// lexer has a token for IS that token, so MOVE's synonym stands where MOVE stands and is refused where a name is required.</summary>
    [Fact]
    public void ASynonymOfAWordTheLexerHasATokenFor_IsRefusedAsAName()
    {
        Assert.NotEmpty(CompileErrors(SynonymProgram("EQUATE \"MOVE\" WITH \"SYN\"", "SYN")));
        Assert.NotEmpty(CompileErrors(SynonymProgram("EQUATE \"MOVE\" WITH \"SYN\"", "R", "SYN")));
    }

    private static string WordsProgram(string directive, string dataItem, string statement) =>
        $"       >>COBOL-WORDS {directive}\n       IDENTIFICATION DIVISION.\n       PROGRAM-ID. CWPB4573.\n"
        + $"       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       01 {dataItem} PIC X.\n"
        + $"       PROCEDURE DIVISION.\n       MAIN.\n           {statement}.\n           STOP RUN.\n";

    /// <summary>⛔ kb/Work PB4573 (ISO §2023:7.3.10.4 GR2, GR4; §2023:8.3.2.1 1): the COBOL0901 for a synonym of a reserved word is ONE
    /// sentence whichever stage reports it, naming the word the source WROTE and the reserved word it stands for
    /// (<see cref="ReservedWordSet.UserWordViolationMessage"/>). The funnel's report (a synonym the lexer has no token for:
    /// LOCALE, in a data-name slot) and the parser's two (a synonym the lexer retyped to its original's token, in an operand
    /// slot and in a procedure-name slot) once said different things: the parser's named the ORIGINAL, "'DISPLAY' is a
    /// reserved word", which the source never wrote. The lower-case spelling is the source's own, upper-cased as a keyword is.</summary>
    [Theory]
    [InlineData("EQUATE \"DISPLAY\" WITH \"SHOW\"", "W-X", "MOVE \"A\" TO SHOW", "SHOW", "DISPLAY")]
    [InlineData("EQUATE \"DISPLAY\" WITH \"SHOW\"", "W-X", "MOVE \"A\" TO Show", "SHOW", "DISPLAY")]
    [InlineData("EQUATE \"READ\" WITH \"FETCH\"", "W-X", "PERFORM fetch", "FETCH", "READ")]
    [InlineData("SUBSTITUTE \"DISPLAY\" BY \"SHOW\"", "W-X", "MOVE \"A\" TO SHOW", "SHOW", "DISPLAY")]
    [InlineData("EQUATE \"CONSTANT\" WITH \"CNST\"", "CNST", "STOP RUN", "CNST", "CONSTANT")]
    [InlineData("EQUATE \"LOCALE\" WITH \"LOC\"", "LOC", "STOP RUN", "LOC", "LOCALE")]
    public void TheCobol0901OfASynonymNamesTheWordWrittenAndItsOriginal_WhicheverStageReportsIt(
        string directive, string dataItem, string statement, string written, string original)
    {
        var refusal = Assert.Single(CompileErrors(WordsProgram(directive, dataItem, statement)), e => e.Contains("COBOL0901"));
        Assert.Contains(ReservedWordSet.UserWordViolationMessage(written, CobolWordsMap.FromDirectiveText($">>COBOL-WORDS {directive}")!, 2023), refusal);
        Assert.Contains($"'{written}' is a synonym for the reserved word {original}", refusal);
    }

    /// <summary>The control of the sentence above: the reserved word itself, written where a name stands while a directive of
    /// the group is in force, is "reserved in COBOL-2023", not a synonym of anything.</summary>
    [Fact]
    public void TheReservedWordItself_IsNamedAsReserved_WhileADirectiveIsInForce()
    {
        var refusal = Assert.Single(
            CompileErrors(WordsProgram("EQUATE \"DISPLAY\" WITH \"SHOW\"", "W-X", "MOVE \"A\" TO display")), e => e.Contains("COBOL0901"));
        Assert.Contains(ReservedWordSet.UserWordViolationMessage("DISPLAY", CobolWordsMap.Empty, 2023), refusal);
        Assert.DoesNotContain("synonym", refusal);
    }

    private static string SpecialNamesProgram(string directive, string clause) =>
        $"       >>COBOL-WORDS {directive}\n       IDENTIFICATION DIVISION.\n       PROGRAM-ID. CWPB4573S.\n"
        + $"       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       SPECIAL-NAMES.\n           {clause}.\n"
        + "       PROCEDURE DIVISION.\n           STOP RUN.\n";

    /// <summary>⛔ kb/Work PB4573 (ISO §2023:7.3.10.4 GR3, GR4): a clause binder that finds the word its clause REQUIRES withdrawn by
    /// a directive names the directive through the one sentence <see cref="CobolWordsOp.Withdrew"/> builds, and says what is
    /// left of the clause: under UNDEFINE it "shall not be available" (GR3), under SUBSTITUTE it is written with literal-5
    /// (GR4), which is not "not available" (the DECIMAL-POINT arm said so for both). Whatever word follows IS draws the
    /// withdrawal, the cause of every refusal while COMMA is withdrawn.</summary>
    [Theory]
    [InlineData("UNDEFINE \"COMMA\"", "DECIMAL-POINT IS COMMA", "COBOL0894", ">>COBOL-WORDS UNDEFINE \"COMMA\" withdrew COMMA (ISO §2023:7.3.10.4 GR3)",
        "so it is not available in this compilation group")]
    [InlineData("SUBSTITUTE \"COMMA\" BY \"CMA\"", "DECIMAL-POINT IS COMMA", "COBOL0894",
        ">>COBOL-WORDS SUBSTITUTE \"COMMA\" BY \"CMA\" withdrew COMMA, and CMA now stands for it (ISO §2023:7.3.10.4 GR4)",
        "so it is written with CMA in this compilation group")]
    [InlineData("SUBSTITUTE \"COMMA\" BY \"CMA\"", "DECIMAL-POINT IS XYZ", "COBOL0894",
        ">>COBOL-WORDS SUBSTITUTE \"COMMA\" BY \"CMA\" withdrew COMMA, and CMA now stands for it (ISO §2023:7.3.10.4 GR4)",
        "so it is written with CMA in this compilation group")]
    [InlineData("UNDEFINE \"SYMBOL\"", "CURRENCY SIGN IS \"EUR\" WITH PICTURE SYMBOL \"#\"", "COBOL0892",
        ">>COBOL-WORDS UNDEFINE \"SYMBOL\" withdrew SYMBOL (ISO §2023:7.3.10.4 GR3)", "so it is not available in this compilation group")]
    [InlineData("SUBSTITUTE \"SYMBOL\" BY \"SYM\"", "CURRENCY SIGN IS \"EUR\" WITH PICTURE SYMBOL \"#\"", "COBOL0892",
        ">>COBOL-WORDS SUBSTITUTE \"SYMBOL\" BY \"SYM\" withdrew SYMBOL, and SYM now stands for it (ISO §2023:7.3.10.4 GR4)",
        "so it is written with SYM in this compilation group")]
    public void AClauseWordADirectiveWithdrew_NamesTheDirectiveAndWhatIsLeftOfTheClause(
        string directive, string clause, string code, string withdrew, string left)
    {
        var refusal = Assert.Single(CompileErrors(SpecialNamesProgram(directive, clause)), e => e.Contains(code));
        Assert.Contains(withdrew, refusal);
        Assert.Contains(left, refusal);
        Assert.DoesNotContain("the only form is", refusal);
        Assert.DoesNotContain("expected 'WITH PICTURE SYMBOL'", refusal);
    }

    /// <summary>The controls of the sentence above: with no directive withdrawing the word the old sentences stand, and a
    /// synonym is the word (GR2, GR4), so neither clause is refused for it.</summary>
    [Theory]
    [InlineData("EQUATE \"DISPLAY\" WITH \"SHOW\"", "DECIMAL-POINT IS XYZ", "COBOL0894", "the only form is DECIMAL-POINT IS COMMA")]
    [InlineData("EQUATE \"DISPLAY\" WITH \"SHOW\"", "CURRENCY SIGN IS \"EUR\" WITH PICTURE XYZ \"#\"", "COBOL0892",
        "expected 'WITH PICTURE SYMBOL', found 'PICTURE XYZ'")]
    public void AClauseWordNoDirectiveWithdrew_KeepsItsOwnRefusal(string directive, string clause, string code, string sentence)
    {
        var refusal = Assert.Single(CompileErrors(SpecialNamesProgram(directive, clause)), e => e.Contains(code));
        Assert.Contains(sentence, refusal);
        Assert.DoesNotContain("withdrew", refusal);
    }

    [Theory]
    [InlineData("SUBSTITUTE \"COMMA\" BY \"CMA\"", "DECIMAL-POINT IS CMA")]
    [InlineData("SUBSTITUTE \"SYMBOL\" BY \"SYM\"", "CURRENCY SIGN IS \"EUR\" WITH PICTURE SYM \"#\"")]
    public void TheWordASubstituteStandsInFor_IsTheWordTheClauseRequires(string directive, string clause)
    {
        Assert.Empty(CompileErrors(SpecialNamesProgram(directive, clause)));
    }

    /// <summary>The controls of kb/Work PB4488: SUBSTITUTE de-reserves its literal-4, so the word it replaced is a user word
    /// (GR4) and only the synonym is refused, and a synonym of a word §2023:8.9 does not reserve (a §2023:8.10 context-sensitive word,
    /// an intrinsic function name) keeps the status of its original.</summary>
    [Theory]
    [InlineData("SUBSTITUTE \"LOCALE\" BY \"LOC\"", "LOCALE")]
    [InlineData("EQUATE \"STACK\" WITH \"SYN\"", "SYN")]
    [InlineData("EQUATE \"TRIM\" WITH \"SYN\"", "SYN")]
    public void AWordTheDirectiveLeavesUnreserved_IsStillAUserDefinedWord(string directive, string dataName)
    {
        Assert.Empty(CompileErrors(SynonymProgram(directive, dataName)));
    }

    /// <summary>kb/Work PB4488 (ISO §2023:7.3.10.4 GR6; §2023:7.2.2.2; §2023:7.2.1 Steps 1 to 4): a COBOL-WORDS directive does not affect a
    /// compiler directing statement, so a synonym of COPY or REPLACE begins no statement of the stage that finds them. The
    /// syntax error stands, now without the guess that a period is missing from a sentence that has one, and COBOL6085
    /// beside it names the directive and the rule, once, at the directive.</summary>
    [Theory]
    [InlineData("EQUATE \"COPY\" WITH \"CPY\"", "CPY BOOK.", "COPY")]
    [InlineData("SUBSTITUTE \"COPY\" BY \"CPY\"", "CPY BOOK.", "COPY")]
    [InlineData("EQUATE \"REPLACE\" WITH \"RPL\"", "RPL OFF.", "REPLACE")]
    public void ASynonymOfADirectingStatement_BeginsNoStatement_AndIsNamedBesideTheSyntaxError(string directive, string line, string original)
    {
        var r = Compile($"       >>COBOL-WORDS {directive}\n       IDENTIFICATION DIVISION.\n       PROGRAM-ID. CWPB4488B.\n"
            + $"       PROCEDURE DIVISION.\n       MAIN.\n           DISPLAY \"A\".\n           {line}\n           STOP RUN.\n");
        var error = Assert.Single(r.Errors, e => e.Contains("COBOL0001") || e.Contains("COBOL0307"));
        Assert.DoesNotContain("period may be missing", error);
        var note = Assert.Single(r.Warnings, w => w.Contains("COBOL6085"));
        Assert.Contains($">>COBOL-WORDS {directive}: a COBOL-WORDS directive does not affect a compiler directing statement "
            + $"(ISO §2023:7.3.10.4 GR6), so '{line.Split(' ')[0]}' begins no {original} statement", note);
        Assert.Contains("(1,", note);   // at the directive's own line
    }

    /// <summary>The control: a syntax error away from any synonym of COPY or REPLACE draws no COBOL6085, and a synonym of an
    /// ordinary verb is not a directing-statement synonym.</summary>
    [Fact]
    public void ASyntaxErrorAwayFromADirectingStatementSynonym_DrawsNoNote()
    {
        var r = Compile("       >>COBOL-WORDS EQUATE \"DISPLAY\" WITH \"SHOW\"\n       IDENTIFICATION DIVISION.\n"
            + "       PROGRAM-ID. CWPB4488C.\n       PROCEDURE DIVISION.\n       MAIN.\n           SHOW.\n           STOP RUN.\n");
        Assert.NotEmpty(r.Errors);
        Assert.DoesNotContain(r.Warnings, w => w.Contains("COBOL6085"));
    }

    /// <summary>⛔ kb/Work PB4573, review r1 R1-2 (ISO §2023:7.3.10.4 GR2, GR4): the withdrawn-word note tests the word the SOURCE wrote.
    /// A synonym of a lexer-token word is that word's token with the original's <c>Text</c>, so a listener that read the text
    /// named SHOW a withdrawn DISPLAY (<c>SUBSTITUTE "DISPLAY" BY "SHOW"</c>, then the statement <c>SHOW.</c>): COBOL5926 said
    /// DISPLAY was "a user-defined word in this compilation group" when the source wrote SHOW. The error stands (the lone word
    /// is no statement); the note is the false part.</summary>
    [Fact]
    public void ASynonymOfAWithdrawnWord_IsNoWithdrawnWord_ForTheSyntaxErrorNote()
    {
        var r = Compile("       >>COBOL-WORDS SUBSTITUTE \"DISPLAY\" BY \"SHOW\"\n       IDENTIFICATION DIVISION.\n"
            + "       PROGRAM-ID. CWPB4573N.\n       PROCEDURE DIVISION.\n       MAIN.\n           SHOW.\n           STOP RUN.\n");
        Assert.NotEmpty(r.Errors);
        Assert.DoesNotContain(r.Warnings, w => w.Contains("COBOL5926"));
    }

    /// <summary>⛔ kb/Work PB4573, review r1 R1-1 (ISO §2023:7.3.10.4 GR2): the parser's syntax-error sentence names the word the source
    /// WROTE, not the original the retype spelled into the token: <c>MAIN SECT.</c> after <c>EQUATE "SECTION" WITH "SECT"</c>
    /// is a legal section header, and the SECT after <c>MOVE 1 TO W-X</c> is an unexpected SECT: no sentence of the compilation
    /// says SECTION was written there.</summary>
    [Fact]
    public void ASyntaxErrorNamesTheSynonymTheSourceWrote_NotItsOriginal()
    {
        var errors = CompileErrors("       >>COBOL-WORDS EQUATE \"SECTION\" WITH \"SECT\"\n       IDENTIFICATION DIVISION.\n"
            + "       PROGRAM-ID. CWPB4573E.\n       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       01 W-X PIC 9.\n"
            + "       PROCEDURE DIVISION.\n       MAIN SECT.\n           MOVE 1 TO W-X SECT.\n           STOP RUN.\n");
        Assert.Contains(errors, e => e.Contains("COBOL0307") && e.Contains("unexpected 'SECT'"));
        Assert.DoesNotContain(errors, e => e.Contains("unexpected 'SECTION'"));
    }

    /// <summary>⛔ kb/Work PB4573, review r2 R2-1 (ISO §2023:7.3.10.4 GR2; §2023:8.8.4.7.2): the sign-condition spelling message quotes the
    /// word the source WROTE. After <c>EQUATE "ZEROS" WITH "NIX"</c> the condition <c>W-X IS NIX</c> is refused as a different
    /// reserved word than the keyword ZERO, and the sentence named ZEROS where the source wrote NIX.</summary>
    [Fact]
    public void ASignConditionSpellingMessageNamesTheSynonymTheSourceWrote_NotItsOriginal()
    {
        var errors = CompileErrors("       >>COBOL-WORDS EQUATE \"ZEROS\" WITH \"NIX\"\n       IDENTIFICATION DIVISION.\n"
            + "       PROGRAM-ID. CWPB4573S.\n       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       01 W-X PIC 9.\n"
            + "       PROCEDURE DIVISION.\n       MAIN.\n           IF W-X IS NIX DISPLAY \"Z\" END-IF.\n           STOP RUN.\n");
        var error = Assert.Single(errors, e => e.Contains("COBOL2418"));
        Assert.Contains("and NIX is a different reserved word", error);
        Assert.DoesNotContain("ZEROS is a different", error);
    }

    /// <summary>⛔ kb/Work PB4590 (filed from PB4573's lead R3-1; ISO §2023:7.3.10.4 GR2): the mnemonic-name-suffix sentence of DISPLAY ... UPON and
    /// ACCEPT ... FROM quotes the word the source WROTE: <c>UPON BEEP(1)</c> after <c>EQUATE "BELL" WITH "BEEP"</c> names BEEP,
    /// never the BELL the retype spelled into the token.</summary>
    [Fact]
    public void AMnemonicSuffixMessageNamesTheSynonymTheSourceWrote_NotItsOriginal()
    {
        var errors = CompileErrors("       >>COBOL-WORDS EQUATE \"BELL\" WITH \"BEEP\"\n       IDENTIFICATION DIVISION.\n"
            + "       PROGRAM-ID. CWPB4573U.\n       PROCEDURE DIVISION.\n       MAIN.\n           DISPLAY \"X\" UPON BEEP(1).\n"
            + "           STOP RUN.\n");
        Assert.Contains(errors, e => e.Contains("COBOL2269") && e.Contains("'BEEP' stands in the mnemonic-name-1 position"));
        Assert.DoesNotContain(errors, e => e.Contains("'BELL' stands in the mnemonic-name-1 position"));
    }

    [Fact] // SR3 end-to-end — the existing word must be reserved/context/intrinsic.
    public void Sr3_ExistingNotAWord_Rejected1623()
    {
        var errors = CompileErrors(
            "       >>COBOL-WORDS EQUATE \"NOTAWORD\" WITH \"SYN\"\n" +
            "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. CWE2.\n" +
            "       PROCEDURE DIVISION.\n       MAIN.\n           DISPLAY \"X\".\n           STOP RUN.\n");
        Assert.Contains(errors, e => e.Contains("COBOL1623") && e.Contains("SR3"));
    }

    [Fact] // SR4 end-to-end — the new word must not itself be reserved.
    public void Sr4_NewWordReserved_Rejected1623()
    {
        var errors = CompileErrors(
            "       >>COBOL-WORDS RESERVE \"MOVE\"\n" +
            "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. CWE3.\n" +
            "       PROCEDURE DIVISION.\n       MAIN.\n           DISPLAY \"X\".\n           STOP RUN.\n");
        Assert.Contains(errors, e => e.Contains("COBOL1623") && e.Contains("SR4"));
    }

    private static string NoOpProgram(string directive) =>
        "       " + directive + "\n       IDENTIFICATION DIVISION.\n       PROGRAM-ID. CWP.\n"
        + "       PROCEDURE DIVISION.\n       MAIN.\n           DISPLAY \"X\".\n           STOP RUN.\n";

    [Theory] // §2023:7.3.10.3 SR3 — the existing word is in the 2023 population (§2023:8.9 ∪ §2023:8.10 ∪ §2023:8.11): a word §2023:8.9 dropped
    // before 2023 (AUTHOR, MEMORY) is in none of them, however the lexer or an older edition treats it.
    [InlineData("UNDEFINE \"AUTHOR\"")]
    [InlineData("EQUATE \"MEMORY\" WITH \"RAM\"")]
    [InlineData("SUBSTITUTE \"AUTHOR\" BY \"WRITER\"")]
    public void Sr3_AWordNo2023ListHolds_Rejected1623(string option)
    {
        var errors = CompileErrors(NoOpProgram(">>COBOL-WORDS " + option));
        Assert.Contains(errors, e => e.Contains("COBOL1623") && e.Contains("SR3"));
    }

    [Theory] // SR4 — the fresh word is in none of the three 2023 lists: AUTHOR (a lexer keyword, not a §2023:8.9 word) is legal.
    [InlineData("RESERVE \"AUTHOR\"")]
    [InlineData("EQUATE \"DISPLAY\" WITH \"MEMORY\"")]
    public void Sr4_AWordNo2023ListHolds_IsAFreshWord(string option)
    {
        var errors = CompileErrors(NoOpProgram(">>COBOL-WORDS " + option));
        Assert.DoesNotContain(errors, e => e.Contains("COBOL1623"));
    }

    [Theory] // SR3 admits each of the three lists: §2023:8.9 (DISPLAY), §2023:8.10 (HEX), §2023:8.11 (SQRT, an intrinsic-function-name).
    [InlineData("EQUATE \"DISPLAY\" WITH \"SHOWIT\"")]
    [InlineData("EQUATE \"HEX\" WITH \"HEXIT\"")]
    [InlineData("EQUATE \"SQRT\" WITH \"ROOT\"")]
    public void Sr3_EachOfTheThreeLists_IsAnExistingWord(string option)
    {
        var errors = CompileErrors(NoOpProgram(">>COBOL-WORDS " + option));
        Assert.DoesNotContain(errors, e => e.Contains("COBOL1623"));
    }

    [Theory] // SR4 refuses a word of each of the three lists as the fresh word.
    [InlineData("RESERVE \"DISPLAY\"")]
    [InlineData("RESERVE \"HEX\"")]
    [InlineData("RESERVE \"SQRT\"")]
    public void Sr4_EachOfTheThreeLists_IsNotAFreshWord(string option)
    {
        var errors = CompileErrors(NoOpProgram(">>COBOL-WORDS " + option));
        Assert.Contains(errors, e => e.Contains("COBOL1623") && e.Contains("SR4"));
    }

    /// <summary>Every word the §2023:4.2.10 register reserves (JSON, XML, COMP-3, ENTRY and the rest): the fourth list SR3 and SR4
    /// speak of. It is the REGISTER's rows, never a literal list, so a word added to a row is asked with no edit here
    /// (kb/Work PB4239: a fixed list of the three printed ones is what let fifteen words fall through).</summary>
    public static TheoryData<string> ExtensionReservedWords() =>
        new(NonstandardExtensionRegister.Entries.SelectMany(e => e.ReservedWords).Distinct(StringComparer.Ordinal));

    [Theory] // §2023:7.3.10.3 SR3 admits a §2023:4.2.10 reserved word as the existing word (owner decision 2026-10-31, kb/Work PB4239).
    [MemberData(nameof(ExtensionReservedWords))]
    public void Sr3_AnExtensionReservedWord_IsAnExistingWord(string word)
    {
        Assert.DoesNotContain(CompileErrors(NoOpProgram($">>COBOL-WORDS UNDEFINE \"{word}\"")), e => e.Contains("COBOL1623"));
        Assert.DoesNotContain(CompileErrors(NoOpProgram($">>COBOL-WORDS EQUATE \"{word}\" WITH \"ZQXSYN\"")), e => e.Contains("COBOL1623"));
        Assert.DoesNotContain(CompileErrors(NoOpProgram($">>COBOL-WORDS SUBSTITUTE \"{word}\" BY \"ZQXSUB\"")), e => e.Contains("COBOL1623"));
    }

    [Theory] // SR4 refuses a §2023:4.2.10 reserved word as the new word, and the sentence names the reservation that refuses it.
    [MemberData(nameof(ExtensionReservedWords))]
    public void Sr4_AnExtensionReservedWord_IsNotAFreshWord(string word)
    {
        foreach (var option in new[] { $"RESERVE \"{word}\"", $"EQUATE \"MOVE\" WITH \"{word}\"", $"SUBSTITUTE \"DISPLAY\" BY \"{word}\"" })
        {
            var refusal = Assert.Single(CompileErrors(NoOpProgram(">>COBOL-WORDS " + option)), e => e.Contains("COBOL1623"));
            Assert.Contains("SR4", refusal);
            Assert.Contains($"the new word '{word}' is a word this implementation reserves for a nonstandard extension (ISO §2023:4.2.10", refusal);
        }
    }

    [Fact] // kb/Work PB4239 S1: a word of a standard list as the new word keeps the generic sentence (no §2023:4.2.10 claim).
    public void Sr4_AStandardWord_KeepsTheGenericSentence()
    {
        var refusal = Assert.Single(CompileErrors(NoOpProgram(">>COBOL-WORDS RESERVE \"DISPLAY\"")), e => e.Contains("COBOL1623"));
        Assert.Contains("is a reserved / context-sensitive / intrinsic-function word", refusal);
        Assert.DoesNotContain("§2023:4.2.10", refusal);
    }

    [Theory] // kb/Work PB4586: the SR3 / SR4 verdict needs no parse tree, so a statement error beside it does not hide it.
    [InlineData("UNDEFINE \"AUTHOR\"", "SR3")]
    [InlineData("EQUATE \"MEMORY\" WITH \"RAM\"", "SR3")]
    [InlineData("RESERVE \"MOVE\"", "SR4")]
    [InlineData("EQUATE \"DISPLAY\" WITH \"JSON\"", "SR4")]
    public void TheCategoryVerdict_IsReportedBesideAParseError(string option, string rule)
    {
        var errors = CompileErrors("       >>COBOL-WORDS " + option + "\n       IDENTIFICATION DIVISION.\n       PROGRAM-ID. CWPB4586.\n"
            + "       PROCEDURE DIVISION.\n       MAIN.\n           MOVE TO.\n           STOP RUN.\n");
        Assert.Contains(errors, e => e.Contains("COBOL5506") || e.Contains("COBOL0001"));   // the statement is still no statement
        var verdict = Assert.Single(errors, e => e.Contains("COBOL1623"));
        Assert.Contains(rule, verdict);
        Assert.Contains("cw.cob(1,8)", verdict);   // at the directive's own line and column
    }

    [Fact] // kb/Work PB4239 S4: a synonym of an extension word used as a data-name is refused with a sentence naming the cause.
    public void ASynonymOfAnExtensionWord_UsedAsADataName_NamesTheReservation()
    {
        var errors = CompileErrors("       >>COBOL-WORDS EQUATE \"JSON\" WITH \"JS\"\n       IDENTIFICATION DIVISION.\n       PROGRAM-ID. CWPB4239S.\n"
            + "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       01 JS PIC X VALUE \"C\".\n"
            + "       PROCEDURE DIVISION.\n       MAIN.\n           DISPLAY JS.\n           STOP RUN.\n");
        Assert.DoesNotContain(errors, e => e.Contains("COBOL1623"));   // the directive holds (SR3: JSON is reserved; SR4: JS is no word)
        var refusal = errors.First(e => e.Contains("COBOL0313") && e.Contains("unexpected 'JS'"));
        Assert.Contains("JSON is a word this implementation reserves for a nonstandard extension (ISO §2023:4.2.10; docs/CONFORMANCE.md D-RW1)", refusal);
        Assert.Contains("'JS' is its synonym here (ISO §2023:7.3.10.4 GR2), so neither is a user-defined word", refusal);
        Assert.DoesNotContain("unexpected 'JSON'", string.Concat(errors));   // never the original, which the source did not write
    }

    [Fact] // kb/Work PB4239 S4 (review R1-1): SUBSTITUTE frees the original (GR4), so the sentence names GR4 and never calls JSON no user-defined word.
    public void ASubstituteSynonymOfAnExtensionWord_UsedAsADataName_NamesGr4AndFreesTheOriginal()
    {
        var errors = CompileErrors("       >>COBOL-WORDS SUBSTITUTE \"JSON\" BY \"JSN\"\n       IDENTIFICATION DIVISION.\n       PROGRAM-ID. CWPB4239R.\n"
            + "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       01 JSN PIC X VALUE \"C\".\n       01 JSON PIC X VALUE \"D\".\n"
            + "       PROCEDURE DIVISION.\n       MAIN.\n           DISPLAY JSON.\n           STOP RUN.\n");
        Assert.DoesNotContain(errors, e => e.Contains("COBOL1623"));   // the directive holds
        var refusal = errors.First(e => e.Contains("COBOL0313") && e.Contains("unexpected 'JSN'"));
        Assert.Contains("SUBSTITUTE \"JSON\" BY \"JSN\" replaces it with 'JSN' (ISO §2023:7.3.10.4 GR4)", refusal);
        Assert.DoesNotContain("GR2", refusal);
        Assert.DoesNotContain("neither is a user-defined word", refusal);
        Assert.DoesNotContain(errors, e => e.Contains("unexpected 'JSON'"));   // JSON is the data-name GR4 makes it
    }

    /// <summary>⛔ kb/Work PB4589 (ISO §2023:7.3.10.4 GR4, §2023:8.3.2.1 rule 1): a SUBSTITUTE synonym of a reservation-gated word §2023:8.9
    /// reserves at the compile edition (RECEIVE, COBOL-2023) is that word, so it is no user-defined word; the original, which
    /// GR4 frees, is one. Only a DECLARATION is written, so the refusal is the funnel's (the parser draws none): it asked the
    /// group's overlay about the synonym's token text, the ORIGINAL's, which SUBSTITUTE had suppressed.</summary>
    [Fact]
    public void ASubstituteSynonymOfAReservedGatedWord_DeclaredAsAName_IsRefusedAndTheOriginalIsAName()
    {
        var errors = CompileErrors("       >>COBOL-WORDS SUBSTITUTE \"RECEIVE\" BY \"RCV\"\n       IDENTIFICATION DIVISION.\n"
            + "       PROGRAM-ID. CWPB4589D.\n       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       01 RCV PIC X.\n"
            + "       01 RECEIVE PIC X.\n       PROCEDURE DIVISION.\n       STOP RUN.\n");
        var refusal = Assert.Single(errors, e => e.Contains("COBOL0901"));
        Assert.Contains("'RCV' is a synonym for the reserved word RECEIVE", refusal);
        Assert.Contains("cw.cob(6,", refusal);   // the declaration of RCV, not the original's (line 7)
    }

    /// <summary>⛔ kb/Work PB4589 (ISO §2023:7.3.10.4 GR4, §2023:8.3.2.1 rule 1): <c>--permissive</c> restores the pre-removal reading
    /// of a word §2023:8.9 took away, so the gate admits the synonym as a name, and the migration mode's shape is a WARNING for
    /// it as for the word itself (COBOL0901), not silence: the admitted token is an IDENTIFIER with the original's text, and
    /// the funnel must tell the synonym from the freed original (which stays a plain name, with no diagnostic at all).</summary>
    [Fact]
    public void ASubstituteSynonymOfAReservedGatedWord_UnderPermissive_IsWarnedAndTheOriginalIsAName()
    {
        var r = Compile("       >>COBOL-WORDS SUBSTITUTE \"RECEIVE\" BY \"RCV\"\n       IDENTIFICATION DIVISION.\n"
            + "       PROGRAM-ID. CWPB4589P.\n       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       01 RCV PIC X.\n"
            + "       PROCEDURE DIVISION.\n           MOVE \"A\" TO RCV.\n           STOP RUN.\n", permissive: true);
        Assert.Empty(r.Errors);
        var warning = Assert.Single(r.Warnings, w => w.Contains("COBOL0901"));
        Assert.Contains("'RCV' is a synonym for the reserved word RECEIVE", warning);
        var original = Compile("       >>COBOL-WORDS SUBSTITUTE \"RECEIVE\" BY \"RCV\"\n       IDENTIFICATION DIVISION.\n"
            + "       PROGRAM-ID. CWPB4589Q.\n       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       01 RECEIVE PIC X.\n"
            + "       PROCEDURE DIVISION.\n           MOVE \"A\" TO RECEIVE.\n           STOP RUN.\n", permissive: true);
        Assert.Empty(original.Errors);
        Assert.DoesNotContain(original.Warnings, w => w.Contains("COBOL0901"));
    }

    /// <summary>⛔ kb/Work PB4589 (ISO §2023:7.3.10.4 GR4): the synonym stands wherever the original is "documented as required or
    /// optional" - a SPECIAL-NAMES clause the parser recognizes by <c>reservedHere</c> as much as an operand list the
    /// reservation gate reads. <c>CRTX STATUS IS</c> after <c>SUBSTITUTE "CRT" BY "CRTX"</c> is the CRT STATUS clause (declined
    /// as the optional screen module, COBOL1560, as the unsubstituted clause is), not an unknown clause (COBOL1970), and a
    /// statement written with the synonym of COMMIT ends the DISPLAY before it instead of becoming its operand (the item
    /// COMMIT, which the retyped token's text named).</summary>
    [Fact]
    public void ASubstituteSynonym_IsTheKeywordOfItsOriginal_InTheClauseAndTheOperandList()
    {
        var clause = CompileErrors("       >>COBOL-WORDS SUBSTITUTE \"CRT\" BY \"CRTX\"\n       IDENTIFICATION DIVISION.\n"
            + "       PROGRAM-ID. CWPB4589C.\n       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       SPECIAL-NAMES.\n"
            + "           CRTX STATUS IS ST.\n       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       01 ST PIC 9(4).\n"
            + "       PROCEDURE DIVISION.\n       STOP RUN.\n");
        Assert.Contains(clause, e => e.Contains("COBOL1560"));
        Assert.DoesNotContain(clause, e => e.Contains("COBOL1970"));
        var statement = Compile("       >>COBOL-WORDS SUBSTITUTE \"COMMIT\" BY \"CMT\"\n       IDENTIFICATION DIVISION.\n"
            + "       PROGRAM-ID. CWPB4589S.\n       PROCEDURE DIVISION.\n           DISPLAY \"A\" CMT.\n           STOP RUN.\n");
        Assert.Empty(statement.Errors);   // no item COMMIT is declared: were CMT an operand, the item COMMIT would be undefined
    }

    /// <summary>⛔ kb/Work PB4589 (ISO §2023:7.3.10.4 GR5): a <c>RESERVE</c> of a word the lexer has a keyword token for and no
    /// gate retypes (REMARKS, PACKED, PARSE: §2023:8.9 lists none of them at COBOL-2023) is reserved for the group, and a
    /// declaration of it is COBOL0901 like a RESERVE of any other fresh word. The funnel asks such a token of
    /// <see cref="ReservedWordSet.KeywordToken"/>, which drops the SUBSTITUTE/UNDEFINE suppressions (the token is no longer
    /// the word they freed) and keeps the RESERVE layer; asking the generated table alone let these through.</summary>
    [Theory]
    [InlineData("REMARKS")]
    [InlineData("PACKED")]
    [InlineData("PARSE")]
    [InlineData("GENERIC")]
    [InlineData("PROCESSING")]
    public void AReserveOfALexerTokenedWord_IsRefusedAsAUserWord(string word)
    {
        var errors = CompileErrors($"       >>COBOL-WORDS RESERVE \"{word}\"\n       IDENTIFICATION DIVISION.\n"
            + "       PROGRAM-ID. CWPB4589R.\n       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n"
            + $"       01 {word} PIC X VALUE \"R\".\n       PROCEDURE DIVISION.\n           DISPLAY {word}.\n           STOP RUN.\n");
        Assert.Contains(errors, e => e.Contains("COBOL0901") && e.Contains($"'{word}'"));
    }

    [Fact] // kb/Work PB4586 control: a directive that holds is not reported because the program beside it fails to parse.
    public void ALegalDirective_BesideAParseError_DrawsNoCategoryVerdict()
    {
        var errors = CompileErrors("       >>COBOL-WORDS UNDEFINE \"JSON\"\n       IDENTIFICATION DIVISION.\n       PROGRAM-ID. CWPB4586C.\n"
            + "       PROCEDURE DIVISION.\n       MAIN.\n           MOVE TO.\n           STOP RUN.\n");
        Assert.NotEmpty(errors);
        Assert.DoesNotContain(errors, e => e.Contains("COBOL1623"));
    }

    // ── Increment C — the lexer retype's de-reserved token-type set (CobolWordsRewriter.Plan) ──────────────────

    [Fact] // UNDEFINE of a keyword contributes its token type to the types the lexer retypes to IDENTIFIER.
    public void DeReservedTokenTypes_IncludesTheUndefinedKeyword()
    {
        var map = new CobolWordsMap([new CobolWordsOp(CobolWordsAction.Undefine, "MOVE", null, default)]);
        CobolKeywordTokens.TryTokenType("MOVE", out int moveType);
        Assert.Contains(moveType, CobolWordsRewriter.DeReservedTokenTypes(map));
    }

    [Fact] // the empty map yields no de-reserved token types.
    public void DeReservedTokenTypes_EmptyMap_Empty()
    {
        Assert.Empty(CobolWordsRewriter.DeReservedTokenTypes(CobolWordsMap.Empty));
    }

    [Fact] // a well-formed EQUATE (existing reserved, new a user word) raises no SR / reserved diagnostic.
    public void ValidEquate_NoDiagnostic()
    {
        var errors = CompileErrors(
            "       >>COBOL-WORDS EQUATE \"DISPLAY\" WITH \"SHOW\"\n" +
            "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. CWE4.\n" +
            "       PROCEDURE DIVISION.\n       MAIN.\n           DISPLAY \"X\".\n           STOP RUN.\n");
        Assert.DoesNotContain(errors, e => e.Contains("COBOL1623") || e.Contains("COBOL0901"));
    }

    // ── Increment D — intrinsic-function-name synonyms (GR2/GR3/GR4 for a FUNCTION name) ─────────────────────

    private const string IntrinsicProgram =
        "{DIRECTIVE}" +
        "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. CWFN.\n" +
        "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       01 R PIC 9.\n" +
        "       PROCEDURE DIVISION.\n       MAIN.\n" +
        "           COMPUTE R = FUNCTION {NAME}(3 7 5).\n           STOP RUN.\n";

    [Fact] // GR2 — EQUATE a synonym for an intrinsic; FUNCTION synonym(...) resolves to the intrinsic (no error).
    public void Intrinsic_EquateSynonym_Resolves()
    {
        var errors = CompileErrors(IntrinsicProgram
            .Replace("{DIRECTIVE}", "       >>COBOL-WORDS EQUATE \"MAX\" WITH \"MYMAX\"\n")
            .Replace("{NAME}", "MYMAX"));
        Assert.DoesNotContain(errors, e => e.Contains("COBOL1501"));
    }

    [Fact] // GR3 — UNDEFINE an intrinsic; FUNCTION MAX(...) is no longer a function: COBOL5776 names the directive (kb/Work PB4309).
    public void Intrinsic_Undefine_NoLongerAFunction()
    {
        var errors = CompileErrors(IntrinsicProgram
            .Replace("{DIRECTIVE}", "       >>COBOL-WORDS UNDEFINE \"MAX\"\n")
            .Replace("{NAME}", "MAX"));
        // The refusal tells the directive's story, never the misspelling's: MAX IS an intrinsic function of ISO/IEC 1989.
        Assert.DoesNotContain(errors, e => e.Contains("COBOL1501"));
        var refusal = Assert.Single(errors, e => e.Contains("COBOL5776"));
        Assert.Contains(">>COBOL-WORDS UNDEFINE \"MAX\"", refusal);
        Assert.Contains("line 1", refusal);   // the directive's own line, mapped to the user's source (CobolWordsOp.At)
        Assert.Contains("§2023:7.3.10.4 GR3", refusal);
    }

    [Fact] // The control (kb/Work PB4309): a name that is no intrinsic function at all keeps COBOL1501 with a directive in force.
    public void Intrinsic_Misspelled_StaysNotAnIntrinsicFunction()
    {
        var errors = CompileErrors(IntrinsicProgram
            .Replace("{DIRECTIVE}", "       >>COBOL-WORDS UNDEFINE \"MAX\"\n")
            .Replace("{NAME}", "MAXX"));
        Assert.Contains(errors, e => e.Contains("COBOL1501") && e.Contains("FUNCTION MAXX is not an intrinsic function"));
        Assert.DoesNotContain(errors, e => e.Contains("COBOL5776"));
    }

    [Theory] // Review r1 R1-1 (kb/Work PB4309): a RESERVED word the directive removed that never named a function
    [InlineData("UNDEFINE \"ACCEPT\"")]                  // GR3 — SR3 lets UNDEFINE name any reserved word
    [InlineData("SUBSTITUTE \"ACCEPT\" BY \"MYACCEPT\"")] // GR4 — the same for literal-4
    public void Intrinsic_RemovedReservedWordThatIsNoFunction_KeepsNotAnIntrinsicFunction(string directive)
    {
        var errors = CompileErrors(IntrinsicProgram
            .Replace("{DIRECTIVE}", $"       >>COBOL-WORDS {directive}\n")
            .Replace("{NAME}", "ACCEPT"));
        // ACCEPT is no intrinsic function of ISO/IEC 1989, so the directive withdrew no function: COBOL1501 is true.
        Assert.Contains(errors, e => e.Contains("COBOL1501") && e.Contains("FUNCTION ACCEPT is not an intrinsic function"));
        Assert.DoesNotContain(errors, e => e.Contains("COBOL5776"));
    }

    [Fact] // GR4 — SUBSTITUTE: the new name resolves; the old intrinsic name is no longer a function.
    public void Intrinsic_Substitute_NewResolves_OldRemoved()
    {
        var okErrors = CompileErrors(IntrinsicProgram
            .Replace("{DIRECTIVE}", "       >>COBOL-WORDS SUBSTITUTE \"MAX\" BY \"MYMAX\"\n")
            .Replace("{NAME}", "MYMAX"));
        Assert.DoesNotContain(okErrors, e => e.Contains("COBOL1501") || e.Contains("COBOL5776"));

        var oldErrors = CompileErrors(IntrinsicProgram
            .Replace("{DIRECTIVE}", "       >>COBOL-WORDS SUBSTITUTE \"MAX\" BY \"MYMAX\"\n")
            .Replace("{NAME}", "MAX"));
        // PB4309: the refusal names the SUBSTITUTE and the spelling that now denotes the function (GR4's literal-5).
        Assert.DoesNotContain(oldErrors, e => e.Contains("COBOL1501"));
        var refusal = Assert.Single(oldErrors, e => e.Contains("COBOL5776"));
        Assert.Contains(">>COBOL-WORDS SUBSTITUTE \"MAX\" BY \"MYMAX\"", refusal);
        Assert.Contains("FUNCTION MYMAX now denotes it", refusal);
        Assert.Contains("§2023:7.3.10.4 GR4", refusal);
    }
}
