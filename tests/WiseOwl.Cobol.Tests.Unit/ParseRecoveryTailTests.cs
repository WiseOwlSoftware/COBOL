// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
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

    /// <summary>The terminators that are also reservation-gated words (<c>reservedGatedWord</c>: END-ACCEPT, END-DISPLAY,
    /// END-RECEIVE, END-SEND, free as user-defined words before 2002), read from the ATN. A stray one parses as a
    /// data-name, so the §8.9 funnel answers it (COBOL0901) where the misplaced-terminator hint is never asked: still
    /// exactly one diagnostic, but not COBOL0306.</summary>
    private static bool IsGatedWord(string word)
    {
        var atn = CobolParserCore._ATN;
        var gated = atn.NextTokens(atn.ruleToStartState[CobolParserCore.RULE_reservedGatedWord]);
        return gated.ToList().Any(t => CobolLexer.DefaultVocabulary.GetLiteralName(t)?.Trim('\'') == word);
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
        if (IsGatedWord(word))
        {
            Assert.Contains("COBOL0901", errors[0]);
            return;
        }
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
