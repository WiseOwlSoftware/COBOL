// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using Antlr4.Runtime;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Frontend.Parsing;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A FAULT FOUND AFTER ITS STATEMENTS CLOSED IS ONE DIAGNOSTIC, NOT ONE AND THE RECOVERY'S OWN (kb/Work PB3843).
/// An explicit scope terminator is optional (ISO §14.5.3.2), so a statement ends wherever its next token is not its own
/// and the refusal is deferred to the sentence's period. By then the IF around <c>IF c MOVE 1 TO N END-ADD MOVE 2 TO N
/// END-IF.</c> is closed, and ANTLR's recovery parsed the rest of the sentence as new statements of a sentence the IF
/// no longer belonged to: its END-IF met no IF, and the one stray END-ADD drew a true COBOL0306 and a false one. A
/// phrase no statement owns (§14.5.3.3: a second <c>ON OVERFLOW</c>, an <c>ON EXCEPTION</c> on a format that prints
/// none) did the same with the tail of its statement (its <c>NOT ON …</c> phrase, its END-x). The recovery of such a
/// fault now stops only at the sentence's separator period (<c>CobolErrorStrategy.ConsumeUntil</c>), so each draws
/// exactly the diagnostic of the fault.
/// <para>Like <see cref="ParseRecoveryParenGroupTests"/>, these assert the COUNT: a fixture that carries such a source
/// asserts that its code is PRESENT, never that it is the only one.</para>
/// </summary>
public sealed class ParseRecoveryTailTests
{
    private static readonly string[] Terminators = ScopeTerminators();

    private static string[] ScopeTerminators()
    {
        var parser = new CobolParserCore(new CommonTokenStream(new CobolLexer(new AntlrInputStream(""))));
        return [.. CobolErrorStrategy.ScopeTerminatorWords(parser)];
    }

    /// <summary>The statements a stray terminator is written inside: none (the construct is closed), and each kind of
    /// block that holds statements (IF, inline PERFORM, EVALUATE, a conditional phrase of ADD), each with the words that
    /// close it.</summary>
    public static TheoryData<string, string, string> EveryTerminatorAgainstOpenAndClosedStatements()
    {
        var data = new TheoryData<string, string, string>();
        foreach (string word in Terminators)
        {
            data.Add(word, "", "");
            data.Add(word, "IF W-N = 0", "END-IF");
            data.Add(word, "PERFORM UNTIL W-N = 1", "END-PERFORM");
            data.Add(word, "EVALUATE W-N WHEN 1", "END-EVALUATE");
            data.Add(word, "ADD 1 TO W-K ON SIZE ERROR", "END-ADD");
        }
        return data;
    }

    [Fact]
    public void TheDriftSweepReadsTheGrammarsTerminators()
    {
        // the sweep below is the whole of Table 12's terminators and the block statements', never a hand-kept list
        Assert.Contains("END-IF", Terminators);
        Assert.Contains("END-STRING", Terminators);
        Assert.Contains("END-READ", Terminators);
        Assert.DoesNotContain("END-OF-PAGE", Terminators);   // a WRITE phrase, no statement's terminator
        Assert.True(Terminators.Length >= 20, $"the grammar spells {Terminators.Length} scope terminators");
    }

    /// <summary>The places a stray terminator is written where no statement is open, each a sentence of MAIN-PARA with
    /// <c>{0}</c> for the word: after a complete MOVE whose receiving list would take another operand, beginning a
    /// sentence after another, and beginning the paragraph's first sentence. At the start of a sentence a
    /// reservation-gated terminator could also be read as a paragraph header, which it is only where the header's
    /// period follows (ISO §14.4.3, <c>CobolParserCoreBase.procedureHeaderHere</c>, kb/Work PB4132 review R1-1).</summary>
    private static readonly string[] StrayPositions =
    [
        "MOVE 1 TO W-N {0} MOVE 2 TO W-K.",
        "MOVE 1 TO W-N. {0} MOVE 2 TO W-K.",
        "{0} MOVE 1 TO W-N.",
    ];

    /// <summary>Every terminator at the start of a sentence, after another sentence and as the paragraph's first, at
    /// the default edition: the terminator with no statement to end (ISO §14.5.3.2), named once at the word. The
    /// position after a complete statement is the empty-opening row of <see cref="AStrayTerminator_DrawsExactlyOneDiagnostic"/>.</summary>
    public static TheoryData<string, string> EveryTerminatorBeginningASentence()
    {
        var data = new TheoryData<string, string>();
        foreach (string word in Terminators)
            foreach (string position in StrayPositions.Skip(1))
                data.Add(word, position);
        return data;
    }

    [Theory]
    [MemberData(nameof(EveryTerminatorBeginningASentence))]
    public void AStrayTerminatorBeginningASentence_DrawsExactlyOneDiagnostic(string word, string position)
        => AssertTheMisplacedTerminator(word, string.Format(CultureInfo.InvariantCulture, position, word), RecoverySources.DefaultEdition);

    /// <summary>The editions that RESERVE each terminator that is also a reservation-gated word (<c>reservedGatedWord</c>,
    /// read from the ATN: END-ACCEPT, END-DISPLAY, END-RECEIVE and END-SEND, each a user-defined word at some edition
    /// §8.9 leaves it free; kb/Work PB4132). There the token is its keyword, so a stray one is the misplaced terminator
    /// every other terminator is (COBOL0306), never a user-defined word refused by §8.9 (COBOL0901) nor a paragraph
    /// header missing its period (COBOL0307 at the next word).</summary>
    private static IEnumerable<(string Word, int Edition)> GatedTerminatorReservations()
    {
        var atn = CobolParserCore._ATN;
        var gated = atn.NextTokens(atn.ruleToStartState[CobolParserCore.RULE_reservedGatedWord]).ToList()
            .Select(t => CobolLexer.DefaultVocabulary.GetLiteralName(t)?.Trim('\'')).ToHashSet(StringComparer.Ordinal);
        foreach (string word in Terminators.Where(gated.Contains))
            foreach (int edition in (int[])[85, 2002, 2014, 2023])
                if (ReservedWordSet.Default.RejectsAt(word, edition)) yield return (word, edition);
    }

    /// <summary>Each gated terminator in every stray position at every OTHER edition that reserves it: the default
    /// edition's rows are the all-terminator sweeps above, which hold these words too (review R1-3 of PB4132: the same
    /// compile twice is repeated work, never a second witness).</summary>
    public static TheoryData<string, int, string> EveryGatedTerminatorAtEveryOtherEditionThatReservesIt()
    {
        var data = new TheoryData<string, int, string>();
        foreach (var (word, edition) in GatedTerminatorReservations().Where(r => r.Edition != RecoverySources.DefaultEdition))
            foreach (string position in StrayPositions)
                data.Add(word, edition, position);
        return data;
    }

    [Fact]
    public void TheGatedTerminatorSweepReadsTheGrammar()
    {
        // the four the note names, each reserved at the default edition, which the all-terminator sweeps cover; and the
        // earlier editions reserving them, which the gated sweep covers (END-DISPLAY from 2002; END-SEND only at 2023),
        // so neither sweep is empty (a sweep that matched nothing would prove nothing)
        var reservations = GatedTerminatorReservations().ToList();
        string[] named = ["END-ACCEPT", "END-DISPLAY", "END-RECEIVE", "END-SEND"];
        Assert.Superset(named.ToHashSet(), reservations.Where(r => r.Edition == RecoverySources.DefaultEdition).Select(r => r.Word).ToHashSet());
        Assert.Contains(reservations, r => r.Word == "END-DISPLAY" && r.Edition == 2002);
    }

    [Theory]
    [MemberData(nameof(EveryGatedTerminatorAtEveryOtherEditionThatReservesIt))]
    public void AStrayGatedTerminator_IsTheMisplacedTerminator_AtEveryOtherEditionThatReservesIt(string word, int edition, string position)
        => AssertTheMisplacedTerminator(word, string.Format(CultureInfo.InvariantCulture, position, word), edition);

    private static void AssertTheMisplacedTerminator(string word, string body, int edition)
    {
        var errors = RecoverySources.Errors(RecoverySources.Program(body), edition);
        Assert.True(errors.Count == 1, $"[{body} at {edition}] expected exactly one diagnostic, got:\n{RecoverySources.Listing(errors)}");
        Assert.Contains("COBOL0306", errors[0]);
        Assert.Contains($"'{word}'", errors[0]);
        Assert.Contains($"{word} appears without a matching", errors[0]);
    }

    /// <summary>A terminator written after a MOVE, with the statement after it still inside the open construct: the
    /// stray terminator is the one fault. The enclosing construct's OWN terminator after it is the construct closing as
    /// written, or, where the stray one is that same word, a second stray.</summary>
    [Theory]
    [MemberData(nameof(EveryTerminatorAgainstOpenAndClosedStatements))]
    public void AStrayTerminator_DrawsExactlyOneDiagnostic(string word, string opening, string closing)
    {
        string body = opening.Length == 0
            ? $"MOVE 1 TO W-N {word} MOVE 2 TO W-K."
            : $"{opening} MOVE 1 TO W-N {word} MOVE 2 TO W-K {closing}.";
        var errors = RecoverySources.Errors(RecoverySources.Program(body));
        // the word closes the construct it names when that is the open one: then it is the CLOSING that is written
        // twice, and the second is the stray one
        string stray = errors.Count == 1 ? errors[0] : string.Join("\n", errors);
        Assert.True(errors.Count == 1, $"[{opening} … {word}] expected exactly one diagnostic, got:\n{stray}");
        Assert.Contains("COBOL0306", errors[0]);
        Assert.Contains($"{word} appears without a matching", errors[0]);
    }

    /// <summary>The rows of kb/Work PB3843 and the phrases §14.5.3.3 leaves unowned: each statement's tail after the
    /// refused word (its second phrase of the same family, the NOT phrase, its END-x) is not a second fault.</summary>
    [Theory]
    [InlineData("IF W-N = 0 MOVE 1 TO W-N END-ADD MOVE 2 TO W-N END-IF.", "COBOL0306", "END-ADD")]
    [InlineData("IF W-N = 1 DISPLAY \"A\" ELSE DISPLAY \"B\" ELSE DISPLAY \"C\" END-IF.", "COBOL2269", "ELSE")]
    [InlineData("EVALUATE W-N WHEN 1 DISPLAY \"1\" WHEN OTHER DISPLAY \"O\" WHEN OTHER DISPLAY \"P\" END-EVALUATE.", "COBOL2073", "WHEN")]
    [InlineData("STRING W-A DELIMITED BY SIZE INTO W-B ON OVERFLOW DISPLAY \"1\" ON OVERFLOW DISPLAY \"2\" END-STRING.", "COBOL2269", "ON")]
    // An identifier operand leaves ACCEPT Format 3 (screen) possible to the parser, so the phrases are the ACCEPT's
    // and the BINDER names the shape once it sees W-A is no screen-name (kb/Work PB1528): one diagnostic, naming W-A.
    [InlineData("ACCEPT W-A ON EXCEPTION DISPLAY \"E\" NOT ON EXCEPTION DISPLAY \"OK\" END-ACCEPT.", "COBOL2269", "W-A")]
    [InlineData("DISPLAY W-A ON EXCEPTION DISPLAY \"E\" NOT ON EXCEPTION DISPLAY \"OK\" END-DISPLAY.", "COBOL2269", "W-A")]
    [InlineData("DISPLAY \"A\" ON EXCEPTION DISPLAY \"E\" NOT ON EXCEPTION DISPLAY \"OK\" END-DISPLAY.", "COBOL2269", "ON")]
    [InlineData("UNSTRING W-A DELIMITED BY \",\" INTO W-B ON OVERFLOW DISPLAY \"1\" ON OVERFLOW DISPLAY \"2\" END-UNSTRING.", "COBOL2269", "ON")]
    [InlineData("ADD 1 TO W-N ON SIZE ERROR DISPLAY \"X\" ON SIZE ERROR DISPLAY \"Y\" END-ADD.", "COBOL2269", "ON")]
    [InlineData("ADD 1 TO W-N ON SIZE ERROR DISPLAY \"X\" END-SUBTRACT NOT ON SIZE ERROR DISPLAY \"Y\" END-ADD.", "COBOL0306", "END-SUBTRACT")]
    [InlineData("IF W-N = 1 DISPLAY \"A\" ELSE DISPLAY \"B\" ELSE DISPLAY \"C\" DISPLAY \"D\" END-IF DISPLAY \"E\".", "COBOL2269", "ELSE")]
    public void AnUnownedPhraseOrStrayTerminator_DrawsOnlyItsOwnDiagnostic(string statements, string code, string word)
    {
        var errors = RecoverySources.Errors(RecoverySources.Program(statements));
        Assert.True(errors.Count == 1, $"expected exactly one diagnostic, got:\n{string.Join("\n", errors)}");
        Assert.Contains(code, errors[0]);
        Assert.Contains($"'{word}'", errors[0]);
    }

    /// <summary>The discard stops at the fault's own sentence: the next sentence is parsed afresh, so a second FAULT in
    /// it is a second diagnostic (never lost to the first), and a sentence without a fault draws none.</summary>
    [Fact]
    public void TheNextSentence_IsParsedAfresh()
    {
        var errors = RecoverySources.Errors(RecoverySources.Program(
            "IF W-N = 0 MOVE 1 TO W-N END-ADD MOVE 2 TO W-N END-IF. MOVE 3 TO W-N END-EVALUATE. DISPLAY W-N."));
        Assert.Equal(2, errors.Count);
        Assert.Contains("'END-ADD'", errors[0]);
        Assert.Contains("'END-EVALUATE'", errors[1]);
    }
}
