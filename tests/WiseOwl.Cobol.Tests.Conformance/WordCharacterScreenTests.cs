// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Linq;
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
}
