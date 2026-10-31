// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// ⭐ THE CHARACTER HALF OF ISO §8.3.2.1 IS ASKED OF THE LEXER'S WORDS, NOT OF THE PARSE TREE (kb/Work PB3845).
/// <c>WordCharacterScreen</c> walks the token stream, so a word the grammar cannot place still draws the diagnostic that
/// names its character; the refused-program goldens <c>pb3845-*</c> pin the code at each edition, and this class holds
/// the facts a golden cannot: the screen does not read a comment-entry's text as words, a distinct word is reported once,
/// and <c>--permissive</c> keeps the removed-construct warning next to the parse error.
/// </summary>
public sealed class WordCharacterScreenTests
{
    private const string Euro = "€";
    private const string Katakana = "・";

    /// <summary>A program whose second DISPLAY does not parse (<c>MOVE MOVE</c>), so nothing here reaches a bound tree.</summary>
    private static string Unparseable(string name, string uses) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. PB3845T.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 {name} PIC X VALUE "M".
        PROCEDURE DIVISION.
            DISPLAY {uses} MOVE MOVE.
            STOP RUN.
        """;

    /// <summary>§8.3.2.1 governs a COBOL WORD; the text of a comment-entry is not one. <c>AUTHOR. JOSÉ € MY_NAME</c> is
    /// legal COBOL-85 (the paragraph itself is deleted by 2002, which the construct gate for it says).</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void ACommentEntry_IsNotWords(int edition)
    {
        string source = $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB3845C.
            AUTHOR. JOS{"É"} {Euro} MY_NAME{Katakana}.
            PROCEDURE DIVISION.
                STOP RUN.
            """;
        var (ok, errors, warnings) = EditionHarness.CompileFull(source, edition);
        string all = string.Join('\n', errors.Concat(warnings));
        Assert.DoesNotContain("COBOL2773", all);
        Assert.DoesNotContain("extended letter", all);
        Assert.DoesNotContain("underscore", all);
        if (edition == 85)
            Assert.True(ok, all);
    }

    /// <summary>One diagnostic per distinct word, however often the program spells it (the length ceiling's posture).</summary>
    [Fact]
    public void ADistinctWord_IsReportedOnce()
    {
        string word = "X" + Euro;
        var (ok, errors, _) = EditionHarness.CompileFull(Unparseable(word, word + " " + word + " " + word.ToLowerInvariant()), 2023);
        Assert.False(ok);
        Assert.Single(errors, e => e.Contains("COBOL2773"));
        Assert.Contains(errors, e => e.Contains("COBOL0001"));
    }

    /// <summary>Annex E.2 item 4 makes U+30FB a removed construct at 2023: <c>--permissive</c> keeps the warning beside the
    /// parse error, strict reports it as an error.</summary>
    [Fact]
    public void ARemovedCharacter_IsAWarningUnderPermissive_AndAnErrorOtherwise()
    {
        string word = "ABC" + Katakana;
        var (_, strict, _) = EditionHarness.CompileFull(Unparseable(word, word), 2023);
        Assert.Contains(strict, e => e.Contains("COBOL0902"));

        var (okPermissive, errors, warnings) = EditionHarness.CompileFull(Unparseable(word, word), 2023, permissive: true);
        Assert.False(okPermissive);
        Assert.Contains(warnings, w => w.Contains("COBOL0902"));
        Assert.DoesNotContain(errors, e => e.Contains("COBOL0902"));
        Assert.Contains(errors, e => e.Contains("COBOL0001"));
    }

    /// <summary>A source that PARSES keeps its bind-time diagnostics beside the word-character one (the binder driver screens
    /// the tree's words through the compiler's channel, so the report does not stop the compile before the binder).</summary>
    [Fact]
    public void AParsedSource_KeepsItsBindErrorsBesideTheWordCharacterError()
    {
        string word = "X" + Euro;
        var (ok, errors, _) = EditionHarness.CompileFull(Unparseable(word, word + " NOWHERE-DEFINED").Replace(" MOVE MOVE", ""), 2023);
        Assert.False(ok);
        Assert.Contains(errors, e => e.Contains("COBOL2773"));
        Assert.Contains(errors, e => e.Contains("NOWHERE-DEFINED"));
        Assert.DoesNotContain(errors, e => e.Contains("COBOL0001"));
    }

    /// <summary>A word that no grammar rule uses as a name (a phrase word of a statement) is still a word of the program.</summary>
    [Fact]
    public void AWordInAnyPosition_IsScreened()
    {
        string source = $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB3845P.
            PROCEDURE DIVISION.
                DISPLAY "X" UPON OUT{Euro} MOVE MOVE.
                STOP RUN.
            """;
        var (_, errors, _) = EditionHarness.CompileFull(source, 2023);
        Assert.Contains(errors, e => e.Contains("COBOL2773") && e.Contains("U+20AC"));
    }

    /// <summary>The comment-entry exclusion on a FAILED parse is the screen's own (the token walk, then the partial tree names
    /// the entries): <c>AUTHOR. JOSE_X.</c> is legal '85 comment text beside the parse error that stops the compile.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    public void ACommentEntry_IsNotWords_OnAFailedParse(int edition)
    {
        string source = $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB3845F.
            AUTHOR. JOSE_X {Euro}.
            PROCEDURE DIVISION.
                DISPLAY "A" MOVE MOVE.
                STOP RUN.
            """;
        var (ok, errors, warnings) = EditionHarness.CompileFull(source, edition);
        string all = string.Join('\n', errors.Concat(warnings));
        Assert.False(ok);
        Assert.Contains("COBOL0001", all);
        Assert.DoesNotContain("COBOL2773", all);
        Assert.DoesNotContain("COBOL0900", all);
        Assert.DoesNotContain("underscore", all);
    }

    /// <summary>A fixed-form debugging line without WITH DEBUGGING MODE is a COMMENT (the rewriter moves its tokens to the
    /// ABSENT_DEBUG_LINE channel; kb/Work PB1705), so its words are not the program's: with an unrelated parse error below
    /// it, the failed-parse screen names no character of the comment line, at the edition that gates the underscore and at the
    /// one that gates the euro sign.</summary>
    [Theory]
    [InlineData(85, "MY_VAR")]
    [InlineData(2023, "OUT€")]
    public void ADebuggingLineThatIsAComment_IsNotWords_OnAFailedParse(int edition, string word)
    {
        string source = string.Join('\n',
            "       IDENTIFICATION DIVISION.",
            "       PROGRAM-ID. PB3845D.",
            "       PROCEDURE DIVISION.",
            "      D    DISPLAY " + word + ".",
            "           DISPLAY \"A\" MOVE MOVE.",
            "           STOP RUN.",
            "");
        var (ok, errors, warnings) = EditionHarness.CompileFull(source, edition);
        string all = string.Join('\n', errors.Concat(warnings));
        Assert.False(ok);
        Assert.Contains("COBOL0001", all);
        Assert.DoesNotContain("COBOL2773", all);
        Assert.DoesNotContain("COBOL0900", all);
    }

    /// <summary>ISO §8.1.3.2 GR4 makes the extended letters Annex B's characters; a character that NO edition's Annex B holds
    /// (U+00A0, the euro sign) is no extended letter, so at COBOL-85 it is the word-character error COBOL2773 naming it, not
    /// the 2002 introduction gate COBOL0900 (kb/Work PB4130). Both word sources run the one body, so a failed parse (the
    /// lexer's stream) and a parsed source (the tree's words) answer alike; an Annex B letter at 85 stays the gate
    /// (golden <c>pb3845-extended-letter-unparseable-85</c>).</summary>
    [Theory]
    [InlineData(" ", "U+00A0", false)]
    [InlineData(" ", "U+00A0", true)]
    [InlineData(Euro, "U+20AC", false)]
    [InlineData(Euro, "U+20AC", true)]
    public void ACharacterInNoAnnexB_IsTheWordCharacterError_NotTheExtendedLetterGate_At85(string character, string named, bool parses)
    {
        string word = "PREIS" + character;
        string source = Unparseable(word, word);
        if (parses) source = source.Replace(" MOVE MOVE", "");
        var (ok, errors, warnings) = EditionHarness.CompileFull(source, 85);
        string all = string.Join('\n', errors.Concat(warnings));
        Assert.False(ok);
        Assert.Contains(errors, e => e.Contains("COBOL2773") && e.Contains(named));
        // The gate's own detail text; COBOL2773's message names "the extended letters of Annex B" as what the word may hold.
        Assert.DoesNotContain("COBOL0900", all);
        Assert.DoesNotContain("the extended letter in the COBOL word", all);
        Assert.Equal(!parses, all.Contains("COBOL0001"));
    }

    /// <summary>The 2002 gate is decided per CHARACTER (kb/Work PB4156): <c>CAFÉ€</c> holds an extended letter (É, Annex B) AND a
    /// character no edition's Annex B holds (€). At COBOL-85 the word draws both the introduction gate COBOL0900 (for the É) and
    /// the word-character error COBOL2773 (for the €); from 2002 the gate does not apply, so only COBOL2773 remains. Both word
    /// sources (a failed parse's token stream and a parsed source's tree) answer alike.</summary>
    [Theory]
    [InlineData(85, false)]
    [InlineData(85, true)]
    [InlineData(2002, false)]
    [InlineData(2002, true)]
    [InlineData(2014, true)]
    [InlineData(2023, true)]
    public void AnExtendedLetterBesideACharacterInNoAnnexB_IsGatedAt85AndRefusedAtEveryTarget(int edition, bool parses)
    {
        string word = "CAFÉ" + Euro;
        string source = Unparseable(word, word);
        if (parses) source = source.Replace(" MOVE MOVE", "");
        var (ok, errors, warnings) = EditionHarness.CompileFull(source, edition);
        Assert.False(ok);
        Assert.Contains(errors, e => e.Contains("COBOL2773") && e.Contains("U+20AC"));
        string all = string.Join('\n', errors.Concat(warnings));
        Assert.Equal(edition == 85, all.Contains("COBOL0900"));
        if (edition == 85) Assert.Contains(errors, e => e.Contains("COBOL0900") && e.Contains(word));
    }

    /// <summary>A CAUSE READS BEFORE ITS EFFECT (kb/Work PB4131): the word the screen names is why the parser died at the NEXT
    /// token, so COBOL2773 precedes the COBOL0001 it explains although the screen needs the failed parse and reports last. Placement
    /// is by source position among the parse's own reports: a parse error that sits BEFORE the word (unrelated to it) stays before,
    /// one after it stays after, and the report order is the same at every edition.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void TheWordCharacterCause_ReadsBeforeTheParseErrorItExplains(int edition)
    {
        string source = $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB4131A.
            PROCEDURE DIVISION.
                DISPLAY "A" MOVE MOVE.
                DISPLAY X{Euro} MOVE MOVE.
                DISPLAY "B" MOVE MOVE.
                STOP RUN.
            """;
        var (ok, errors, _) = EditionHarness.CompileFull(source, edition);
        Assert.False(ok);
        var codes = errors.Select(e => e.Contains("COBOL2773") ? "COBOL2773" : e.Contains("COBOL0001") ? "COBOL0001" : "other").ToList();
        Assert.Equal(["COBOL0001", "COBOL2773", "COBOL0001", "COBOL0001"], codes);
        Assert.Contains("(4,", errors[0]);
        Assert.Contains("(5,", errors[1]);
        Assert.Contains("(5,", errors[2]);
        Assert.Contains("(6,", errors[3]);
    }

    /// <summary>The same placement when the word is the whole line, as in golden <c>pb3845-nbsp-word-unparseable</c> (the parse
    /// dies on the NEXT line's first token): the word's line reads first.</summary>
    [Fact]
    public void TheWordCharacterCause_OnItsOwnLine_ReadsBeforeTheParseErrorOfTheNextLine()
    {
        string source = $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB4131B.
            PROCEDURE DIVISION.
            {" "}
                STOP RUN.
            """;
        var (ok, errors, _) = EditionHarness.CompileFull(source, 2023);
        Assert.False(ok);
        int cause = errors.ToList().FindIndex(e => e.Contains("COBOL2773"));
        int effect = errors.ToList().FindIndex(e => e.Contains("COBOL0001"));
        Assert.True(cause >= 0 && effect >= 0, string.Join('\n', errors));
        Assert.True(cause < effect, string.Join('\n', errors));
    }

    /// <summary>Two diagnoses of ONE word at ONE position read in the order the screen makes them, whichever arm reports them
    /// (kb/Work PB4200): <c>CAFÉ€</c> at COBOL-85 draws the introduction gate for É (COBOL0900) and then the word-character
    /// error for € (COBOL2773), both at the word's position. The frontend arm (a failed parse's token stream) used to print
    /// them reversed, the second cause meeting the first it had just inserted; the binder arm (a parsed source's tree) never did.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TwoCausesAtOnePosition_ReadInTheOrderTheScreenMakesThem_OnBothArms(bool parses)
    {
        string word = "CAFÉ" + Euro;
        string source = Unparseable(word, word);
        if (parses) source = source.Replace(" MOVE MOVE", "");
        var (ok, errors, _) = EditionHarness.CompileFull(source, 85);
        Assert.False(ok);
        var codes = errors
            .Select(e => e.Contains("COBOL0900") ? "COBOL0900" : e.Contains("COBOL2773") ? "COBOL2773" : e.Contains("COBOL0001") ? "COBOL0001" : "other")
            .ToList();
        Assert.Equal(parses ? ["COBOL0900", "COBOL2773"] : ["COBOL0900", "COBOL2773", "COBOL0001"], codes);
    }

    /// <summary>The cross-file arm of the placement (kb/Work PB4200): a parse error INSIDE a COPY member is never compared with
    /// the cause, whose position is in the program file, even when the member's line number would sort after the cause's. The
    /// cause reads before the next effect of ITS OWN file and after the member's error. The member's error is at line 9, the
    /// cause at line 6, so comparing across files would have put the cause before the member's error.</summary>
    [Fact]
    public void TheCause_IsNotComparedWithAnEffectInACopyMember()
    {
        string source = $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB4200C.
            PROCEDURE DIVISION.
                DISPLAY "A" MOVE MOVE.
                COPY BADMEMBER.
                DISPLAY X{Euro} MOVE MOVE.
                STOP RUN.
            """;
        string member = "\n\n\n\n\n\n\n\n    DISPLAY \"C\" MOVE MOVE.\n";
        var (ok, errors, _) = EditionHarness.CompileFull(source, 2023,
            copybooks: new Dictionary<string, string> { ["BADMEMBER.cpy"] = member });
        Assert.False(ok);
        Assert.Equal(4, errors.Count);
        Assert.Contains("prog.cob(4,", errors[0]);
        Assert.Contains("COBOL0001", errors[0]);
        Assert.Contains("BADMEMBER.cpy(9,", errors[1]);
        Assert.Contains("COBOL0001", errors[1]);
        Assert.Contains("prog.cob(6,", errors[2]);
        Assert.Contains("COBOL2773", errors[2]);
        Assert.Contains("prog.cob(6,", errors[3]);
        Assert.Contains("COBOL0001", errors[3]);
    }

    /// <summary>A cause with no effect of its own file after it is appended, behind an effect of another file that the
    /// diagnostics of its own file never order against (kb/Work PB4200).</summary>
    [Fact]
    public void ACauseWithNoLaterEffectInItsFile_IsAppendedAfterAnEffectInACopyMember()
    {
        string source = $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB4200D.
            PROCEDURE DIVISION.
                DISPLAY "A" MOVE MOVE.
                DISPLAY X{Euro}.
                COPY BADMEMBER.
                STOP RUN.
            """;
        string member = "    DISPLAY \"C\" MOVE MOVE.\n";
        var (ok, errors, _) = EditionHarness.CompileFull(source, 2023,
            copybooks: new Dictionary<string, string> { ["BADMEMBER.cpy"] = member });
        Assert.False(ok);
        Assert.Equal(3, errors.Count);
        Assert.Contains("prog.cob(4,", errors[0]);
        Assert.Contains("BADMEMBER.cpy(1,", errors[1]);
        Assert.Contains("prog.cob(5,", errors[2]);
        Assert.Contains("COBOL2773", errors[2]);
    }

    /// <summary>The binder driver screens only a compilation whose frontend reported nothing, so a source that PARSES beside
    /// another frontend error (here the §8.3.5 separator rule: a semicolon must be followed by a space) is screened by the
    /// frontend too: the underscore gate of '85 is not lost until the separator is fixed.</summary>
    [Fact]
    public void AParsedSourceWithAnotherFrontendError_IsStillScreened()
    {
        string source = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB3845S.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 MY_NAME PIC X VALUE "A".
            PROCEDURE DIVISION.
                MOVE "A" TO MY_NAME;DISPLAY MY_NAME.
                STOP RUN.
            """;
        var (ok, errors, _) = EditionHarness.CompileFull(source, 85);
        Assert.False(ok);
        Assert.Contains(errors, e => e.Contains("COBOL2631"));
        Assert.Contains(errors, e => e.Contains("COBOL0900") && e.Contains("MY_NAME"));
    }

    /// <summary>⛔ A WORD THAT STARTS OR ENDS ON AN UNDERSCORE IS A WORD, ILL-FORMED (kb/Work PB4234). ISO §8.3.2.1: "The hyphen
    /// or underscore shall not appear as the first or last character in such words." The lexer used to split <c>_NAME</c> and
    /// <c>NAME_</c> at the underscore, so the user read "unexpected '_'" and the rule was never named. The word is now one
    /// IDENTIFIER and the screen names it, in either position, at every edition, from both word sources (the lexer's stream when
    /// the parse fails; the tree when it parses), beside the COBOL-85 underscore gate that applies to it too.</summary>
    [Theory]
    [InlineData(85, "_NAME", "first", false)]
    [InlineData(85, "NAME_", "last", false)]
    [InlineData(85, "NAME_", "last", true)]
    [InlineData(2002, "_NAME", "first", true)]
    [InlineData(2014, "NAME_", "last", false)]
    [InlineData(2023, "_NAME", "first", false)]
    [InlineData(2023, "NAME_", "last", true)]
    [InlineData(2023, "__", "first", true)]
    [InlineData(2023, "_1", "first", true)]
    [InlineData(2023, "1_", "last", true)]
    [InlineData(2023, "NAME-X_", "last", true)]
    public void AnUnderscoreAtAWordEdge_IsNamedByTheSection8321Rule_AtEveryEdition(int edition, string word, string position, bool parses)
    {
        string source = Unparseable(word, word);
        if (parses) source = source.Replace(" MOVE MOVE", "");
        var (ok, errors, warnings) = EditionHarness.CompileFull(source, edition);
        Assert.False(ok);
        Assert.Contains(errors, e => e.Contains("COBOL2773") && e.Contains($"'{word}'") && e.Contains($"as its {position} character")
                                     && e.Contains("§8.3.2.1"));
        Assert.DoesNotContain(errors, e => e.Contains("COBOL0902"));
        string all = string.Join('\n', errors.Concat(warnings));
        Assert.Equal(edition == 85, all.Contains("COBOL0900"));
        Assert.Equal(!parses, all.Contains("COBOL0001"));
        Assert.DoesNotContain("unexpected '_'", all);
    }

    /// <summary>The edge underscore is no 2023 REMOVAL: the 2014 rule is violated too, so <c>--permissive</c>, which keeps the
    /// removed-construct warning for U+30FB, keeps the ERROR here (<c>CobolWordRule.RemovedAt2023</c> asks the whole character rule).</summary>
    [Fact]
    public void AnUnderscoreAtAWordEdge_IsAnErrorUnderPermissive()
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Unparseable("NAME_", "NAME_").Replace(" MOVE MOVE", ""), 2023, permissive: true);
        Assert.False(ok);
        Assert.Contains(errors, e => e.Contains("COBOL2773") && e.Contains("'NAME_'"));
    }

    /// <summary>The directive stages scan their own words (a directive is removed before the lexer runs) and ask the same
    /// §8.3.2.1 position rule (kb/Work PB4234): a <c>&gt;&gt;DEFINE</c> name and a <c>&gt;&gt;COBOL-WORDS</c> fresh word that
    /// start or end on an underscore are refused AND the refusal names the rule, not only "not a COBOL word".</summary>
    [Fact]
    public void AnEdgeUnderscore_InADirectiveWord_NamesTheRule()
    {
        string source = string.Join('\n',
            ">>SOURCE FORMAT FREE",
            ">>DEFINE FLAG_ AS 1",
            ">>DEFINE _FLAG AS 1",
            ">>COBOL-WORDS EQUATE \"ADD\" WITH \"ADD_\"",
            "IDENTIFICATION DIVISION.",
            "PROGRAM-ID. PB4234D.",
            "PROCEDURE DIVISION.",
            "    STOP RUN.",
            "");
        var (ok, errors, _) = EditionHarness.CompileFull(source, 2023);
        Assert.False(ok);
        Assert.Contains(errors, e => e.Contains("'FLAG_'") && e.Contains("as its last character") && e.Contains("compilation-variable-name-1"));
        Assert.Contains(errors, e => e.Contains("'_FLAG'") && e.Contains("as its first character") && e.Contains("compilation-variable-name-1"));
        Assert.Contains(errors, e => e.Contains("'ADD_'") && e.Contains("as its last character"));
    }

    /// <summary>The same word as a statement operand, a paragraph-name and a comment-entry: the first two are words of the program,
    /// the comment-entry is not (<c>AUTHOR. _JOSE_</c> is legal COBOL-85 comment text).</summary>
    [Fact]
    public void AnEdgeUnderscore_IsAWordInEveryPosition_AndNoWordInACommentEntry()
    {
        string source = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB4234W.
            AUTHOR. _JOSE_.
            PROCEDURE DIVISION.
            _PARA.
                MOVE 1 TO _CNT.
                STOP RUN.
            """;
        var (ok, errors, _) = EditionHarness.CompileFull(source, 85);
        Assert.False(ok);
        Assert.Contains(errors, e => e.Contains("COBOL2773") && e.Contains("'_PARA'"));
        Assert.Contains(errors, e => e.Contains("COBOL2773") && e.Contains("'_CNT'"));
        Assert.DoesNotContain(errors, e => e.Contains("'_JOSE_'"));
    }
}
