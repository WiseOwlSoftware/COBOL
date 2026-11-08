// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using Antlr4.Runtime.Atn;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Frontend.Cst;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A DECLARED FREE WORD DOES NOT HIDE ITS CONSTRUCT'S DIAGNOSIS (kb/Work PB4933, PB4958). The §2023:8.9 reservation gate
/// retypes every occurrence of a reservation-gated word the program declares where the edition leaves it free, and the
/// union grammar spells a later (or a deleted) edition's clause and paragraph with that word at the editions that lack
/// them, so its named edition refusal can answer. Before PB4958 a declared word turned its clause into an unknown one:
/// <c>01 TYPEDEF PIC X.</c> made <c>01 T PIC X TYPEDEF.</c> at COBOL-85 COBOL1941 "'TYPEDEF' is not a clause" instead of
/// the TYPEDEF clause's COBOL0900, and a FACTORY paragraph header a COBOL5506. <c>ReservationGateRewriter</c> now gives a
/// token its keyword back where its user-word reading fails (<c>NameReadingsRefused</c>), so the program that declares
/// the word draws exactly the diagnoses of the one that does not.
/// <para>The words are COMPUTED, never listed: every reservation-gated token that opens an alternative of a closed
/// format's clause or paragraph list (the rules that write the error production, <see cref="ClosedFormats.ErrorProductionRules"/>),
/// that §2023:8.9 leaves free at some edition, and that no keyword rule reads (a <c>keywordWhereFree</c> word reads its
/// retyped spelling and loses nothing). Each needs a <see cref="Samples"/> row, so the next such word fails
/// <see cref="Every_Closed_List_Word_Has_A_Sample"/> until its construct is proved here.</para>
/// </summary>
public sealed class DeclaredFreeWordKeywordTests
{
    private const string Ws = "       WORKING-STORAGE SECTION.\n";

    private static string Data(string entries) => "       DATA DIVISION.\n" + Ws + entries;

    private static string Files(string select, string fd, string ioControl = "") =>
        "       ENVIRONMENT DIVISION.\n       INPUT-OUTPUT SECTION.\n       FILE-CONTROL.\n"
        + $"           SELECT F ASSIGN TO \"F.DAT\"\n               ORGANIZATION SEQUENTIAL{select}.\n" + ioControl
        + $"       DATA DIVISION.\n       FILE SECTION.\n       FD F{fd}.\n       01 R PIC X.\n" + Ws;

    private static string SpecialNames(string clause) =>
        "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       SPECIAL-NAMES.\n"
        + $"           {clause}.\n" + Data("       01 S PIC 9(4).\n");

    private static string Usage(string usage) => Data($"       01 T {usage}.\n");

    private static string IdParagraph(string paragraph) => $"       {paragraph}\n" + Data("");

    /// <summary>Each closed-list word: an edition where §2023:8.9 leaves it free, and the text between PROGRAM-ID and the
    /// PROCEDURE DIVISION that writes its construct. The text ends in the WORKING-STORAGE SECTION header's entries, where
    /// the test adds the item that declares the word.</summary>
    internal static readonly Dictionary<string, (int Edition, string Text)> Samples = new(StringComparer.Ordinal)
    {
        ["ALIGNED"] = (85, Data("       01 G.\n          05 T PIC 1(4) USAGE BIT ALIGNED.\n")),
        ["AUTHOR"] = (2002, IdParagraph("AUTHOR. A PERSON.")),
        ["BASED"] = (85, Data("       01 T PIC X BASED.\n")),
        ["BINARY-CHAR"] = (85, Usage("BINARY-CHAR")),
        ["BINARY-DOUBLE"] = (85, Usage("BINARY-DOUBLE")),
        ["BINARY-LONG"] = (85, Usage("BINARY-LONG")),
        ["BINARY-SHORT"] = (85, Usage("BINARY-SHORT")),
        ["BIT"] = (85, Usage("PIC 1(8) BIT")),
        ["CONSTANT"] = (85, Data("       01 T CONSTANT RECORD.\n          05 F PIC X VALUE \"A\".\n")),
        ["CRT"] = (85, SpecialNames("CRT STATUS IS S")),
        ["CURSOR"] = (85, SpecialNames("CURSOR IS S")),
        ["DATE-COMPILED"] = (2002, IdParagraph("DATE-COMPILED. TODAY.")),
        ["DATE-WRITTEN"] = (2002, IdParagraph("DATE-WRITTEN. TODAY.")),
        ["DEFAULT"] = (85, Data("       01 T PIC X DEFAULT IS \"A\".\n")),
        ["FLOAT-BINARY-128"] = (85, Usage("FLOAT-BINARY-128")),
        ["FLOAT-BINARY-32"] = (85, Usage("FLOAT-BINARY-32")),
        ["FLOAT-BINARY-64"] = (85, Usage("FLOAT-BINARY-64")),
        ["FLOAT-DECIMAL-16"] = (85, Usage("FLOAT-DECIMAL-16")),
        ["FLOAT-DECIMAL-34"] = (85, Usage("FLOAT-DECIMAL-34")),
        ["FLOAT-EXTENDED"] = (85, Usage("FLOAT-EXTENDED")),
        ["FLOAT-LONG"] = (85, Usage("FLOAT-LONG")),
        ["FLOAT-SHORT"] = (85, Usage("FLOAT-SHORT")),
        ["FORMAT"] = (85, Files("", " FORMAT CHARACTER DATA")),
        ["FUNCTION-POINTER"] = (85, Usage("FUNCTION-POINTER")),
        ["GROUP-USAGE"] = (85, Data("       01 T GROUP-USAGE BIT.\n          05 F PIC 1.\n")),
        ["INSTALLATION"] = (2002, IdParagraph("INSTALLATION. HERE.")),
        ["LABEL"] = (2002, Files("", " LABEL RECORDS ARE STANDARD")),
        ["MESSAGE-TAG"] = (85, Usage("MESSAGE-TAG")),
        ["NATIONAL"] = (85, Usage("PIC N NATIONAL")),
        ["OBJECT"] = (85, Usage("OBJECT REFERENCE")),
        ["OPTIONS"] = (85, IdParagraph("OPTIONS.\n           DEFAULT ROUNDED MODE IS TRUNCATION.")),
        ["PADDING"] = (2014, Files(" PADDING CHARACTER IS \"*\"", "")),
        ["PRESENT"] = (85, Data("       01 T PIC X PRESENT WHEN T = \"A\".\n")),
        ["PROGRAM-POINTER"] = (85, Usage("PROGRAM-POINTER")),
        ["PROPERTY"] = (85, Data("       01 T PIC X PROPERTY.\n")),
        ["REPOSITORY"] = (85, "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       REPOSITORY.\n"
            + "           FUNCTION ALL INTRINSIC.\n" + Data("")),
        ["RERUN"] = (2002, Files("", "", "       I-O-CONTROL.\n           RERUN EVERY 100 RECORDS OF F.\n")),
        ["SECURITY"] = (2002, IdParagraph("SECURITY. NONE.")),
        ["SHARING"] = (85, Files(" SHARING WITH ALL OTHER", "")),
        ["TYPEDEF"] = (85, Data("       01 T TYPEDEF PIC X.\n")),
        ["VAL-STATUS"] = (85, Data("       01 T PIC X VAL-STATUS IS 1 FOR T.\n")),
        ["VALIDATE-STATUS"] = (85, Data("       01 T PIC X VALIDATE-STATUS IS 1 FOR T.\n")),
    };

    private static readonly int[] Editions = [85, 2002, 2014, 2023];

    /// <summary>The reservation-gated words that open an alternative of a closed format's list, are free at some edition
    /// (§2023:8.9), and carry no <c>keywordWhereFree</c> determination: the FIRST set of every rule that writes the error
    /// production, read off the generated parser's ATN.</summary>
    internal static SortedSet<string> ClosedListWords()
    {
        var atn = CobolParserCore._ATN;
        var lists = atn.states
            .Where(s => Enumerable.Range(0, s.NumberOfTransitions)
                .Any(i => s.Transition(i) is RuleTransition rt && ClosedFormats.IsErrorProduction(rt.target.ruleIndex)))
            .Select(s => s.ruleIndex).ToHashSet();
        var words = new SortedSet<string>(StringComparer.Ordinal);
        foreach (int rule in lists)
            foreach (int type in atn.NextTokens(atn.ruleToStartState[rule]).ToList())
            {
                if (!CobolLexer.IsReservationGated(type) || CobolLexer.DefaultVocabulary.GetSymbolicName(type) is not { } name) continue;
                string word = CobolWordsDriftTests.ToWord(name);
                if (!CobolLexer.IsKeywordWhereFree(word) && Editions.Any(e => IsFree(word, e))) words.Add(word);
            }
        return words;
    }

    private static bool IsFree(string word, int edition) => ReservedWordSet.Default.AdmitsAsUserWord(word, EditionInfo.Of(edition));

    /// <summary>⛔ THE SAMPLES ARE EXACTLY THE COMPUTED WORDS, AND EACH IS FREE AT ITS EDITION: a gated word a closed list
    /// opens with tomorrow fails here until a sample proves its construct keeps its diagnosis.</summary>
    [Fact]
    public void Every_Closed_List_Word_Has_A_Sample()
    {
        var words = ClosedListWords();
        Assert.True(words.Count >= 30, $"the ATN scan found {words.Count} closed-list word(s), fewer than it was written against: the scan is blind");
        var missing = words.Where(w => !Samples.ContainsKey(w)).ToList();
        var stale = Samples.Keys.Where(w => !words.Contains(w)).Order(StringComparer.Ordinal).ToList();
        var reserved = Samples.Where(s => !IsFree(s.Key, s.Value.Edition)).Select(s => s.Key).Order(StringComparer.Ordinal).ToList();
        Assert.True(missing.Count == 0 && stale.Count == 0 && reserved.Count == 0,
            $"closed-list word(s) with no sample [{string.Join(",", missing)}]; sample(s) for no such word [{string.Join(",", stale)}]; "
            + $"sample(s) at an edition that reserves the word [{string.Join(",", reserved)}]");
    }

    public static TheoryData<string> SampleWords => [.. Samples.Keys.Order(StringComparer.Ordinal)];

    /// <summary>The program that declares the word draws the same diagnostics, by code, as the one that does not: the
    /// construct's own edition refusal, never an unknown clause or paragraph (COBOL1941, COBOL1970, COBOL1971) or a syntax
    /// error at the header (COBOL5506, COBOL0001) that the declaration alone caused.</summary>
    [Theory]
    [MemberData(nameof(SampleWords))]
    public void A_Declared_Word_Keeps_Its_Constructs_Diagnosis(string word)
    {
        var (edition, text) = Samples[word];
        var plain = Codes(edition, text, "");
        var declared = Codes(edition, text, $"       01 {word} PIC X.\n");
        Assert.True(plain.Count > 0, $"{word} at {edition}: the sample compiles clean, so it proves no refusal");
        Assert.True(plain.SetEquals(declared),
            $"{word} at {edition}: without the item [{string.Join(",", plain)}], with it [{string.Join(",", declared)}]");
    }

    private static SortedSet<string> Codes(int edition, string text, string declaration)
    {
        string source = "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. P.\n" + text + declaration
            + "       PROCEDURE DIVISION.\n       P1.\n           STOP RUN.\n";
        string dir = ScratchDirectory.Create("pb4958");
        try
        {
            string src = Path.Combine(dir, "P.cob");
            File.WriteAllText(src, source);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(src, DialectLevel: edition, CheckOnly: true,
                SourceFormat: InitialReferenceFormat.Fixed));
            return new SortedSet<string>(r.Errors.SelectMany(e => Regex.Matches(e, @"COBOL\d{4}").Select(m => m.Value)),
                StringComparer.Ordinal);
        }
        finally { ScratchDirectory.Remove(dir); }
    }
}
