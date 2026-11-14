// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// ⛔ BELOW COBOL-2002 A COPY STATEMENT'S TEXT-NAME AND LIBRARY-NAME ARE USER-DEFINED WORDS OF THE PROGRAM (kb/Work PB5482).
/// §1985:IV.4.2.2.1.1: "All user-defined words, except segment-numbers and level-numbers, can belong to one and only one of these
/// disjoint sets", and library-names and text-names are two of the sets, "Within a given source program, but excluding any
/// contained program". §2002:F.2 (item 135) lifts it: "Text-names and library-names are now system-names, instead of
/// user-defined words, and do not have to be different from user-defined words". The preprocessor resolves the names, so they
/// reach no declaration of the tree; <c>DataBinder.DeclareUserWord</c>, the one word-type funnel, is told each of them. The suite
/// goldens <c>pb5482-*</c> pin the index-name case and the contained-program case; this class holds what a golden cannot: every
/// other kind of word, the library-name, a text-name that is also a library-name, the edition boundary, the case-insensitive
/// spelling and where the refusal is sited.
/// </summary>
public sealed class CopyNameUserWordTests
{
    private const string Conflict = "COBOL2692";

    /// <summary>Library text that adds nothing to the program, so what each fact tests is the NAMES of the COPY statement.</summary>
    private const string Empty = "      * library text that holds no program text\n";

    private static IReadOnlyDictionary<string, string> Copybooks => new Dictionary<string, string>
    {
        ["PB5482TXT.cpy"] = Empty,
        ["PB5482NOP.cpy"] = Empty,
        ["PB5482LIB/PB5482TXT.cpy"] = Empty,
        ["PB5482TXT/PB5482TXT.cpy"] = Empty,
    };

    /// <summary>A program whose WORKING-STORAGE holds <paramref name="declaration"/> and that copies <paramref name="copy"/>; its
    /// PROCEDURE DIVISION opens with paragraph <paramref name="paragraph"/>.</summary>
    private static string Program(string declaration, string copy, string paragraph = "MAIN-PARA") =>
        "       IDENTIFICATION DIVISION.\n"
        + "       PROGRAM-ID. PB5482T.\n"
        + "       DATA DIVISION.\n"
        + "       WORKING-STORAGE SECTION.\n"
        + "       01 PB5482Z PIC X VALUE \"Z\".\n"
        + $"       {declaration}\n"
        + $"       {copy}\n"
        + "       PROCEDURE DIVISION.\n"
        + $"       {paragraph}.\n"
        + "           DISPLAY PB5482Z.\n"
        + "           STOP RUN.\n";

    /// <summary>The words that make a second kind with a COPY statement's name: each is refused at 85 and compiles from 2002.</summary>
    public static TheoryData<string, string, string> Spellings => new()
    {
        { "a data-name", "01 PB5482TXT PIC X.", "COPY PB5482TXT." },
        { "a data-name in the other case", "01 pb5482txt PIC X.", "COPY PB5482TXT." },
        { "an index-name", "01 T. 05 E PIC X OCCURS 2 INDEXED BY PB5482TXT.", "COPY PB5482TXT." },
        { "a data-name that is the library-name", "01 PB5482LIB PIC X.", "COPY PB5482TXT OF PB5482LIB." },
    };

    [Theory]
    [MemberData(nameof(Spellings))]
    public void AWordOfTwoDisjointSets_IsRefusedAt85(string what, string declaration, string copy)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Program(declaration, copy), 85, copybooks: Copybooks);
        Assert.False(ok, what);
        Assert.Contains(errors, e => e.Contains(Conflict));
    }

    [Theory]
    [MemberData(nameof(Spellings))]
    public void TheSameWord_IsNoConflictFromCobol2002(string what, string declaration, string copy)
    {
        foreach (int edition in new[] { 2002, 2014, 2023 })
        {
            var (ok, errors, _) = EditionHarness.CompileFull(Program(declaration, copy), edition, copybooks: Copybooks);
            Assert.True(ok, $"{what} at {edition}: {string.Join('\n', errors)}");
        }
    }

    /// <summary>text-name and library-name are two sets of their own: one word cannot be both.</summary>
    [Fact]
    public void ATextNameThatIsAlsoTheLibraryName_IsRefusedAt85()
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Program("", "COPY PB5482TXT OF PB5482TXT."), 85, copybooks: Copybooks);
        Assert.False(ok);
        string error = Assert.Single(errors, e => e.Contains(Conflict));
        Assert.Contains("(text-name and library-name)", error);
        Assert.Contains("§1985:IV.4.2.2.1.1", error);
    }

    /// <summary>A paragraph-name spelled like the text-name: the paragraph is the second declaration, so the refusal is sited at it.</summary>
    [Fact]
    public void AParagraphName_IsRefusedAtItsOwnLine()
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Program("", "COPY PB5482TXT.", paragraph: "PB5482TXT"), 85,
            copybooks: Copybooks);
        Assert.False(ok);
        string error = Assert.Single(errors, e => e.Contains(Conflict));
        Assert.Contains("(text-name and paragraph-name)", error);
        Assert.Contains("prog.cob(9,", error);   // the paragraph header, line 9 of the program
    }

    /// <summary>The same text-name copied twice, and a library-name written twice, are one type of user-defined word each.</summary>
    [Fact]
    public void TheSameName_WrittenTwice_IsNoConflict()
    {
        var (ok, errors, _) = EditionHarness.CompileFull(
            Program("", "COPY PB5482NOP.\n       COPY PB5482NOP.\n       COPY PB5482TXT OF PB5482LIB.\n       COPY PB5482NOP OF PB5482LIB."),
            85, copybooks: new Dictionary<string, string>(Copybooks) { ["PB5482LIB/PB5482NOP.cpy"] = Empty });
        Assert.True(ok, string.Join('\n', errors));
    }

    /// <summary>A name written as a literal is no user-defined word (and below 2002 is refused as a literal form: a different
    /// diagnostic): no conflict is reported for it.</summary>
    [Fact]
    public void ALiteralName_IsNoUserDefinedWord()
    {
        var (_, errors, _) = EditionHarness.CompileFull(Program("01 PB5482TXT PIC X.", "COPY \"PB5482TXT.cpy\"."), 85,
            copybooks: Copybooks);
        Assert.DoesNotContain(errors, e => e.Contains(Conflict));
    }
}
