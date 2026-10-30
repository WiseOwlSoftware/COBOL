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
        Assert.DoesNotContain("COBOL0900", all);
        Assert.DoesNotContain("extended letter", all);
        Assert.Equal(!parses, all.Contains("COBOL0001"));
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
}
